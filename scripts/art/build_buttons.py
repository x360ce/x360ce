"""Writes the pictures of x360ce v4's Buttons page from the path data of x360ce 5's AxisToButtonControl.xaml.

buttons/AxisToButtonControl.xaml beside this script is a copy of x360ce 5's App/Controls/AxisToButtonControl.xaml.
Each Xbox button there is a ControlTemplate of Paths drawn in a 142 x 142 square and shown in a 24 x 24 box,
and Axis_Arrow is the rounded triangle shown between the mapped axis and the button, drawn the same way. This
script reads the Paths in the path mini-language (M, L, H, V, C, S, Q, T, A and Z, absolute and relative, and
the fill rule: F1 fills non-zero, F0 or none even-odd, as WPF does), fills each with its colour, a later Path over
an earlier one, and writes the square at 24, 36 and 48 pixels: the size the page draws it at, and 1.5 and 2 times
that.

- Resources/Images/v4/xbox/Button_*.png and Axis_Arrow.png at 24 pixels, and their versions named by their size,
  such as Button_A_36x36.png. A picture with a colour of the theme also comes for the dark theme, in xbox/dark
  with the same file names.
- App.v4/Properties/Icons.resx, through build_icons.write_icons_resx: the versions and the dark pictures of every
  picture Properties/Resources.resx names. Resources.resx names the light picture at 24 pixels, as the designer
  and the program know it.

Colours are the XAML's. The arrow's, BackgroundDark, is a colour of the theme rather than of the file: #DEDEDE in
the light theme (x360ce 5's Default.xaml) and #454545 in the dark one (Default_DarkTheme.xaml), so the arrow comes
in both. The buttons' colours are the file's own, the same in both themes: a button is a dark body with its
symbol in it, and the symbol is the body showing through, so lifting the greys for the dark theme would wash the
symbol out, which is why build_icons.py keeps the dark greys of such icons (KEEP_DARK). The program hands out the
light picture where there is no dark one.

Each pixel is split into 16 x 16 points, a point takes the colour of the last Path that covers it, and the pixel
the average of its points, counted in whole points: running the script again writes the same bytes.

    python scripts/art/build_buttons.py

Needs Python 3 with Pillow and NumPy.
"""
import argparse
import re
from pathlib import Path

import numpy as np
from PIL import Image

from build_icons import write_icons_resx

# Picture -> the XAML's ControlTemplate it is drawn from.
PICTURES = {
    "Button_A": "ControlTemplate_ActionA",
    "Button_B": "ControlTemplate_ActionB",
    "Button_X": "ControlTemplate_ActionX",
    "Button_Y": "ControlTemplate_ActionY",
    "Button_LeftShoulder": "ControlTemplate_BumperLeft",
    "Button_RightShoulder": "ControlTemplate_BumperRight",
    "Button_Back": "ControlTemplate_MenuBack",
    "Button_Start": "ControlTemplate_MenuStart",
    "Button_DPadUp": "ControlTemplate_DPadUp",
    "Button_DPadDown": "ControlTemplate_DPadDown",
    "Button_DPadLeft": "ControlTemplate_DPadLeft",
    "Button_DPadRight": "ControlTemplate_DPadRight",
    "Button_LeftThumb": "ControlTemplate_StickLeft",
    "Button_RightThumb": "ControlTemplate_StickRight",
    "Axis_Arrow": "Axis_Arrow",
}

# Colours of the theme the XAML takes from outside the file: (light theme, dark theme).
THEME = {"BackgroundDark": ("#DEDEDE", "#454545")}

# The square the paths are drawn in, and the size the page draws a picture at.
BOX = 142
SIZE = 24
MULTIPLES = (1, 1.5, 2)
# Points across a pixel, and steps a curve is drawn in.
POINTS = 16
STEPS = 32


# --- Path mini-language ---------------------------------------------------------------------------

