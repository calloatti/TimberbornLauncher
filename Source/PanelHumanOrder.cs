using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace TimberbornLauncher;

public partial class PanelHumanOrder : UserControl
{
  public static float[] SavedFillWeights = Array.Empty<float>();

  private DataTable _modsTable = new();
  private BindingSource _modsBinding = new();
  private string? _highlightQuery;

  public PanelHumanOrder()
  {
    InitializeComponent();
    BuildColumns();
    ModsGrid.Layout += ModsGrid_Layout;
    ModsGrid.CellFormatting += ModsGrid_CellFormatting;
    VisibleChanged += PanelHumanOrder_VisibleChanged;
    LoadMods();
  }

  private void PanelHumanOrder_VisibleChanged(object? sender, EventArgs e)
  {
    if (Visible)
    {
      RefreshFromDb();
    }
  }

  private void ModsGrid_Layout(object? sender, LayoutEventArgs e)
  {
    // Run only once
    ModsGrid.Layout -= ModsGrid_Layout;

    // 1. Lock first column (Enabled) to fixed width
    //ModsGrid.Columns[0].Width = 60;
    ModsGrid.Columns[0].MinimumWidth = 30;
    //ModsGrid.Columns[0].Resizable = DataGridViewTriState.False;
    //ModsGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

    // 2. Auto-size all columns to content (grid is fully laid out)
    ModsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
    ModsGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

    // 3. Set FillWeight for columns 1..N to their current widths
    for (int i = 0; i < ModsGrid.Columns.Count; i++)
    {
      ModsGrid.Columns[i].FillWeight = ModsGrid.Columns[i].Width;
    }

    // Save the measured fill weights for reuse by other panels (e.g. PanelGameLoadOrder)
    SavedFillWeights = new float[ModsGrid.Columns.Count];
    for (int i = 0; i < ModsGrid.Columns.Count; i++)
    {
      SavedFillWeights[i] = ModsGrid.Columns[i].FillWeight;
    }

    // 4. Switch to Fill mode — now they will stretch proportionally
    ModsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

    // Park the current cell on the Name column so the active-column header highlight is consistent
    if (ModsGrid.Rows.Count > 0)
      ModsGrid.CurrentCell = ModsGrid.Rows[0].Cells["Enabled"];
  }

  // WARNING: This column sizing logic is final and correct. It uses AllCells to measure content,
  // then switches to Fill mode so columns stretch proportionally with no gray empty space.
  // DO NOT change the algorithm, the order of operations, or remove any step.

  // ====== All your existing methods (BuildColumns, LoadMods, Search, Filters, Refresh, CellClick) are unchanged ======
  // They are exactly as you originally wrote them.

