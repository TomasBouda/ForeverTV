"""Builds the app icon (icon.png 256 x 256, icon.ico 16-256) and the browser extension icons (16-128).
Source: tools/Icon/icon-painted.png when it exists (Tomas's painted square icon), otherwise the painted BILGEBOX
master in tools/TVArt, whose transparent screen gets a dark green glass fill so the TV reads at 16 px.
Run: py tools/Icon/make-icon.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
PAINTED = ROOT / "tools" / "Icon" / "icon-painted.png"


def from_master():
    master = Image.open(ROOT / "tools" / "TVArt" / "tv-painted.png").convert("RGBA")

    glass = Image.new("RGBA", master.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(glass)
    draw.rectangle((100, 410, 975, 1135), fill=(10, 48, 30, 255))
    glare = Image.new("RGBA", master.size, (0, 0, 0, 0))
    ImageDraw.Draw(glare).ellipse((180, 470, 620, 760), fill=(120, 220, 150, 90))
    glass = Image.alpha_composite(glass, glare.filter(ImageFilter.GaussianBlur(40)))
    picture = Image.alpha_composite(glass, master)

    # Square canvas, the TV centred, a little margin.
    side = max(picture.size)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(picture, ((side - picture.width) // 2, (side - picture.height) // 2))
    return square


def from_painted():
    image = Image.open(PAINTED).convert("RGBA")
    side = min(image.size)
    left, top = (image.width - side) // 2, (image.height - side) // 2
    return image.crop((left, top, left + side, top + side))


square = from_painted() if PAINTED.exists() else from_master()
icon = square.resize((256, 256), Image.LANCZOS)
icon.save(ROOT / "icon.png")
icon.save(ROOT / "src" / "ForeverTV.App" / "icon.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
icons = ROOT / "extension" / "icons"
icons.mkdir(parents=True, exist_ok=True)
for size in (16, 32, 48, 128):
    square.resize((size, size), Image.LANCZOS).save(icons / f"icon-{size}.png")
print("wrote icon.png, src/ForeverTV.App/icon.ico and extension/icons from", PAINTED.name if PAINTED.exists() else "tv-painted.png")
