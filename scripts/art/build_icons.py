"""Builds the interface icons of x360ce v3 and v4 from the IconExperience G2 collection.

Every icon is G2's own PNG in the standard style at each size it is shown at, recoloured pastel, so
the shapes are G2's hand-tuned pixels and every icon softens by the same measure. Two sets are made:

- Light theme: soft pastel. Only colours move; G2's greys and whites keep the contrast they were
  drawn with.
- Dark theme: pastel, with G2's dark greys lifted so they show on a dark background, except in the
  icons where a dark grey is a symbol on a light shape (the warning mark, the help badge and the
  oscilloscope screen). Empty check boxes are dark inside, as Windows draws them in that theme.

Each icon is written at the size the program asks for (16 pixels; the help bulb 24) and at 1.5 and 2
times that: light under Resources/Images/{shared,v3,v4}/icons, dark under icons/dark, with the same
file names. The light icon at the asked size is the one Properties/Resources.resx and the designer
know. Every other size and every dark icon is listed in Properties/Icons.resx of each program, which
this script writes, and the program picks from that list by screen scale and theme.

A colour moves in OKLCH: lightness rises towards white by K_L of the distance, chroma is multiplied
by K_C, and the hue stays. Four icons G2 does not have are put together from G2 parts: a folder with
a magnifier, a floppy disk with a plus, a check box with a filled square, and a red record dot drawn
the way G2 draws a ball. The greyed "off" icons are made from the icon they switch off.

The G2 collection is licensed and not part of this repository. Point the script at its PNG folder of
the standard style, the one holding the 16x16, 24x24, ... folders:

    python scripts/art/build_icons.py --g2 "<IconExperience>/iconex_g2/g_collection/g_collection_png/standard"

Needs Python 3 with Pillow and NumPy.
"""
import argparse
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# Program icon -> (folder under Resources/Images, G2 icon). A G2 name starting with "@" is put
# together below; "@off:name" is the greyed G2 icon.
ICONS = {
    "add_16x16": ("shared", "plus"),
    "arrow_right_16x16": ("shared", "nav_right"),
    "arrow_right_gray_16x16": ("shared", "arrow_right"),
    "bullet_ball_glass_red_16x16": ("shared", "@record"),
    "check_16x16": ("shared", "check"),
    "check_disabled_16x16": ("shared", "@off:check"),
    "checkbox_16x16": ("shared", "checkbox"),
    "checkbox_unchecked_16x16": ("shared", "checkbox_unchecked"),
    "checkbox_undefined_16x16": ("shared", "@undefined"),
    "data_into_16x16": ("shared", "import"),
    "data_out_16x16": ("shared", "export"),
    "delete_16x16": ("shared", "delete"),
    "exit_16x16": ("shared", "door_exit"),
    "fix_16x16": ("shared", "sign_warning"),
    "folder_16x16": ("shared", "folder"),
    "folder_view_16x16": ("shared", "@folder_view"),
    "launch_16x16": ("shared", "media_play"),
    "map_to_16x16": ("shared", "arrow_into"),
    "online_help_16x16": ("shared", "help_earth"),
    "refresh_16x16": ("shared", "refresh"),
    "remove_16x16": ("shared", "minus"),
    "reset_16x16": ("shared", "undo"),
    "save_16x16": ("shared", "floppy_disk"),
    "tip_24x24": ("shared", "information"),
    "cloud_computing_download_16x16": ("v4", "cloud_download"),
    "cloud_computing_upload_16x16": ("v4", "cloud_upload"),
    "copy_16x16": ("v4", "copy"),
    "edit_note_16x16": ("v4", "edit"),
    "enable_16x16": ("v4", "checks"),
    "error_16x16": ("v4", "error"),
    "fix_off_16x16": ("v4", "@off:sign_warning"),
    "hardware_16x16": ("v4", "toolbox"),
    "information_16x16": ("v4", "information"),
    "nav_down_16x16": ("v4", "nav_down"),
    "nav_up_16x16": ("v4", "nav_up"),
    "ok_16x16": ("v4", "ok"),
    "ok_off_16x16": ("v4", "@off:ok"),
    "paste_16x16": ("v4", "clipboard_paste"),
    "test_16x16": ("v4", "window_oscillograph"),
    "load_16x16": ("v3", "inbox_out"),
    "save_add_16x16": ("v3", "@save_add"),
}

# Soft pastel for the light theme, pastel for the dark one: (K_L, K_C).
LIGHT = (0.20, 0.70)
DARK = (0.30, 0.55)

# Icons whose dark greys are a symbol on a light shape: lifting them would erase the symbol.
KEEP_DARK = {"sign_warning", "help_earth", "window_oscillograph"}

# Check boxes whose white inside would glare in the dark theme: there it takes the dark colour of a
# text box (#2D2D2D), as the check boxes Windows draws in that theme do.
DARK_INSIDE = {"checkbox_unchecked", "@undefined"}

# Sizes G2 ships, the ones between are reduced from the next larger.
G2_SIZES = (16, 24, 32, 48, 64, 128, 256)

DRAWING = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"


# --- Colour ---------------------------------------------------------------------------------------

