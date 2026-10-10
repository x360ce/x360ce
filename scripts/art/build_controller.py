"""Writes the controller pictures and button glyphs of x360ce v3 and v4, at 1, 1.5 and 2 times the
size the programs draw them at.

Pictures and glyphs are cut from the masters, controller/xboxController_6x.png and
controller/xboxControllerDark_6x.png beside this script, which hold both views of the controller at six
times the size: the top view in rows 0-630, an 8-pixel gap (48 at 6x) and the front view in rows
678-1734. Every piece is cut out and reduced on premultiplied colour, so its edges gain no fringe, and
the background is made fully clear where it is almost clear. Every size is cut from the same master, so
each part stays where the programs' marks expect it.

- Pictures: Resources/Images/shared/xbox/xboxController{Top,Front}{,Dark}.png at 256 x 105 and
  256 x 176, and their versions named by their size, such as xboxControllerTop_384x158.png. The
  programs load a picture with its versions (EngineHelper.GetResourcePicture) and draw the one nearest
  above the size they need.
- Button glyphs: Resources/Images/shared/xbox/Button_*.png, each button cut out in its own shape (the
  D-pad glyphs are its four sides), with its versions named by their size, and the dark theme's in
  xbox/dark, its greys lifted as the dark icons' are, so a greyed-out glyph still shows on the dark
  background. Back and Start get the arrows the real buttons carry, which the masters lack.
  build_icons.py lists the versions in Properties/Icons.resx: run it after this script.

    python scripts/art/build_controller.py

Needs Python 3 with Pillow and NumPy.
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from build_icons import pastel

SCALE = 6
# View -> (first row and height at 1x in the master, width at 1x).
VIEWS = {"Top": (0, 105, 256), "Front": (113, 176, 256)}
MULTIPLES = (1, 1.5, 2)

# Button glyph -> (centre x, centre y in master pixels, width and height at 1x, master pixels to a glyph
# pixel, shape, arrow). Shape: ("circle", r) round on the centre; ("ellipse", rx, ry) on the centre;
# ("disc", x, y, r) a circle centred elsewhere, for a side of the D-pad; ("bar", x0, y0, x1, y1, r, side)
# a bumper, rounded on its outer side.
GLYPHS = {
    "Button_A": (1180, 1072, 20, 20, 5.4, ("circle", 53), None),
    "Button_B": (1290, 960, 20, 20, 5.4, ("circle", 53), None),
    "Button_X": (1070, 960, 20, 20, 5.4, ("circle", 53), None),
    "Button_Y": (1180, 850, 20, 20, 5.4, ("circle", 53), None),
    "Button_Back": (617, 971, 28, 24, 2.9, ("ellipse", 38, 35), "left"),
    "Button_Start": (917, 971, 28, 24, 2.9, ("ellipse", 38, 35), "right"),
    "Button_LeftThumb": (355, 961, 27, 28, 7.85, ("circle", 104), None),
    "Button_RightThumb": (960, 1205, 27, 28, 7.85, ("circle", 104), None),
    "Button_DPadUp": (550, 1153, 28, 28, 5.7, ("disc", 550, 1205, 129), None),
    "Button_DPadDown": (550, 1257, 28, 28, 5.7, ("disc", 550, 1205, 129), None),
    "Button_DPadLeft": (498, 1205, 28, 28, 5.7, ("disc", 550, 1205, 129), None),
    "Button_DPadRight": (602, 1205, 28, 28, 5.7, ("disc", 550, 1205, 129), None),
    "Button_LeftShoulder": (345, 396, 28, 12, 9.6, ("bar", 212, 349, 478, 443, 46, "left"), None),
    "Button_RightShoulder": (1191, 396, 28, 12, 9.6, ("bar", 1058, 349, 1324, 443, 46, "right"), None),
}

# Shapes are drawn at twice the size they are applied at and reduced, so their edges are smooth.
SUPER = 2


def reduce(image, size):
    """An image at a smaller size, reduced on premultiplied colour so its edges gain no fringe."""
    return image.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


def save(image, path, xbox):
    pixels = np.asarray(image).copy()
    pixels[pixels[..., 3] < 3] = 0
    path.parent.mkdir(parents=True, exist_ok=True)
    # No resolution is written: the programs read that as 96 dpi, one pixel to a pixel.
    Image.fromarray(pixels, "RGBA").save(path, optimize=True)
    print(f"{path.relative_to(xbox).as_posix()}: {image.width} x {image.height}")


def sized(name, size, multiple):
    return name if multiple == 1 else f"{name}_{size[0]}x{size[1]}"


# --- Pictures -------------------------------------------------------------------------------------

def write_pictures(master, theme, xbox):
    for view, (top, height, width) in VIEWS.items():
        box = (0, top * SCALE, width * SCALE, (top + height) * SCALE)
        for multiple in MULTIPLES:
            size = (round(width * multiple), round(height * multiple))
            name = sized(f"xboxController{view}{theme}", size, multiple)
            save(reduce(master.crop(box), size), xbox / f"{name}.png", xbox)


# --- Button glyphs --------------------------------------------------------------------------------

def glyph_mask(box, shape):
    """The opacity of a glyph's box: the shape of the button it shows."""
    x0, y0, x1, y1 = box
    s = SUPER
    image = Image.new("L", ((x1 - x0) * s, (y1 - y0) * s), 0)
    draw = ImageDraw.Draw(image)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    if shape[0] == "circle":
        rx = ry = shape[1]
    elif shape[0] == "ellipse":
        rx, ry = shape[1], shape[2]
    if shape[0] in ("circle", "ellipse"):
        draw.ellipse([(cx - rx - x0) * s, (cy - ry - y0) * s, (cx + rx - x0) * s, (cy + ry - y0) * s], fill=255)
    elif shape[0] == "disc":
        _, dx, dy, r = shape
        draw.ellipse([(dx - r - x0) * s, (dy - r - y0) * s, (dx + r - x0) * s, (dy + r - y0) * s], fill=255)
    elif shape[0] == "bar":
        _, bx0, by0, bx1, by1, r, outer = shape
        corners = (True, False, False, True) if outer == "left" else (False, True, True, False)
        draw.rounded_rectangle([(bx0 - x0) * s, (by0 - y0) * s, (bx1 - x0) * s, (by1 - y0) * s],
                               radius=r * s, fill=255, corners=corners)
    return image.resize((x1 - x0, y1 - y0), Image.LANCZOS)


