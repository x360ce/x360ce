# Interface icons

`build_icons.py` makes the interface icons of v3 and v4 from the IconExperience G2 collection
(standard style), recoloured pastel: soft pastel for the light theme, pastel with lifted greys for
the dark theme. Each icon is written at the size the program asks for and at 1.5 and 2 times that,
so it stays sharp on screens set above 100%.

```powershell
python scripts/art/build_icons.py --g2 "<IconExperience>\iconex_g2\g_collection\g_collection_png\standard" --core "<ClassLibrary>\Core"
```

It writes:

- `Resources/Images/{shared,v3,v4}/icons/*.png` — light theme icons.
- `Resources/Images/{shared,v3,v4}/icons/dark/*.png` — dark theme icons, same file names.
- `App.v3/Properties/Icons.resx`, `App.v4/Properties/Icons.resx` — every size but the asked one,
  and every dark icon; the program picks from these by screen scale and theme
  (`ThemeResourceManager`).
- `Engine/JocysCom/Controls/Themes/Images/**` and `ThemeResourceManager.resx` beside them — the icons
  of the JocysCom class library's own controls (issues list, hardware list, message box), every size
  and both themes, handed out by `ThemeResourceManager.Library`. With `--core` the same files are
  written into the class library itself, so the copy in `Engine/JocysCom` stays the same as the original.

The icon lists (program or library icon, G2 icon) are at the top of the script. To add a program icon:
add its 16-pixel entry to `Properties/Resources.resx` as usual, add a line to the list, and run the
script. A library icon needs only its line: its control asks `ThemeResourceManager.Library` for it.

Needs Python 3 with Pillow and NumPy. The G2 collection is licensed and not part of the repository.

# Controller pictures and button glyphs

`build_controller.py` cuts the controller pictures (top and front view, light and dark theme) and the
button glyphs from their masters in `controller/`, which hold both views at six times the size, and
writes each at the size the programs draw it and at 1.5 and 2 times that. Run `build_icons.py` after
it, so the glyph versions are listed in `Icons.resx`:

```powershell
python scripts/art/build_controller.py
```

It writes `Resources/Images/shared/xbox/xboxController{Top,Front}{,Dark}.png` and the versions named
by their size, such as `xboxControllerTop_384x158.png`. The programs load a picture with its versions
(`EngineHelper.GetResourcePicture`) and draw the version nearest above the size they need. Every
size is cut from the same master, so each part stays where the marks drawn over it expect it.
A new version must also be embedded in `App.v3` and `App.v4` beside the picture it belongs to.

It also writes the glyphs, `Resources/Images/shared/xbox/Button_*.png`, each button cut out in its own
shape, with the versions named by their size, such as `Button_A_30x30.png`. The dark theme's are in
`xbox/dark`, cut from the dark master with their greys lifted as the dark icons' are, so a greyed-out
glyph still shows on the dark background. The glyph list is at the top of the script.