def _to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def _to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def _oklab(rgb):
    lms = np.cbrt(_to_linear(rgb) @ np.array([[0.4122214708, 0.2119034982, 0.0883024619],
                                              [0.5363325363, 0.6806995451, 0.2817188376],
                                              [0.0514459929, 0.1073969566, 0.6299787005]]))
    return lms @ np.array([[0.2104542553, 1.9779984951, 0.0259040371],
                           [0.7936177850, -2.4285922050, 0.7827717662],
                           [-0.0040720468, 0.4505937099, -0.8086757660]])


def _from_oklab(lab):
    lms = (lab @ np.array([[1.0, 1.0, 1.0],
                           [0.3963377774, -0.1055613458, -0.0894841775],
                           [0.2158037573, -0.0638541728, -1.2914855480]])) ** 3
    return _to_srgb(lms @ np.array([[4.0767416621, -1.2684380046, -0.0041960863],
                                    [-3.3077115913, 2.6097574011, -0.7034186147],
                                    [0.2309699292, -0.3413193965, 1.7076147010]]))


def pastel(image, k_l, k_c, lift_greys):
    """Moves colours towards pastel; with lift_greys, dark greys are lifted for a dark background.

    Pixels lighten in proportion to their chroma, so greys and whites keep their lightness. Lifted,
    a grey's lightness L becomes 0.58 + (L - 0.30) * 0.6: white stays white, and the light and dark
    halves of G2's two-tone shading stay apart.
    """
    a = np.asarray(image.convert("RGBA")).astype(np.float64) / 255
    lab = _oklab(a[..., :3])
    chroma = np.hypot(lab[..., 1], lab[..., 2])
    light = lab[..., 0]
    lab[..., 0] = light + (1 - light) * k_l * np.clip(chroma / 0.10, 0, 1)
    if lift_greys:
        grey = np.clip(1 - chroma / 0.06, 0, 1)
        lifted = np.where(light >= 0.30, 0.58 + (light - 0.30) * 0.6, light * 0.58 / 0.30)
        lab[..., 0] = lab[..., 0] * (1 - grey) + lifted * grey
    lab[..., 1:] *= k_c
    a[..., :3] = _from_oklab(lab)
    return Image.fromarray((a * 255 + 0.5).astype(np.uint8), "RGBA")


def dark_inside(image):
    """Turns the white and near-white greys dark: lightness L becomes 0.30 + (1 - L), so white
    becomes the dark theme's text box colour and G2's two shades inside stay apart."""
    a = np.asarray(image.convert("RGBA")).astype(np.float64) / 255
    lab = _oklab(a[..., :3])
    pale = (np.hypot(lab[..., 1], lab[..., 2]) < 0.04) & (lab[..., 0] > 0.80)
    lab[..., 0] = np.where(pale, 0.30 + (1 - lab[..., 0]), lab[..., 0])
    a[..., :3] = _from_oklab(lab)
    return Image.fromarray((a * 255 + 0.5).astype(np.uint8), "RGBA")


def greyed(image):
    """The switched-off look: grey and partly see-through, so it suits both backgrounds."""
    a = np.asarray(image.convert("RGBA")).astype(np.float64)
    a[..., :3] = (a[..., :3] @ np.array([0.299, 0.587, 0.114]))[..., None]
    a[..., 3] *= 0.55
    return Image.fromarray((a + 0.5).astype(np.uint8), "RGBA")


# --- G2 pictures and the ones put together --------------------------------------------------------

