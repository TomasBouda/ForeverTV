namespace ForeverScreen.Core;

/// <summary>Where the video goes, in the game window's client coordinates.</summary>
public readonly record struct VideoRect(int X, int Y, int Width, int Height)
{
    private const int MinimumSide = 64;
    private const int AlignmentTolerance = 2;

    /// <summary>
    /// The top markers sit just above the video and the bottom markers just below it, flush with its left and right
    /// edges. Null unless all four are present and form a rectangle.
    /// </summary>
    public static VideoRect? FromMarkers(IReadOnlyCollection<MarkerHit> hits)
    {
        if (Get(Corner.TopLeft) is not { } topLeft || Get(Corner.TopRight) is not { } topRight
            || Get(Corner.BottomLeft) is not { } bottomLeft || Get(Corner.BottomRight) is not { } bottomRight)
        {
            return null;
        }
        if (Math.Abs(topLeft.Y - topRight.Y) > AlignmentTolerance || Math.Abs(bottomLeft.Y - bottomRight.Y) > AlignmentTolerance
            || Math.Abs(topLeft.X - bottomLeft.X) > AlignmentTolerance || Math.Abs(topRight.X - bottomRight.X) > AlignmentTolerance)
        {
            return null;
        }
        var top = topLeft.Y + MarkerDetector.MarkerHeight;
        var width = topRight.X + MarkerDetector.MarkerWidth - topLeft.X;
        var height = bottomLeft.Y - top;
        if (width < MinimumSide || height < MinimumSide) return null;
        return new VideoRect(topLeft.X, top, width, height);

        MarkerHit? Get(Corner corner)
        {
            foreach (var hit in hits)
            {
                if (hit.Corner == corner) return hit;
            }
            return null;
        }
    }
}
