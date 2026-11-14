using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ForeverScreen.App;

/// <summary>The current user's Run key. Off until switched on from the tray menu.</summary>
internal static class StartWithWindows
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ForeverScreen";

    public static bool IsOn
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Set(bool on, Action<string> report)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (on) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
        report($"Start with Windows {(on ? "on" : "off")}");
    }
}

/// <summary>Asks for a YouTube link when the clipboard has none.</summary>
internal static class LinkPrompt
{
    public static string? Ask()
    {
        using var form = new Form
        {
            Text = "ForeverScreen - play in WoW",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(520, 96),
            TopMost = true,
        };
        var label = new Label { Text = "YouTube link or video id:", Location = new Point(12, 12), AutoSize = true };
        var box = new TextBox { Location = new Point(12, 34), Width = 496 };
        var ok = new Button { Text = "Play", DialogResult = DialogResult.OK, Location = new Point(352, 62), Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(433, 62), Width = 75 };
        form.Controls.AddRange([label, box, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK && box.Text.Trim().Length > 0 ? box.Text.Trim() : null;
    }
}

/// <summary>Today's log, newest line last, with a refresh button.</summary>
internal sealed class LogWindow : Form
{
    private readonly string _folder;
    private readonly TextBox _text = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9f),
    };

    public LogWindow(string folder)
    {
        _folder = folder;
        Text = "ForeverScreen - log";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 520);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4) };
        var refresh = new Button { Text = "Refresh", AutoSize = true, MinimumSize = new Size(90, 30) };
        refresh.Click += (_, _) => LoadLog();
        var open = new Button { Text = "Open folder", AutoSize = true, MinimumSize = new Size(110, 30) };
        open.Click += (_, _) => System.Diagnostics.Process.Start("explorer.exe", _folder);
        bar.Controls.AddRange([refresh, open]);
        Controls.Add(_text);
        Controls.Add(bar);
        Shown += (_, _) => LoadLog();
    }

    private void LoadLog()
    {
        var latest = new DirectoryInfo(_folder).GetFiles("*.log").OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault();
        if (latest is null)
        {
            _text.Text = "No log yet.";
            return;
        }
        using var stream = new FileStream(latest.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var lines = reader.ReadToEnd().Split('\n');
        _text.Text = string.Join(Environment.NewLine, lines.TakeLast(2000).Select(line => line.TrimEnd('\r')));
        _text.SelectionStart = _text.TextLength;
        _text.ScrollToCaret();
    }
}