  private void BuildColumns()
  {
    ModsGrid.AutoGenerateColumns = false;

    var enabledCol = new DataGridViewTextBoxColumn
    {
      Name = "Enabled",
      HeaderText = "",
      ReadOnly = true,
      DataPropertyName = "Enabled",
      HeaderCell = { Style = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
      CellTemplate = new DataGridViewTextBoxCell { Style = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
      DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Symbol", 14F) },
      SortMode = DataGridViewColumnSortMode.NotSortable
    };

    var sourceCol = new DataGridViewTextBoxColumn
    {
      Name = "Source",
      HeaderText = "Source",
      ReadOnly = true,
      DataPropertyName = "Source"
    }; 
    
    var nameCol = new DataGridViewTextBoxColumn
    {
      Name = "Name",
      HeaderText = "Name",
      ReadOnly = true,
      DataPropertyName = "Name"
    };

    var versionCol = new DataGridViewTextBoxColumn
    {
      Name = "Version",
      HeaderText = "Version",
      ReadOnly = true,
      DataPropertyName = "Version"
    };

    var idCol = new DataGridViewTextBoxColumn
    {
      Name = "Id",
      HeaderText = "Id",
      ReadOnly = true,
      DataPropertyName = "Id"
    };

    var originNameCol = new DataGridViewTextBoxColumn
    {
      Name = "OriginName",
      HeaderText = "Origin_Name",
      ReadOnly = true,
      DataPropertyName = "OriginName"
    };

    var folderCol = new DataGridViewTextBoxColumn
    {
      Name = "Folder",
      HeaderText = "Version_Folder",
      ReadOnly = true,
      DataPropertyName = "Folder"
    };

    ModsGrid.Columns.AddRange(enabledCol, sourceCol, nameCol, versionCol, idCol, originNameCol, folderCol);
    ModsGrid.ColumnHeadersHeight = 24;
  }

  private void LoadMods()
  {
    _modsTable = AppDatabase.GetModsGridTable("name");
    _modsBinding.DataSource = _modsTable;
    ModsGrid.DataSource = _modsBinding;
    // The Layout event will fire and apply the sizing.
  }

  private void SearchTextBox_TextChanged(object? sender, EventArgs e) => ApplyFilters();
  private void Filter_CheckedChanged(object? sender, EventArgs e) => ApplyFilters();

  private void ApplyFilters()
  {
    if (_modsBinding.DataSource == null) return;

    string? selectedPath = null;
    if (ModsGrid.CurrentRow?.DataBoundItem is DataRowView drv)
      selectedPath = drv.Row.Field<string>("mod_path");

    string search = SearchTextBox.Text.Trim();
    bool highlight = search.StartsWith("@");
    string term = highlight ? search[1..].Trim() : search;

    if (!highlight || term.Length == 0)
      _highlightQuery = null;
    else
      _highlightQuery = term;

    string[] parts = new string[3];
    int count = 0;

    if (!highlight && term.Length > 0)
      parts[count++] = $"Name LIKE '%{EscapeLike(term)}%' OR Id LIKE '%{EscapeLike(term)}%' OR Source LIKE '%{EscapeLike(term)}%'";

    bool localChecked = LocalFilterChk.Checked;
    bool steamChecked = SteamFilterChk.Checked;
    if (localChecked && !steamChecked)
      parts[count++] = "Source = 'local'";
    else if (!localChecked && steamChecked)
      parts[count++] = "Source = 'steam'";

    bool enabledChecked = EnabledFilterChk.Checked;
    bool disabledChecked = DisabledFilterChk.Checked;
    if (enabledChecked && !disabledChecked)
      parts[count++] = $"Enabled = '{AppDatabase.CharEnabled}'";
    else if (!enabledChecked && disabledChecked)
      parts[count++] = $"Enabled = '{AppDatabase.CharDisabled}'";

    _modsBinding.Filter = count > 0 ? string.Join(" AND ", parts.Take(count)) : null;
    ModsGrid.Invalidate(); // repaint to apply/clear row highlights

    if (selectedPath != null)
      RestoreSelection(selectedPath);
  }

  private void ModsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
  {
    if (string.IsNullOrEmpty(_highlightQuery) || e.RowIndex < 0 || e.ColumnIndex < 0) return;

    if (ModsGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;
    if (drv.Row is null) return;
    DataRow row = drv.Row;

    string id   = row.Field<string>("Id") ?? "";
    string name = row.Field<string>("Name") ?? "";
    string src  = row.Field<string>("Source") ?? "";

    bool isHit = id.Contains(_highlightQuery, StringComparison.OrdinalIgnoreCase)
      || name.Contains(_highlightQuery, StringComparison.OrdinalIgnoreCase)
      || src.Contains(_highlightQuery, StringComparison.OrdinalIgnoreCase);
    if (isHit)
    {
      if (ModsGrid.Rows[e.RowIndex].Selected)
      {
         Color c = HighlightCurrentBackColor;
         e.CellStyle!.BackColor = c;
         e.CellStyle.SelectionBackColor = c;
         e.CellStyle.SelectionForeColor = Color.Black;
       }
       else
       {
         e.CellStyle!.BackColor = HighlightRowBackColor;
       }
     }
   }

  private static readonly Color HighlightCurrentBackColor = Color.FromArgb(214, 215, 0);
  private static readonly Color HighlightRowBackColor   = Color.FromArgb(255, 255, 180);

  private static string EscapeLike(string s) =>
      s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

  public void RefreshFromDb()
  {
    string? selectedPath = null;
    if (ModsGrid.CurrentRow?.DataBoundItem is DataRowView drv)
      selectedPath = drv.Row.Field<string>("mod_path");

    int firstVisibleRow = ModsGrid.Rows.Count > 0 ? ModsGrid.FirstDisplayedScrollingRowIndex : -1;
    LoadMods();
    ApplyFilters();

    if (selectedPath != null && RestoreSelection(selectedPath))
      return;

    ModsGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(firstVisibleRow, ModsGrid.Rows.Count - 1));
  }

  private bool RestoreSelection(string modPath)
  {
    foreach (DataGridViewRow row in ModsGrid.Rows)
    {
      if (row.DataBoundItem is not DataRowView drv) continue;
      if (drv.Row.Field<string>("mod_path") == modPath)
      {
        row.Selected = true;
        ModsGrid.CurrentCell = row.Cells["Enabled"];
        int first = Math.Max(0, Math.Min(row.Index - ModsGrid.DisplayedRowCount(true) / 2, ModsGrid.Rows.Count - 1));
        ModsGrid.FirstDisplayedScrollingRowIndex = first;
        return true;
      }
    }
    return false;
  }

  private void ModsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
  {
    if (e.RowIndex < 0 || e.ColumnIndex == 0) return;
    ToggleEnabled(e.RowIndex);
  }

  private void ModsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
  {
    if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
    ToggleEnabled(e.RowIndex);
  }

  private void ToggleEnabled(int rowIndex)
  {
    if (ModsGrid.Rows[rowIndex].DataBoundItem is not DataRowView drv) return;
    DataRow row = drv.Row;

    string modPath = row.Field<string>("mod_path")!;
    string val = row["Enabled"].ToString()!;
    Log.Debug($"Toggle Enabled on row {rowIndex}, Enabled = '{val}', mod_path = {modPath}");
    if (val == AppDatabase.CharEnabled)
    {
      row["Enabled"] = AppDatabase.CharDisabled;
      AppDatabase.SetModEnabledByPath(modPath, 0);
      Log.Debug($"Set mod {modPath} to disabled");
    }
    else
    {
      row["Enabled"] = AppDatabase.CharEnabled;
      AppDatabase.SetModEnabledByPath(modPath, 1);
      Log.Debug($"Set mod {modPath} to enabled");
    }
    ModValidator.RefreshWarnings();
  }
}