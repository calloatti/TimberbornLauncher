using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TimberbornLauncher;

partial class PanelUserLoadOrder
{
    private IContainer components = null;

    private TableLayoutPanel MainTable;
private TableLayoutPanel TopPanel;
    private FlowLayoutPanel SearchPanel;
    private SearchTextBox Mod1SearchBox;
    private DataGridView ModsGrid1;
    private SearchTextBox Mod2SearchBox;
    private DataGridView ModsGrid2;
    private SearchTextBox DependenciesSearchBox;
    private DataGridView DependenciesGrid;
    private FlowLayoutPanel ButtonPanel;
    private Button Mod1BeforeMod2Button;
    private Button Mod1AfterMod2Button;
    private Button Mod1ConflictsMod2Button;
    private Button DeleteButton;

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
    Mod1SearchBox = new SearchTextBox();
    Mod2SearchBox = new SearchTextBox();
    DependenciesSearchBox = new SearchTextBox();
    ModsGrid1 = new DataGridView();
    ModsGrid2 = new DataGridView();
    DependenciesGrid = new DataGridView();
    ButtonPanel = new FlowLayoutPanel();
    Mod1AfterMod2Button = new Button();
    Mod1BeforeMod2Button = new Button();
    Mod1ConflictsMod2Button = new Button();
    DeleteButton = new Button();
    MainTable.SuspendLayout();
    TopPanel.SuspendLayout();
    SearchPanel.SuspendLayout();
    ((ISupportInitialize)ModsGrid1).BeginInit();
    ((ISupportInitialize)ModsGrid2).BeginInit();
    ((ISupportInitialize)DependenciesGrid).BeginInit();
    ButtonPanel.SuspendLayout();
    SuspendLayout();
    // 
    // MainTable
    // 
    MainTable.ColumnCount = 1;
    MainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    MainTable.Controls.Add(TopPanel, 0, 0);
    MainTable.Controls.Add(ModsGrid1, 0, 1);
    MainTable.Controls.Add(ModsGrid2, 0, 2);
    MainTable.Controls.Add(DependenciesGrid, 0, 3);
    MainTable.Controls.Add(ButtonPanel, 0, 4);
    MainTable.Dock = DockStyle.Fill;
    MainTable.Location = new Point(0, 0);
    MainTable.Name = "MainTable";
    MainTable.Padding = new Padding(0);
    MainTable.RowCount = 5;
    MainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
    MainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
    MainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
    MainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334F));
    MainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
    MainTable.Size = new Size(1100, 668);
    MainTable.TabIndex = 0;
    // 
    // TopPanel
    // 
    TopPanel.ColumnCount = 1;
    TopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    TopPanel.Controls.Add(SearchPanel, 0, 0);
    TopPanel.Dock = DockStyle.Fill;
    TopPanel.Location = new Point(15, 15);
    TopPanel.Name = "TopPanel";
    TopPanel.RowCount = 1;
    TopPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    TopPanel.Size = new Size(1070, 30);
    TopPanel.TabIndex = 0;
    // 
    // SearchPanel
    // 
    SearchPanel.Controls.Add(Mod1SearchBox);
    SearchPanel.Controls.Add(Mod2SearchBox);
    SearchPanel.Controls.Add(DependenciesSearchBox);
    SearchPanel.Dock = DockStyle.Fill;
    SearchPanel.Location = new Point(0, 0);
    SearchPanel.Margin = new Padding(0);
    SearchPanel.Name = "SearchPanel";
    SearchPanel.Size = new Size(973, 30);
    SearchPanel.TabIndex = 7;
    // 
    // Mod1SearchBox
    // 
    Mod1SearchBox.Location = new Point(0, 4);
    Mod1SearchBox.Margin = new Padding(0, 4, 12, 4);
    Mod1SearchBox.MaximumSize = new Size(270, 28);
    Mod1SearchBox.MinimumSize = new Size(270, 28);
    Mod1SearchBox.Name = "Mod1SearchBox";
    Mod1SearchBox.PlaceholderText = "Filter mods...";
    Mod1SearchBox.Size = new Size(270, 28);
    Mod1SearchBox.TabIndex = 0;
    // 
    // Mod2SearchBox
    // 
    Mod2SearchBox.Location = new Point(282, 4);
    Mod2SearchBox.Margin = new Padding(0, 4, 12, 4);
    Mod2SearchBox.MaximumSize = new Size(270, 28);
    Mod2SearchBox.MinimumSize = new Size(270, 28);
    Mod2SearchBox.Name = "Mod2SearchBox";
    Mod2SearchBox.PlaceholderText = "Filter mods...";
    Mod2SearchBox.Size = new Size(270, 28);
    Mod2SearchBox.TabIndex = 2;
    // 
    // DependenciesSearchBox
    // 
    DependenciesSearchBox.Location = new Point(564, 4);
    DependenciesSearchBox.Margin = new Padding(0, 4, 0, 4);
    DependenciesSearchBox.MaximumSize = new Size(270, 28);
    DependenciesSearchBox.MinimumSize = new Size(270, 28);
    DependenciesSearchBox.Name = "DependenciesSearchBox";
    DependenciesSearchBox.PlaceholderText = "Filter mods...";
    DependenciesSearchBox.Size = new Size(270, 28);
    DependenciesSearchBox.TabIndex = 4;
    // 
    // ModsGrid1
    // 
    ModsGrid1.AllowUserToAddRows = false;
    ModsGrid1.AllowUserToDeleteRows = false;
    ModsGrid1.AllowUserToResizeRows = false;
    ModsGrid1.BorderStyle = BorderStyle.None;
    ModsGrid1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    ModsGrid1.Dock = DockStyle.Fill;
    ModsGrid1.GridColor = Color.LightGray;
    ModsGrid1.Location = new Point(15, 51);
    ModsGrid1.MultiSelect = false;
    ModsGrid1.Name = "ModsGrid1";
    ModsGrid1.RowHeadersVisible = false;
    ModsGrid1.ScrollBars = ScrollBars.Vertical;
    ModsGrid1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    ModsGrid1.ShowCellToolTips = true;
    ModsGrid1.Size = new Size(1070, 184);
    ModsGrid1.TabIndex = 1;
    // 
    // ModsGrid2
    // 
    ModsGrid2.AllowUserToAddRows = false;
    ModsGrid2.AllowUserToDeleteRows = false;
    ModsGrid2.AllowUserToResizeRows = false;
    ModsGrid2.BorderStyle = BorderStyle.None;
    ModsGrid2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    ModsGrid2.Dock = DockStyle.Fill;
    ModsGrid2.GridColor = Color.LightGray;
    ModsGrid2.Location = new Point(15, 241);
    ModsGrid2.MultiSelect = false;
    ModsGrid2.Name = "ModsGrid2";
    ModsGrid2.RowHeadersVisible = false;
    ModsGrid2.ScrollBars = ScrollBars.Vertical;
    ModsGrid2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    ModsGrid2.ShowCellToolTips = true;
    ModsGrid2.Size = new Size(1070, 184);
    ModsGrid2.TabIndex = 3;
    // 
    // DependenciesGrid
    // 
    DependenciesGrid.AllowUserToAddRows = false;
    DependenciesGrid.AllowUserToDeleteRows = false;
    DependenciesGrid.AllowUserToResizeRows = false;
    DependenciesGrid.BorderStyle = BorderStyle.None;
    DependenciesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    DependenciesGrid.Dock = DockStyle.Fill;
    DependenciesGrid.GridColor = Color.LightGray;
    DependenciesGrid.Location = new Point(15, 431);
    DependenciesGrid.MultiSelect = false;
    DependenciesGrid.Name = "DependenciesGrid";
    DependenciesGrid.RowHeadersVisible = false;
    DependenciesGrid.ScrollBars = ScrollBars.Vertical;
    DependenciesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    DependenciesGrid.ShowCellToolTips = true;
    DependenciesGrid.Size = new Size(1070, 184);
    DependenciesGrid.TabIndex = 5;
    // 
    // ButtonPanel
    // 
    ButtonPanel.Controls.Add(Mod1AfterMod2Button);
    ButtonPanel.Controls.Add(Mod1BeforeMod2Button);
    ButtonPanel.Controls.Add(Mod1ConflictsMod2Button);
    ButtonPanel.Controls.Add(DeleteButton);
    ButtonPanel.Dock = DockStyle.Fill;
    ButtonPanel.FlowDirection = FlowDirection.RightToLeft;
    ButtonPanel.Location = new Point(15, 621);
    ButtonPanel.Name = "ButtonPanel";
    ButtonPanel.Size = new Size(1070, 32);
    ButtonPanel.TabIndex = 6;
    // 
        // Mod1BeforeMod2Button
    // 
    Mod1BeforeMod2Button.AutoSize = true;
    Mod1BeforeMod2Button.Location = new Point(939, 3);
    Mod1BeforeMod2Button.Name = "Mod1BeforeMod2Button";
    Mod1BeforeMod2Button.Size = new Size(128, 25);
    Mod1BeforeMod2Button.TabIndex = 1;
    Mod1BeforeMod2Button.Text = "mod1 before mod2";
    Mod1BeforeMod2Button.UseVisualStyleBackColor = true;
    // 
    // Mod1AfterMod2Button
    // 
    Mod1AfterMod2Button.AutoSize = true;
    Mod1AfterMod2Button.Location = new Point(805, 3);
    Mod1AfterMod2Button.Name = "Mod1AfterMod2Button";
    Mod1AfterMod2Button.Size = new Size(128, 25);
    Mod1AfterMod2Button.TabIndex = 0;
    Mod1AfterMod2Button.Text = "mod1 after mod2";
    Mod1AfterMod2Button.UseVisualStyleBackColor = true;
    // 
    // Mod1ConflictsMod2Button
    // 
    Mod1ConflictsMod2Button.AutoSize = true;
    Mod1ConflictsMod2Button.Location = new Point(537, 3);
    Mod1ConflictsMod2Button.Name = "Mod1ConflictsMod2Button";
    Mod1ConflictsMod2Button.Size = new Size(128, 25);
    Mod1ConflictsMod2Button.TabIndex = 2;
    Mod1ConflictsMod2Button.Text = "mod1 conflicts mod2";
    Mod1ConflictsMod2Button.UseVisualStyleBackColor = true;
    // 
    // DeleteButton
    // 
    DeleteButton.AutoSize = true;
    DeleteButton.Location = new Point(671, 3);
    DeleteButton.Name = "DeleteButton";
    DeleteButton.Size = new Size(128, 25);
    DeleteButton.TabIndex = 2;
    DeleteButton.Text = "Delete";
    DeleteButton.UseVisualStyleBackColor = true;
    // 
    // PanelUserLoadOrder
    // 
    AutoScaleDimensions = new SizeF(7F, 15F);
    AutoScaleMode = AutoScaleMode.Font;
    Controls.Add(MainTable);
    Name = "PanelUserLoadOrder";
    Size = new Size(1100, 668);
    MainTable.ResumeLayout(false);
    TopPanel.ResumeLayout(false);
    TopPanel.PerformLayout();
    SearchPanel.ResumeLayout(false);
    ((ISupportInitialize)ModsGrid1).EndInit();
    ((ISupportInitialize)ModsGrid2).EndInit();
    ((ISupportInitialize)DependenciesGrid).EndInit();
    ButtonPanel.ResumeLayout(false);
    ButtonPanel.PerformLayout();
    ResumeLayout(false);
  }
}













