"""Render the IUUT app icon: an amber Icarus wing crossed by a blue wrench on the app's dark tile.

The wing is Icarus; the wrench is what IUUT does (repair, rescue, edit). Colours are the app theme's
own: AmberCore #F59E0B and IonCore #0A84E0 on the SpaceVoid tile.

Drawn at 1024 and downsampled per size. Sizes <= 32 use a simplified mark (fewer feathers, thicker
strokes) because detail that reads at 256 turns to mush at 16.

Dev-only: needs Python 3 + Pillow. Not a build or runtime dependency — the generated files are
committed. Regenerate with:

    python -I scripts/branding/make-icon.py <out-dir>

then copy IUUT.ico and iuut-icon-256.png (as IUUT-256.png) into src/IUUT.App/Assets/, and
iuut-icon-256.png to docs/images/iuut-icon.png. preview-sheet.png shows every size on light and dark.
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(sys.argv[1])
OUT.mkdir(parents=True, exist_ok=True)

S = 1024
AMBER = (245, 158, 11, 255)
AMBER_HI = (252, 196, 84, 255)
ION = (10, 132, 224, 255)
ION_HI = (88, 176, 245, 255)
TILE_TOP = (42, 40, 38, 255)
TILE_BOTTOM = (16, 15, 14, 255)
EDGE = (70, 66, 60, 255)


def tile(img, inset, radius):
    """Rounded dark tile with a vertical gradient and a hairline edge."""
    grad = Image.new("RGBA", (S, S))
    gd = ImageDraw.Draw(grad)
    for y in range(S):
        t = y / (S - 1)
        c = tuple(int(TILE_TOP[i] + (TILE_BOTTOM[i] - TILE_TOP[i]) * t) for i in range(3)) + (255,)
        gd.line([(0, y), (S, y)], fill=c)
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([inset, inset, S - inset, S - inset], radius, fill=255)
    img.paste(grad, (0, 0), mask)
    ImageDraw.Draw(img).rounded_rectangle([inset, inset, S - inset, S - inset], radius, outline=EDGE, width=10)


def capsule(draw, p0, p1, width, fill):
    draw.line([p0, p1], fill=fill, width=width)
    r = width / 2
    for (x, y) in (p0, p1):
        draw.ellipse([x - r, y - r, x + r, y + r], fill=fill)


def wing(draw, feathers, width):
    """A heraldic wing: a curved spine on the right, feathers sweeping back up-left off it,
    longest at the top and stepping shorter toward the bottom."""
    # A solid wing silhouette in the upper-left: feathers overlap (spacing < width) so they merge
    # into one shape, leaving a scalloped trailing edge — the part that still reads at 16px.
    top, bottom = (650, 300), (615, 610)
    top_angle, bottom_angle = math.radians(203), math.radians(178)
    longest, shortest = 430, 220
    roots = []
    for i in range(feathers):
        t = i / (feathers - 1)
        roots.append((top[0] + (bottom[0] - top[0]) * t, top[1] + (bottom[1] - top[1]) * t))
    # draw bottom feather first so each higher one overlaps the one below, like real plumage
    for i in reversed(range(feathers)):
        t = i / (feathers - 1)
        angle = top_angle + (bottom_angle - top_angle) * t
        length = longest + (shortest - longest) * t
        root = roots[i]
        tip = (root[0] + math.cos(angle) * length, root[1] + math.sin(angle) * length)
        capsule(draw, root, tip, width, AMBER_HI if i == 0 else AMBER)
        # a thin dark separation line along each feather's lower edge so the scallops read
        if i < feathers - 1:
            off = width * 0.42
            draw.line([(root[0] - 20, root[1] + off), (tip[0] + 25, tip[1] + off)], fill=(150, 90, 6, 255),
                      width=max(2, width // 9))
    # the shoulder: a solid rounded mass binding the feather roots together
    xs = [r[0] for r in roots]
    ys = [r[1] for r in roots]
    draw.rounded_rectangle([min(xs) - width * 0.5, min(ys) - width * 0.5, max(xs) + width * 0.5, max(ys) + width * 0.5],
                           radius=width * 0.5, fill=AMBER)


def wrench(img, scale_width):
    """A wrench on the opposite diagonal: round open jaw upper-left... drawn on its own layer."""
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    a = (755, 625)   # jaw end, right, just below the wing's shoulder
    b = (285, 830)   # handle end, lower left
    capsule(d, a, b, scale_width, ION)
    ux, uy = (a[0] - b[0]), (a[1] - b[1])
    n = math.hypot(ux, uy)
    ux, uy = ux / n, uy / n                      # direction from handle toward the jaw
    px, py = -uy, ux                             # perpendicular
    r_out = scale_width * 1.3
    d.ellipse([a[0] - r_out, a[1] - r_out, a[0] + r_out, a[1] + r_out], fill=ION)
    r_in = scale_width * 0.55
    d.ellipse([a[0] - r_in, a[1] - r_in, a[0] + r_in, a[1] + r_in], fill=(0, 0, 0, 0))
    # open jaw: a notch cut outward along the wrench's own axis
    w = r_in * 0.95
    notch = [
        (a[0] + px * w, a[1] + py * w),
        (a[0] + px * w + ux * r_out * 1.4, a[1] + py * w + uy * r_out * 1.4),
        (a[0] - px * w + ux * r_out * 1.4, a[1] - py * w + uy * r_out * 1.4),
        (a[0] - px * w, a[1] - py * w),
    ]
    d.polygon(notch, fill=(0, 0, 0, 0))
    # highlight along the handle
    off = scale_width * 0.18
    capsule(d, (b[0] + ux * 40 + px * off, b[1] + uy * 40 + py * off),
            (a[0] - ux * r_out * 1.4 + px * off, a[1] - uy * r_out * 1.4 + py * off),
            max(4, scale_width // 6), ION_HI)
    # dark outline so the wrench separates from the wing where they cross
    alpha = layer.split()[3]
    halo = alpha.filter(ImageFilter.MaxFilter(31))
    outline = Image.new("RGBA", (S, S), TILE_BOTTOM)
    outline.putalpha(halo)
    img.alpha_composite(outline)
    img.alpha_composite(layer)


def render(simple):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    tile(img, inset=40 if not simple else 0, radius=210 if not simple else 180)
    d = ImageDraw.Draw(img)
    if simple:
        wing(d, feathers=4, width=140)
        wrench(img, scale_width=112)
    else:
        wing(d, feathers=6, width=104)
        wrench(img, scale_width=92)
    return img


full = render(simple=False)
small = render(simple=True)
full.save(OUT / "iuut-icon-1024.png")

sizes = [256, 128, 64, 48, 32, 24, 16]
frames = []
for size in sizes:
    src = small if size <= 32 else full
    frames.append(src.resize((size, size), Image.LANCZOS))
    frames[-1].save(OUT / f"iuut-icon-{size}.png")

frames[0].save(OUT / "IUUT.ico", format="ICO", sizes=[(s, s) for s in sizes], append_images=frames[1:])

# Preview sheet at real pixel sizes, on light and dark backgrounds, for eyeballing.
sheet = Image.new("RGBA", (900, 330), (245, 245, 245, 255))
ImageDraw.Draw(sheet).rectangle([0, 165, 900, 330], fill=(32, 32, 32, 255))
x = 10
for size, frame in zip(sizes, frames):
    sheet.alpha_composite(frame, (x, (165 - size) // 2))
    sheet.alpha_composite(frame, (x, 165 + (165 - size) // 2))
    x += size + 18
sheet.save(OUT / "preview-sheet.png")
print("ok", OUT)
