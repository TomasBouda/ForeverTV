using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using ForeverScreen.Core;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ForeverScreen.App;

/// <summary>
/// The window over the game: always on top, never takes focus, lets every click through and is left out of screen
/// captures so that it cannot hide the markers from the tracker while it lags behind a drag.
/// </summary>
internal sealed class OverlayForm : Form
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint LWA_ALPHA = 0x2;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint GW_HWNDPREV = 3;
    private static readonly nint HWND_TOP = 0;
    private static readonly nint HWND_TOPMOST = -1;
    private static readonly nint HWND_NOTOPMOST = -2;

    private const string Origin = "https://foreverscreen.local";

    private bool _muted;
    private readonly System.Windows.Forms.Timer _repaint = new() { Interval = 50 };
    private string _stats = "";
    private bool _playing = true;
    private WebView2? _view;
    private Task? _starting;
    private bool _pageReady;
    private VideoRequest? _current;
    private int _sweep;
    private bool _topmost;

    public OverlayForm(bool muted)
    {
        _muted = muted;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(16, 40, 60);
        DoubleBuffered = true;
        Bounds = new Rectangle(-4000, -4000, 640, 360);
        _repaint.Tick += (_, _) =>
        {
            if (_playing) _sweep = (_sweep + 8) % Math.Max(1, ClientSize.Width);
            Invalidate();
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public string CaptureExclusion { get; private set; } = "not set";

    /// <summary>Lines for the log: how the video start went.</summary>
    public event Action<string>? Status;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetLayeredWindowAttributes(Handle, 0, 255, LWA_ALPHA);
        var excluded = SetWindowDisplayAffinity(Handle, WDA_EXCLUDEFROMCAPTURE);
        CaptureExclusion = excluded ? "on" : $"failed, Win32 error {Marshal.GetLastWin32Error()}";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _repaint.Start();
    }

    /// <summary>The video playing now, if any.</summary>
    public VideoRequest? Current => _current;

    public bool Playing => _playing && _current is not null;

    public bool Muted => _muted;

    /// <summary>The player page reports the title of the video once it is known: video id, title.</summary>
    public event Action<string, string>? TitleChanged;

    /// <summary>YouTube refused the video (code 2, 5, 100, 101 or 150).</summary>
    public event Action<int>? PlayerError;

    /// <summary>Loads a video, playing or paused, starting the browser on first use. Later calls only swap the video.</summary>
    public async void Play(VideoRequest request, bool playing)
    {
        _current = request;
        _playing = playing;
        try
        {
            _starting ??= StartPlayer();
            await _starting;
            LoadCurrent();
        }
        catch (Exception exception)
        {
            // Keep tracking with the test picture, so a video problem does not end the app.
            Status?.Invoke($"Video failed to start: {exception.GetType().Name}: {exception.Message}");
            _stats = "Video failed to start, see the log.";
            _starting = null;
        }
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (_pageReady && _view?.CoreWebView2 is { } core) _ = core.ExecuteScriptAsync(muted ? "command('mute')" : "command('unMute')");
    }

    private void LoadCurrent()
    {
        if (!_pageReady || _current is null || _view?.CoreWebView2 is not { } core) return;
        var url = _current.EmbedUrl(Origin, _muted);
        Status?.Invoke($"Loading video {_current.VideoId} at {_current.StartSeconds} s");
        _ = core.ExecuteScriptAsync($"loadVideo({JsonSerializer.Serialize(url)}, {(_playing ? "true" : "false")})");
    }

    /// <summary>
    /// Moves the window and shows it, without touching focus. Screen coordinates, physical pixels. While the game is
    /// the active window the video is topmost; otherwise it sits directly above the game in the z-order, so it stays
    /// visible next to other windows (a browser on another monitor) and windows over the game cover it too (ADR 0008).
    /// </summary>
    public void Place(Rectangle bounds, nint game, bool gameActive)
    {
        nint? after;
        if (gameActive)
        {
            after = HWND_TOPMOST;
        }
        else
        {
            if (_topmost) SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            var above = GetWindow(game, GW_HWNDPREV);
            after = above == Handle ? null : above == 0 ? HWND_TOP : above;
        }
        _topmost = gameActive;
        var flags = SWP_NOACTIVATE | SWP_SHOWWINDOW | (after is null ? SWP_NOZORDER : 0);
        SetWindowPos(Handle, after ?? 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags);
    }

    /// <summary>Parks the window off the screen. Hiding it could stop the page, and the video pause comes later.</summary>
    public void Park()
    {
        SetWindowPos(Handle, 0, -4000, -4000, Width, Height, SWP_NOACTIVATE | SWP_NOZORDER);
    }

    public void SetStats(string stats) => _stats = stats;

    /// <summary>Plays or pauses the video (the test picture stops its sweep).</summary>
    public void SetPlaying(bool playing)
    {
        _playing = playing;
        Invalidate();
        ApplyPlaying();
    }

    private void ApplyPlaying()
    {
        if (_pageReady && _view?.CoreWebView2 is { } core) _ = core.ExecuteScriptAsync(_playing ? "setPlaying(true)" : "setPlaying(false)");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var bar = new SolidBrush(Color.FromArgb(90, 200, 250));
        e.Graphics.FillRectangle(bar, _sweep, 0, 8, ClientSize.Height);
        using var font = new Font("Consolas", 11f);
        var heading = _current is null
            ? "ForeverScreen - no video yet\nTray icon: Paste YouTube link, or Play in WoW in the browser."
            : "ForeverScreen" + (_playing ? "" : " - PAUSED");
        e.Graphics.DrawString(heading + "\n" + _stats, font, Brushes.White, 12, 12);
    }

    private async Task StartPlayer()
    {
        var dataFolder = Path.Combine(AppContext.BaseDirectory, "WebView2");
        var pageFolder = Path.Combine(dataFolder, "page");
        Directory.CreateDirectory(pageFolder);
        await File.WriteAllTextAsync(Path.Combine(pageFolder, "player.html"), PlayerPage);

        var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
        Status?.Invoke("Creating the WebView2 environment");
        var environment = await CoreWebView2Environment.CreateAsync(null, dataFolder, options);
        var view = await CreateView(environment);
        _view = view;
        Status?.Invoke($"WebView2 runtime {environment.BrowserVersionString} ready");
        var loaded = new TaskCompletionSource();
        view.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            Status?.Invoke($"Player page: success {args.IsSuccess}, status {args.HttpStatusCode}, error {args.WebErrorStatus}");
            _pageReady = args.IsSuccess;
            loaded.TrySetResult();
        };
        view.CoreWebView2.WebMessageReceived += (_, args) => OnPageMessage(args.TryGetWebMessageAsString());
        view.CoreWebView2.SetVirtualHostNameToFolderMapping("foreverscreen.local", pageFolder, CoreWebView2HostResourceAccessKind.Allow);
        view.CoreWebView2.Navigate(Origin + "/player.html");
        await loaded.Task;
        _repaint.Stop();
    }

    private void OnPageMessage(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            var kind = root.GetProperty("kind").GetString();
            if (kind == "state")
            {
                Status?.Invoke("Player state: " + root.GetProperty("value").GetString());
            }
            else if (kind == "title" && _current is { } current && root.GetProperty("value").GetString() is { Length: > 0 } title)
            {
                TitleChanged?.Invoke(current.VideoId, title);
            }
            else if (kind == "error" && root.GetProperty("value").TryGetInt32(out var code))
            {
                Status?.Invoke($"Player error {code}");
                PlayerError?.Invoke(code);
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Status?.Invoke("Page: " + message);
        }
    }

    /// <summary>
    /// Closing the console kills this app, and the old browser processes hold the profile for some seconds after that.
    /// </summary>
    private async Task<WebView2> CreateView(CoreWebView2Environment environment)
    {
        const int profileInUse = unchecked((int)0x800700AA);
        for (var attempt = 1; ; attempt++)
        {
            var view = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(view);
            try
            {
                await view.EnsureCoreWebView2Async(environment);
                return view;
            }
            catch (COMException exception) when (exception.HResult == profileInUse && attempt < 15)
            {
                Status?.Invoke($"Browser profile still in use, try {attempt}");
                Controls.Remove(view);
                view.Dispose();
                await Task.Delay(2000);
            }
        }
    }

    // The app calls loadVideo, setPlaying and command. The player ignores commands until it is ready, so the wanted
    // state is sent again for a few seconds after the iframe loads and after every change.
    private const string PlayerPage = """
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"><meta name="referrer" content="strict-origin-when-cross-origin">
        <style>html,body{margin:0;height:100%;background:#000;overflow:hidden}iframe{border:0;width:100%;height:100%}</style>
        </head><body>
        <iframe id="player" allow="autoplay; encrypted-media" referrerpolicy="strict-origin-when-cross-origin"></iframe>
        <script>
        let wanted = true, lastState = null, lastTitle = null;
        const player = document.getElementById('player');
        const report = (kind, value) => window.chrome.webview.postMessage(JSON.stringify({ kind, value }));
        function command(func) {
          if (player.src && player.contentWindow) player.contentWindow.postMessage(JSON.stringify({ event: 'command', func, args: [] }), '*');
        }
        function send() { command(wanted ? 'playVideo' : 'pauseVideo'); }
        function sendForAWhile() { [0, 500, 1500, 3000].forEach(delay => setTimeout(send, delay)); }
        function setPlaying(playing) { wanted = playing; sendForAWhile(); }
        function loadVideo(url, playing) { wanted = playing; lastState = null; lastTitle = null; player.src = url; }
        // Report state, title and errors to the app, so the log shows whether commands arrived.
        const names = { '-1': 'unstarted', '0': 'ended', '1': 'playing', '2': 'paused', '3': 'buffering', '5': 'cued' };
        window.addEventListener('message', event => {
          if (event.origin !== 'https://www.youtube.com') return;
          let data;
          try { data = JSON.parse(event.data); } catch { return; }
          if (data.event === 'onError') { report('error', data.info); return; }
          const info = data.info || {};
          if (typeof info.playerState === 'number' && info.playerState !== lastState) {
            lastState = info.playerState;
            report('state', names[info.playerState] || String(info.playerState));
          }
          const title = info.videoData && info.videoData.title;
          if (title && title !== lastTitle) { lastTitle = title; report('title', title); }
        });
        player.addEventListener('load', () => {
          if (!player.src) return;
          player.contentWindow.postMessage(JSON.stringify({ event: 'listening', id: 'player' }), '*');
          sendForAWhile();
        });
        </script>
        </body></html>
        """;

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Status?.Invoke($"Window closed, reason {e.CloseReason}");
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _repaint.Dispose();
        base.Dispose(disposing);
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(nint window, uint key, byte alpha, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowDisplayAffinity(nint window, uint affinity);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
