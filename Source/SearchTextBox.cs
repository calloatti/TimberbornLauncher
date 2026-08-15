using System;
using System.Drawing;
using System.Windows.Forms;

namespace TimberbornLauncher;

public sealed class SearchTextBox : UserControl
{
    private readonly TextBox _textBox;
    private readonly Button _clearButton;

    public SearchTextBox()
    {
        int textBoxWidth = 200;
        int buttonWidth = 65;
        int gap = 5;
        int height = 28;

        this.Dock = DockStyle.None;
        this.Size = new Size(textBoxWidth + gap + buttonWidth, height);
        this.MinimumSize = this.Size;
        this.MaximumSize = this.Size;

        _textBox = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(textBoxWidth, height),
            TabStop = true,
            TabIndex = 0,
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.ControlText
        };

        _clearButton = new Button
        {
            Location = new Point(_textBox.Right + gap, -1),
            Size = new Size(buttonWidth, height - 3),
            Text = "Clear",
            TabStop = false,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true
        };

        _clearButton.Click += OnClearClicked;
        _textBox.TextChanged += OnTextChanged;

        Controls.Add(_textBox);
        Controls.Add(_clearButton);

        TabStop = true;
    }

    public new string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? "";
    }

    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value ?? "";
    }

    public new Font Font
    {
        get => _textBox.Font;
        set => _textBox.Font = value ?? _textBox.Font;
    }

    public override Color ForeColor
    {
        get => _textBox.ForeColor;
        set => _textBox.ForeColor = value;
    }

    public new event EventHandler? TextChanged;

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _textBox.Clear();
        _textBox.Focus();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        TextChanged?.Invoke(this, e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _textBox.Focus();
    }
}
