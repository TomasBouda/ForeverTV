using System.Runtime.InteropServices;
using ForeverScreen.Core;
using TomLabs.PixelBridge.Host;

namespace ForeverScreen.App;

/// <summary>Copies a part of the game's client area from the screen with GDI. The cursor is not included.</summary>
internal sealed class GdiRegionCapture : IRegionCapture
{
    private const uint SRCCOPY = 0x00CC0020;

    public GameWindow? Window { get; set; }

    public PixelRegion? Capture(int x, int y, int width, int height)
    {
        if (Window is not { IsMinimized: false } window) return null;
        var left = Math.Max(0, x);
        var top = Math.Max(0, y);
        var regionWidth = Math.Min(x + width, window.ClientWidth) - left;
        var regionHeight = Math.Min(y + height, window.ClientHeight) - top;
        if (regionWidth <= 0 || regionHeight <= 0) return null;

        var screen = GetDC(0);
        var memory = CreateCompatibleDC(screen);
        var header = new BitmapInfoHeader { Size = 40, Width = regionWidth, Height = -regionHeight, Planes = 1, BitCount = 32 };
        var bitmap = CreateDIBSection(memory, ref header, 0, out var bits, 0, 0);
        try
        {
            if (bitmap == 0) return null;
            var old = SelectObject(memory, bitmap);
            var copied = BitBlt(memory, 0, 0, regionWidth, regionHeight, screen, window.ClientX + left, window.ClientY + top, SRCCOPY);
            SelectObject(memory, old);
            if (!copied) return null;
            var pixels = new byte[regionWidth * regionHeight * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            return new PixelRegion(pixels, regionWidth, regionHeight, left, top);
        }
        finally
        {
            if (bitmap != 0) DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(0, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size, Width, Height;
        public short Planes, BitCount;
        public int Compression, SizeImage, XPelsPerMeter, YPelsPerMeter, ClrUsed, ClrImportant;
    }

    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfoHeader header, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint gdiObject);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint gdiObject);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint target, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
}