TOKEN = re.compile(r"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
ARGUMENTS = {"M": 2, "L": 2, "H": 1, "V": 1, "C": 6, "S": 4, "Q": 4, "T": 2, "A": 7, "Z": 0}


def parse_path(data):
    """The figures of a path's data, each a list of points with the curves drawn in steps, and whether the path
    fills even-odd."""
    tokens = TOKEN.findall(data)
    even_odd = True
    if tokens and tokens[0] in ("F", "f"):
        even_odd = float(tokens[1]) == 0
        tokens = tokens[2:]
    figures, figure = [], None
    x = y = 0.0
    start = (0.0, 0.0)
    # The last control point of a cubic or a quadratic curve, for S and T to mirror.
    cubic = quadratic = None
    command, i = None, 0
    while i < len(tokens):
        if tokens[i].isalpha():
            command = tokens[i]
            i += 1
        elif command is None or command in "Zz":
            raise ValueError(f"Number without a command at {i}: {data[:40]}")
        upper = command.upper()
        count = ARGUMENTS[upper]
        values = [float(t) for t in tokens[i:i + count]]
        i += count
        relative = command.islower()
        # The point the arguments are relative to.
        ox, oy = (x, y) if relative else (0.0, 0.0)
        if upper not in "MZ" and figure is None:
            figure = [(x, y)]
            figures.append(figure)
        last_cubic, last_quadratic = cubic, quadratic
        cubic = quadratic = None
        if upper == "M":
            x, y = ox + values[0], oy + values[1]
            figure = [(x, y)]
            figures.append(figure)
            start = (x, y)
            # Pairs after a move are lines.
            command = "l" if relative else "L"
        elif upper == "L":
            x, y = ox + values[0], oy + values[1]
            figure.append((x, y))
        elif upper == "H":
            x = ox + values[0]
            figure.append((x, y))
        elif upper == "V":
            y = oy + values[0]
            figure.append((x, y))
        elif upper in "CS":
            if upper == "C":
                c1 = (ox + values[0], oy + values[1])
                values = values[2:]
            else:
                c1 = (2 * x - last_cubic[0], 2 * y - last_cubic[1]) if last_cubic else (x, y)
            c2 = (ox + values[0], oy + values[1])
            end = (ox + values[2], oy + values[3])
            figure.extend(cubic_points((x, y), c1, c2, end))
            cubic = c2
            x, y = end
        elif upper in "QT":
            if upper == "Q":
                c = (ox + values[0], oy + values[1])
                values = values[2:]
            else:
                c = (2 * x - last_quadratic[0], 2 * y - last_quadratic[1]) if last_quadratic else (x, y)
            end = (ox + values[0], oy + values[1])
            c1 = (x + 2 / 3 * (c[0] - x), y + 2 / 3 * (c[1] - y))
            c2 = (end[0] + 2 / 3 * (c[0] - end[0]), end[1] + 2 / 3 * (c[1] - end[1]))
            figure.extend(cubic_points((x, y), c1, c2, end))
            quadratic = c
            x, y = end
        elif upper == "A":
            end = (ox + values[5], oy + values[6])
            figure.extend(arc_points((x, y), values[0], values[1], values[2], values[3] != 0, values[4] != 0, end))
            x, y = end
        elif upper == "Z":
            # A figure is filled closed either way; the next one starts where this one did.
            x, y = start
            figure = None
    return [f for f in figures if len(f) > 2], even_odd


def cubic_points(p0, p1, p2, p3):
    """The points a cubic Bezier curve passes, after its start, in STEPS steps."""
    points = []
    for step in range(1, STEPS + 1):
        t = step / STEPS
        u = 1 - t
        a, b, c, d = u * u * u, 3 * u * u * t, 3 * u * t * t, t * t * t
        points.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return points


def arc_points(p0, rx, ry, rotation, large, clockwise, p1):
    """The points an elliptical arc passes, after its start, in STEPS steps: the SVG arc, whose sweep flag is
    WPF's sweep direction."""
    if p0 == p1:
        return []
    rx, ry = abs(rx), abs(ry)
    if rx == 0 or ry == 0:
        return [p1]
    phi = np.radians(rotation)
    cos, sin = float(np.cos(phi)), float(np.sin(phi))
    dx, dy = (p0[0] - p1[0]) / 2, (p0[1] - p1[1]) / 2
    x1, y1 = cos * dx + sin * dy, -sin * dx + cos * dy
    # A radius too small to reach the end is enlarged until it does.
    grow = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry)
    if grow > 1:
        rx, ry = rx * grow ** 0.5, ry * grow ** 0.5
    num = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1
    den = rx * rx * y1 * y1 + ry * ry * x1 * x1
    k = max(0.0, num / den) ** 0.5 * (-1 if large == clockwise else 1)
    cx1, cy1 = k * rx * y1 / ry, -k * ry * x1 / rx
    cx = cos * cx1 - sin * cy1 + (p0[0] + p1[0]) / 2
    cy = sin * cx1 + cos * cy1 + (p0[1] + p1[1]) / 2
    a0 = float(np.arctan2((y1 - cy1) / ry, (x1 - cx1) / rx))
    a1 = float(np.arctan2((-y1 - cy1) / ry, (-x1 - cx1) / rx))
    sweep = a1 - a0
    if clockwise and sweep < 0:
        sweep += 2 * np.pi
    elif not clockwise and sweep > 0:
        sweep -= 2 * np.pi
    angles = a0 + sweep * np.arange(1, STEPS + 1) / STEPS
    ex, ey = rx * np.cos(angles), ry * np.sin(angles)
    points = list(zip(cos * ex - sin * ey + cx, sin * ex + cos * ey + cy))
    points[-1] = p1
    return points


