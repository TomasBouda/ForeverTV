"""Build slice-safe painted TV textures, uncompressed BGRA TGA files and an offline contact sheet.

Run from any directory: python tools/TVArt/make-art.py (Python 3 + Pillow).
The saved image-generation master is never modified.
"""
import struct
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ART = HERE.parent.parent / "addon" / "ForeverScreen" / "art"
LANCZOS = Image.Resampling.LANCZOS


def write_tga(image, path):
    w, h = image.size
    assert w & (w - 1) == 0 and h & (h - 1) == 0
    header = struct.pack("<BBBHHBHHHHBB", 0, 0, 2, 0, 0, 0, 0, 0, w, h, 32, 8)
    pixels = image.tobytes("raw", "BGRA")
    path.write_bytes(header + b"".join(pixels[y*w*4:(y+1)*w*4] for y in range(h-1, -1, -1)))
    assert len(path.read_bytes()) == 18 + w * h * 4
    with Image.open(path) as decoded:
        assert decoded.convert("RGBA").tobytes() == image.tobytes(), "TGA round-trip or orientation mismatch"


def uniform(image, axis):
    """Replicate a painted profile, without stretching screws or painted grain."""
    w, h = image.size
    if axis == "x":
        return image.crop((w // 2, 0, w // 2 + 1, h)).resize((w, h))
    return image.crop((0, h // 2, w, h // 2 + 1)).resize((w, h))


def blend_end(image, profile, side, count=5):
    """Meet a uniform middle exactly at each cap boundary; preserve painted cap interiors."""
    result = image.copy()
    for i in range(count):
        weight = (count - i) / count
        if side in ("left", "right"):
            x = i if side == "left" else image.width - 1 - i
            strip = Image.blend(image.crop((x, 0, x+1, image.height)), profile, weight)
            result.paste(strip, (x, 0))
        else:
            y = i if side == "top" else image.height - 1 - i
            strip = Image.blend(image.crop((0, y, image.width, y+1)), profile, weight)
            result.paste(strip, (0, y))
    return result


def build():
    master = Image.open(HERE / "tv-painted.png").convert("RGBA")
    assert master.size == (1254, 1254), "Crop contract requires the saved 1254-square master"

    def crop(box, size):
        return master.crop(box).resize(size, LANCZOS)

    # The master is art, not an exact generated grid: these measured bounds exclude its empty aperture.
    rails = Image.new("RGBA", (256, 128))
    for y, height, source_top, source_bottom, sample_x in ((0, 28, 322, 425, 270), (32, 22, 1126, 1218, 270)):
        middle = uniform(crop((sample_x, source_top, sample_x+1, source_bottom), (174, height)), "x")
        profile = middle.crop((0, 0, 1, height))
        left = crop((24, source_top, 114, source_bottom), (22, height))
        right = crop((958, source_top, 1232, source_bottom), (60, height))
        rails.paste(blend_end(left, profile, "right"), (0, y))
        rails.paste(middle, (22, y))
        rails.paste(blend_end(right, profile, "left"), (196, y))
    left = uniform(crop((24, 800, 114, 801), (22, 64)), "y")
    rails.paste(left, (0, 64))

    controls = Image.new("RGBA", (64, 256))
    # The complete dials are separate fixed sprites, on an uninterrupted painted brass column.
    middle = uniform(crop((958, 832, 1232, 833), (60, 4)), "y")
    top = middle.resize((60, 124))
    bottom = crop((958, 923, 1232, 1126), (60, 64))
    row = middle.crop((0, 0, 60, 1))
    controls.paste(blend_end(top, row, "bottom"), (0, 0))
    controls.paste(middle, (0, 128))
    controls.paste(blend_end(bottom, row, "top"), (0, 160))

    switch_master = Image.open(HERE / "controls-painted.png").convert("RGBA")
    assert switch_master.size == (1254, 1254), "Control crop contract requires the saved square master"
    switches = Image.new("RGBA", (128, 128))
    # Equal square crops keep both round knob states aligned and preserve transparent edge clearance.
    for box, size, destination in (
        ((60, 60, 560, 560), (60, 60), (2, 2)),
        ((688, 60, 1188, 560), (60, 60), (66, 2)),
        ((168, 672, 452, 1162), (30, 52), (17, 70)),
        ((672, 700, 1196, 1120), (58, 46), (67, 73)),
    ):
        switches.paste(switch_master.crop(box).resize(size, LANCZOS), destination)

    antenna = crop((82, 18, 372, 316), (128, 128))
    badge = Image.new("RGBA", (256, 32))
    badge.paste(crop((362, 330, 765, 416), (146, 24)), (0, 0))
    textures = {"rails": rails, "controls": controls, "antenna": antenna, "badge": badge, "switches": switches}
    # Generation can leave saturated colour in antialiased cutout fringes. Keep those fringes brass;
    # only the speaker's interior is allowed a green jewel. Alpha and the painted master stay intact.
    for name, image in textures.items():
        for y in range(image.height):
            for x in range(image.width):
                if name == "controls" and 12 <= x < 50 and 164 <= y < 190:
                    continue
                r, g, b, a = image.getpixel((x, y))
                if r < 70 and g > 200:
                    g = min(190, r + 60)
                if r > 200 and g < 70 and b > 200:
                    b = 100
                image.putpixel((x, y), (r, g, b, a))
    ART.mkdir(parents=True, exist_ok=True)
    for name, image in textures.items():
        image.save(HERE / (name + ".png"))
        write_tga(image, ART / (name + ".tga"))

    # Stretch contracts are exact, including RGBA, rather than approximate visual similarities.
    for y, h in ((0, 28), (32, 22)):
        sample = rails.crop((22, y, 23, y+h)).tobytes()
        assert all(rails.crop((x, y, x+1, y+h)).tobytes() == sample for x in range(22, 196))
    assert all(left.crop((0, y, 22, y+1)).tobytes() == left.crop((0, 0, 22, 1)).tobytes() for y in range(64))
    assert all(controls.crop((0, y, 60, y+1)).tobytes() == row.tobytes() for y in range(128, 132))
    # A stronger check than the boundary rule: only the fixed speaker section may contain marker-like green.
    def marker_colour(pixel):
        r, g, b, a = pixel
        return a > 0 and ((r > 200 and g < 70 and b > 200) or (r < 70 and g > 200))
    assert not any(marker_colour(p) for p in rails.getdata())
    assert not any(marker_colour(p) for p in controls.crop((0, 0, 60, 132)).getdata())
    assert not any(marker_colour(p) for p in controls.crop((0, 160, 8, 224)).getdata())
    assert not any(marker_colour(p) for p in switches.getdata())
    preview(textures)
    print("Built 5 power-of-two RGBA PNG / uncompressed 32-bit TGA pairs; uniform stretch and edge palette checks passed.")


def preview(textures):
    sheet = Image.new("RGBA", (1500, 1120), (25, 29, 32, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=17)
    small = ImageFont.load_default(size=12)

    def label(x, y, text, size=font):
        draw.text((x, y), text, font=size, fill="#efd69d")

    def paste_piece(name, box, destination, size):
        sheet.alpha_composite(textures[name].crop(box).resize(size, LANCZOS), destination)

    def tv(x, y, w, h, paused=False):
        # x/y is the ORIGINAL frame's top-left, with all original geometry retained.
        draw.rectangle((x, y, x+w-1, y+h-1), fill=(20, 20, 25))
        draw.rectangle((x+6, y+32, x+w-7, y+h-29), fill=(0, 0, 0))
        paste_piece("rails", (22,0,196,28), (x,y), (w,26))
        paste_piece("rails", (22,32,196,54), (x,y+h-22), (w,22))
        colours = [(255, 0, 255), (0, 255, 0), (0, 255, 255)]
        for mx, my, order in ((x+6,y+26,(0,1,2)), (x+w-24,y+26,(1,2,0)),
                              (x+6,y+h-28,(2,0,1)), (x+w-24,y+h-28,(0,2,1))):
            for i, c in enumerate(order):
                draw.rectangle((mx+i*6,my,mx+i*6+5,my+5), fill=colours[c])
        draw.rectangle((x+24,y+26,x+29,y+31), fill=colours[2 if paused else 1])
        for sy, height, dy in ((0,28,y-28),(32,22,y+h)):
            paste_piece("rails", (0,sy,22,sy+height), (x-22,dy), (22,height))
            paste_piece("rails", (22,sy,196,sy+height), (x,dy), (w,height))
            paste_piece("rails", (196,sy,256,sy+height), (x+w,dy), (60,height))
        paste_piece("rails", (0,64,22,128), (x-22,y), (22,h))
        paste_piece("controls", (0,0,60,124), (x+w,y), (60,124))
        paste_piece("controls", (0,128,60,132), (x+w,y+124), (60,h-188))
        paste_piece("controls", (0,160,60,224), (x+w,y+h-64), (60,64))
        sx = 64 if paused else 0
        paste_piece("switches", (sx,0,sx+64,64), (x+w+6,y+8), (48,48))
        paste_piece("switches", (0,64,64,128), (x+w+6,y+68), (48,48))
        paste_piece("switches", (64,64,128,128), (x+w+22,y+h-10), (32,32))
        paste_piece("antenna", (0,0,128,128), (x-12,y-132), (104,104))
        bx = x + (w+38-146)//2
        paste_piece("badge", (0,0,146,24), (bx,y-26), (146,24))
        draw.text((bx+40,y-20), "BILGEBOX", font=small, fill="#482407", stroke_width=0)

    label(30, 20, "FOREVERSCREEN / BILGEBOX - painted brass + copper")
    label(30, 49, "Offline composition. TUNE toggles playback; power hides the frame; brass crank resizes.", small)
    tv(60, 215, 652, 420)
    label(40, 680, "Default: original frame 652 x 420; rails 28 / 22 / 22 / 60 px")
    tv(850, 215, 252, 195, paused=True)
    label(830, 465, "Minimum: 252 x 195 / paused pointer", small)
    label(830, 485, "Fixed 124 px controls + 64 px speaker", small)
    label(830, 505, "7 px uniform middle; no detail stretches", small)
    paste_piece("switches", (0,0,64,64), (850,560), (96,96))
    paste_piece("switches", (64,0,128,64), (1010,560), (96,96))
    label(850, 660, "Playing", small)
    label(1010, 660, "Paused", small)
    label(40, 730, "DELIVERY TEXTURES (native resolution, transparent unused pixels)")
    for name, x, y in (("rails",40,800),("controls",380,800),("antenna",520,800),("badge",730,800),("switches",1050,800)):
        label(x, y-28, name + ".tga", small)
        sheet.alpha_composite(textures[name], (x,y))
    label(40, 1080, "Fixed corners | pixel-identical stretch profiles | green jewel restricted to speaker interior", small)
    sheet.convert("RGB").save(HERE / "preview.png")


if __name__ == "__main__":
    build()
