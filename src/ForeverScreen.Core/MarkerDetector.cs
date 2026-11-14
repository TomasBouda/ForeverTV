namespace ForeverScreen.Core;

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

/// <summary>A marker's top-left pixel, in the game window's client coordinates.</summary>
public readonly record struct MarkerHit(Corner Corner, int X, int Y);

/// <summary>Part of the game window's client area as 32 bpp BGRA, top-down.</summary>
public sealed record PixelRegion(byte[] Pixels, int Width, int Height, int OriginX, int OriginY);

/// <summary>
/// Finds the four corner markers the addon draws. A marker is three 6 x 6 px cells side by side in saturated colours;
/// the order of the colours says which corner it is. Cells are sampled at their centres, so blended edges from a
/// fractional frame position do not matter.
/// </summary>
public static class MarkerDetector
{
    public const int CellSize = 6;
    public const int MarkerWidth = CellSize * 3;
    public const int MarkerHeight = CellSize;

    private enum Hue { None, Magenta, Green, Cyan }

    private readonly record struct Pattern(Corner Corner, Hue A, Hue B, Hue C);

    private static readonly Pattern[] Patterns =
    [
        new(Corner.TopLeft, Hue.Magenta, Hue.Green, Hue.Cyan),
        new(Corner.TopRight, Hue.Green, Hue.Cyan, Hue.Magenta),
        new(Corner.BottomLeft, Hue.Cyan, Hue.Magenta, Hue.Green),
        new(Corner.BottomRight, Hue.Magenta, Hue.Cyan, Hue.Green),
    ];

    /// <summary>Scans a region and returns the first hit for each wanted corner (all four when null).</summary>
    public static List<MarkerHit> Find(PixelRegion region, Corner? only = null)
    {
        var hits = new List<MarkerHit>(4);
        var wanted = only is null ? 4 : 1;
        for (var y = 0; y < region.Height && hits.Count < wanted; y += 2)
        {
            for (var x = 0; x < region.Width - 2 * CellSize; x += 2)
            {
                var first = HueAt(region, x, y);
                if (first == Hue.None) continue;
                foreach (var pattern in Patterns)
                {
                    if (pattern.A != first || (only is not null && pattern.Corner != only)) continue;
                    if (hits.Exists(h => h.Corner == pattern.Corner)) continue;
                    if (!Matches(region, x, y, pattern)) continue;
                    var hit = Refine(region, x, y, pattern);
                    if (IsSolid(region, hit, pattern)) hits.Add(hit);
                }
            }
        }
        return hits;
    }

    /// <summary>
    /// The cell right of the top-left marker carries the play state: green plays, cyan pauses. Neither colour after the
    /// marker's own green and cyan forms another corner's pattern. Null when the cell is not readable in this region.
    /// </summary>
    public static bool? ReadPlaying(PixelRegion region, MarkerHit topLeft)
    {
        var x = topLeft.X - region.OriginX + MarkerWidth + CellSize / 2;
        var y = topLeft.Y - region.OriginY + CellSize / 2;
        return HueAt(region, x, y) switch
        {
            Hue.Green => true,
            Hue.Cyan => false,
            _ => null,
        };
    }

    private static bool Matches(PixelRegion r, int x, int y, Pattern p) =>
        HueAt(r, x, y) == p.A && HueAt(r, x + CellSize, y) == p.B && HueAt(r, x + 2 * CellSize, y) == p.C;

    /// <summary>Every sample point inside the first cell matches, so the middle of that run is the cell's centre.</summary>
    private static MarkerHit Refine(PixelRegion r, int x, int y, Pattern p)
    {
        int left = x, right = x;
        while (Matches(r, left - 1, y, p)) left--;
        while (Matches(r, right + 1, y, p)) right++;
        var middleX = (left + right) / 2;
        int top = y, bottom = y;
        while (Matches(r, middleX, top - 1, p)) top--;
        while (Matches(r, middleX, bottom + 1, p)) bottom++;
        var half = (CellSize - 1) / 2.0;
        return new MarkerHit(p.Corner,
            r.OriginX + (int)Math.Round((left + right) / 2.0 - half, MidpointRounding.AwayFromZero),
            r.OriginY + (int)Math.Round((top + bottom) / 2.0 - half, MidpointRounding.AwayFromZero));
    }

    /// <summary>Three sample points can match by chance on a busy picture, so the inside of every cell must match.</summary>
    private static bool IsSolid(PixelRegion r, MarkerHit hit, Pattern p)
    {
        var left = hit.X - r.OriginX;
        var top = hit.Y - r.OriginY;
        Hue[] hues = [p.A, p.B, p.C];
        for (var cell = 0; cell < hues.Length; cell++)
        {
            for (var y = 1; y < CellSize - 1; y++)
            {
                for (var x = 1; x < CellSize - 1; x++)
                {
                    if (HueAt(r, left + cell * CellSize + x, top + y) != hues[cell]) return false;
                }
            }
        }
        return true;
    }

    private static Hue HueAt(PixelRegion r, int x, int y)
    {
        if (x < 0 || y < 0 || x >= r.Width || y >= r.Height) return Hue.None;
        var i = (y * r.Width + x) * 4;
        int blue = r.Pixels[i], green = r.Pixels[i + 1], red = r.Pixels[i + 2];
        const int high = 200, low = 70;
        if (green < low && red > high && blue > high) return Hue.Magenta;
        if (green > high && red < low && blue < low) return Hue.Green;
        if (green > high && red < low && blue > high) return Hue.Cyan;
        return Hue.None;
    }
}
