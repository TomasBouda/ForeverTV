using System.Diagnostics;

namespace ForeverScreen.Core;

public interface IRegionCapture
{
    /// <summary>A part of the client area, clamped to it; null when it cannot be read.</summary>
    PixelRegion? Capture(int x, int y, int width, int height);
}

public enum ScanKind { Local, Full }

/// <summary>Playing is the addon's play/pause state, null when it was not readable in this tick.</summary>
public sealed record TrackResult(VideoRect? Rect, ScanKind Scan, int NextDelayMs, bool? Playing = null);

/// <summary>
/// Follows the markers. Normally it reads small areas around where each marker should be; when one is missing it scans
/// the whole client area. A brief loss keeps the last rectangle so one torn capture does not blink the video.
/// </summary>
public sealed class MarkerTracker(IRegionCapture capture, Func<long>? clockMs = null)
{
    public const int LocalRadius = 120;
    public const int LostGraceMs = 200;
    public const int MovingDelayMs = 8;
    public const int StillDelayMs = 33;
    public const int LostDelayMs = 250;
    public const int MovingWindowMs = 500;

    private readonly Func<long> _clock = clockMs ?? SystemClock();
    private MarkerHit[]? _last;
    private VideoRect? _rect;
    private int _deltaX, _deltaY;
    private long _lastSeen = long.MinValue / 2;
    private long _lastMoved = long.MinValue / 2;

    public TrackResult Tick(int clientWidth, int clientHeight)
    {
        var now = _clock();
        var scan = ScanKind.Local;
        PixelRegion? topLeftRegion = null;
        var hits = _last is null ? null : ScanLocal(out topLeftRegion);
        if (hits is null)
        {
            scan = ScanKind.Full;
            topLeftRegion = capture.Capture(0, 0, clientWidth, clientHeight);
            hits = topLeftRegion is null ? [] : MarkerDetector.Find(topLeftRegion);
        }

        if (VideoRect.FromMarkers(hits) is { } found)
        {
            if (_rect is { } previous && previous != found)
            {
                _deltaX = found.X - previous.X;
                _deltaY = found.Y - previous.Y;
                _lastMoved = now;
            }
            else
            {
                _deltaX = _deltaY = 0;
            }
            _rect = found;
            _last = hits.ToArray();
            _lastSeen = now;
            var playing = topLeftRegion is null ? null
                : MarkerDetector.ReadPlaying(topLeftRegion, hits.First(h => h.Corner == Corner.TopLeft));
            return new TrackResult(found, scan, now - _lastMoved < MovingWindowMs ? MovingDelayMs : StillDelayMs, playing);
        }

        _last = null;
        _deltaX = _deltaY = 0;
        if (now - _lastSeen <= LostGraceMs) return new TrackResult(_rect, scan, MovingDelayMs);
        _rect = null;
        return new TrackResult(null, scan, LostDelayMs);
    }

    private List<MarkerHit>? ScanLocal(out PixelRegion? topLeftRegion)
    {
        topLeftRegion = null;
        var hits = new List<MarkerHit>(4);
        foreach (var last in _last!)
        {
            var region = capture.Capture(
                last.X + _deltaX - LocalRadius,
                last.Y + _deltaY - LocalRadius,
                MarkerDetector.MarkerWidth + 2 * LocalRadius,
                MarkerDetector.MarkerHeight + 2 * LocalRadius);
            if (region is null) return null;
            var found = MarkerDetector.Find(region, last.Corner);
            if (found.Count == 0) return null;
            hits.Add(found[0]);
            if (last.Corner == Corner.TopLeft) topLeftRegion = region;
        }
        return hits;
    }

    private static Func<long> SystemClock()
    {
        var watch = Stopwatch.StartNew();
        return () => watch.ElapsedMilliseconds;
    }
}
