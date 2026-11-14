using ForeverTV.Core;

namespace ForeverTV.Core.Tests;

public class MarkerTests
{
    private static readonly VideoRect Video = new(300, 200, 640, 360);

    private static List<MarkerHit> FindAll(FakeScreen screen) =>
        MarkerDetector.Find(screen.Capture(0, 0, screen.Width, screen.Height)!);

    [Fact]
    public void Finds_the_exact_rectangle_on_an_empty_screen()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        Assert.Equal(Video, VideoRect.FromMarkers(FindAll(screen)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Finds_the_exact_rectangle_on_random_noise(int seed)
    {
        var screen = new FakeScreen(1280, 720);
        screen.Noise(seed);
        screen.PaintFrame(Video);
        Assert.Equal(Video, VideoRect.FromMarkers(FindAll(screen)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Random_noise_alone_finds_nothing(int seed)
    {
        var screen = new FakeScreen(1280, 720);
        screen.Noise(seed);
        Assert.Empty(FindAll(screen));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Play_state_cell_is_read_and_makes_no_extra_marker(bool playing)
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        screen.PaintPlayState(Video, playing);
        var hits = FindAll(screen);

        Assert.Equal(4, hits.Count);
        Assert.Equal(Video, VideoRect.FromMarkers(hits));
        var whole = screen.Capture(0, 0, screen.Width, screen.Height)!;
        Assert.Equal(playing, MarkerDetector.ReadPlaying(whole, hits.Single(h => h.Corner == Corner.TopLeft)));
    }

    [Fact]
    public void Tracker_reports_the_play_state_in_full_and_local_scans()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        screen.PaintPlayState(Video, true);
        long now = 0;
        var tracker = new MarkerTracker(screen, () => now);

        Assert.True(tracker.Tick(screen.Width, screen.Height).Playing);
        screen.PaintPlayState(Video, false);
        now += 33;
        var local = tracker.Tick(screen.Width, screen.Height);

        Assert.Equal(ScanKind.Local, local.Scan);
        Assert.False(local.Playing);
    }

    [Fact]
    public void A_frame_without_a_play_state_cell_reports_unknown()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        var tracker = new MarkerTracker(screen, () => 0);
        Assert.Null(tracker.Tick(screen.Width, screen.Height).Playing);
    }

    [Fact]
    public void A_covered_corner_gives_no_rectangle()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        screen.Cover(Video.X, Video.Y + Video.Height);
        Assert.Null(VideoRect.FromMarkers(FindAll(screen)));
    }

    [Fact]
    public void A_stray_marker_elsewhere_does_not_make_a_skewed_rectangle()
    {
        var hits = new List<MarkerHit>
        {
            new(Corner.TopLeft, 100, 100),
            new(Corner.TopRight, 500, 300),
            new(Corner.BottomLeft, 100, 400),
            new(Corner.BottomRight, 500, 400),
        };
        Assert.Null(VideoRect.FromMarkers(hits));
    }

    [Fact]
    public void Tracker_uses_small_captures_while_the_frame_is_where_it_was()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        long now = 0;
        var tracker = new MarkerTracker(screen, () => now);

        var first = tracker.Tick(screen.Width, screen.Height);
        Assert.Equal(ScanKind.Full, first.Scan);
        now += 33;
        var second = tracker.Tick(screen.Width, screen.Height);

        Assert.Equal(ScanKind.Local, second.Scan);
        Assert.Equal(Video, second.Rect);
        Assert.Equal(MarkerTracker.StillDelayMs, second.NextDelayMs);
        Assert.Equal(1, screen.FullCaptures);
    }

    [Fact]
    public void Tracker_follows_a_drag_with_local_captures_and_speeds_up()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        long now = 0;
        var tracker = new MarkerTracker(screen, () => now);
        tracker.Tick(screen.Width, screen.Height);

        var rect = Video;
        for (var step = 0; step < 10; step++)
        {
            rect = rect with { X = rect.X + 25, Y = rect.Y + 5 };
            screen.Clear();
            screen.PaintFrame(rect);
            now += 8;
            var result = tracker.Tick(screen.Width, screen.Height);
            Assert.Equal(rect, result.Rect);
            Assert.Equal(ScanKind.Local, result.Scan);
            Assert.Equal(MarkerTracker.MovingDelayMs, result.NextDelayMs);
        }
    }

    [Fact]
    public void Tracker_falls_back_to_a_full_scan_after_a_jump()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        long now = 0;
        var tracker = new MarkerTracker(screen, () => now);
        tracker.Tick(screen.Width, screen.Height);

        var far = new VideoRect(20, 20, 640, 360);
        screen.Clear();
        screen.PaintFrame(far);
        now += 8;
        var result = tracker.Tick(screen.Width, screen.Height);

        Assert.Equal(far, result.Rect);
        Assert.Equal(ScanKind.Full, result.Scan);
    }

    [Fact]
    public void Tracker_keeps_the_rectangle_through_a_short_loss_then_drops_it()
    {
        var screen = new FakeScreen(1280, 720);
        screen.PaintFrame(Video);
        long now = 0;
        var tracker = new MarkerTracker(screen, () => now);
        tracker.Tick(screen.Width, screen.Height);

        screen.Clear();
        now += MarkerTracker.LostGraceMs;
        Assert.Equal(Video, tracker.Tick(screen.Width, screen.Height).Rect);

        now += 1;
        var lost = tracker.Tick(screen.Width, screen.Height);
        Assert.Null(lost.Rect);
        Assert.Equal(MarkerTracker.LostDelayMs, lost.NextDelayMs);

        screen.PaintFrame(Video);
        now += 250;
        Assert.Equal(Video, tracker.Tick(screen.Width, screen.Height).Rect);
    }
}