class G2:
    def __init__(self, folder):
        self.folder = Path(folder)

    def icon(self, name, size):
        """G2's PNG at that size, or the next larger size reduced smoothly."""
        exact = self.folder / f"{size}x{size}" / f"{name}.png"
        if exact.exists():
            return Image.open(exact).convert("RGBA")
        larger = min(s for s in G2_SIZES if s > size)
        image = Image.open(self.folder / f"{larger}x{larger}" / f"{name}.png").convert("RGBA")
        return image.resize((size, size), Image.LANCZOS)

    def overlay(self, base, part, size, share=0.625):
        """Puts a smaller G2 icon in the bottom-right corner, with a one-pixel gap cut around it."""
        s = round(size * share)
        small = self.icon(part, s) if s >= 16 else self.icon(part, 16).resize((s, s), Image.LANCZOS)
        place = (size - s, size - s)
        mask = Image.new("L", (size, size), 0)
        mask.paste(small.getchannel("A"), place)
        gap = np.asarray(mask.filter(ImageFilter.MaxFilter(3))).astype(np.float64) / 255
        out = base.copy()
        out.putalpha(Image.fromarray((np.asarray(out.getchannel("A")) * (1 - gap)).astype(np.uint8)))
        out.alpha_composite(small, place)
        return out

    def undefined_box(self, size):
        """G2's empty check box with a filled square in the middle, in G2's check box greens."""
        box = self.icon("checkbox_unchecked", size)
        inner = max(4, round(size * 0.375))
        o = (size - inner) // 2
        d = ImageDraw.Draw(box)
        d.rectangle([o, o, o + inner - 1, o + inner - 1], fill=(0x38, 0x8d, 0x3c, 255))
        d.rectangle([o + 1, o + 1, o + inner - 2, o + inner - 2], fill=(0x42, 0x9f, 0x46, 255))
        return box

    @staticmethod
    def record_dot(size):
        """A red ball drawn the way G2 draws one: a darker rim, and the lower-right half in shade."""
        z = 8
        full = size * z
        big = Image.new("RGBA", (full, full), (0, 0, 0, 0))
        m = round(full * 0.125)
        outer = [m, m, full - m - 1, full - m - 1]
        inner = [outer[0] + z, outer[1] + z, outer[2] - z, outer[3] - z]
        ImageDraw.Draw(big).ellipse(outer, fill=(0xbd, 0x36, 0x0c, 255))
        ImageDraw.Draw(big).ellipse(inner, fill=(0xf3, 0x50, 0x1e, 255))
        lower = Image.new("RGBA", big.size, (0, 0, 0, 0))
        ImageDraw.Draw(lower).ellipse(inner, fill=(0xe5, 0x4a, 0x18, 255))
        half = Image.new("L", big.size, 0)
        ImageDraw.Draw(half).polygon([(full, 0), (full, full), (0, full)], fill=255)
        shade = Image.fromarray(np.minimum(np.asarray(lower.getchannel("A")), np.asarray(half)))
        big.paste(lower, (0, 0), shade)
        return big.resize((size, size), Image.LANCZOS)

    def source(self, recipe, size):
        if recipe == "@record":
            return self.record_dot(size)
        if recipe == "@undefined":
            return self.undefined_box(size)
        if recipe == "@folder_view":
            return self.overlay(self.icon("folder", size), "magnifying_glass", size, 0.6875)
        if recipe == "@save_add":
            return self.overlay(self.icon("floppy_disk", size), "plus", size)
        return self.icon(recipe.replace("@off:", ""), size)


def build(g2, recipe, size, dark):
    k_l, k_c = DARK if dark else LIGHT
    lift = dark and recipe.replace("@off:", "") not in KEEP_DARK
    image = pastel(g2.source(recipe, size), k_l, k_c, lift)
    if dark and recipe in DARK_INSIDE:
        image = dark_inside(image)
    return greyed(image) if recipe.startswith("@off:") else image


def sizes_of(key):
    """The size the program asks for, and 1.5 and 2 times it."""
    base = int(re.search(r"_(\d+)x\d+$", key).group(1))
    return [base, base * 3 // 2, base * 2]


def file_name(key, size):
    return f"{key.rsplit('_', 1)[0]}_{size}x{size}.png"


# --- Resource lists -------------------------------------------------------------------------------

def write_icons_resx(repo, app):
    """Properties/Icons.resx of a program: every size but the asked one, and every dark icon."""
    properties = repo / f"App.{app}" / "Properties"
    resources = (properties / "Resources.resx").read_text(encoding="utf-8-sig")
    header = resources[:resources.index("<data name=", resources.index("</xsd:schema>"))].rstrip(" ")
    entries = []
    for key, (folder, _) in ICONS.items():
        if folder not in ("shared", app):
            continue
        for i, size in enumerate(sizes_of(key)):
            name = file_name(key, size)[:-4]
            for dark in (False, True):
                if i == 0 and not dark:
                    continue
                path = f"..\\..\\Resources\\Images\\{folder}\\icons\\" + ("dark\\" if dark else "") + file_name(key, size)
                entries.append((name + ("_dark" if dark else ""), path))
    lines = [header.rstrip("\r\n")]
    for name, path in sorted(entries, key=lambda e: e[0].lower()):
        lines += [f'  <data name="{name}" type="System.Resources.ResXFileRef, System.Windows.Forms">',
                  f"    <value>{path};System.Drawing.Bitmap, {DRAWING}</value>",
                  "  </data>"]
    lines.append("</root>")
    text = "\r\n".join(line.replace("\r", "") for line in "\n".join(lines).split("\n")) + "\r\n"
    (properties / "Icons.resx").write_bytes(b"\xef\xbb\xbf" + text.encode("utf-8"))
    return len(entries)


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--g2", required=True, help="G2 PNG folder of the standard style (holds 16x16, 24x24, ...)")
    parser.add_argument("--repo", default=str(Path(__file__).resolve().parents[2]), help="x360ce repository root")
    args = parser.parse_args()
    g2, repo = G2(args.g2), Path(args.repo)
    if not (g2.folder / "16x16").is_dir():
        sys.exit(f"Not a G2 PNG folder: {g2.folder}")
    count = 0
    for key, (folder, recipe) in ICONS.items():
        icons = repo / "Resources" / "Images" / folder / "icons"
        (icons / "dark").mkdir(parents=True, exist_ok=True)
        for size in sizes_of(key):
            build(g2, recipe, size, False).save(icons / file_name(key, size))
            build(g2, recipe, size, True).save(icons / "dark" / file_name(key, size))
            count += 2
    print(f"{count} icons written")
    for app in ("v3", "v4"):
        print(f"App.{app}/Properties/Icons.resx: {write_icons_resx(repo, app)} entries")


if __name__ == "__main__":
    main()
