using System;
using System.Linq;
using System.Windows.Forms;
using TimberbornLauncher.Mods;
using TimberbornLauncher.Versioning;

namespace TimberbornLauncher;

    public partial class MainForm : Form
{
    private UserControl? _activeView;

    /// <summary>Static hook so ModValidator.RefreshWarnings can update the warnings status label.</summary>
    public static ToolStripStatusLabel? WarningsStatusLabelInstance;


    public string? SelectedModPath { get; set; }

    private PanelHumanOrder? _humanOrderView;
    private PanelGameLoadOrder? _loadOrderView;
    private PanelUserLoadOrder? _userLoadOrderView;
    private PanelWarnings? _warningsView;

    public MainForm()
    {
        InitializeComponent();
        WarningsStatusLabelInstance = WarningsStatusLabel;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        if (LaunchOptions.ShouldLaunchGameDirectly())
        {
            _ = ModSave.LaunchGameAsync(this);
            return;
        }
        UpdateSummary();
        ShowHumanOrderView();
    }

    private void UpdateSummary()
    {
        List<ModEntry> mods = AppDatabase.GetModList();
        int localCount = mods.Count(m => m.IsUserMod);
        int workshopCount = mods.Count - localCount;
        GameVersion? gameVersion = GameVersionReader.TryReadCurrentVersion();
        VersionStatusLabel.Text = gameVersion == null ? "" : "Game v" + gameVersion.Numeric;
        string steamArgs = LaunchOptions.Arguments.Count > 0 ? "  \u2022  " + string.Join(" ", LaunchOptions.Arguments) : "";
        ModsStatusLabel.Text = $"{mods.Count} mods  \u2022  Local: {localCount}  \u2022  Steam Workshop: {workshopCount}" + steamArgs;
        ModValidator.RefreshWarnings();
    }

    private void HumanOrderViewButton_Click(object? sender, EventArgs e)
    {
        ShowHumanOrderView();
    }

    private void LoadOrderViewButton_Click(object? sender, EventArgs e)
    {
        ShowLoadOrderView();
    }

    private void UserLoadOrderViewButton_Click(object? sender, EventArgs e)
    {
        ShowUserLoadOrderView();
    }

    private void WarningsViewButton_Click(object? sender, EventArgs e)
    {
        ShowWarningsView();
    }

    private void ShowHumanOrderView()
    {
        if (_humanOrderView == null || _humanOrderView.IsDisposed)
        {
            _humanOrderView = new PanelHumanOrder();
            _viewContainer.Controls.Add(_humanOrderView);
        }
        ActivateView(_humanOrderView);
        HumanOrderViewButton.Checked = true;
    }

    private void ShowLoadOrderView()
    {
        ModSorter.ComputeLoadOrder();
        if (_loadOrderView == null || _loadOrderView.IsDisposed)
        {
            _loadOrderView = new PanelGameLoadOrder();
            _viewContainer.Controls.Add(_loadOrderView);
        }
        ActivateView(_loadOrderView);
        LoadOrderViewButton.Checked = true;
    }

    private void ShowUserLoadOrderView()
    {
        if (_userLoadOrderView == null || _userLoadOrderView.IsDisposed)
        {
            _userLoadOrderView = new PanelUserLoadOrder();
            _viewContainer.Controls.Add(_userLoadOrderView);
        }
        ActivateView(_userLoadOrderView);
        UserLoadOrderViewButton.Checked = true;
    }

    public void ShowWarningsView()
    {
        if (_warningsView != null && !_warningsView.IsDisposed)
        {
            _warningsView.Dispose();
            _viewContainer.Controls.Remove(_warningsView);
        }
        _warningsView = new PanelWarnings();
        _viewContainer.Controls.Add(_warningsView);
        ActivateView(_warningsView);
        WarningsViewButton.Checked = true;
    }

    private void ActivateView(UserControl view)
    {
        if (_activeView != null && !_activeView.IsDisposed && !ReferenceEquals(_activeView, view))
        {
            _activeView.Hide();
        }
        _activeView = view;
        view.Dock = DockStyle.Fill;
        view.Show();
        view.BringToFront();
    }

    private void RunGameButton_Click(object? sender, EventArgs e)
    {
        Log.Info("RunGameButton_Click: clicked");
        if (!HasBlockingWarnings())
        {
            Log.Info("RunGameButton_Click: no blocking warnings, calling ApplyAndLaunchAsync");
            _ = ModSave.ApplyAndLaunchAsync(this);
        }
        else
        {
            Log.Info("RunGameButton_Click: blocking warnings present");
        }
    }

    private void SaveChangesButton_Click(object? sender, EventArgs e)
    {
        ModSorter.Apply(this);
    }

    /// <summary>
    /// Returns true when the enabled mod set would break the game (duplicate enabled ids,
    /// required dependency missing or disabled). Blocks Run Game in that case.
    /// </summary>
    private bool HasBlockingWarnings()
    {
        ModValidator.RefreshWarnings();
        int count = AppDatabase.GetBlockingWarningCount();
        if (count > 0)
        {
            ShowWarningsView();
            return true;
        }
        return false;
    }
}


