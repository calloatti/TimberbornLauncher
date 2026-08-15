using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace TimberbornLauncher;

public partial class PanelGameLoadOrder : UserControl
{
    private DataTable _modsTable = new();
    private BindingSource _modsBinding = new();

    public PanelGameLoadOrder()
    {
        InitializeComponent();
        BuildColumns();
        ModsGrid.Layout += ModsGrid_Layout;
        VisibleChanged += PanelGameLoadOrder_VisibleChanged;
        LoadMods();
    }

    private void PanelGameLoadOrder_VisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
        {
            RefreshFromDb();
        }
    }

    private void ModsGrid_Layout(object? sender, LayoutEventArgs e)
    {
        ModsGrid.Layout -= ModsGrid_Layout;
        ModsGrid.Columns[0].MinimumWidth = 30;

        if (PanelHumanOrder.SavedFillWeights.Length == ModsGrid.Columns.Count)
        {
            for (int i = 0; i < ModsGrid.Columns.Count; i++)
            {
                ModsGrid.Columns[i].FillWeight = PanelHumanOrder.SavedFillWeights[i];
            }
        }
        else
        {
            ModsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            ModsGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            for (int i = 0; i < ModsGrid.Columns.Count; i++)
            {
                ModsGrid.Columns[i].FillWeight = ModsGrid.Columns[i].Width;
            }
        }

        ModsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        if (ModsGrid.Rows.Count > 0)
            ModsGrid.CurrentCell = ModsGrid.Rows[0].Cells["Name"];
    }

    // WARNING: This column sizing logic is final and correct. It uses AllCells to measure content,
    // then switches to Fill mode so columns stretch proportionally with no gray empty space.
    // DO NOT change the algorithm, the order of operations, or remove any step.

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
            DataPropertyName = "Source",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var nameCol = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Name",
            ReadOnly = true,
            DataPropertyName = "Name",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var versionCol = new DataGridViewTextBoxColumn
        {
            Name = "Version",
            HeaderText = "Version",
            ReadOnly = true,
            DataPropertyName = "Version",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var idCol = new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "Id",
            ReadOnly = true,
            DataPropertyName = "Id",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var originNameCol = new DataGridViewTextBoxColumn
        {
            Name = "OriginName",
            HeaderText = "Origin_Name",
            ReadOnly = true,
            DataPropertyName = "OriginName",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var folderCol = new DataGridViewTextBoxColumn
        {
            Name = "Folder",
            HeaderText = "Version_Folder",
            ReadOnly = true,
            DataPropertyName = "Folder",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        ModsGrid.Columns.AddRange(enabledCol, sourceCol, nameCol, versionCol, idCol, originNameCol, folderCol);
        ModsGrid.ColumnHeadersHeight = 24;
    }

    private void LoadMods()
    {
        _modsTable = AppDatabase.GetModsGridTable("priority_value DESC");
        _modsBinding.DataSource = _modsTable;
        ModsGrid.DataSource = _modsBinding;
    }

    private void SearchTextBox_TextChanged(object? sender, EventArgs e) => ApplyFilters();
    private void Filter_CheckedChanged(object? sender, EventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (_modsBinding.DataSource == null) return;

        string? selectedPath = null;
        if (ModsGrid.CurrentRow?.DataBoundItem is DataRowView drv)
            selectedPath = drv.Row.Field<string>("mod_path");

        string query = SearchTextBox.Text.Trim();
        string[] parts = new string[3];
        int count = 0;

        if (query.Length > 0)
            parts[count++] = $"Name LIKE '%{EscapeLike(query)}%' OR Id LIKE '%{EscapeLike(query)}%' OR Source LIKE '%{EscapeLike(query)}%'";

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

        if (selectedPath != null)
            RestoreSelection(selectedPath);
    }

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
                ModsGrid.CurrentCell = row.Cells["Name"];
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
