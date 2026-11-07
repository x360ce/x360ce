# Interface icons

`build_icons.py` makes the interface icons of v3 and v4 from the IconExperience G2 collection
(standard style), recoloured pastel: soft pastel for the light theme, pastel with lifted greys for
the dark theme. Each icon is written once, at twice the size it shows at 100%, and the program brings it down
to the screen's size: a whole number of pixels at 100, 125, 150, 175 and 200%.

```powershell
python scripts/art/build_icons.py --g2 "<IconExperience>\iconex_g2\g_collection\g_collection_png\standard" --core "<ClassLibrary>\Core"
```

It writes:

- `Resources/Images/{shared,v3,v4}/icons/*.png` — light theme icons.
- `Resources/Images/{shared,v3,v4}/icons/dark/*.png` — dark theme icons, same file names.
- `App.v3/Properties/Icons.resx`, `App.v4/Properties/Icons.resx` — the dark pictures, which the program
  hands out in the dark theme (`ThemeResourceManager`).
- `Engine/JocysCom/Controls/Themes/Images/**` and `ThemeResourceManager.resx` beside them — the icons
  of the JocysCom class library's own controls (issues list, hardware list, message box), in both
  themes, handed out by `ThemeResourceManager.Library`. With `--core` the same files are
  written into the class library itself, so the copy in `Engine/JocysCom` stays the same as the original.

The icon lists (program or library icon, G2 icon) are at the top of the script. To add a program icon:
add its 32-pixel entry to `Properties/Resources.resx` as usual, add a line to the list, and run the
script. A library icon needs only its line: its control asks `ThemeResourceManager.Library` for it.

Needs Python 3 with Pillow and NumPy. The G2 collection is licensed and not part of the repository.

# Controller pictures and button glyphs

`build_controller.py` cuts the controller pictures (top and front view, light and dark theme) and the
button glyphs from their masters in `controller/`, which hold both views at six times the size, and
writes each at twice the size the programs show it at 100%. Run `build_icons.py` after it, so the dark glyphs
are listed in `Icons.resx`:

```powershell
python scripts/art/build_controller.py
```

It writes `Resources/Images/shared/xbox/xboxController{Top,Front}{,Dark}_512x{210,352}.png`. The programs load
each once (`EngineHelper.GetResourcePicture`) and bring it down to the size they need.

It also writes the glyphs, `Resources/Images/shared/xbox/Button_*_{w}x{h}.png`, each button cut out in its own
shape. The dark theme's are in
`xbox/dark`, cut from the dark master with their greys lifted as the dark icons' are, so a greyed-out
glyph still shows on the dark background. The glyph list is at the top of the script. v3's Buttons page
shows them; v4's draws its own, below.

# Buttons page pictures

`build_buttons.py` draws the pictures of v4's Buttons page, a picture for each Xbox button and the grey
arrow beside it, from the path data of x360ce 5's `AxisToButtonControl.xaml`, kept as
`buttons/AxisToButtonControl.xaml` beside the script. It reads the path mini-language, fills each path in the
XAML's colours, and writes each picture at 48 pixels, twice the size the page shows it at 100%:

```powershell
python scripts/art/build_buttons.py
```

It writes `Resources/Images/v4/xbox/Button_*_48x48.png` and `Axis_Arrow_48x48.png`, and lists the dark one in
`App.v4/Properties/Icons.resx`. The arrow is drawn in a
colour of the theme, so it also comes for the dark theme, in `xbox/dark`; the buttons are drawn in their own
colours and are the same in both themes. Every pixel is counted from whole points, so running the script again
writes the same bytes. To add a picture: add its line to the list at the top of the script, its 48-pixel entry
to `App.v4/Properties/Resources.resx`, and run the script.
