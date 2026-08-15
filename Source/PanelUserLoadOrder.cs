using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TimberbornLauncher.Mods;

namespace TimberbornLauncher;

public partial class PanelUserLoadOrder : UserControl
{
    private DataTable _modsTable = new();
    private BindingSource _modsBinding1 = new();
    private BindingSource _modsBinding2 = new();
    private DataTable _depsTable = new();
    private BindingSource _depsBinding = new();

    public PanelUserLoadOrder()
    {
        InitializeComponent();
        BuildColumns();
        ModsGrid1.Layout += ModsGrid1_Layout;
        ModsGrid2.Layout += ModsGrid2_Layout;
        VisibleChanged += PanelUserLoadOrder_VisibleChanged;
        Mod1SearchBox.TextChanged += Mod1SearchBox_TextChanged;
        Mod2SearchBox.TextChanged += Mod2SearchBox_TextChanged;
        DependenciesSearchBox.TextChanged += DependenciesSearchBox_TextChanged;
        Mod1BeforeMod2Button.Click += Mod1BeforeMod2Button_Click;
        Mod1AfterMod2Button.Click += Mod1AfterMod2Button_Click;
        Mod1ConflictsMod2Button.Click += Mod1ConflictsMod2Button_Click;
        DeleteButton.Click += DeleteButton_Click;
        Mod1ToTopButton.Click += Mod1ToTopButton_Click;
        Mod1ToBottomButton.Click += Mod1ToBottomButton_Click;
        LoadAll();
    }

