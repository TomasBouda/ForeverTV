using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ForeverScreen.Core;
using TomLabs.PixelBridge.Host;

namespace ForeverScreen.App;

// Tray app: follows the addon's markers with a click-through video window over WoW.
// Usage: ForeverScreen [<YouTube link or id>] [--video <link or id>] [--mute]
// A second start hands its video to the running one.
internal static class Program
{
    // WebView2 and WinForms need a single-threaded apartment, which top-level statements cannot ask for.
    [STAThread]
    private static void Main(string[] args)
    {
        using var instance = SingleInstance.TryStart(args);
        if (instance is null) return;

        var settings = AppSettings.Load();
        if (args.Contains("--mute")) settings.Muted = true;
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        var logFolder = Path.Combine(AppSettings.Folder, "logs");
        Directory.CreateDirectory(logFolder);
        var logPath = Path.Combine(logFolder, $"foreverscreen-{DateTime.Now:yyyy-MM-dd}.log");
        using var log = TextWriter.Synchronized(new StreamWriter(new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true });

        void Report(string line)
        {
            var stamped = $"{DateTime.Now:HH:mm:ss} {line}";
            Console.WriteLine(stamped);
            log.WriteLine(stamped);
        }

        Report($"ForeverScreen {version} started, data folder {AppSettings.Folder}");

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var overlay = new OverlayForm(settings.Muted);
        overlay.Status += Report;
        var locator = new GameWindowLocator();
        var capture = new GdiRegionCapture();
        var tracker = new MarkerTracker(capture);

        // Shared between the tracker thread and the UI thread.
        var tvVisible = false;
        var resync = false;
        string? currentTitle = null;

        var tray = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application,
            Text = "ForeverScreen",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };

        void Toast(string text, ToolTipIcon icon = ToolTipIcon.Info) => tray.ShowBalloonTip(5000, "ForeverScreen", text, icon);

        // Every way in (tray menu, API, a second start) ends here, on the UI thread.
        void PlayNow(VideoRequest request, string source)
        {
            var visible = Volatile.Read(ref tvVisible);
            Report($"Play {request.VideoId} at {request.StartSeconds} s from {source}, TV {(visible ? "visible" : "not visible")}");
            currentTitle = settings.Recent.FirstOrDefault(video => video.VideoId == request.VideoId)?.Title;
            overlay.Play(request, playing: visible);
            Volatile.Write(ref resync, true);
            settings.Remember(request, currentTitle);
            if (!visible) Toast("Video ready. Switch to WoW and open the TV (/fscreen); it starts there.");
        }

        void SetPausedNow(bool paused, string source)
        {
            Report($"{(paused ? "Pause" : "Resume")} from {source}");
            overlay.SetPlaying(!paused);
        }

        overlay.TitleChanged += (videoId, title) =>
        {
            currentTitle = title;
            settings.SetTitle(videoId, title);
            Report($"Title: {title}");
        };
        overlay.PlayerError += code => Toast(code switch
        {
            101 or 150 => "The owner does not allow this video outside YouTube.",
            100 => "This video is private or removed.",
            _ => $"YouTube could not play this video (error {code}).",
        }, ToolTipIcon.Warning);

        var screen = new ScreenControl(overlay, PlayNow, SetPausedNow, () => new ScreenStatus(
            overlay.Current?.VideoId, currentTitle, overlay.Playing, Volatile.Read(ref tvVisible), overlay.Muted));