# --- Filling --------------------------------------------------------------------------------------

def covered(figures, even_odd, size):
    """Which points of a size x size picture, POINTS x POINTS to a pixel, the figures fill."""
    n = size * POINTS
    scale = n / BOX
    ends = [(np.array(f) * scale, np.roll(np.array(f) * scale, -1, axis=0)) for f in figures]
    x0, y0 = np.concatenate([a[:, 0] for a, _ in ends]), np.concatenate([a[:, 1] for a, _ in ends])
    x1, y1 = np.concatenate([b[:, 0] for _, b in ends]), np.concatenate([b[:, 1] for _, b in ends])
    keep = y0 != y1
    x0, y0, x1, y1 = x0[keep], y0[keep], x1[keep], y1[keep]
    # An edge crosses the rows of points whose middle, at row + 0.5, lies from its top to just above its bottom.
    first = np.clip(np.ceil(np.minimum(y0, y1) - 0.5), 0, n).astype(np.int64)
    last = np.clip(np.ceil(np.maximum(y0, y1) - 0.5), 0, n).astype(np.int64)
    counts = last - first
    edge = np.repeat(np.arange(len(counts)), counts)
    rows = first[edge] + np.arange(counts.sum()) - np.repeat(np.cumsum(counts) - counts, counts)
    t = (rows + 0.5 - y0[edge]) / (y1[edge] - y0[edge])
    x = x0[edge] + t * (x1[edge] - x0[edge])
    # The crossing counts for every point whose middle lies right of it.
    columns = np.clip(np.floor(x - 0.5).astype(np.int64) + 1, 0, n)
    winding = np.zeros((n, n + 1), np.int64)
    np.add.at(winding, (rows, columns), np.where(y1 > y0, 1, -1)[edge])
    winding = np.cumsum(winding, axis=1)[:, :n]
    return (winding % 2 == 1) if even_odd else (winding != 0)