    private void PanelUserLoadOrder_VisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
        {
            LoadAll();
        }
    }

    private void BuildColumns()
    {
        var modCols = new ModColumnSet();
        var depCols = new DepColumnSet();

        // Grid1
        ModsGrid1.AutoGenerateColumns = false;
        ModsGrid1.Columns.AddRange(modCols.Build());
        ModsGrid1.ColumnHeadersHeight = 24;

        // Grid2
        ModsGrid2.AutoGenerateColumns = false;
        ModsGrid2.Columns.AddRange(modCols.Build());
        ModsGrid2.ColumnHeadersHeight = 24;

        // Grid3
        DependenciesGrid.AutoGenerateColumns = false;
        DependenciesGrid.Columns.AddRange(depCols.Build());
        DependenciesGrid.ColumnHeadersHeight = 24;
    }

    private sealed class ModColumnSet
    {
        public DataGridViewColumn[] Build() => new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "Enabled", HeaderText = "", ReadOnly = true,
                DataPropertyName = "Enabled",
                HeaderCell = { Style = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                CellTemplate = new DataGridViewTextBoxCell { Style = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Symbol", 14F) },
                SortMode = DataGridViewColumnSortMode.NotSortable
            },
            new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Source", ReadOnly = true, DataPropertyName = "Source" },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", ReadOnly = true, DataPropertyName = "Name" },
            new DataGridViewTextBoxColumn { Name = "Version", HeaderText = "Version", ReadOnly = true, DataPropertyName = "Version" },
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", ReadOnly = true, DataPropertyName = "Id" },
            new DataGridViewTextBoxColumn { Name = "OriginName", HeaderText = "Origin_Name", ReadOnly = true, DataPropertyName = "OriginName" },
            new DataGridViewTextBoxColumn { Name = "Folder", HeaderText = "Version_Folder", ReadOnly = true, DataPropertyName = "Folder" }
        };
    }

    private sealed class DepColumnSet
    {
        public DataGridViewColumn[] Build() => new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "Mod", HeaderText = "Mod", ReadOnly = true, DataPropertyName = "ModId" },
            new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Dependency_Type", ReadOnly = true, DataPropertyName = "DependencyType" },
            new DataGridViewTextBoxColumn { Name = "Dependency", HeaderText = "Dependency", ReadOnly = true, DataPropertyName = "DependencyId" }
        };
    }

    private void LoadAll()
    {
        LoadModsGrid();
        LoadDepsGrid();
        ApplyFilters();
    }

    private void LoadModsGrid()
    {
        _modsTable = AppDatabase.GetModsGridTable("name");
        _modsBinding1.DataSource = new DataView(_modsTable);
        ModsGrid1.DataSource = _modsBinding1;
        _modsBinding2.DataSource = new DataView(_modsTable);
        ModsGrid2.DataSource = _modsBinding2;
    }

    private void LoadDepsGrid()
    {
        string query = @"
            SELECT hash AS Hash, mod_id AS ModId, dependency_type AS DependencyType, dependency_id AS DependencyId
            FROM user_dependencies";

        _depsTable = AppDatabase.ExecuteQuery(query);
        _depsBinding.DataSource = _depsTable;
        DependenciesGrid.DataSource = _depsBinding;
        DependenciesGrid.Layout += DependenciesGrid_Layout;
    }

    private void ModsGrid1_Layout(object? sender, LayoutEventArgs e)
    {
        ModsGrid1.Layout -= ModsGrid1_Layout;
        ModsGrid1.Columns[0].MinimumWidth = 30;

        if (PanelHumanOrder.SavedFillWeights.Length == ModsGrid1.Columns.Count)
        {
            for (int i = 0; i < ModsGrid1.Columns.Count; i++)
                ModsGrid1.Columns[i].FillWeight = PanelHumanOrder.SavedFillWeights[i];
        }
        else
        {
            ModsGrid1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            ModsGrid1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            for (int i = 0; i < ModsGrid1.Columns.Count; i++)
                ModsGrid1.Columns[i].FillWeight = ModsGrid1.Columns[i].Width;
        }

        ModsGrid1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        if (ModsGrid1.Rows.Count > 0)
            ModsGrid1.CurrentCell = ModsGrid1.Rows[0].Cells["Enabled"];
    }

    private void ModsGrid2_Layout(object? sender, LayoutEventArgs e)
    {
        ModsGrid2.Layout -= ModsGrid2_Layout;
        ModsGrid2.Columns[0].MinimumWidth = 30;

        if (PanelHumanOrder.SavedFillWeights.Length == ModsGrid2.Columns.Count)
        {
            for (int i = 0; i < ModsGrid2.Columns.Count; i++)
                ModsGrid2.Columns[i].FillWeight = PanelHumanOrder.SavedFillWeights[i];
        }
        else
        {
            ModsGrid2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            ModsGrid2.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            for (int i = 0; i < ModsGrid2.Columns.Count; i++)
                ModsGrid2.Columns[i].FillWeight = ModsGrid2.Columns[i].Width;
        }

        ModsGrid2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        if (ModsGrid2.Rows.Count > 0)
            ModsGrid2.CurrentCell = ModsGrid2.Rows[0].Cells["Enabled"];
    }

    private void DependenciesGrid_Layout(object? sender, LayoutEventArgs e)
    {
        DependenciesGrid.Layout -= DependenciesGrid_Layout;
        DependenciesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        DependenciesGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        for (int i = 0; i < DependenciesGrid.Columns.Count; i++)
            DependenciesGrid.Columns[i].FillWeight = DependenciesGrid.Columns[i].Width;
        DependenciesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private void Mod1SearchBox_TextChanged(object? sender, EventArgs e) => ApplyMod1Filter();

    private void Mod2SearchBox_TextChanged(object? sender, EventArgs e) => ApplyMod2Filter();

    private void DependenciesSearchBox_TextChanged(object? sender, EventArgs e) => ApplyDepsFilter();

    private void ApplyFilters()
    {
        ApplyMod1Filter();
        ApplyMod2Filter();
        ApplyDepsFilter();
    }

    private void ApplyMod1Filter()
    {
        string q = Mod1SearchBox.Text.Trim();
        string[] p1 = new string[4]; int c1 = 0;
        if (q.Length > 0) p1[c1++] = $"Name LIKE '%{EscapeLike(q)}%' OR Id LIKE '%{EscapeLike(q)}%' OR Source LIKE '%{EscapeLike(q)}%' OR OriginName LIKE '%{EscapeLike(q)}%'";
        _modsBinding1.Filter = c1 > 0 ? string.Join(" AND ", p1.Take(c1)) : null;
    }

    private void ApplyMod2Filter()
    {
        string q = Mod2SearchBox.Text.Trim();
        string[] p2 = new string[4]; int c2 = 0;
        if (q.Length > 0) p2[c2++] = $"Name LIKE '%{EscapeLike(q)}%' OR Id LIKE '%{EscapeLike(q)}%' OR Source LIKE '%{EscapeLike(q)}%' OR OriginName LIKE '%{EscapeLike(q)}%'";
        _modsBinding2.Filter = c2 > 0 ? string.Join(" AND ", p2.Take(c2)) : null;
    }

    private void ApplyDepsFilter()
    {
        string q = DependenciesSearchBox.Text.Trim();
        string[] p3 = new string[3]; int c3 = 0;
        if (q.Length > 0) p3[c3++] = $"ModId LIKE '%{EscapeLike(q)}%' OR DependencyId LIKE '%{EscapeLike(q)}%'";
        _depsBinding.Filter = c3 > 0 ? string.Join(" AND ", p3.Take(c3)) : null;
    }

    private static string EscapeLike(string s) => s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private void ReloadDepsGrid()
    {
        LoadDepsGrid();
        ApplyFilters();
    }

    private void Mod1BeforeMod2Button_Click(object? sender, EventArgs e)
    {
        if (ModsGrid1.CurrentRow?.DataBoundItem is not DataRowView drv1 ||
            ModsGrid2.CurrentRow?.DataBoundItem is not DataRowView drv2) return;
        string mod1Id = drv1.Row["Id"]?.ToString()!;
        string mod2Id = drv2.Row["Id"]?.ToString()!;
        AppDatabase.InsertUserDependency(mod2Id, "optional", mod1Id);
        ReloadDepsGrid();
    }

    private void Mod1AfterMod2Button_Click(object? sender, EventArgs e)
    {
        if (ModsGrid1.CurrentRow?.DataBoundItem is not DataRowView drv1 ||
            ModsGrid2.CurrentRow?.DataBoundItem is not DataRowView drv2) return;
        string mod1Id = drv1.Row["Id"]?.ToString()!;
        string mod2Id = drv2.Row["Id"]?.ToString()!;
        AppDatabase.InsertUserDependency(mod1Id, "optional", mod2Id);
        ReloadDepsGrid();
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (DependenciesGrid.CurrentRow?.DataBoundItem is not DataRowView drv) return;
        string hash = drv.Row["Hash"]?.ToString()!;
        AppDatabase.DeleteUserDependency(hash);
        ReloadDepsGrid();
    }

    private void Mod1ConflictsMod2Button_Click(object? sender, EventArgs e)
    {
        if (ModsGrid1.CurrentRow?.DataBoundItem is not DataRowView drv1 ||
            ModsGrid2.CurrentRow?.DataBoundItem is not DataRowView drv2) return;
        string mod1Id = drv1.Row["Id"]?.ToString()!;
        string mod2Id = drv2.Row["Id"]?.ToString()!;
        AppDatabase.InsertUserConflict(mod1Id, mod2Id);
        ReloadDepsGrid();
    }

    private void Mod1ToTopButton_Click(object? sender, EventArgs e)
    {
        if (ModsGrid1.CurrentRow?.DataBoundItem is not DataRowView drv) return;
        string modId = drv.Row["Id"]?.ToString()!;
        if (AppDatabase.InsertUserLoadExtreme(modId, "top"))
            ReloadDepsGrid();
    }

    private void Mod1ToBottomButton_Click(object? sender, EventArgs e)
    {
        if (ModsGrid1.CurrentRow?.DataBoundItem is not DataRowView drv) return;
        string modId = drv.Row["Id"]?.ToString()!;
        if (AppDatabase.InsertUserLoadExtreme(modId, "bottom"))
            ReloadDepsGrid();
    }
}
