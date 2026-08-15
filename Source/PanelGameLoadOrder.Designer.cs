using System.Drawing;
using System.Windows.Forms;

namespace TimberbornLauncher;

partial class PanelGameLoadOrder
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel MainTable;
    private TableLayoutPanel TopPanel;
    private FlowLayoutPanel SearchPanel;
    private SearchTextBox SearchTextBox;

    private CheckBox EnabledFilterChk;

    private CheckBox DisabledFilterChk;

    private CheckBox LocalFilterChk;

    private CheckBox SteamFilterChk;

    private DataGridView ModsGrid;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

private void InitializeComponent()
  {
    MainTable = new TableLayoutPanel();
    TopPanel = new TableLayoutPanel();
    SearchPanel = new FlowLayoutPanel();
    SearchTextBox = new SearchTextBox();
    EnabledFilterChk = new CheckBox();
    DisabledFilterChk = new CheckBox();
    LocalFilterChk = new CheckBox();
    SteamFilterChk = new CheckBox();
    ModsGrid = new DataGridView();
    ((System.ComponentModel.ISupportInitialize)ModsGrid).BeginInit();
    MainTable.SuspendLayout();
    TopPanel.SuspendLayout();
    SearchPanel.SuspendLayout();
    SuspendLayout();
    // 
    // MainTable
    // 
    MainTable.ColumnCount = 1;
    MainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    MainTable.Controls.Add(TopPanel, 0, 0);
    MainTable.Controls.Add(ModsGrid, 0, 1);
    MainTable.Dock = DockStyle.Fill;
    MainTable.Location = new Point(0, 0);
    MainTable.Name = "MainTable";
    MainTable.Padding = new Padding(0);
    MainTable.RowCount = 2;
    MainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
    MainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    MainTable.Size = new Size(1100, 668);
    MainTable.TabIndex = 0;
    // 
    // TopPanel
    // 
    TopPanel.ColumnCount = 1;
    TopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    TopPanel.Controls.Add(SearchPanel, 0, 0);
    TopPanel.Dock = DockStyle.Fill;
    TopPanel.Location = new Point(0, 0);
    TopPanel.Name = "TopPanel";
    TopPanel.RowCount = 1;
    TopPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    TopPanel.Size = new Size(1100, 36);
    TopPanel.TabIndex = 7;
    // 
    // SearchPanel
    // 
    SearchPanel.Controls.Add(SearchTextBox);
    SearchPanel.Controls.Add(EnabledFilterChk);
    SearchPanel.Controls.Add(DisabledFilterChk);
    SearchPanel.Controls.Add(LocalFilterChk);
    SearchPanel.Controls.Add(SteamFilterChk);
    SearchPanel.Dock = DockStyle.Fill;
    SearchPanel.FlowDirection = FlowDirection.LeftToRight;
    SearchPanel.Location = new Point(0, 0);
    SearchPanel.Margin = new Padding(0);
    SearchPanel.Name = "SearchPanel";
    SearchPanel.Size = new Size(0, 36);
    SearchPanel.TabIndex = 8;
    // 
    // SearchTextBox
    // 
    SearchTextBox.Margin = new Padding(0, 4, 12, 4);
    SearchTextBox.MaximumSize = new Size(270, 28);
    SearchTextBox.MinimumSize = new Size(270, 28);
    SearchTextBox.Name = "SearchTextBox";
    SearchTextBox.PlaceholderText = "Search mods...";
    SearchTextBox.Size = new Size(270, 28);
    SearchTextBox.TabIndex = 0;
    SearchTextBox.TextChanged += SearchTextBox_TextChanged;
    ModsGrid.CellDoubleClick += ModsGrid_CellDoubleClick;
    ModsGrid.CellClick += ModsGrid_CellClick;
    // 
    // EnabledFilterChk
    // 
    EnabledFilterChk.AutoSize = true;
    EnabledFilterChk.Margin = new Padding(0, 8, 12, 9);
    EnabledFilterChk.Name = "EnabledFilterChk";
    EnabledFilterChk.Size = new Size(68, 19);
    EnabledFilterChk.TabIndex = 1;
    EnabledFilterChk.Text = "Enabled";
    EnabledFilterChk.UseVisualStyleBackColor = true;
    EnabledFilterChk.CheckedChanged += Filter_CheckedChanged;
    // 
    // DisabledFilterChk
    // 
    DisabledFilterChk.AutoSize = true;
    DisabledFilterChk.Margin = new Padding(0, 8, 12, 9);
    DisabledFilterChk.Name = "DisabledFilterChk";
    DisabledFilterChk.Size = new Size(71, 19);
    DisabledFilterChk.TabIndex = 2;
    DisabledFilterChk.Text = "Disabled";
    DisabledFilterChk.UseVisualStyleBackColor = true;
    DisabledFilterChk.CheckedChanged += Filter_CheckedChanged;
    // 
    // LocalFilterChk
    // 
    LocalFilterChk.AutoSize = true;
    LocalFilterChk.Margin = new Padding(0, 8, 12, 9);
    LocalFilterChk.Name = "LocalFilterChk";
    LocalFilterChk.Size = new Size(54, 19);
    LocalFilterChk.TabIndex = 3;
    LocalFilterChk.Text = "Local";
    LocalFilterChk.UseVisualStyleBackColor = true;
    LocalFilterChk.CheckedChanged += Filter_CheckedChanged;
    // 
    // SteamFilterChk
    // 
    SteamFilterChk.AutoSize = true;
    SteamFilterChk.Margin = new Padding(0, 8, 0, 9);
    SteamFilterChk.Name = "SteamFilterChk";
    SteamFilterChk.Size = new Size(59, 19);
    SteamFilterChk.TabIndex = 4;
    SteamFilterChk.Text = "Steam";
    SteamFilterChk.UseVisualStyleBackColor = true;
    SteamFilterChk.CheckedChanged += Filter_CheckedChanged;
    // 
    // ModsGrid
    // 
    ModsGrid.AllowUserToAddRows = false;
    ModsGrid.AllowUserToDeleteRows = false;
    ModsGrid.AllowUserToResizeRows = false;
    ModsGrid.BorderStyle = BorderStyle.None;
    ModsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    ModsGrid.Dock = DockStyle.Fill;
    ModsGrid.GridColor = Color.LightGray;
    ModsGrid.MultiSelect = false;
    ModsGrid.Name = "ModsGrid";
    ModsGrid.RowHeadersVisible = false;
    ModsGrid.ScrollBars = ScrollBars.Vertical;
    ModsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    ModsGrid.ShowCellToolTips = true;
    ModsGrid.Size = new Size(1100, 632);
    ModsGrid.TabIndex = 5;
    // 
    // PanelGameLoadOrder
    // 
    AutoScaleDimensions = new SizeF(7F, 15F);
    AutoScaleMode = AutoScaleMode.Font;
    Controls.Add(MainTable);
    Name = "PanelGameLoadOrder";
    Size = new Size(1100, 668);
    MainTable.ResumeLayout(false);
    MainTable.PerformLayout();
    SearchPanel.ResumeLayout(false);
    TopPanel.ResumeLayout(false);
    TopPanel.PerformLayout();
    ((System.ComponentModel.ISupportInitialize)ModsGrid).EndInit();
    ResumeLayout(false);
    PerformLayout();
  }
}













