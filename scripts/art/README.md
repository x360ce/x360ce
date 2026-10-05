# Interface icons

`build_icons.py` makes the interface icons of v3 and v4 from the IconExperience G2 collection
(standard style), recoloured pastel: soft pastel for the light theme, pastel with lifted greys for
the dark theme. Each icon is written at the size the program asks for and at 1.5 and 2 times that,
so it stays sharp on screens set above 100%.

```powershell
python scripts/art/build_icons.py --g2 "<IconExperience>\iconex_g2\g_collection\g_collection_png\standard"
```

It writes:

- `Resources/Images/{shared,v3,v4}/icons/*.png` — light theme icons.
- `Resources/Images/{shared,v3,v4}/icons/dark/*.png` — dark theme icons, same file names.
- `App.v3/Properties/Icons.resx`, `App.v4/Properties/Icons.resx` — every size but the asked one,
  and every dark icon; the program picks from these by screen scale and theme
  (`ThemeResourceManager`).

The icon list (program icon, G2 icon) is at the top of the script. To add an icon: add its 16-pixel
entry to `Properties/Resources.resx` as usual, add a line to the list, and run the script.

Needs Python 3 with Pillow and NumPy. The G2 collection is licensed and not part of the repository.
