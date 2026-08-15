using System;
using System.Data;
using System.Windows.Forms;

namespace TimberbornLauncher;

public partial class PanelWarnings : UserControl
{
    private DataTable _warningsTable = new();
    private BindingSource _warningsBinding = new();

    public PanelWarnings()
    {
        InitializeComponent();
        BuildColumns();
        VisibleChanged += PanelWarnings_VisibleChanged;
        LoadWarnings();
    }

    private void PanelWarnings_VisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
        {
            LoadWarnings();
        }
    }

    private void BuildColumns()
    {
        ModsGrid.AutoGenerateColumns = false;

        var severityCol = new DataGridViewTextBoxColumn
        {
            Name = "Severity",
            HeaderText = "Severity",
            Width = 80,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            ReadOnly = true,
            DataPropertyName = "Severity"
        };
        var messageCol = new DataGridViewTextBoxColumn
        {
            Name = "Message",
            HeaderText = "Message",
            SortMode = DataGridViewColumnSortMode.NotSortable,
            ReadOnly = true,
            DataPropertyName = "Message",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        ModsGrid.Columns.AddRange(severityCol, messageCol);
        ModsGrid.ColumnHeadersHeight = 24;
    }

    private void LoadWarnings()
    {
        ModValidator.RefreshWarnings();
        _warningsTable = AppDatabase.ExecuteQuery(
            """
            SELECT CASE WHEN is_blocking = 1 THEN 'Error' ELSE 'Warning' END AS Severity, message AS Message
            FROM warnings
            ORDER BY message;
            """);
        _warningsBinding.DataSource = _warningsTable;
        ModsGrid.DataSource = _warningsBinding;
        ApplyFilters();
    }

    private void SearchTextBox_TextChanged(object? sender, EventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (_warningsBinding.DataSource == null) return;

        string query = SearchTextBox.Text.Trim();
        _warningsBinding.Filter = query.Length > 0
            ? $"Message LIKE '%{EscapeLike(query)}%'"
            : null;
    }

    private static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}