def draw_arrow(piece, direction, dark):
    """Back's or Start's arrow, pointing the way the real button's does."""
    w, h = piece.size
    s = SUPER * 2
    layer = Image.new("RGBA", (w * s, h * s), (0, 0, 0, 0))
    cx, cy = w * s / 2, h * s / 2
    size = h * s * 0.36
    tip = -1 if direction == "left" else 1
    points = [(cx + tip * size * 0.62, cy), (cx - tip * size * 0.5, cy - size * 0.66),
              (cx - tip * size * 0.5, cy + size * 0.66)]
    fill = (128, 128, 128, 235) if dark else (150, 150, 150, 235)
    edge = (170, 170, 170, 255) if dark else (110, 110, 110, 255)
    ImageDraw.Draw(layer).polygon(points, fill=fill, outline=edge, width=round(s * 1.2))
    piece.alpha_composite(layer.resize((w, h), Image.LANCZOS))
    return piece


def write_glyphs(master, dark, xbox):
    for name, (cx, cy, w, h, scale, shape, arrow) in GLYPHS.items():
        bw, bh = round(w * scale), round(h * scale)
        left, top = round(cx - bw / 2), round(cy - bh / 2)
        box = (left, top, left + bw, top + bh)
        piece = master.crop(box)
        alpha = np.asarray(piece.getchannel("A")).astype(np.float32) * np.asarray(glyph_mask(box, shape)) / 255
        piece.putalpha(Image.fromarray(alpha.round().astype(np.uint8)))
        if arrow:
            piece = draw_arrow(piece, arrow, dark)
        if dark:
            piece = pastel(piece, k_l=0, k_c=1, lift_greys=True)
        for multiple in MULTIPLES:
            size = (round(w * multiple), round(h * multiple))
            folder = xbox / "dark" if dark else xbox
            save(reduce(piece, size), folder / f"{sized(name, size, multiple)}.png", xbox)


def main():
    here = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--repo", default=str(here.parents[1]), help="x360ce repository root")
    args = parser.parse_args()
    xbox = Path(args.repo) / "Resources" / "Images" / "shared" / "xbox"
    for theme in ("", "Dark"):
        master = Image.open(here / "controller" / f"xboxController{theme}_6x.png").convert("RGBA")
        write_pictures(master, theme, xbox)
        write_glyphs(master, theme == "Dark", xbox)


if __name__ == "__main__":
    main()
