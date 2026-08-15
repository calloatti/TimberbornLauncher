using System.Drawing;
using System.Windows.Forms;

namespace TimberbornLauncher;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel _viewContainer;

    private StatusStrip StatusBar;

    private ToolStripStatusLabel VersionStatusLabel;

    private ToolStripStatusLabel ModsStatusLabel;

    private ToolStripStatusLabel WarningsStatusLabel;

    private Button RunGameButton;

    private Button SaveChangesButton;

    private RadioButton HumanOrderViewButton;

    private RadioButton LoadOrderViewButton;

    private RadioButton UserLoadOrderViewButton;

    private RadioButton WarningsViewButton;

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
    _viewContainer = new Panel();
    StatusBar = new StatusStrip();
    VersionStatusLabel = new ToolStripStatusLabel();
    ModsStatusLabel = new ToolStripStatusLabel();
    WarningsStatusLabel = new ToolStripStatusLabel();
    RunGameButton = new Button();
    SaveChangesButton = new Button();
    HumanOrderViewButton = new RadioButton();
    LoadOrderViewButton = new RadioButton();
    UserLoadOrderViewButton = new RadioButton();
    WarningsViewButton = new RadioButton();
    StatusBar.SuspendLayout();
    SuspendLayout();
    // 
    // _viewContainer
    // 
    _viewContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    _viewContainer.Location = new Point(16, 12);
    _viewContainer.Name = "_viewContainer";
    _viewContainer.Size = new Size(1090, 718);
    _viewContainer.TabIndex = 0;
    // 
    // StatusBar
    // 
    StatusBar.Items.AddRange(new ToolStripItem[] { VersionStatusLabel, ModsStatusLabel, WarningsStatusLabel });
    StatusBar.Location = new Point(0, 749);
    StatusBar.Name = "StatusBar";
    StatusBar.Size = new Size(1264, 22);
    StatusBar.TabIndex = 3;
    // 
    // VersionStatusLabel
    // 
    VersionStatusLabel.Name = "VersionStatusLabel";
    VersionStatusLabel.Size = new Size(0, 17);
    // 
    // ModsStatusLabel
    // 
    ModsStatusLabel.Name = "ModsStatusLabel";
    ModsStatusLabel.Size = new Size(0, 17);
    // 
    // WarningsStatusLabel
    // 
    WarningsStatusLabel.Name = "WarningsStatusLabel";
    WarningsStatusLabel.Size = new Size(0, 17);
    // 
    // RunGameButton
    // 
    RunGameButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    RunGameButton.Location = new Point(1116, 213);
    RunGameButton.Name = "RunGameButton";
    RunGameButton.Size = new Size(136, 25);
    RunGameButton.TabIndex = 4;
    RunGameButton.Text = "Run Game";
    RunGameButton.UseVisualStyleBackColor = true;
    RunGameButton.Click += RunGameButton_Click;
    // 
    // SaveChangesButton
    // 
    SaveChangesButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    SaveChangesButton.Location = new Point(1116, 180);
    SaveChangesButton.Name = "SaveChangesButton";
    SaveChangesButton.Size = new Size(136, 25);
    SaveChangesButton.TabIndex = 8;
    SaveChangesButton.Text = "Save changes";
    SaveChangesButton.UseVisualStyleBackColor = true;
    SaveChangesButton.Click += SaveChangesButton_Click;
    // 
    // HumanOrderViewButton
    // 
    HumanOrderViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    HumanOrderViewButton.Appearance = Appearance.Button;
    HumanOrderViewButton.Location = new Point(1116, 48);
    HumanOrderViewButton.Name = "HumanOrderViewButton";
    HumanOrderViewButton.Size = new Size(136, 25);
    HumanOrderViewButton.TabIndex = 5;
    HumanOrderViewButton.Text = "Human order";
    HumanOrderViewButton.TextAlign = ContentAlignment.MiddleCenter;
    HumanOrderViewButton.Click += HumanOrderViewButton_Click;
    // 
    // LoadOrderViewButton
    // 
    LoadOrderViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    LoadOrderViewButton.Appearance = Appearance.Button;
    LoadOrderViewButton.Location = new Point(1116, 81);
    LoadOrderViewButton.Name = "LoadOrderViewButton";
    LoadOrderViewButton.Size = new Size(136, 25);
    LoadOrderViewButton.TabIndex = 6;
    LoadOrderViewButton.Text = "Game load order";
    LoadOrderViewButton.TextAlign = ContentAlignment.MiddleCenter;
    LoadOrderViewButton.Click += LoadOrderViewButton_Click;
    // 
    // UserLoadOrderViewButton
    // 
    UserLoadOrderViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    UserLoadOrderViewButton.Appearance = Appearance.Button;
    UserLoadOrderViewButton.Location = new Point(1116, 114);
    UserLoadOrderViewButton.Name = "UserLoadOrderViewButton";
    UserLoadOrderViewButton.Size = new Size(136, 25);
    UserLoadOrderViewButton.TabIndex = 7;
    UserLoadOrderViewButton.Text = "User load order";
    UserLoadOrderViewButton.TextAlign = ContentAlignment.MiddleCenter;
    UserLoadOrderViewButton.Click += UserLoadOrderViewButton_Click;
    // 
    // WarningsViewButton
    // 
    WarningsViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    WarningsViewButton.Appearance = Appearance.Button;
    WarningsViewButton.Location = new Point(1116, 147);
    WarningsViewButton.Name = "WarningsViewButton";
    WarningsViewButton.Size = new Size(136, 25);
    WarningsViewButton.TabIndex = 9;
    WarningsViewButton.Text = "Warnings";
    WarningsViewButton.TextAlign = ContentAlignment.MiddleCenter;
    WarningsViewButton.Click += WarningsViewButton_Click;
    // 
    // MainForm
    // 
    AutoScaleDimensions = new SizeF(7F, 15F);
    AutoScaleMode = AutoScaleMode.Font;
    ClientSize = new Size(1264, 771);
    Controls.Add(_viewContainer);
    Controls.Add(WarningsViewButton);
    Controls.Add(UserLoadOrderViewButton);
    Controls.Add(LoadOrderViewButton);
    Controls.Add(HumanOrderViewButton);
    Controls.Add(RunGameButton);
    Controls.Add(SaveChangesButton);
    Controls.Add(StatusBar);
    MinimumSize = new Size(700, 480);
    Name = "MainForm";
    StartPosition = FormStartPosition.CenterScreen;
    Text = "Timberborn Launcher";
    Load += MainForm_Load;
    StatusBar.ResumeLayout(false);
    StatusBar.PerformLayout();
    ResumeLayout(false);
    PerformLayout();
  }
}