        BuildMenu();
        tray.ContextMenuStrip.Opening += (_, _) => BuildMenu();
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && overlay.Current is not null) SetPausedNow(overlay.Playing, "tray");
        };

        LocalApi? api = null;
        overlay.Shown += async (_, _) =>
        {
            var thread = new Thread(TrackLoop) { IsBackground = true, Name = "Tracker" };
            thread.Start();
            try
            {
                api = await LocalApi.StartAsync(settings, screen, version, Report);
            }
            catch (Exception exception)
            {
                Report($"Local API failed on port {settings.Port}: {exception.Message}");
                Toast($"Port {settings.Port} is taken, so the browser cannot reach ForeverScreen. Change Port in settings.json.", ToolTipIcon.Warning);
            }
            if (StartRequest(args) is { } first) PlayNow(first, "command line");
        };
        instance.Listen(received =>
        {
            if (StartRequest(received) is { } request) Post(() => PlayNow(request, "second start"));
        });

        timeBeginPeriod(1);
        Application.Run(overlay);
        timeEndPeriod(1);
        tray.Visible = false;
        tray.Dispose();
        api?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
        overlay.Dispose();
        Report("Stopped");

        void BuildMenu()
        {
            var menu = tray.ContextMenuStrip!;
            menu.Items.Clear();
            menu.Items.Add(new ToolStripMenuItem($"ForeverScreen {version}") { Enabled = false });
            var now = overlay.Current is null ? "No video" : currentTitle ?? overlay.Current.VideoId;
            menu.Items.Add(new ToolStripMenuItem((overlay.Playing && overlay.Current is not null ? "Playing: " : "Paused: ") + Shorten(now)) { Enabled = false, Visible = overlay.Current is not null });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(overlay.Playing ? "Pause" : "Play", null, (_, _) => SetPausedNow(overlay.Playing, "tray")) { Enabled = overlay.Current is not null });
            menu.Items.Add(new ToolStripMenuItem("Mute", null, (_, _) =>
            {
                settings.Muted = !settings.Muted;
                settings.Save();
                overlay.SetMuted(settings.Muted);
            }) { Checked = settings.Muted });
            menu.Items.Add(new ToolStripMenuItem("Paste YouTube link", null, (_, _) => PasteLink()));
            var recent = new ToolStripMenuItem("Recent videos") { Enabled = settings.Recent.Count > 0 };
            foreach (var video in settings.Recent)
            {
                var request = new VideoRequest(video.VideoId, 0, video.PlaylistId);
                recent.DropDownItems.Add(new ToolStripMenuItem(Shorten(video.Title ?? video.VideoId), null, (_, _) => PlayNow(request, "recent")));
            }
            menu.Items.Add(recent);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Pair browser", null, (_, _) =>
            {
                Clipboard.SetText(settings.Token);
                Toast($"Pairing token copied. Paste it in the extension's options (port {settings.Port}).");
            }));
            menu.Items.Add(new ToolStripMenuItem("Start with Windows", null, (_, _) => StartWithWindows.Set(!StartWithWindows.IsOn, Report)) { Checked = StartWithWindows.IsOn });
            menu.Items.Add(new ToolStripMenuItem("Logs", null, (_, _) => new LogWindow(logFolder).Show()));
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => overlay.Close()));
        }

        void PasteLink()
        {
            var text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            if (!VideoRequest.TryParse(text, out var request))
            {
                text = LinkPrompt.Ask();
                if (text is null) return;
                if (!VideoRequest.TryParse(text, out request))
                {
                    Toast("That is not a YouTube video link.", ToolTipIcon.Warning);
                    return;
                }
            }
            PlayNow(request, "pasted link");
        }

        void TrackLoop()
        {
            GameWindow? game = null;
            var statsWatch = Stopwatch.StartNew();
            var findWatch = Stopwatch.StartNew();
            int ticks = 0, fullScans = 0, moves = 0, foregroundLosses = 0;
            double tickMsTotal = 0, tickMsMax = 0, fullMsMax = 0;
            Rectangle? placed = null;
            bool? applied = null;
            var lostWatch = new Stopwatch();
            var wasForeground = true;
            bool? placedActive = null;
            var zWatch = Stopwatch.StartNew();
            var overlayHandle = (nint)overlay.Invoke(() => overlay.Handle);

            while (!overlay.IsDisposed)
            {
                if (game is null)
                {
                    Volatile.Write(ref tvVisible, false);
                    if (findWatch.ElapsedMilliseconds >= 1000)
                    {
                        findWatch.Restart();
                        game = locator.Find();
                        if (game is not null) Report($"Game window: {game.ProcessName}, client {game.ClientWidth}x{game.ClientHeight} at {game.ClientX},{game.ClientY}");
                    }
                    if (game is null) { Thread.Sleep(100); continue; }
                }
                game = locator.Refresh(game);
                capture.Window = game;
                if (game is null || IsIconic(game.Handle)) { Hide(); Thread.Sleep(game is null ? 0 : 100); continue; }

                var foreground = GetForegroundWindow() == game.Handle;
                if (wasForeground && !foreground) foregroundLosses++;
                wasForeground = foreground;

                var started = Stopwatch.GetTimestamp();
                var result = tracker.Tick(game.ClientWidth, game.ClientHeight);
                var tickMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                Volatile.Write(ref tvVisible, result.Rect is not null);

                ticks++;
                tickMsTotal += tickMs;
                tickMsMax = Math.Max(tickMsMax, tickMs);
                if (result.Scan == ScanKind.Full)
                {
                    fullScans++;
                    fullMsMax = Math.Max(fullMsMax, tickMs);
                }

                // Play what the addon asks for. A frame gone for a while (closed, Alt+Z) pauses, so it does not play unseen.
                // Losing focus does not, so the sound can go on while you are in another window. A new video forgets the
                // last applied state, so the addon's state is applied again as soon as the TV shows.
                if (Volatile.Read(ref resync))
                {
                    Volatile.Write(ref resync, false);
                    applied = null;
                }
                if (result.Rect is not null) lostWatch.Reset();
                else if (foreground && !lostWatch.IsRunning) lostWatch.Start();
                var wanted = result.Rect is not null ? result.Playing : lostWatch.ElapsedMilliseconds > 1500 ? false : null;
                if (wanted is { } play && play != applied)
                {
                    applied = play;
                    Report(play ? "Play (addon)" : "Pause (addon or TV gone)");
                    Post(() => overlay.SetPlaying(play));
                }

                // Shown whenever the TV is on the screen, not only while WoW is active (ADR 0008). Out of focus the window
                // stays just above WoW in the z-order, so a window over WoW covers the video as well.
                if (result.Rect is { } rect)
                {
                    var bounds = new Rectangle(game.ClientX + rect.X, game.ClientY + rect.Y, rect.Width, rect.Height);
                    var zWrong = !foreground && zWatch.ElapsedMilliseconds >= 250 && GetWindow(game.Handle, 3 /* GW_HWNDPREV */) != overlayHandle;
                    if (zWrong) zWatch.Restart();
                    if (placed != bounds || placedActive != foreground || zWrong)
                    {
                        if (placed != bounds) moves++;
                        placed = bounds;
                        placedActive = foreground;
                        var handle = game.Handle;
                        var active = foreground;
                        Post(() => overlay.Place(bounds, handle, active));
                    }
                }
                else
                {
                    Hide();
                }

                if (statsWatch.ElapsedMilliseconds >= 60000)
                {
                    var seconds = statsWatch.Elapsed.TotalSeconds;
                    var line = $"{ticks / seconds:F0} ticks/s, tick avg {tickMsTotal / Math.Max(1, ticks):F1} ms max {tickMsMax:F1} ms, "
                        + $"full scans {fullScans} (max {fullMsMax:F1} ms), window moves {moves}, foreground lost {foregroundLosses}, "
                        + (placed is { } shown ? $"at {shown.X},{shown.Y} {shown.Width}x{shown.Height}" : "hidden");
                    Report(line);
                    Post(() => overlay.SetStats(line.Replace(", ", "\n")));
                    statsWatch.Restart();
                    ticks = fullScans = moves = foregroundLosses = 0;
                    tickMsTotal = tickMsMax = fullMsMax = 0;
                }

                Thread.Sleep(result.NextDelayMs);
            }

            void Hide()
            {
                if (placed is null) return;
                placed = null;
                placedActive = null;
                Post(overlay.Park);
            }
        }

        void Post(Action action)
        {
            try
            {
                overlay.BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // The window is closing.
            }
        }

        [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
        [DllImport("user32.dll")] static extern nint GetForegroundWindow();
        [DllImport("user32.dll")] static extern nint GetWindow(nint window, uint command);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool IsIconic(nint window);
    }

    /// <summary>The video from the command line: --video &lt;link&gt; or the first argument that is a link or id.</summary>
    private static VideoRequest? StartRequest(string[] args)
    {
        var at = Array.IndexOf(args, "--video");
        if (at >= 0 && at + 1 < args.Length && VideoRequest.TryParse(args[at + 1], out var named)) return named;
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal) && VideoRequest.TryParse(arg, out var request)) return request;
        }
        return null;
    }

    private static string Shorten(string text) => text.Length <= 60 ? text : text[..57] + "...";

    /// <summary>Hands API calls to the UI thread.</summary>
    private sealed class ScreenControl(
        Control ui, Action<VideoRequest, string> play, Action<bool, string> setPaused, Func<ScreenStatus> status) : IScreenControl
    {
        public void Play(VideoRequest request, string source) => ui.BeginInvoke(() => play(request, source));

        public void SetPaused(bool paused, string source) => ui.BeginInvoke(() => setPaused(paused, source));

        public ScreenStatus GetStatus() => (ScreenStatus)ui.Invoke(status);
    }
}
