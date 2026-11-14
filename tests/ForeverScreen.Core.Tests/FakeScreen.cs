using ForeverScreen.Core;

namespace ForeverScreen.Core.Tests;

/// <summary>A pretend game window. Tests paint a marker frame on it and hand it to the tracker as its capture.</summary>
public sealed class FakeScreen(int width, int height) : IRegionCapture
{
    private static readonly (byte R, byte G, byte B) M = (255, 0, 255), G = (0, 255, 0), C = (0, 255, 255);

    public int Width => width;
    public int Height => height;
    public int FullCaptures { get; private set; }
    public int LocalCaptures { get; private set; }

    private byte[] _pixels = new byte[width * height * 4];

    public void Clear() => _pixels = new byte[width * height * 4];

    public void Noise(int seed)
    {
        new Random(seed).NextBytes(_pixels);
    }

    /// <summary>Paints the four markers around a video rectangle, the way the addon lays them out.</summary>
    public void PaintFrame(VideoRect rect)
    {
        var size = MarkerDetector.MarkerHeight;
        var right = rect.X + rect.Width - MarkerDetector.MarkerWidth;
        PaintMarker(rect.X, rect.Y - size, M, G, C);
        PaintMarker(right, rect.Y - size, G, C, M);
        PaintMarker(rect.X, rect.Y + rect.Height, C, M, G);
        PaintMarker(right, rect.Y + rect.Height, M, C, G);
    }

    /// <summary>Paints the addon's play state cell right of the top-left marker.</summary>
    public void PaintPlayState(VideoRect rect, bool playing) =>
        PaintBox(rect.X + MarkerDetector.MarkerWidth, rect.Y - MarkerDetector.MarkerHeight, MarkerDetector.CellSize, playing ? G : C);

    public void PaintMarker(int x, int y, (byte R, byte G, byte B) a, (byte R, byte G, byte B) b, (byte R, byte G, byte B) c)
    {
        var cell = MarkerDetector.CellSize;
        PaintBox(x, y, cell, a);
        PaintBox(x + cell, y, cell, b);
        PaintBox(x + 2 * cell, y, cell, c);
    }

    /// <summary>Hides one marker by painting over it, as a game window covering that corner would.</summary>
    public void Cover(int x, int y) => PaintBlock(x, y, MarkerDetector.MarkerWidth, MarkerDetector.MarkerHeight, (40, 40, 40));

    private void PaintBox(int x, int y, int size, (byte R, byte G, byte B) colour) => PaintBlock(x, y, size, size, colour);

    private void PaintBlock(int x, int y, int w, int h, (byte R, byte G, byte B) colour)
    {
        for (var py = y; py < y + h; py++)
        {
            for (var px = x; px < x + w; px++)
            {
                if (px < 0 || py < 0 || px >= width || py >= height) continue;
                var i = (py * width + px) * 4;
                _pixels[i] = colour.B;
                _pixels[i + 1] = colour.G;
                _pixels[i + 2] = colour.R;
                _pixels[i + 3] = 255;
            }
        }
    }

    public PixelRegion? Capture(int x, int y, int w, int h)
    {
        if (x == 0 && y == 0 && w == width && h == height) FullCaptures++;
        else LocalCaptures++;
        var left = Math.Max(0, x);
        var top = Math.Max(0, y);
        var regionWidth = Math.Min(x + w, width) - left;
        var regionHeight = Math.Min(y + h, height) - top;
        if (regionWidth <= 0 || regionHeight <= 0) return null;
        var pixels = new byte[regionWidth * regionHeight * 4];
        for (var row = 0; row < regionHeight; row++)
        {
            Array.Copy(_pixels, ((top + row) * width + left) * 4, pixels, row * regionWidth * 4, regionWidth * 4);
        }
        return new PixelRegion(pixels, regionWidth, regionHeight, left, top);
    }
}