def draw(layers, size):
    """A picture of the layers, (figures, even_odd, colour) each, at size x size pixels."""
    n = size * POINTS
    colour = np.zeros((n, n, 3), np.int64)
    filled = np.zeros((n, n), bool)
    for figures, even_odd, rgb in layers:
        mask = covered(figures, even_odd, size)
        colour[mask] = rgb
        filled |= mask
    count = filled.reshape(size, POINTS, size, POINTS).sum(axis=(1, 3))
    total = colour.reshape(size, POINTS, size, POINTS, 3).sum(axis=(1, 3))
    rgb = (total + count[..., None] // 2) // np.maximum(count, 1)[..., None]
    alpha = (count * 255 + POINTS * POINTS // 2) // (POINTS * POINTS)
    pixels = np.dstack([rgb, alpha]).astype(np.uint8)
    return Image.fromarray(pixels, "RGBA")


# --- The XAML -------------------------------------------------------------------------------------

def rgb_of(text):
    """The colour of "#RRGGBB" or "#AARRGGBB"; every colour the pictures use is opaque."""
    value = text.lstrip("#")
    if len(value) == 8:
        if value[:2].upper() != "FF":
            raise ValueError(f"Not opaque: {text}")
        value = value[2:]
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def attributes(tag):
    """The attributes of an element's start tag, by name."""
    return dict(re.findall(r'([\w:]+)="([^"]*)"', tag))


def read_templates(xaml):
    """Each ControlTemplate's Paths, as (data, brush name), and the colour of each brush the file names."""
    text = Path(xaml).read_text(encoding="utf-8-sig")
    brushes = {}
    for tag in re.findall(r"<SolidColorBrush\b[^>]*>", text):
        brush = attributes(tag)
        brushes[brush["x:Key"]] = brush["Color"]
    templates = {}
    for tag, body in re.findall(r"(<ControlTemplate\b[^>]*>)(.*?)</ControlTemplate>", text, re.S):
        paths = []
        for path in map(attributes, re.findall(r"<Path\b[^>]*>", body)):
            brush = re.fullmatch(r"\{StaticResource (\w+)\}", path["Fill"]).group(1)
            paths.append((path["Data"], brush))
        templates[attributes(tag)["x:Key"]] = paths
    return templates, brushes


def colour_of(brush, brushes, dark):
    """A brush's colour in a theme: the theme's own for the ones the file takes from outside."""
    if brush in THEME:
        return rgb_of(THEME[brush][1 if dark else 0])
    return rgb_of(brushes[brush])


def save(image, path, root):
    path.parent.mkdir(parents=True, exist_ok=True)
    # No resolution is written: the programs read that as 96 dpi, one pixel to a pixel.
    image.save(path, optimize=True)
    print(f"{path.relative_to(root).as_posix()}: {image.width} x {image.height}")


def main():
    here = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--repo", default=str(here.parents[1]), help="x360ce repository root")
    parser.add_argument("--xaml", default=str(here / "buttons" / "AxisToButtonControl.xaml"),
                        help="x360ce 5's AxisToButtonControl.xaml")
    args = parser.parse_args()
    repo = Path(args.repo)
    xbox = repo / "Resources" / "Images" / "v4" / "xbox"
    templates, brushes = read_templates(args.xaml)
    for name, key in PICTURES.items():
        paths = [parse_path(data) + (brush,) for data, brush in templates[key]]
        themed = any(brush in THEME for _, _, brush in paths)
        for dark in (False, True) if themed else (False,):
            layers = [(figures, even_odd, colour_of(brush, brushes, dark)) for figures, even_odd, brush in paths]
            for multiple in MULTIPLES:
                size = round(SIZE * multiple)
                file = name if multiple == 1 else f"{name}_{size}x{size}"
                save(draw(layers, size), (xbox / "dark" if dark else xbox) / f"{file}.png", xbox)
    print(f"App.v4/Properties/Icons.resx: {write_icons_resx(repo, 'v4')} entries")


if __name__ == "__main__":
    main()
