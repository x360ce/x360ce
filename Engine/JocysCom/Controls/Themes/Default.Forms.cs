#nullable disable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;

namespace JocysCom.ClassLibrary.Controls.Themes
{
	/// <summary>
	/// Light and dark theme for Windows Forms, in the colours of Default_DarkTheme.xaml.
	/// </summary>
	/// <remarks>
	/// The dark theme gives the system colours of the process the dark values of the WPF keys that hold the
	/// same colours in the light theme: Window takes BackgroundWhite, Control takes BackgroundLight,
	/// ControlText takes ForegroundBlack, and so on. Designer settings, control defaults and painting code
	/// that use <see cref="SystemColors"/> follow without change. <see cref="Apply"/> covers what Windows
	/// draws itself: native backgrounds, buttons, check boxes, drop-down lists, tab strips, sliders, scrollbars and
	/// title bars.
	/// The light theme is the Windows colours. In both themes, push buttons, tool bars, tab strips, check boxes, group boxes
	/// and drop-down lists are drawn as the WPF programs draw them with Default.xaml: buttons and lists flat, filled and
	/// rounded, tabs rounded and apart, frames rounded, and a disabled control at half strength. Scroll bars are drawn whole,
	/// with their arrows, where Windows 11 draws them thin until the mouse is over them. High contrast keeps the Windows look.
	/// <para>
	/// The colours are read as XML from Default_DarkTheme.xaml, embedded in the program as a resource, and no
	/// WPF type is named here: loading WPF makes a Windows Forms process DPI aware mid-run.
	/// </para>
	/// <para>
	/// The system colours are changed in .NET Framework's colour table. On other runtimes the theme stays light.
	/// </para>
	/// </remarks>
	public static class FormsTheme
	{
		#region Theme

		/// <summary>The theme asked for.</summary>
		public static ThemeType Theme { get; private set; }

		/// <summary>True while the dark colours are in use.</summary>
		public static bool IsDark { get; private set; }

		/// <summary>Raised on the interface thread after the colours changed.</summary>
		public static event EventHandler ThemeChanged;

		/// <summary>
		/// Sets the theme. Call it on the interface thread before the first window is made, and again when
		/// the choice changes: the open windows follow at once.
		/// </summary>
		/// <remarks>Auto follows the Windows setting for apps. High contrast always keeps the Windows colours.</remarks>
		public static void SetTheme(ThemeType theme)
		{
			Theme = theme;
			if (_context == null)
			{
				// The interface thread, for the Windows setting changes that arrive on another one.
				_context = new WindowsFormsSynchronizationContext();
				SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
				_hookProc = HookProc;
				_hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_CBT, _hookProc, IntPtr.Zero, NativeMethods.GetCurrentThreadId());
				AppDomain.CurrentDomain.ProcessExit += (sender, e) => RemoveHook();
				// Every control's look is the theme's from now on, so the shared helpers leave it to the theme.
				ControlsHelper.LookFromTheme = true;
			}
			UpdateColors(true);
		}

		/// <summary>True when Windows is set to show apps light. Older Windows has no setting and is light.</summary>
		public static bool WindowsAppsUseLightTheme()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
			{
				var value = key?.GetValue("AppsUseLightTheme");
				return !(value is int) || (int)value != 0;
			}
		}

		static WindowsFormsSynchronizationContext _context;

		/// <summary>Whether high contrast was on when the colours were last set.</summary>
		static bool _highContrast;

		/// <summary>Works out light or dark, changes the system colours to it and brings the open windows along.</summary>
		/// <param name="themeChanged">True when the choice changed, so the open windows are themed again even if dark stayed dark.</param>
		static void UpdateColors(bool themeChanged)
		{
			var dark = Theme == ThemeType.Dark || (Theme == ThemeType.Auto && !WindowsAppsUseLightTheme());
			dark &= !SystemInformation.HighContrast && Palette.Count > 0;
			if (!SetSystemColors(dark))
				dark = false;
			var changed = dark != IsDark || SystemInformation.HighContrast != _highContrast;
			_highContrast = SystemInformation.HighContrast;
			IsDark = dark;
			// The grids dim an unavailable row in the theme's disabled text colour; high contrast keeps Windows' grey text.
			ControlsHelper.UnavailableForeColor = _highContrast ? Color.Empty : GetColor("ForegroundDisabled");
			if (dark || changed)
			{
				ToolStripManager.VisualStylesEnabled = !dark;
				// The professional renderer keeps the colours it worked out, so it starts again from the new ones.
				if (ToolStripManager.Renderer.GetType() == typeof(ToolStripProfessionalRenderer))
					ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
			}
			if (!changed && !themeChanged)
				return;
			// What is on each window shown is restyled unseen and drawn again once, whole, rather than control by control as
			// each changes. A window itself keeps drawing: Windows stops drawing one by taking away its visibility.
			var forms = Application.OpenForms.Cast<Form>().ToArray();
			var shown = forms.Where(x => x.IsHandleCreated && x.Visible).ToArray();
			var paused = shown.SelectMany(x => x.Controls.Cast<Control>()).Where(x => x.IsHandleCreated && x.Visible).ToArray();
			foreach (var control in paused)
				ControlsHelper.SuspendDrawing(control);
			try
			{
				foreach (var form in forms)
					Apply(form);
				ThemeChanged?.Invoke(null, EventArgs.Empty);
			}
			finally
			{
				foreach (var control in paused)
					ControlsHelper.ResumeDrawing(control);
				foreach (var form in shown)
					form.Invalidate(false);
			}
		}

		static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			// The colour table reloads the Windows colours whenever any of them change, so they are set again.
			if (e.Category == UserPreferenceCategory.Color || e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.VisualStyle)
				_context.Post(x => UpdateColors(false), null);
		}

		#endregion

		#region Colours

		/// <summary>Default_DarkTheme.xaml key of the dark colour each system colour takes.</summary>
		/// <remarks>The key whose light value is that system colour in the light theme, Default.xaml.</remarks>
		static readonly Dictionary<KnownColor, string> SystemColorKeys = new Dictionary<KnownColor, string>
		{
			{ KnownColor.Window, "BackgroundWhite" },
			{ KnownColor.ControlLightLight, "BorderHighlight" },
			{ KnownColor.ButtonHighlight, "BorderHighlight" },
			{ KnownColor.Control, "BackgroundLight" },
			{ KnownColor.ButtonFace, "BackgroundLight" },
			{ KnownColor.Menu, "BackgroundLight" },
			{ KnownColor.MenuBar, "BackgroundLight" },
			{ KnownColor.ControlLight, "BackgroundDark" },
			{ KnownColor.AppWorkspace, "BackgroundDark" },
			{ KnownColor.ControlDark, "BorderDark" },
			{ KnownColor.ControlDarkDark, "BorderDark" },
			{ KnownColor.ButtonShadow, "BorderDark" },
			{ KnownColor.ActiveBorder, "BorderDark" },
			{ KnownColor.InactiveBorder, "BorderDark" },
			{ KnownColor.WindowFrame, "BorderDark" },
			{ KnownColor.ScrollBar, "BorderDark" },
			{ KnownColor.ControlText, "ForegroundBlack" },
			{ KnownColor.WindowText, "ForegroundBlack" },
			{ KnownColor.MenuText, "ForegroundBlack" },
			{ KnownColor.InfoText, "ForegroundBlack" },
			{ KnownColor.ActiveCaptionText, "ForegroundBlack" },
			{ KnownColor.GrayText, "ForegroundGray" },
			{ KnownColor.InactiveCaptionText, "ForegroundGray" },
			{ KnownColor.Highlight, "SelectedBackground" },
			{ KnownColor.MenuHighlight, "SelectedBackground" },
			{ KnownColor.HighlightText, "SelectedForeground" },
			{ KnownColor.Info, "BackgroundHelp" },
			{ KnownColor.HotTrack, "ForegroundLink" },
		};

		/// <summary>Dark colours by Default_DarkTheme.xaml key; empty when the program does not carry the file.</summary>
		public static Dictionary<string, Color> Palette
		{
			get
			{
				if (_Palette == null)
					_Palette = ReadPalette();
				return _Palette;
			}
		}
		static Dictionary<string, Color> _Palette;

		const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

		static Dictionary<string, Color> ReadPalette()
		{
			var palette = new Dictionary<string, Color>();
			var assembly = typeof(FormsTheme).Assembly;
			var name = assembly.GetManifestResourceNames()
				.FirstOrDefault(x => x.EndsWith("Default_DarkTheme.xaml", StringComparison.OrdinalIgnoreCase));
			if (name == null)
				return palette;
			var doc = new XmlDocument();
			using (var stream = assembly.GetManifestResourceStream(name))
				doc.Load(stream);
			foreach (var element in doc.DocumentElement.ChildNodes.OfType<XmlElement>())
			{
				var key = element.GetAttribute("Key", XamlNamespace);
				if (element.LocalName != "SolidColorBrush" || string.IsNullOrEmpty(key))
					continue;
				var value = element.HasAttribute("Color") ? element.GetAttribute("Color") : element.InnerText;
				Color color;
				if (TryParseColor(value, out color))
					palette[key] = color;
			}
			return palette;
		}

		/// <summary>Reads #RRGGBB or #AARRGGBB.</summary>
		static bool TryParseColor(string value, out Color color)
		{
			color = Color.Empty;
			value = (value ?? "").Trim();
			uint argb;
			if (!value.StartsWith("#") || (value.Length != 7 && value.Length != 9)
				|| !uint.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out argb))
				return false;
			if (value.Length == 7)
				argb |= 0xFF000000;
			color = Color.FromArgb(unchecked((int)argb));
			return true;
		}

		/// <summary>The dark colour of a Default_DarkTheme.xaml key while the theme is dark; otherwise, or without the key, the light colour.</summary>
		public static Color GetColor(string key, Color lightColor)
		{
			Color color;
			return IsDark && Palette.TryGetValue(key, out color) ? color : lightColor;
		}

		/// <summary>
		/// The colour of a theme key in the theme in use: Default_DarkTheme.xaml's while the theme is dark, Default.xaml's
		/// (<see cref="LightPalette"/>) while it is light. Text of a role takes its key's colour, one key for each role:
		/// ForegroundWarning for an error, ForegroundCaution for an issue that is not an error, ForegroundInformation for a
		/// notice, ForegroundSuccess for success, ForegroundLink for a link, ForegroundGray for secondary text,
		/// ForegroundDisabled for the text of any disabled control and of a list row whose item is unplugged or switched off,
		/// ForegroundActive for what a controller does now, ForegroundOutput for what a chart's curve gives, ForegroundCode
		/// for a value as it is typed and ForegroundCommand for the name of a button to use. A chart's curve takes ColorCurve.
		/// A colour taken once is taken again on <see cref="ThemeChanged"/>.
		/// </summary>
		public static Color GetColor(string key)
		{
			return Themed(key, SystemColors.ControlText);
		}

		/// <summary>
		/// The light colours of the Default.xaml keys the buttons, tool bars, tab strips, fields, check boxes, groups and grid
		/// headers are drawn with, the text of each role, and the charts.
		/// </summary>
		/// <remarks>Copied here because not every program carries Default.xaml; a test holds them equal to it.</remarks>
		public static readonly Dictionary<string, Color> LightPalette = new Dictionary<string, Color>
		{
			{ "BackgroundWhite", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "BackgroundLight", Color.FromArgb(0xF0, 0xF0, 0xF0) },
			{ "BackgroundDark", Color.FromArgb(0xDE, 0xDE, 0xDE) },
			{ "BackgroundDarkSelected", Color.FromArgb(0xC2, 0xE7, 0xFF) },
			{ "BackgroundDarkPressed", Color.FromArgb(0xC2, 0xFF, 0xE7) },
			{ "BackgroundTabSelected", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "BackgroundHeader", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "BorderDark", Color.FromArgb(0xCC, 0xCC, 0xCC) },
			{ "BorderGray", Color.FromArgb(0xAC, 0xAC, 0xAC) },
			{ "BorderField", Color.FromArgb(0xDE, 0xDE, 0xDE) },
			{ "BorderCheck", Color.FromArgb(0xDE, 0xDE, 0xDE) },
			{ "BorderShadow", Color.FromArgb(0xCC, 0xCC, 0xCC) },
			{ "BorderHighlight", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "ForegroundBlack", Color.FromArgb(0x00, 0x00, 0x00) },
			{ "ForegroundGray", Color.FromArgb(0x5A, 0x5A, 0x5A) },
			{ "ForegroundDisabled", Color.FromArgb(0x78, 0x78, 0x78) },
			{ "ForegroundWarning", Color.FromArgb(0x80, 0x00, 0x00) },
			{ "ForegroundCaution", Color.FromArgb(0x9F, 0x4F, 0x00) },
			{ "ForegroundInformation", Color.FromArgb(0x00, 0x5F, 0xB8) },
			{ "ForegroundSuccess", Color.FromArgb(0x00, 0x64, 0x00) },
			{ "ForegroundLink", Color.FromArgb(0x00, 0x00, 0xFF) },
			{ "ForegroundActive", Color.FromArgb(0x00, 0x64, 0x00) },
			{ "ForegroundOutput", Color.FromArgb(0x00, 0x00, 0xFF) },
			{ "ForegroundCode", Color.FromArgb(0x00, 0x64, 0x00) },
			{ "ForegroundCommand", Color.FromArgb(0x00, 0x5F, 0xB8) },
			{ "ForegroundTab", Color.FromArgb(0x5A, 0x5A, 0x5A) },
			{ "ForegroundTabSelected", Color.FromArgb(0x00, 0x00, 0x00) },
			{ "SelectedBackground", Color.FromArgb(0x00, 0x78, 0xD4) },
			{ "ColorNormal", Color.FromArgb(0x66, 0x99, 0xFF) },
			{ "ColorActive", Color.FromArgb(0x42, 0xC7, 0x65) },
			{ "ColorCurve", Color.FromArgb(0xFF, 0x00, 0x00) },
		};

		/// <summary>The colour of a WPF theme key in the theme in use, light or dark; without the key, the fallback.</summary>
		static Color Themed(string key, Color fallback)
		{
			Color color;
			return (IsDark ? Palette : LightPalette).TryGetValue(key, out color) ? color : fallback;
		}

		/// <summary>Puts the dark colours in the system colour table, or the Windows ones back.</summary>
		/// <returns>False when the table cannot be changed, so the theme stays light.</returns>
		static bool SetSystemColors(bool dark)
		{
#if NETFRAMEWORK
			var tableType = typeof(Color).Assembly.GetType("System.Drawing.KnownColorTable");
			var field = tableType?.GetField("colorTable", BindingFlags.Static | BindingFlags.NonPublic);
			var reload = tableType?.GetMethod("UpdateSystemColors", BindingFlags.Static | BindingFlags.NonPublic);
			// Reading a colour fills the table first.
			SystemColors.Control.ToArgb();
			var table = field?.GetValue(null) as int[];
			if (table == null || reload == null)
				return false;
			reload.Invoke(null, new object[] { table });
			if (dark)
			{
				foreach (var pair in SystemColorKeys)
				{
					Color color;
					if (Palette.TryGetValue(pair.Value, out color))
						table[(int)pair.Key] = color.ToArgb();
				}
			}
			// System brushes and pens made earlier keep the colour they were made with until told.
			var tracker = typeof(Color).Assembly.GetType("System.Drawing.Internal.SystemColorTracker");
			tracker?.GetMethod("OnUserPreferenceChanged", BindingFlags.Static | BindingFlags.NonPublic)?
				.Invoke(null, new object[] { null, new UserPreferenceChangedEventArgs(UserPreferenceCategory.Color) });
			return true;
#else
			return !dark;
#endif
		}

		#endregion

		#region Fonts

		/// <summary>
		/// The interface font, which every control that does not choose a font of its own is drawn in; null while Windows Forms'
		/// own default font is in use. See <see cref="SetFonts"/>.
		/// </summary>
		public static Font Font { get; private set; }

		/// <summary>The family code and values whose letters line up are drawn in, in place of Consolas; null keeps Consolas.</summary>
		public static FontFamily CodeFamily { get; private set; }

		/// <summary>
		/// The families Windows and the Windows Forms designer give text, which the interface family stands in for, with Windows
		/// Forms' own default font's.
		/// </summary>
		static readonly string[] WindowsFamilies = { "Microsoft Sans Serif", "Segoe UI" };

		/// <summary>The family Windows shows code in, which <see cref="CodeFamily"/> stands in for.</summary>
		const string WindowsCodeFamily = "Consolas";

		/// <summary>The size Windows Forms' designer writes for its default font, which a control naming it means as the interface size.</summary>
		const float DesignerFontSize = 8.25f;

		/// <summary>Windows Forms' own default font, read before <see cref="SetFonts"/> first changed it.</summary>
		static Font _windowsFont;

		/// <summary>The family given for the interface; null keeps the family each font names.</summary>
		static FontFamily _interfaceFamily;

		/// <summary>How much larger than in Windows' font the handle windows are measured with lays them out.</summary>
		static float _layoutScale;

		/// <summary>How much larger the interface font is than Windows Forms' default font, which the fonts it replaces were designed with.</summary>
		static float _textScale;

		/// <summary>The interface thread, for the Windows setting changes that arrive on another one.</summary>
		static WindowsFormsSynchronizationContext _fontContext;

		static readonly FieldInfo DefaultFontField = typeof(Control).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic);
		static readonly FieldInfo DefaultFontHandleField = typeof(Control).GetField("defaultFontHandleWrapper", BindingFlags.Static | BindingFlags.NonPublic);
		static readonly FieldInfo MenuFontField = typeof(ToolStripManager).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic);
		static readonly MethodInfo DefaultFontHandleMethod = typeof(Control).GetMethod("GetDefaultFontHandleWrapper", BindingFlags.Static | BindingFlags.NonPublic);

		/// <summary>
		/// Draws the interface in a family of the program's choice at a size, and code in another. Call it on the interface
		/// thread before the first window is made. Without an interface family, at Windows Forms' default size and layout, the
		/// windows made after it have Windows Forms' own fonts again.
		/// </summary>
		/// <param name="interfaceFamily">The family text is drawn in; null keeps the family each font names.</param>
		/// <param name="codeFamily">The family code is drawn in, in place of Consolas; null keeps Consolas. Used with an interface family only.</param>
		/// <param name="sizeInPoints">The interface font's size; 0 for the size of Windows Forms' default font.</param>
		/// <param name="layoutScale">How much larger than in Windows Forms' default font windows are laid out, and than Windows' menus the menus are drawn; 1 for both as Windows has them.</param>
		/// <remarks>
		/// <para>
		/// Every window keeps the layout it has in Windows' font, made larger or smaller by the layout scale. Windows Forms
		/// scales a window by how much larger the font in use is than the one it was designed with (AutoScaleMode.Font), so a
		/// face whose lines are taller at the same size would make every window taller. It measures a window that has no parent
		/// with the handle of its default font, and that handle is made here, from Windows' font at the layout scale, before the
		/// default font changes: the scale stays Windows', or as much larger as asked, and the text is drawn in the new font.
		/// </para>
		/// <para>
		/// Windows' own controls draw with the font handle of the window they are on, which for a window without a font of its
		/// own is that same handle. So each window is given the interface font as its own when it is themed: the font it
		/// already has, which lays nothing out again, with a handle of its own made from it.
		/// </para>
		/// <para>
		/// A control, a tool bar item or a grid style that names Windows Forms' default family, Microsoft Sans Serif or Segoe
		/// UI is drawn in the interface family, one that names Consolas in the code family, in its own style. Every font
		/// replaced is drawn larger or smaller by as much as the interface font is than Windows Forms' default font, which it
		/// stands in for: a control that names Windows Forms' default size is drawn at the interface size. A font made from a
		/// family rather than named, as a program makes one from the interface font, is left as it is. A control holding a
		/// layout of its own (a <see cref="ContainerControl"/>) keeps a font it names, which would scale it again. The menus
		/// are drawn in the interface family at the size of Windows' menu font, which is Windows' text size already, made
		/// larger or smaller by the layout scale.
		/// </para>
		/// <para>
		/// Windows Forms forgets both default fonts when Windows' settings change: they are set again after each change.
		/// The default fonts are Windows Forms' own fields, as .NET Framework names them; where they are named otherwise,
		/// the fonts stay Windows Forms' own, and where the handle is named otherwise, windows are laid out in Windows' font.
		/// </para>
		/// </remarks>
		public static void SetFonts(FontFamily interfaceFamily, FontFamily codeFamily, float sizeInPoints = 0, float layoutScale = 1)
		{
			if (DefaultFontField == null || DefaultFontHandleMethod == null)
				return;
			if (_windowsFont == null)
			{
				_windowsFont = Control.DefaultFont;
				// The handle a window without a parent is measured with, made now, from Windows' font, and kept.
				DefaultFontHandleMethod.Invoke(null, null);
				_layoutScale = 1;
				_fontContext = new WindowsFormsSynchronizationContext();
				SystemEvents.UserPreferenceChanged += (sender, e) => _fontContext.Post(x => SetDefaultFonts(), null);
			}
			if (layoutScale != _layoutScale && DefaultFontHandleField != null)
			{
				// Made again from Windows' font at the layout scale, and kept.
				DefaultFontField.SetValue(null, new Font(_windowsFont.FontFamily, _windowsFont.SizeInPoints * layoutScale, _windowsFont.Style, GraphicsUnit.Point));
				DefaultFontHandleField.SetValue(null, null);
				DefaultFontHandleMethod.Invoke(null, null);
				_layoutScale = layoutScale;
			}
			if (sizeInPoints <= 0)
				sizeInPoints = _windowsFont.SizeInPoints;
			_textScale = sizeInPoints / DesignerFontSize;
			_interfaceFamily = interfaceFamily;
			// Asked for by face name only, so Windows finds the face whatever the language of the default font.
			Font = interfaceFamily == null && sizeInPoints == _windowsFont.SizeInPoints ? null
				: new Font(interfaceFamily ?? _windowsFont.FontFamily, sizeInPoints, _windowsFont.Style, GraphicsUnit.Point);
			CodeFamily = interfaceFamily == null ? null : codeFamily;
			SetDefaultFonts();
		}

		/// <summary>A font for code at a size: in <see cref="CodeFamily"/>, or in Consolas while the program has none.</summary>
		public static Font NewCodeFont(float sizeInPoints)
		{
			return CodeFamily == null ? new Font(WindowsCodeFamily, sizeInPoints) : new Font(CodeFamily, sizeInPoints);
		}

		/// <summary>Gives Windows Forms the interface font as its default, and the menus their size of it; or their own fonts back.</summary>
		static void SetDefaultFonts()
		{
			DefaultFontField.SetValue(null, Font);
			if (MenuFontField == null)
				return;
			// Not set, Windows Forms works the menus' font out again from Windows' menu font.
			using (var windows = SystemFonts.MenuFont)
				MenuFontField.SetValue(null, InterfaceFontFor(windows, _layoutScale));
		}

		/// <summary>
		/// The program's font in place of one a control, a tool bar item or a grid style names (<see cref="InterfaceFontFor"/>);
		/// null to keep it. A font made from a family rather than asked for by its name, as a program makes a smaller or a bold
		/// one from the interface font, is the program's own already, and replacing it would scale it twice.
		/// </summary>
		static Font ReplaceFont(Font font)
		{
			return string.IsNullOrEmpty(font?.OriginalFontName) ? null : InterfaceFontFor(font, _textScale);
		}

		/// <summary>
		/// A font in the interface family in place of one in a family Windows gives text, or in the code family in place of
		/// Consolas, in its style and larger or smaller by a scale; null for any other font, and while the program draws in
		/// Windows' fonts. Without an interface or a code family a font keeps its family and only its size changes.
		/// </summary>
		static Font InterfaceFontFor(Font font, float scale)
		{
			if (font == null || Font == null)
				return null;
			var name = font.FontFamily.Name;
			FontFamily family;
			if (name == WindowsCodeFamily)
				family = CodeFamily ?? font.FontFamily;
			else if (WindowsFamilies.Contains(name) || name == _windowsFont.FontFamily.Name)
				family = _interfaceFamily ?? font.FontFamily;
			else
				return null;
			if (!family.IsStyleAvailable(font.Style))
				return null;
			return new Font(family, font.Size * scale, font.Style, font.Unit);
		}

		/// <summary>Draws a control in the program's fonts, and gives a window the interface font as its own, as the theme first meets it.</summary>
		static void ApplyFonts(Control control, State state)
		{
			if (Font == null)
				return;
			if (control is Form && !state.FontSet)
			{
				// Equal to the font it has, so nothing is measured or laid out again: only the handle the controls on it draw
				// with is made from it from now on, and those already made are given it.
				if (control.Font.Equals(Font))
				{
					control.Font = Font;
					SetWindowFonts(control);
				}
			}
			else if (state.FontSet && !(control is ContainerControl))
			{
				var font = ReplaceFont(control.Font);
				if (font != null)
					control.Font = font;
			}
			if (control is DataGridView grid)
				ApplyFonts(grid);
			if (control is ToolStrip strip)
				ApplyFonts(strip.Items);
		}

		/// <summary>
		/// Gives the Windows controls on a control, made already, the font handle they draw with now. A rich text box is left
		/// as it is: the font would replace every font its text carries.
		/// </summary>
		static void SetWindowFonts(Control control)
		{
			foreach (Control child in control.Controls)
			{
				if (child.IsHandleCreated && !(child is RichTextBox))
					SetWindowFontMethod?.Invoke(child, null);
				SetWindowFonts(child);
			}
		}

		/// <summary>Draws a grid's styles that name a font in the program's fonts.</summary>
		static void ApplyFonts(DataGridView grid)
		{
			var style = ReplaceFont(grid.DefaultCellStyle);
			if (style != null)
				grid.DefaultCellStyle = style;
			style = ReplaceFont(grid.ColumnHeadersDefaultCellStyle);
			if (style != null)
				grid.ColumnHeadersDefaultCellStyle = style;
			style = ReplaceFont(grid.RowHeadersDefaultCellStyle);
			if (style != null)
				grid.RowHeadersDefaultCellStyle = style;
			style = ReplaceFont(grid.RowsDefaultCellStyle);
			if (style != null)
				grid.RowsDefaultCellStyle = style;
			style = ReplaceFont(grid.AlternatingRowsDefaultCellStyle);
			if (style != null)
				grid.AlternatingRowsDefaultCellStyle = style;
			foreach (DataGridViewColumn column in grid.Columns)
			{
				if (!column.HasDefaultCellStyle)
					continue;
				style = ReplaceFont(column.DefaultCellStyle);
				if (style != null)
					column.DefaultCellStyle = style;
			}
		}

		/// <summary>A copy of a grid style in the program's font in place of the one it names; null to keep the style.</summary>
		static DataGridViewCellStyle ReplaceFont(DataGridViewCellStyle style)
		{
			var font = ReplaceFont(style.Font);
			return font == null ? null : new DataGridViewCellStyle(style) { Font = font };
		}

		/// <summary>Draws the tool bar and menu items that name a font in the program's fonts, those of their menus too.</summary>
		static void ApplyFonts(ToolStripItemCollection items)
		{
			foreach (ToolStripItem item in items)
			{
				if (TypeDescriptor.GetProperties(item)[nameof(ToolStripItem.Font)].ShouldSerializeValue(item))
				{
					var font = ReplaceFont(item.Font);
					if (font != null)
						item.Font = font;
				}
				if (item is ToolStripDropDownItem menu && menu.HasDropDownItems)
					ApplyFonts(menu.DropDownItems);
			}
		}

		#endregion

		#region Controls

		/// <summary>What a control looked like before the dark theme changed it, so the light theme can put it back.</summary>
		class State
		{
			public bool? Dark;
			/// <summary>Whether high contrast was on when the control was last themed: it keeps the Windows look.</summary>
			public bool? HighContrast;
			public Color BackColor;
			public bool BackColorSet;
			public Color ForeColor;
			public bool ForeColorSet;
			public FlatStyle FlatStyle;
			public int FlatBorderSize;
			public Color FlatMouseOverBackColor;
			public Color FlatMouseDownBackColor;
			public bool UseVisualStyleBackColor;
			/// <summary>
			/// A check box's own padding, which the larger box of Default.xaml widens, a number box's, which the room of its frame does, or a tool bar's, which its items' margins replace.
			/// </summary>
			public Padding Padding;
			/// <summary>Whether the program gave a group box a padding of its own, which the theme then keeps.</summary>
			public bool PaddingSet;
			/// <summary>A push button's own largest and smallest sizes, which Default.xaml's height may lower or raise.</summary>
			public Size MaximumSize;
			public Size MinimumSize;
			public bool EnableHeadersVisualStyles;
			public Color LinkColor;
			public Color ActiveLinkColor;
			public Color VisitedLinkColor;
			/// <summary>A grid's link columns and their own link, pressed and visited colours.</summary>
			public List<KeyValuePair<DataGridViewLinkColumn, Color[]>> LinkColumns;
			public ToolStripRenderMode RenderMode;
			/// <summary>Whether the program gave the control a font of its own, which a tool bar then keeps.</summary>
			public bool FontSet;
			public ControlStyles? PaintStyles;
			public List<KeyValuePair<DataGridViewButtonColumn, FlatStyle>> ButtonColumns;
			/// <summary>A property grid's own colours, in the order <see cref="PropertyGridColors"/> reads them.</summary>
			public Color[] PropertyGridColors;
			public bool NativeDark;
			/// <summary>The tab under the mouse on a tab strip painted here, or -1.</summary>
			public int HotTab = -1;
			/// <summary>Whether the window of a tab strip has the tabs of a strip painted here.</summary>
			public bool TabsPadded;
			/// <summary>Whether the window of a tab strip sizes its tabs as Default.xaml's, with the padding given here.</summary>
			public bool TabsSized;
			/// <summary>The blank pictures Windows sizes the tabs of such a strip by, while its tabs have pictures.</summary>
			public ImageList TabSizes;
			/// <summary>
			/// The window of a tab strip or slider painted here, which passes the mouse on to what is drawn under it, and puts a
			/// strip's pages inside the frame drawn round them.
			/// </summary>
			public MouseMap Mouse;
			/// <summary>The window of the list a drop-down list painted here opens.</summary>
			public DropDownList List;
			/// <summary>The window of the text box on an editable drop-down list painted here, and the list's own, which colours it.</summary>
			public EditField Edit;
			public EditColors Colors;
			/// <summary>A text box, number box or list's own edge, which the frame drawn here replaces.</summary>
			public BorderStyle BorderStyle;
			/// <summary>
			/// A text box or number box's own sizing, height and width, which Default.xaml's height and the room of its frame
			/// replace, a slider's own sizing, which the size of Default.xaml's thumb and ticks replaces, and a number box's
			/// buttons' width.
			/// </summary>
			public bool AutoSize;
			public int Height;
			public int Width;
			public int ButtonsWidth;
			/// <summary>A list's own drawing, item height and sizing to whole items, which Default.xaml's items replace, and a drop-down list's own drawing.</summary>
			public DrawMode DrawMode;
			public int ItemHeight;
			public bool IntegralHeight;
			/// <summary>The frame drawn in the window's edge, while the control has one.</summary>
			public Frame Frame;
			/// <summary>The window of a control that can scroll, whose scroll bars are drawn whole.</summary>
			public WholeScrollBars ScrollBars;
			/// <summary>The window of the list a drop-down list opens, whose scroll bar is drawn whole.</summary>
			public WholeScrollBars ListScrollBars;
			/// <summary>What a grid drawn as the WPF programs draw one had before.</summary>
			public GridLook Grid;
			/// <summary>
			/// A container's own padding while a list filling it stands edge to edge in it, and the controls docked above and below
			/// the list that were lent its sides; null while the container has its padding.
			/// </summary>
			public Padding? LentPadding;
			public List<Control> LentTo;
		}

		/// <summary>What a grid drawn as the WPF programs draw one had before, so it can have it back.</summary>
		class GridLook
		{
			public BorderStyle BorderStyle;
			public Color BackgroundColor;
			public Color GridColor;
			public DataGridViewCellBorderStyle CellBorderStyle;
			public DataGridViewHeaderBorderStyle HeadersBorderStyle;
			public Padding HeaderPadding;
			public DataGridViewColumnHeadersHeightSizeMode HeadersHeightSizeMode;
			public int HeadersHeight;
			public int RowHeight;
			public List<KeyValuePair<DataGridViewImageColumn, Padding>> ImagePadding;
			/// <summary>The column given the room before its header and cells as the first one shown.</summary>
			public DataGridViewColumn First;
			/// <summary>
			/// The cell and header paddings last given to a first column, with the room, and its own without: a grid making its
			/// columns from its data again copies them from the columns it had, room and all.
			/// </summary>
			public Padding[] LastPadded;
			public Padding[] LastOwn;
		}

		/// <summary>
		/// The paddings each column's cells and header had before the theme first gave it the room of the first column shown:
		/// the room is added to these each time, so it is never added twice.
		/// </summary>
		static readonly ConditionalWeakTable<DataGridViewColumn, Padding[]> OwnColumnPaddings = new ConditionalWeakTable<DataGridViewColumn, Padding[]>();

		static readonly ConditionalWeakTable<Control, State> States = new ConditionalWeakTable<Control, State>();

		/// <summary>Themes a control and everything on it, and the controls added to it later.</summary>
		/// <remarks>Windows are themed as they first become active, so this is for controls shown some other way.</remarks>
		public static void Apply(Control control)
		{
			if (control == null)
				return;
			// Laid out once, after everything on it is themed, rather than again for each size the theme changes.
			control.SuspendLayout();
			try
			{
				ApplyControl(control);
				foreach (Control child in control.Controls)
					Apply(child);
			}
			finally
			{
				control.ResumeLayout();
			}
		}

		static void ApplyControl(Control control)
		{
			State state;
			if (!States.TryGetValue(control, out state))
			{
				state = Remember(control);
				States.Add(control, state);
				ApplyFonts(control, state);
				// A grid is drawn off the screen first, in every theme, so rows written while it is shown do not flicker.
				if (control is DataGridView)
					SetStyleMethod.Invoke(control, new object[] { ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true });
				control.ControlAdded += (sender, e) => Apply(e.Control);
				control.HandleCreated += (sender, e) =>
				{
					// A window made again has no window style yet, and a tab strip its own padding.
					state.NativeDark = false;
					state.TabsPadded = false;
					ApplyNative((Control)sender);
					AttachScrollBars((Control)sender, state);
				};
			}
			// Whole scroll bars in every theme, also on a control the theme otherwise leaves as it is.
			if (control.IsHandleCreated)
				AttachScrollBars(control, state);
			var highContrast = SystemInformation.HighContrast;
			if (state.Dark == IsDark && state.HighContrast == highContrast)
				return;
			// A control the dark theme never changed is left exactly as it is, but for the buttons, tool bars, tab strips
			// and the rest the light theme draws as the WPF programs do.
			if (state.Dark == null && !IsDark && !DrawnAsWpf(control))
			{
				state.Dark = false;
				state.HighContrast = highContrast;
				return;
			}
			state.Dark = IsDark;
			state.HighContrast = highContrast;
			ApplyColors(control, state);
			ApplyStyle(control, state);
			ApplyNative(control);
		}

		static State Remember(Control control)
		{
			var properties = TypeDescriptor.GetProperties(control);
			var state = new State
			{
				BackColor = control.BackColor,
				BackColorSet = properties[nameof(Control.BackColor)].ShouldSerializeValue(control),
				ForeColor = control.ForeColor,
				ForeColorSet = properties[nameof(Control.ForeColor)].ShouldSerializeValue(control),
				FontSet = properties[nameof(Control.Font)].ShouldSerializeValue(control),
			};
			var button = control as ButtonBase;
			if (button != null)
			{
				state.FlatStyle = button.FlatStyle;
				state.FlatBorderSize = button.FlatAppearance.BorderSize;
				state.FlatMouseOverBackColor = button.FlatAppearance.MouseOverBackColor;
				state.FlatMouseDownBackColor = button.FlatAppearance.MouseDownBackColor;
				state.UseVisualStyleBackColor = button.UseVisualStyleBackColor;
				state.Padding = button.Padding;
				state.AutoSize = button.AutoSize;
				state.MaximumSize = button.MaximumSize;
				state.MinimumSize = button.MinimumSize;
			}
			if (control is ComboBox combo)
			{
				state.FlatStyle = combo.FlatStyle;
				state.Padding = combo.Padding;
				state.DrawMode = combo.DrawMode;
			}
			if (control is TabPage page)
				state.UseVisualStyleBackColor = page.UseVisualStyleBackColor;
			if (control is GroupBox groupBox)
			{
				state.Padding = groupBox.Padding;
				state.PaddingSet = PaddingSet(groupBox);
			}
			if (control is Label rememberedLabel)
			{
				state.AutoSize = rememberedLabel.AutoSize;
				state.MaximumSize = rememberedLabel.MaximumSize;
			}
			if (control is TextBox box)
			{
				state.BorderStyle = box.BorderStyle;
				state.AutoSize = box.AutoSize;
				state.Height = box.Height;
				state.Width = box.Width;
			}
			if (control is UpDownBase upDown)
			{
				state.BorderStyle = upDown.BorderStyle;
				state.Padding = upDown.Padding;
				state.Height = upDown.Height;
				state.Width = upDown.Width;
				state.ButtonsWidth = (int)(ButtonsWidthField?.GetValue(upDown) ?? 0);
			}
			if (control is ListBox list)
			{
				state.BorderStyle = list.BorderStyle;
				state.DrawMode = list.DrawMode;
				state.ItemHeight = list.ItemHeight;
				state.IntegralHeight = list.IntegralHeight;
			}
			if (control is DataGridView grid)
			{
				state.EnableHeadersVisualStyles = grid.EnableHeadersVisualStyles;
				state.ButtonColumns = grid.Columns.OfType<DataGridViewButtonColumn>()
					.Select(x => new KeyValuePair<DataGridViewButtonColumn, FlatStyle>(x, x.FlatStyle)).ToList();
				state.LinkColumns = grid.Columns.OfType<DataGridViewLinkColumn>()
					.Select(x => new KeyValuePair<DataGridViewLinkColumn, Color[]>(x, new[] { x.LinkColor, x.ActiveLinkColor, x.VisitedLinkColor })).ToList();
			}
			if (control is ToolStrip strip)
			{
				state.RenderMode = strip.RenderMode;
				state.Padding = strip.Padding;
			}
			if (control is TrackBar slider)
				state.AutoSize = slider.AutoSize;
			if (control is LinkLabel link)
			{
				state.LinkColor = link.LinkColor;
				state.ActiveLinkColor = link.ActiveLinkColor;
				state.VisitedLinkColor = link.VisitedLinkColor;
			}
			if (control is PropertyGrid propertyGrid)
				state.PropertyGridColors = PropertyGridColors(propertyGrid);
			return state;
		}

		/// <summary>The colours a property grid paints its rows, categories, help and commands with, which are not its own BackColor and ForeColor.</summary>
		static Color[] PropertyGridColors(PropertyGrid grid)
		{
			return new[]
			{
				grid.ViewBackColor, grid.ViewForeColor, grid.LineColor, grid.CategoryForeColor,
				grid.HelpBackColor, grid.HelpForeColor, grid.CommandsBackColor, grid.CommandsForeColor,
			};
		}

		/// <summary>
		/// Gives a property grid its colours: in the dark theme a plain copy of each system colour, which Windows Forms
		/// paints with the theme's value rather than the Windows brush, as <see cref="ApplyColors"/> does a control's.
		/// </summary>
		static void ApplyPropertyGrid(PropertyGrid grid, State state)
		{
			var c = state.PropertyGridColors.Select(x => IsDark && x.IsSystemColor ? Color.FromArgb(x.ToArgb()) : x).ToArray();
			grid.ViewBackColor = c[0];
			grid.ViewForeColor = c[1];
			grid.LineColor = c[2];
			grid.CategoryForeColor = c[3];
			grid.HelpBackColor = c[4];
			grid.HelpForeColor = c[5];
			grid.CommandsBackColor = c[6];
			grid.CommandsForeColor = c[7];
		}

		/// <summary>
		/// Native controls fill a system colour with the Windows brush, which the colour table does not reach:
		/// a plain copy of the same colour makes Windows Forms paint it.
		/// </summary>
		static void ApplyColors(Control control, State state)
		{
			if (control is WebBrowserBase)
				return;
			// Windows Forms paints a button itself, so a button needs no plain copy.
			if (state.BackColor.IsSystemColor && !(control is ButtonBase))
			{
				if (IsDark)
					control.BackColor = Color.FromArgb(state.BackColor.ToArgb());
				else if (state.BackColorSet)
					control.BackColor = state.BackColor;
				else
					control.ResetBackColor();
			}
			if (state.ForeColor.IsSystemColor)
			{
				if (IsDark)
					control.ForeColor = Color.FromArgb(state.ForeColor.ToArgb());
				else if (state.ForeColorSet)
					control.ForeColor = state.ForeColor;
				else
					control.ResetForeColor();
			}
		}

		/// <summary>Swaps the parts Windows draws in its light visual style for ones drawn in the theme colours.</summary>
		static void ApplyStyle(Control control, State state)
		{
			var button = control as ButtonBase;
			if (button != null)
			{
				button.Paint -= Button_Paint;
				// A push button the program left in a Windows style is drawn as the WPF programs draw one, in both
				// themes: flat and borderless, filled and rounded, lighter under the mouse and lighter again pressed.
				if (IsWpfButton(button, state))
				{
					button.FlatStyle = FlatStyle.Flat;
					button.FlatAppearance.BorderSize = 0;
					button.UseVisualStyleBackColor = false;
					if (!state.BackColorSet || state.BackColor.IsSystemColor)
						button.BackColor = Themed("BackgroundDark", SystemColors.ControlLight);
					button.FlatAppearance.MouseOverBackColor = Themed("BackgroundDarkSelected", SystemColors.ControlLight);
					button.FlatAppearance.MouseDownBackColor = Themed("BackgroundDarkPressed", SystemColors.ControlDark);
					button.Paint += Button_Paint;
					// A button that sizes itself stands as tall as Default.xaml's: its text, with its face-coloured line and 4 of
					// room inside it above and below, or its picture or lines of text with the line and room where they are
					// taller. Windows Forms keeps other room round a line of text in some fonts and scales, and works its height
					// out again as its picture is enlarged to the screen's scale, so the button is held to that height.
					var padding = state.Padding;
					var minimum = state.MinimumSize;
					var maximum = state.MaximumSize;
					if (button.AutoSize)
					{
						if (button.MinimumSize != state.MinimumSize)
							button.MinimumSize = state.MinimumSize;
						if (button.MaximumSize != state.MaximumSize)
							button.MaximumSize = state.MaximumSize;
						var height = ControlHeight(button);
						var natural = button.GetPreferredSize(Size.Empty).Height - button.Padding.Vertical + state.Padding.Vertical;
						var extra = Math.Max(0, height - natural);
						padding = new Padding(padding.Left, padding.Top + extra / 2, padding.Right, padding.Bottom + extra - extra / 2);
						// One line of text is as tall as its font; Windows measures a line a pixel or so taller.
						var lines = TextRenderer.MeasureText(button.Text, button.Font).Height;
						var text = lines > LineHeight(button.Font) ? lines : button.Font.Height;
						var held = Math.Max(height, Math.Max(text, button.Image?.Height ?? 0) + (LineWidth() + Pixels(ButtonPadding)) * 2);
						if (minimum.Height < held)
							minimum = new Size(minimum.Width, held);
						if (maximum.Height == 0 || maximum.Height > held)
							maximum = new Size(maximum.Width, held);
					}
					if (button.Padding != padding)
						button.Padding = padding;
					if (button.MinimumSize != minimum)
						button.MinimumSize = minimum;
					if (button.MaximumSize != maximum)
						button.MaximumSize = maximum;
				}
				else
				{
					// Windows draws the text of a system button or check box black.
					button.FlatStyle = IsDark && state.FlatStyle == FlatStyle.System ? FlatStyle.Standard : state.FlatStyle;
					button.FlatAppearance.BorderSize = state.FlatBorderSize;
					button.FlatAppearance.MouseOverBackColor = state.FlatMouseOverBackColor;
					button.FlatAppearance.MouseDownBackColor = state.FlatMouseDownBackColor;
					// The visual style draws a check box or radio button's back light.
					button.UseVisualStyleBackColor = !IsDark && state.UseVisualStyleBackColor;
					if (!(button is CheckBox || button is RadioButton) && button.Padding != state.Padding)
						button.Padding = state.Padding;
					if (button.MinimumSize != state.MinimumSize)
						button.MinimumSize = state.MinimumSize;
					if (button.MaximumSize != state.MaximumSize)
						button.MaximumSize = state.MaximumSize;
				}
				if (!IsWpfButton(button, state) && state.BackColor.IsSystemColor)
				{
					if (state.BackColorSet)
						button.BackColor = state.BackColor;
					else
						button.ResetBackColor();
				}
				// A check box or radio button is drawn by Windows Forms, so it can be drawn again as the WPF programs draw it,
				// with its text moved right to make room for the larger box, and as tall as the box where its text is not.
				button.Paint -= CheckBox_Paint;
				if ((button is CheckBox || button is RadioButton) && !IsWpfButton(button, state))
				{
					var wpf = IsWpfCheckBox(button, state);
					var box = CheckBoxBox(button.Font, button is RadioButton);
					// Never less room than Windows keeps for its own box, which its text follows.
					var extra = wpf ? Math.Max(0, box.Width - Pixels(WindowsCheckBoxSize)) : 0;
					var natural = button.GetPreferredSize(Size.Empty).Height - button.Padding.Vertical + state.Padding.Vertical;
					var taller = wpf ? Math.Max(0, box.Height - natural) : 0;
					var padding = new Padding(state.Padding.Left + extra, state.Padding.Top + taller / 2,
						state.Padding.Right, state.Padding.Bottom + taller - taller / 2);
					if (button.Padding != padding)
						button.Padding = padding;
					button.TextChanged -= CheckBox_TextChanged;
					button.FontChanged -= CheckBox_TextChanged;
					if (wpf)
					{
						if (state.FlatStyle == FlatStyle.System)
							button.FlatStyle = FlatStyle.Standard;
						button.Paint += CheckBox_Paint;
						button.TextChanged += CheckBox_TextChanged;
						button.FontChanged += CheckBox_TextChanged;
						FitCheckBox(button, state);
						button.Invalidate();
					}
					else if (button.AutoSize != state.AutoSize)
						button.AutoSize = state.AutoSize;
				}
				// Windows Forms draws the text of any other disabled button in a shade of its back colour, too dark to read on a
				// dark page, so in the dark theme it is drawn again faded.
				button.Paint -= DisabledText_Paint;
				if (IsDark && !IsWpfButton(button, state) && !IsWpfCheckBox(button, state))
					button.Paint += DisabledText_Paint;
			}
			if (control is ComboBox combo)
			{
				if (state.FlatStyle != FlatStyle.Flat)
					combo.FlatStyle = IsDark ? FlatStyle.Flat : state.FlatStyle;
				var paint = PaintsComboBox(combo);
				SetPaintedHere(combo, state, paint, ComboBox_Paint);
				// The items of the list it opens are drawn here too.
				var drawMode = paint ? DrawMode.OwnerDrawFixed : state.DrawMode;
				if (combo.DrawMode != drawMode)
					combo.DrawMode = drawMode;
				combo.DrawItem -= ComboBox_DrawItem;
				if (paint)
					combo.DrawItem += ComboBox_DrawItem;
				FitComboBox(combo, EventArgs.Empty);
				// Painted here, the list shows the mouse and the choice itself.
				combo.MouseEnter -= Repaint;
				combo.MouseLeave -= Repaint;
				combo.SelectedIndexChanged -= Repaint;
				combo.DropDownClosed -= Repaint;
				combo.GotFocus -= Repaint;
				combo.LostFocus -= Repaint;
				combo.FontChanged -= FitComboBox;
				// Given its items later, from a list, it is fitted to them then.
				combo.DataSourceChanged -= FitComboBox;
				combo.DataSourceChanged += FitComboBox;
				if (paint)
				{
					combo.MouseEnter += Repaint;
					combo.MouseLeave += Repaint;
					combo.SelectedIndexChanged += Repaint;
					combo.DropDownClosed += Repaint;
					combo.GotFocus += Repaint;
					combo.LostFocus += Repaint;
					combo.FontChanged += FitComboBox;
				}
				// Made editable, or a list again, it is drawn in its new shape.
				combo.DropDown -= ComboBox_DropDown;
				if (paint)
					combo.DropDown += ComboBox_DropDown;
				combo.DropDownStyleChanged -= ComboBox_DropDownStyleChanged;
				combo.DropDownStyleChanged += ComboBox_DropDownStyleChanged;
			}
			if (control is GroupBox group)
			{
				group.Paint -= GroupBox_Paint;
				if (PaintsGroupBox(group))
					group.Paint += GroupBox_Paint;
				// Default.xaml's caption stands lower than Windows Forms puts it: what a layout places on the box moves
				// down with it. A box whose padding the program left to Windows Forms takes Default.xaml's: its content the
				// room inside the frame's two lines at the sides and below, and the room below the caption.
				var own = state.Padding;
				var room = Pixels(GroupBoxRoom);
				var side = LineWidth() * 2 + room;
				var padding = !PaintsGroupBox(group) ? own
					: state.PaddingSet ? new Padding(own.Left, own.Top + Pixels(GroupBoxCaptionTop), own.Right, own.Bottom)
					: new Padding(side, Pixels(GroupBoxCaptionTop) + room, side, side);
				if (group.Padding != padding)
					group.Padding = padding;
				group.Invalidate();
			}
			// A page of a strip painted here is filled plain, as the WPF programs fill one, and takes the shape of the frame round
			// it whatever its size.
			if (control is TabPage page)
			{
				page.UseVisualStyleBackColor = state.UseVisualStyleBackColor && !PaintsTabs(page.Parent);
				page.SizeChanged -= TabPage_SizeChanged;
				page.SizeChanged += TabPage_SizeChanged;
				ClipTabPage(page);
			}
			if (control is DataGridView grid)
			{
				var wpf = PaintsGrid(grid, state);
				grid.EnableHeadersVisualStyles = !IsDark && !wpf && state.EnableHeadersVisualStyles;
				// Cells draw their buttons in the light visual style; flat, they take the theme colours.
				foreach (var pair in state.ButtonColumns)
					pair.Key.FlatStyle = IsDark ? FlatStyle.Flat : pair.Value;
				// A link in a cell is in the theme's link colour, as a link label is; on a selected row, in the selection's text colour.
				foreach (var pair in state.LinkColumns)
				{
					pair.Key.LinkColor = GetColor("ForegroundLink", pair.Value[0]);
					pair.Key.ActiveLinkColor = GetColor("ForegroundLink", pair.Value[1]);
					pair.Key.VisitedLinkColor = GetColor("ForegroundLink", pair.Value[2]);
				}
				grid.RowStateChanged -= LinkGrid_RowStateChanged;
				grid.CellStateChanged -= LinkGrid_CellStateChanged;
				if (state.LinkColumns.Count > 0)
				{
					grid.RowStateChanged += LinkGrid_RowStateChanged;
					grid.CellStateChanged += LinkGrid_CellStateChanged;
					foreach (DataGridViewCell cell in grid.SelectedCells)
						ColorSelectedLink(cell);
				}
				ApplyGridLook(grid, state, wpf);
				grid.CellPainting -= CheckCell_Painting;
				grid.CellPainting -= WpfGrid_CellPainting;
				grid.Paint -= WpfGrid_Paint;
				grid.CellMouseEnter -= GridCheckCell_Repaint;
				grid.CellMouseLeave -= GridCheckCell_Repaint;
				grid.ColumnAdded -= Grid_ColumnsChanged;
				grid.ColumnRemoved -= Grid_ColumnsChanged;
				grid.ColumnDisplayIndexChanged -= Grid_ColumnsChanged;
				grid.ColumnStateChanged -= Grid_ColumnsChanged;
				grid.DataBindingComplete -= Grid_ColumnsChanged;
				if (wpf)
				{
					grid.CellPainting += WpfGrid_CellPainting;
					grid.Paint += WpfGrid_Paint;
					grid.CellMouseEnter += GridCheckCell_Repaint;
					grid.CellMouseLeave += GridCheckCell_Repaint;
					grid.ColumnAdded += Grid_ColumnsChanged;
					grid.ColumnRemoved += Grid_ColumnsChanged;
					grid.ColumnDisplayIndexChanged += Grid_ColumnsChanged;
					grid.ColumnStateChanged += Grid_ColumnsChanged;
					grid.DataBindingComplete += Grid_ColumnsChanged;
				}
				else if (IsDark && HasDarkCheckBox())
					grid.CellPainting += CheckCell_Painting;
				grid.Invalidate();
			}
			if (control is TextBox || control is UpDownBase || control is ListBox)
				ApplyFrame(control, state);
			if (control is ProgressBar)
				SetPaintedHere(control, state, PaintsProgress(control), Progress_Paint);
			// A link is in the theme's link colour in the dark theme, pressed or visited too, and in its own in the light one.
			if (control is LinkLabel link)
			{
				link.LinkColor = GetColor("ForegroundLink", state.LinkColor);
				link.ActiveLinkColor = GetColor("ForegroundLink", state.ActiveLinkColor);
				link.VisitedLinkColor = GetColor("ForegroundLink", state.VisitedLinkColor);
			}
			// A label's text is drawn again without the room Windows keeps round it, and a disabled label's at half strength,
			// as the text of every other disabled control: Windows Forms draws it in a shade of its back colour, too dark to
			// read on a dark page and lighter than the theme's other disabled text on a light one.
			if (control is Label label && !(control is LinkLabel))
			{
				label.Paint -= Label_Paint;
				label.Paint += Label_Paint;
				label.FontChanged -= Label_FontChanged;
				label.FontChanged += Label_FontChanged;
				label.TextChanged -= Label_FontChanged;
				label.TextChanged += Label_FontChanged;
				FitLabel(label, state);
			}
			// A tool bar without a renderer of its own is drawn as the WPF programs draw one, in both themes. The system
			// renderer draws a checked button with the Windows colours, so in the dark theme every other strip, a menu
			// or a status bar, is drawn by a professional one. Both are made anew because they keep the colours they
			// worked out.
			if (control is ToolStrip strip && state.RenderMode != ToolStripRenderMode.Custom)
			{
				var wpf = IsWpfToolBar(strip);
				if (wpf)
					strip.Renderer = new WpfToolStripRenderer();
				else if (IsDark)
					strip.Renderer = new DarkToolStripRenderer();
				else
					strip.RenderMode = state.RenderMode;
				// Its text is in the font of what it stands on, as a WPF tool bar's is, where Windows Forms gives every tool
				// bar the menu font, larger than a page's.
				if (!state.FontSet && strip.Parent != null)
				{
					var previous = strip.Font;
					if (!wpf)
						strip.ResetFont();
					else if (!previous.Equals(strip.Parent.Font))
						strip.Font = strip.Parent.Font;
					// A list or box on it keeps a copy of the tool bar's font, so it is given the new one too.
					if (!strip.Font.Equals(previous))
						foreach (var host in strip.Items.OfType<ToolStripControlHost>())
							if (host.Font.Equals(previous))
								host.Font = strip.Font;
				}
				// Its items keep their distance from its edge with their own margins, as a WPF tool bar's do.
				var padding = ToolBarPadding(strip, state, wpf);
				if (strip.Padding != padding)
					strip.Padding = padding;
				strip.Layout -= ToolBar_Layout;
				if (wpf)
					strip.Layout += ToolBar_Layout;
				foreach (ToolStripItem item in strip.Items)
					PadToolBarItem(item, wpf);
			}
			if (control is TabControl tabs && tabs.Alignment == TabAlignment.Top)
			{
				SetPaintedHere(tabs, state, PaintsTabs(tabs), Tabs_Paint);
				// Windows lights the tab under the mouse; painted here, the strip does it itself.
				tabs.MouseMove -= Tabs_MouseMove;
				tabs.MouseLeave -= Tabs_MouseLeave;
				if (PaintsTabs(tabs))
				{
					tabs.MouseMove += Tabs_MouseMove;
					tabs.MouseLeave += Tabs_MouseLeave;
				}
			}
			if (control is TrackBar slider)
			{
				SetPaintedHere(slider, state, !SystemInformation.HighContrast, Track_Paint);
				// Windows repaints a slider where it has the thumb, which is drawn where WPF has it, and its thumb shows
				// the focus.
				slider.ValueChanged -= Repaint;
				slider.GotFocus -= Repaint;
				slider.LostFocus -= Repaint;
				if (!SystemInformation.HighContrast)
				{
					slider.ValueChanged += Repaint;
					slider.GotFocus += Repaint;
					slider.LostFocus += Repaint;
				}
				FitSlider(slider, state);
			}
			if (control is PropertyGrid properties)
				ApplyPropertyGrid(properties, state);
		}

		static readonly MethodInfo SetWindowFontMethod = typeof(Control).GetMethod("SetWindowFont", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Title bar, scrollbars, lists, check boxes, radio buttons, drop-down lists and numeric boxes in the Windows
		/// dark style, on Windows 10 1809 and later.
		/// </summary>
		static void ApplyNative(Control control)
		{
			State state;
			if (!control.IsHandleCreated || !States.TryGetValue(control, out state))
				return;
			// The window of a tab strip says where its pages stand before the strip lays them out again.
			if (control is TabControl || control is TrackBar)
				AttachMouseMap(control, state);
			if (control is TabControl tabs && state.TabsPadded != PaintsTabs(tabs))
				ApplyTabPadding(tabs, state);
			if (control is ComboBox)
				FitComboBox(control, EventArgs.Empty);
			if (HasFrame(control) || state.Frame != null)
				AttachFrame(control, state);
			if (WindowsBuild < 17763)
				return;
			if (control is Form form && form.TopLevel)
				SetTitleBar(form.Handle, IsDark);
			// Windows Forms draws the box of a check box or radio button, and the frame and buttons of a numeric
			// box, with their window's style.
			var spinButtons = control.Parent is UpDownBase && !(control is TextBoxBase);
			var styled = control is TextBoxBase || control is ListBox || control is ListView || control is TreeView
				|| control is ScrollBar || control is CheckBox || control is RadioButton || control is ComboBox
				|| control is UpDownBase || spinButtons
				|| (control is ScrollableControl scrollable && scrollable.AutoScroll);
			if (!styled || state.NativeDark == IsDark)
				return;
			state.NativeDark = IsDark;
			// The Explorer style frames an edit box in white. The file dialog style frames it dark but leaves its
			// scrollbars light; the dark theme style of newer Windows darkens both. The file dialog style also keeps
			// the field of a drop-down list dark when the theme changes after the list was made. Neither has dark
			// buttons for a numeric box; the Explorer style has.
			var edit = !spinButtons && (control is TextBoxBase || control is UpDownBase);
			var style = edit && HasDarkThemeStyle() ? "DarkMode_DarkTheme"
				: edit || control is ComboBox ? "DarkMode_CFD"
				: "DarkMode_Explorer";
			NativeMethods.SetWindowTheme(control.Handle, IsDark ? style : null, null);
		}

		/// <summary>True when Windows has the dark theme style, which gives an edit box a dark frame and dark scrollbars.</summary>
		/// <remarks>
		/// Windows falls back to the light style for a style it does not have, so one of its scrollbar tracks is
		/// drawn by class name and looked at.
		/// </remarks>
		static bool HasDarkThemeStyle()
		{
			if (_HasDarkThemeStyle == null)
			{
				var dark = false;
				if (WindowsBuild >= 17763 && VisualStyleRenderer.IsSupported)
				{
					using (var bitmap = new Bitmap(16, 16))
					{
						using (var g = Graphics.FromImage(bitmap))
							new VisualStyleRenderer("DarkMode_DarkTheme::ScrollBar", SBP_UPPERTRACKVERT, 1)
								.DrawBackground(g, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
						var middle = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
						dark = middle.A > 127 && middle.GetBrightness() < 0.5f;
					}
				}
				_HasDarkThemeStyle = dark;
			}
			return _HasDarkThemeStyle.Value;
		}
		static bool? _HasDarkThemeStyle;

		/// <summary>The buttons of the Windows dark style, named by class so a check box can be drawn in it without a window.</summary>
		const string DarkButtonClass = "DarkMode_Explorer::BUTTON";
		const int BP_CHECKBOX = 3;

		static VisualStyleRenderer _checkRenderer;

		static readonly MethodInfo GetContentBoundsMethod = typeof(DataGridViewCell).GetMethod(nameof(DataGridViewCell.GetContentBounds),
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Graphics), typeof(DataGridViewCellStyle), typeof(int) }, null);

		/// <summary>True when Windows has a dark check box to draw, on Windows 10 1809 and later.</summary>
		static bool HasDarkCheckBox()
		{
			return WindowsBuild >= 17763 && GetContentBoundsMethod != null && VisualStyleRenderer.IsSupported
				&& VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(DarkButtonClass, BP_CHECKBOX, 1));
		}

		/// <summary>
		/// A check box cell draws its box in the light visual style whatever its grid's window style, so in the
		/// dark theme the box is drawn again over it in the Windows dark style, the same as a check box control's.
		/// </summary>
		/// <remarks>
		/// The box is drawn over whatever painted the cell, the grid or a handler of the program. This handler is
		/// added as the theme reaches the grid, after the program made it, so it comes after the program's own.
		/// </remarks>
		static void CheckCell_Painting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.ColumnIndex < 0 || !(grid.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
				|| (e.PaintParts & DataGridViewPaintParts.ContentForeground) == 0)
				return;
			if (!e.Handled)
			{
				e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.ContentForeground);
				e.Handled = true;
			}
			// Where the cell puts the box in the style it is painted with: padding can move it out of sight.
			// The shared row, as the grid paints it, so the row stays shared.
			var cell = grid.Rows.SharedRow(e.RowIndex).Cells[e.ColumnIndex];
			var box = (Rectangle)GetContentBoundsMethod.Invoke(cell, new object[] { e.Graphics, e.CellStyle, e.RowIndex });
			if (box.IsEmpty)
				return;
			box.Offset(e.CellBounds.Location);
			var selected = (e.State & DataGridViewElementStates.Selected) != 0;
			using (var brush = new SolidBrush(selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor))
				e.Graphics.FillRectangle(brush, box);
			var check = e.FormattedValue is CheckState checkState ? checkState
				: e.FormattedValue is bool isChecked && isChecked ? CheckState.Checked : CheckState.Unchecked;
			// Unchecked 1, checked 5, mixed 9; then hot +1, pressed +2, disabled +3. Hot under the mouse or with the focus.
			var state = check == CheckState.Checked ? 5 : check == CheckState.Indeterminate ? 9 : 1;
			var over = box.Contains(grid.PointToClient(Control.MousePosition));
			if (!grid.Enabled)
				state += 3;
			else if (over && (Control.MouseButtons & MouseButtons.Left) != 0)
				state += 2;
			else if (over || HasCellFocus(grid, e.ColumnIndex, e.RowIndex))
				state += 1;
			if (_checkRenderer == null)
				_checkRenderer = new VisualStyleRenderer(DarkButtonClass, BP_CHECKBOX, state);
			else
				_checkRenderer.SetParameters(DarkButtonClass, BP_CHECKBOX, state);
			_checkRenderer.DrawBackground(e.Graphics, box);
		}

		/// <summary>The rounding of a button's corners, a tab's top corners and a tab page's frame, as Default.xaml draws them, in device-independent pixels.</summary>
		const int CornerRadius = 4;

		/// <summary>A size Default.xaml gives in device-independent pixels, in the pixels of the surface drawn on, as WPF scales it.</summary>
		static float Dip(Graphics g, float value)
		{
			return value * g.DpiX / 96f;
		}

		/// <summary>The width of a line Default.xaml draws one device-independent pixel wide, in whole pixels, so it stays sharp.</summary>
		static float Line(Graphics g)
		{
			return Math.Max(1f, (float)Math.Floor(Dip(g, 1)));
		}

		/// <summary>
		/// Smooths the curves drawn on a surface, with whole pixels between whole coordinates, so a straight edge on a whole
		/// coordinate, or a line half its width in from one, stays sharp, as WPF draws them.
		/// </summary>
		/// <returns>The surface's settings before, to restore.</returns>
		static GraphicsState Smooth(Graphics g)
		{
			var saved = g.Save();
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			return saved;
		}

		/// <summary>Whether a button is drawn as the WPF programs draw one: a push button the program left in a Windows style, while high contrast is off.</summary>
		static bool IsWpfButton(ButtonBase button, State state)
		{
			var push = button is Button
				|| (button is CheckBox check && check.Appearance == Appearance.Button)
				|| (button is RadioButton radio && radio.Appearance == Appearance.Button);
			return push && state.FlatStyle != FlatStyle.Flat && state.FlatStyle != FlatStyle.Popup && !SystemInformation.HighContrast;
		}

		/// <summary>
		/// Whether a check box or radio button is drawn as the WPF programs draw one: a box before its text, left to right and
		/// without an image, the program left in a Windows style, while high contrast is off.
		/// </summary>
		static bool IsWpfCheckBox(ButtonBase button, State state)
		{
			var check = button as CheckBox;
			var radio = button as RadioButton;
			if (check == null && radio == null)
				return false;
			var appearance = check != null ? check.Appearance : radio.Appearance;
			var align = check != null ? check.CheckAlign : radio.CheckAlign;
			return appearance == Appearance.Normal && align == System.Drawing.ContentAlignment.MiddleLeft
				&& button.RightToLeft != RightToLeft.Yes && button.Image == null && button.BackgroundImage == null
				&& (state.FlatStyle == FlatStyle.Standard || state.FlatStyle == FlatStyle.System) && !SystemInformation.HighContrast;
		}

		/// <summary>
		/// Sizes a check box or radio button drawn here that sizes itself to its content's layout box, as WPF lays one out:
		/// as tall as its box or its line of text, whichever is taller, and as wide as its box, the gap and its text, with no
		/// room of Windows' own round them, so its margins are the gaps round it. A program's own smallest size is kept.
		/// </summary>
		static void FitCheckBox(ButtonBase button, State state)
		{
			if (!state.AutoSize)
				return;
			var box = CheckBoxBox(button.Font, button is RadioButton);
			var flags = TextFormatFlags.NoPadding | (button.UseMnemonic ? TextFormatFlags.Default : TextFormatFlags.NoPrefix);
			var text = button.Text.Length == 0 ? Size.Empty : MeasureDrawnText(button.Text, button.Font, flags);
			// One line of text is as tall as its font; Windows measures a line a pixel or so taller.
			var line = text.Height > LineHeight(button.Font) ? text.Height : button.Text.Length == 0 ? 0 : button.Font.Height;
			var width = state.Padding.Horizontal + box.Width + (text.Width > 0 ? Pixels(CheckBoxTextGap) + text.Width : 0);
			var size = new Size(Math.Max(width, state.MinimumSize.Width), Math.Max(state.Padding.Vertical + Math.Max(box.Height, line), state.MinimumSize.Height));
			// A layout measures a control by the size Windows Forms works out for it: the theme sizes it instead.
			if (button.AutoSize)
				button.AutoSize = false;
			if (button.Size != size)
				button.Size = size;
		}

		/// <summary>
		/// Where a check box's or radio button's text starts, from its own left edge, as it is drawn now: by the theme, after
		/// its padding, Default.xaml's box and the room after the box; or by Windows, the width Windows Forms works out for it
		/// less its text's.
		/// </summary>
		public static int GetCheckBoxTextLeft(ButtonBase button)
		{
			State state;
			if (States.TryGetValue(button, out state) && IsWpfCheckBox(button, state))
				return state.Padding.Left + CheckBoxBox(button.Font, button is RadioButton).Width + Pixels(CheckBoxTextGap);
			var text = button.Text.Length == 0 ? 0 : TextRenderer.MeasureText(button.Text, button.Font).Width;
			return button.GetPreferredSize(Size.Empty).Width - button.Padding.Right - text;
		}

		/// <summary>
		/// Keeps a note under a check box or radio button with its text under the check box's text, at every font and scale
		/// the box is drawn at: the note's left margin is the check box's and the room before the check box's text.
		/// </summary>
		/// <remarks>The theme draws a note's text from its edge, as every label's.</remarks>
		public static void IndentUnder(Control note, ButtonBase check)
		{
			EventHandler indent = (sender, e) =>
			{
				var left = check.Margin.Left + GetCheckBoxTextLeft(check);
				if (note.Margin.Left != left)
					note.Margin = new Padding(left, note.Margin.Top, note.Margin.Right, note.Margin.Bottom);
			};
			// The theme sizes a check box again whenever its font changes, which is when its box changes.
			check.SizeChanged += indent;
			check.MarginChanged += indent;
			indent(check, EventArgs.Empty);
		}

		/// <summary>Sizes a check box or radio button drawn here to its new text or font.</summary>
		static void CheckBox_TextChanged(object sender, EventArgs e)
		{
			State state;
			var button = (ButtonBase)sender;
			if (States.TryGetValue(button, out state) && IsWpfCheckBox(button, state))
				FitCheckBox(button, state);
		}

		/// <summary>The size of the box Windows draws a check box with, in device-independent pixels.</summary>
		const int WindowsCheckBoxSize = 13;

		/// <summary>
		/// The size of Default.xaml's check box, in device-independent pixels: its tick, 8 wide, with 4 on every side for its
		/// edge and the room round the tick.
		/// </summary>
		static readonly SizeF CheckBoxSize = new SizeF(16f, 16f);

		/// <summary>
		/// The height of a line of Default.xaml's text, in device-independent pixels: Segoe UI at 12, which WPF uses unless
		/// told otherwise. Default.xaml's check box is as tall as it, within a twentieth of a pixel.
		/// </summary>
		const float WpfLineHeight = 15.96f;

		/// <summary>
		/// How much larger a font's text is than Default.xaml's, by which a check box or radio button is drawn larger or
		/// smaller, so it stands to the text beside it as Default.xaml's does to its own.
		/// </summary>
		static float CheckScale(Font font)
		{
			return font.Height / (WpfLineHeight * ControlsHelper.DpiScale);
		}

		/// <summary>The size of Default.xaml's check box, or of its radio button, beside text in a font, in whole pixels.</summary>
		static Size CheckBoxBox(Font font, bool round)
		{
			var size = round ? new SizeF(RadioButtonSize, RadioButtonSize) : CheckBoxSize;
			var scale = CheckScale(font);
			return new Size(Pixels(size.Width * scale), Pixels(size.Height * scale));
		}

		/// <summary>A size in device-independent pixels, in whole pixels of this program's windows.</summary>
		static int Pixels(float value)
		{
			return (int)Math.Round(value * ControlsHelper.DpiScale);
		}

		/// <summary>The rounding of a check box's corners in Default.xaml, in device-independent pixels.</summary>
		const int CheckBoxRadius = 4;

		/// <summary>
		/// The tick of Default.xaml's check box: its outline, as wide as its 8 by 8 room and 7.3 high, keeping its shape, in
		/// device-independent pixels.
		/// </summary>
		static readonly PointF[] CheckMark =
		{
			new PointF(8f, 0.9812f), new PointF(3.768f, 7.2969f), new PointF(3.6264f, 7.2969f), new PointF(0f, 4.165f),
			new PointF(1.0239f, 2.8244f), new PointF(3.4125f, 4.8829f), new PointF(6.6834f, 0f),
		};

		/// <summary>
		/// The room between Default.xaml's check box and its text, in device-independent pixels: the box's margin, the room
		/// Windows leaves before the text of its own check box, which the width Windows Forms gives a check box that sizes
		/// itself counts.
		/// </summary>
		const int CheckBoxTextGap = 4;

		/// <summary>The size of Default.xaml's radio button box, in device-independent pixels, and of its dot.</summary>
		const int RadioButtonSize = 16;
		const int RadioButtonDot = 8;

		/// <summary>
		/// Draws a check box or radio button as Default.xaml does, over what Windows Forms drew: from where the program's
		/// padding ends, its box, then its text. Disabled, WPF draws the whole control at half strength and its box at half
		/// that again.
		/// </summary>
		static void CheckBox_Paint(object sender, PaintEventArgs e)
		{
			var button = (ButtonBase)sender;
			State state;
			if (!States.TryGetValue(button, out state))
				return;
			var g = e.Graphics;
			var client = button.ClientRectangle;
			if (button.BackColor.A < 255)
				ButtonRenderer.DrawParentBackground(g, client, button);
			using (var brush = new SolidBrush(button.BackColor))
				g.FillRectangle(brush, client);
			var radio = button as RadioButton;
			var size = CheckBoxBox(button.Font, radio != null);
			// A control the program made shorter than Default.xaml's box gets a box as tall as itself.
			var height = Math.Min(size.Height, client.Height);
			var box = new Rectangle(state.Padding.Left, (client.Height - height) / 2, size.Width, height);
			var hot = button.Enabled && (button.Focused || client.Contains(button.PointToClient(Control.MousePosition)));
			var check = radio != null ? (radio.Checked ? CheckState.Checked : CheckState.Unchecked) : ((CheckBox)button).CheckState;
			DrawWpfCheck(g, box, CheckScale(button.Font), check, radio != null, hot, button.Enabled);
			var text = Rectangle.FromLTRB(box.Right + Pixels(CheckBoxTextGap), button.Padding.Top,
				client.Width - button.Padding.Right, client.Height - button.Padding.Bottom);
			TextRenderer.DrawText(g, button.Text, button.Font, text, button.Enabled ? button.ForeColor : DisabledText(),
				TextFlags(button) | TextFormatFlags.NoPadding);
			if (button.Focused && (bool)(ShowFocusCuesProperty?.GetValue(button, null) ?? false))
				DrawFocus(g, client, button.ForeColor);
		}

		/// <summary>
		/// Draws Default.xaml's check box: a rounded box in the field colour, with its tick, or a square while it is neither on
		/// nor off; or its radio button, round, with a dot. Its edge is a box to type in's: under the mouse or with the focus,
		/// the selection colour; at rest, the field edge colour while it is ticked, chosen or neither on nor off, and the check
		/// box edge colour while it is not. Disabled, it is drawn at a quarter strength. Its rounding, tick and dot are drawn
		/// larger by the scale of the text beside it.
		/// </summary>
		static void DrawWpfCheck(Graphics g, Rectangle box, float size, CheckState check, bool round, bool hot, bool enabled)
		{
			var alpha = enabled ? 255 : 64;
			var saved = Smooth(g);
			var line = Line(g);
			var radius = round ? box.Height / 2f : Dip(g, CheckBoxRadius) * size;
			var edge = hot ? "BackgroundDarkSelected" : check != CheckState.Unchecked ? "BorderField" : "BorderCheck";
			using (var face = RoundedRectangle(Inset(box, line / 2), radius, radius, radius, radius))
			{
				using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("BackgroundWhite", SystemColors.Window))))
					g.FillPath(brush, face);
				using (var pen = new Pen(Color.FromArgb(alpha, Themed(edge, SystemColors.ControlLight)), line))
					g.DrawPath(pen, face);
			}
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("ForegroundBlack", SystemColors.ControlText))))
			{
				var scale = Dip(g, 1) * size;
				if (round)
				{
					if (check == CheckState.Checked)
					{
						var dot = RadioButtonDot * scale;
						g.FillEllipse(brush, box.X + (box.Width - dot) / 2, box.Y + (box.Height - dot) / 2, dot, dot);
					}
				}
				else
				{
					// The tick's 8 by 8 room, or the square, stands 4 in from every side of the box, as in Default.xaml, and keeps
					// to the middle of the box's height, also of a box made shorter; the tick is at the top of its room.
					var inset = 4 * scale;
					var top = box.Y + (box.Height - 8 * scale) / 2;
					if (check == CheckState.Checked)
						g.FillPolygon(brush, CheckMark.Select(p => new PointF(box.X + inset + p.X * scale, top + p.Y * scale)).ToArray());
					else if (check == CheckState.Indeterminate)
						g.FillRectangle(brush, Inset(box, inset));
				}
			}
			g.Restore(saved);
		}

		/// <summary>
		/// Draws the text of a disabled button the theme does not draw itself again, faded, where Windows Forms put it, over the
		/// darker copy Windows Forms drew there and the lighter one a pixel right and down.
		/// </summary>
		static void DisabledText_Paint(object sender, PaintEventArgs e)
		{
			var button = (ButtonBase)sender;
			Rectangle text, image;
			bool shifted;
			if (button.Enabled || button.Text.Length == 0 || !GetButtonLayout(button, e, out text, out image, out shifted))
				return;
			var behind = button.BackColor.A == 255 ? button.BackColor : button.Parent?.BackColor ?? SystemColors.Control;
			using (var brush = new SolidBrush(behind))
				e.Graphics.FillRectangle(brush, Rectangle.Inflate(text, 1, 1));
			DrawButtonText(e.Graphics, button, text, DisabledText(), TextFlags(button));
		}

		static readonly PropertyInfo DefaultPaddingProperty = typeof(Control).GetProperty("DefaultPadding", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Whether the program gave a control a padding of its own: one other than Windows Forms' default for it, as made or as
		/// Windows Forms scaled it to the screen, which it does to every padding of a window made at another scale.
		/// </summary>
		static bool PaddingSet(Control control)
		{
			var own = control.Padding;
			var standard = (Padding)(DefaultPaddingProperty?.GetValue(control, null) ?? Padding.Empty);
			Func<int, int, bool> scaled = (value, made) => value == made
				|| value == (int)Math.Floor(made * ControlsHelper.DpiScale) || value == (int)Math.Ceiling(made * ControlsHelper.DpiScale);
			return !(scaled(own.Left, standard.Left) && scaled(own.Top, standard.Top) && scaled(own.Right, standard.Right) && scaled(own.Bottom, standard.Bottom));
		}

		/// <summary>
		/// Sizes a label that sizes itself to its text's layout box, as a WPF TextBlock is: Windows measures and draws a label's
		/// text with room of its own at either side, which the label then has no more, so its margins are the gaps beside
		/// it; <see cref="Label_Paint"/> draws its text without that room. A label whose width the program limits, to wrap its
		/// text, that a layout stretches or that a table may wrap keeps Windows' sizing; the smallest size the program gives
		/// it is kept.
		/// </summary>
		static void FitLabel(Label label, State state)
		{
			// A layout measures a label by the size Windows Forms works out for it, before its smallest and largest sizes:
			// the theme sizes it instead, and Windows Forms keeps the size it is given.
			if (state.AutoSize && state.MaximumSize.Width == 0 && DrawsLabelText(label) && !Stretched(label) && !MayWrap(label))
			{
				// Only the room at either side is taken away: the height stays Windows Forms' own, its line and padding.
				var size = new Size(Math.Max(label.MinimumSize.Width, DrawnLabelWidth(label, label.Text)),
					Math.Max(label.MinimumSize.Height, label.GetPreferredSize(Size.Empty).Height));
				if (label.AutoSize)
					label.AutoSize = false;
				if (label.Size != size)
					label.Size = size;
			}
			else if (label.AutoSize != state.AutoSize)
				label.AutoSize = state.AutoSize;
		}

		/// <summary>
		/// The width a label needs to show a text on one line, its own when none is given, as the label is drawn now: by the
		/// theme, without the room Windows keeps at either side of its text, with the label's padding and edge; or by Windows,
		/// its own text as Windows Forms sizes the label, which keeps its smallest size, and another as Windows measures text
		/// for a label it draws.
		/// </summary>
		/// <remarks>Windows Forms measures a label with Windows' room, so a page that sizes something to a label's text asks here.</remarks>
		public static int GetLabelWidth(Label label, string text = null)
		{
			if (States.TryGetValue(label, out _) && DrawsLabelText(label))
				return DrawnLabelWidth(label, text ?? label.Text);
			if (text == null || text == label.Text)
				return label.GetPreferredSize(Size.Empty).Width;
			var flags = (TextFormatFlags)(LabelTextFlagsMethod?.Invoke(label, null) ?? TextFormatFlags.Default);
			return TextRenderer.MeasureText(text, label.Font, Size.Empty, flags).Width + label.Padding.Horizontal + LabelEdge(label);
		}

		/// <summary>
		/// Makes labels as wide as the widest of them, as each is drawn (<see cref="GetLabelWidth"/>), with their margins, so
		/// what stands after them in their tables starts at one place: given to each as its smallest width, so a table that
		/// sizes their column by its labels never measures one itself, with Windows' room round its text, and wraps it.
		/// </summary>
		/// <remarks>Lay the tables out once, after: each label changes the width of its column.</remarks>
		public static void ShareLabelWidth(IEnumerable<Label> labels)
		{
			var shared = labels.ToArray();
			if (shared.Length == 0)
				return;
			// Each measured on its own: Windows Forms sizes a label it draws to at least its smallest width.
			foreach (var label in shared)
				label.MinimumSize = Size.Empty;
			var widest = shared.Max(label => GetLabelWidth(label) + label.Margin.Horizontal);
			foreach (var label in shared)
			{
				label.MinimumSize = new Size(widest - label.Margin.Horizontal, 0);
				// A label the theme sized keeps the width it was given until it is sized again, so it is given this one.
				if (!label.AutoSize)
					label.Width = label.MinimumSize.Width;
			}
		}

		/// <summary>The width of a label the theme draws, for a text on one line: the text's, as drawn, with its padding and edge.</summary>
		static int DrawnLabelWidth(Label label, string text)
		{
			return MeasureDrawnText(text, label.Font, LabelTextFlags(label)).Width + label.Padding.Horizontal + LabelEdge(label);
		}

		/// <summary>
		/// The width of a label's edge at both sides, from its style: while its font changes, the size of its inside is not yet
		/// its size's.
		/// </summary>
		static int LabelEdge(Label label)
		{
			return label.BorderStyle == BorderStyle.FixedSingle ? SystemInformation.BorderSize.Width * 2
				: label.BorderStyle == BorderStyle.Fixed3D ? SystemInformation.Border3DSize.Width * 2 : 0;
		}

		/// <summary>Whether a layout stretches a control: docked, or anchored to both sides across or down.</summary>
		static bool Stretched(Control control)
		{
			const AnchorStyles across = AnchorStyles.Left | AnchorStyles.Right;
			const AnchorStyles down = AnchorStyles.Top | AnchorStyles.Bottom;
			return control.Dock != DockStyle.None || (control.Anchor & across) == across || (control.Anchor & down) == down;
		}

		/// <summary>
		/// Whether a table may wrap a label's text: it measures a label that sizes itself at the width the label's place has,
		/// when that place is a column not sized by its content, or several columns.
		/// </summary>
		static bool MayWrap(Label label)
		{
			var table = label.Parent as TableLayoutPanel;
			if (table == null)
				return false;
			var place = table.GetPositionFromControl(label);
			return table.GetColumnSpan(label) > 1
				|| (place.Column >= 0 && place.Column < table.ColumnStyles.Count && table.ColumnStyles[place.Column].SizeType != SizeType.AutoSize);
		}

		/// <summary>Sizes a label to its text's layout box again for its new text or font.</summary>
		static void Label_FontChanged(object sender, EventArgs e)
		{
			State state;
			if (States.TryGetValue((Control)sender, out state))
				FitLabel((Label)sender, state);
		}

		static readonly MethodInfo LabelTextFlagsMethod = typeof(Label).GetMethod("CreateTextFormatFlags", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

		/// <summary>Whether the theme draws a label's text: one with no picture, drawing its text the newer way, with GDI.</summary>
		static bool DrawsLabelText(Label label)
		{
			return label.Image == null && label.BackgroundImage == null && !label.UseCompatibleTextRendering && LabelTextFlagsMethod != null;
		}

		/// <summary>The flags Windows Forms draws a label's text with, without the room Windows keeps at either side of it.</summary>
		static TextFormatFlags LabelTextFlags(Label label)
		{
			return (TextFormatFlags)LabelTextFlagsMethod.Invoke(label, null) | TextFormatFlags.NoPadding;
		}

		/// <summary>
		/// Draws a label's text again where Windows Forms puts it, with its flags, but without the room Windows keeps at either
		/// side of it, so it starts where the label does; a disabled label's in the colour of every disabled text. A label with
		/// a picture, or one drawing its text the older way, is left as it is.
		/// </summary>
		static void Label_Paint(object sender, PaintEventArgs e)
		{
			var label = (Label)sender;
			if (label.Text.Length == 0 || !DrawsLabelText(label))
				return;
			var g = e.Graphics;
			var client = label.ClientRectangle;
			var face = new Rectangle(client.X + label.Padding.Left, client.Y + label.Padding.Top,
				client.Width - label.Padding.Horizontal, client.Height - label.Padding.Vertical);
			if (label.BackColor.A < 255)
				ButtonRenderer.DrawParentBackground(g, face, label);
			using (var brush = new SolidBrush(label.BackColor))
				g.FillRectangle(brush, face);
			TextRenderer.DrawText(g, label.Text, label.Font, face, label.Enabled ? label.ForeColor : DisabledText(), LabelTextFlags(label));
		}

		/// <summary>A colour at half strength over the one behind it, as WPF fades a disabled control's face.</summary>
		static Color Half(Color color, Color behind)
		{
			return Color.FromArgb((color.R + behind.R) / 2, (color.G + behind.G) / 2, (color.B + behind.B) / 2);
		}

		/// <summary>
		/// The colour of the text of every disabled control, in both toolkits: a disabled label's, the text colour at half
		/// strength over the page (ForegroundDisabled).
		/// </summary>
		static Color DisabledText()
		{
			return Themed("ForegroundDisabled", SystemColors.GrayText);
		}

		const System.Drawing.ContentAlignment AnyCenter = System.Drawing.ContentAlignment.TopCenter
			| System.Drawing.ContentAlignment.MiddleCenter | System.Drawing.ContentAlignment.BottomCenter;
		const System.Drawing.ContentAlignment AnyRight = System.Drawing.ContentAlignment.TopRight
			| System.Drawing.ContentAlignment.MiddleRight | System.Drawing.ContentAlignment.BottomRight;
		const System.Drawing.ContentAlignment AnyTop = System.Drawing.ContentAlignment.TopLeft
			| System.Drawing.ContentAlignment.TopCenter | System.Drawing.ContentAlignment.TopRight;
		const System.Drawing.ContentAlignment AnyBottom = System.Drawing.ContentAlignment.BottomLeft
			| System.Drawing.ContentAlignment.BottomCenter | System.Drawing.ContentAlignment.BottomRight;

		static readonly PropertyInfo ShowKeyboardCuesProperty = typeof(Control).GetProperty("ShowKeyboardCues", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// The flags Windows Forms draws a button's text with: placed as the button aligns it, broken into lines, its
		/// ampersands shown, underlined or hidden as the button and the keyboard ask.
		/// </summary>
		static TextFormatFlags TextFlags(ButtonBase button)
		{
			var align = button.TextAlign;
			// Right to left, the sides swap.
			var rtl = button.RightToLeft == RightToLeft.Yes;
			var right = (align & AnyRight) != 0;
			var left = (align & (AnyCenter | AnyRight)) == 0;
			var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
			flags |= (align & AnyCenter) != 0 ? TextFormatFlags.HorizontalCenter
				: (rtl ? left : right) ? TextFormatFlags.Right : TextFormatFlags.Left;
			flags |= (align & AnyTop) != 0 ? TextFormatFlags.Top : (align & AnyBottom) != 0 ? TextFormatFlags.Bottom : TextFormatFlags.VerticalCenter;
			if (button.AutoEllipsis)
				flags |= TextFormatFlags.EndEllipsis;
			if (rtl)
				flags |= TextFormatFlags.RightToLeft;
			if (!button.UseMnemonic)
				flags |= TextFormatFlags.NoPrefix;
			else if (!(bool)(ShowKeyboardCuesProperty?.GetValue(button, null) ?? true))
				flags |= TextFormatFlags.HidePrefix;
			return flags;
		}

		/// <summary>The method of Windows Forms' button drawing that makes the GDI+ format a button's text is measured and drawn in.</summary>
		static readonly MethodInfo CreateStringFormatMethod = typeof(ButtonBase).GetProperty("Adapter", BindingFlags.Instance | BindingFlags.NonPublic)?.PropertyType
			.GetMethod("CreateStringFormat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

		/// <summary>
		/// Draws a button's text in the colour given where Windows Forms lays it out and as Windows Forms draws it: with GDI+ for a
		/// button that measures its text with GDI+ (<see cref="ButtonBase.UseCompatibleTextRendering"/>), whose layout has room for
		/// that text and not for GDI's, which is wider and would put the last letter on a line of its own, out of sight.
		/// </summary>
		static void DrawButtonText(Graphics g, ButtonBase button, Rectangle text, Color color, TextFormatFlags flags)
		{
			var format = button.UseCompatibleTextRendering ? CreateStringFormatMethod?.Invoke(AdapterProperty.GetValue(button, null), null) as StringFormat : null;
			if (format == null)
			{
				TextRenderer.DrawText(g, button.Text, button.Font, text, color, flags);
				return;
			}
			// As Windows Forms draws it: GDI+ puts text a pixel right of where it is asked to, and a pixel short.
			if ((button.TextAlign & AnyCenter) == 0)
				text.X -= 1;
			text.Width += 1;
			var saved = g.Save();
			g.SmoothingMode = SmoothingMode.Default;
			g.PixelOffsetMode = PixelOffsetMode.Default;
			using (format)
			using (var brush = new SolidBrush(color))
				g.DrawString(button.Text, button.Font, brush, text, format);
			g.Restore(saved);
		}

		/// <summary>Default.xaml's focus mark: a dotted line round the control, its corners rounded, in the colour of its text.</summary>
		static void DrawFocus(Graphics g, Rectangle bounds, Color color)
		{
			var saved = Smooth(g);
			var line = Line(g);
			var radius = Dip(g, CornerRadius);
			using (var pen = new Pen(color, line) { DashPattern = new[] { 1f, 2f } })
			using (var path = RoundedRectangle(Inset(bounds, line / 2), radius, radius, radius, radius))
				g.DrawPath(pen, path);
			g.Restore(saved);
		}

		/// <summary>Whether a strip is a tool bar drawn as the WPF programs draw one: not a menu, a status bar or a drop-down, while high contrast is off.</summary>
		static bool IsWpfToolBar(ToolStrip strip)
		{
			return !(strip is MenuStrip || strip is StatusStrip || strip is ToolStripDropDown) && !SystemInformation.HighContrast;
		}

		/// <summary>Whether a control is drawn as the WPF programs draw it, which the light theme does too; a label for its disabled text.</summary>
		static bool DrawnAsWpf(Control control)
		{
			State state;
			if (control is ButtonBase button && States.TryGetValue(control, out state))
				return IsWpfButton(button, state) || IsWpfCheckBox(button, state);
			if (control is DataGridView grid && States.TryGetValue(control, out state))
				return PaintsGrid(grid, state);
			return PaintsTabs(control)
				|| (control is TabPage && PaintsTabs(control.Parent))
				|| (control is ToolStrip strip && IsWpfToolBar(strip))
				|| PaintsGroupBox(control)
				|| PaintsComboBox(control)
				|| PaintsProgress(control)
				|| (control is TrackBar && !SystemInformation.HighContrast)
				|| HasFrame(control)
				|| ItemRoom(control as ListBox) > 0
				|| (control is Label && !(control is LinkLabel));
		}

		/// <summary>
		/// Rounds a push button's corners into what is behind it, smoothly, and outlines it in the accent colour while it
		/// has the focus, as the WPF programs draw one.
		/// </summary>
		static void Button_Paint(object sender, PaintEventArgs e)
		{
			var button = (ButtonBase)sender;
			var g = e.Graphics;
			var saved = Smooth(g);
			var bounds = new Rectangle(0, 0, button.Width, button.Height);
			// A side another button stands against, as the halves of a split button do, keeps its corners square.
			bool joinedLeft, joinedRight;
			JoinedSides(button, out joinedLeft, out joinedRight);
			var radius = Dip(g, CornerRadius);
			var left = joinedLeft ? 0 : radius;
			var right = joinedRight ? 0 : radius;
			// The corners: the whole button less its rounded face, filled with the colour behind it.
			var behind = button.Parent?.BackColor ?? SystemColors.Control;
			using (var corners = new GraphicsPath(FillMode.Alternate))
			using (var face = RoundedRectangle(bounds, left, right, right, left))
			using (var brush = new SolidBrush(behind))
			{
				corners.AddRectangle(bounds);
				corners.AddPath(face, false);
				g.FillPath(brush, corners);
			}
			// Windows Forms draws a disabled button's text in a shade of its fill, too dark to read on a dark one, so the
			// face is drawn again at half strength, as WPF draws a disabled control, with the text where the layout puts it.
			// A button that opens a list or a menu shows a drop-down list's triangle in the middle in place of its text.
			// Windows Forms centres a button's image on the height of its text, and draws it a pixel right of and below that
			// place, as .NET Framework 1.1 did; the image goes in the middle of the face's height instead, drawn disabled with
			// the face, or enabled over the face's colour where it was, with the text again where that covered it.
			Rectangle text, image;
			bool shifted;
			var picture = button.Image;
			var arrow = OpensList(button);
			if (!button.Enabled && GetButtonLayout(button, e, out text, out image, out shifted))
			{
				using (var brush = new SolidBrush(Half(button.BackColor, behind)))
				using (var face = RoundedRectangle(bounds, left, right, right, left))
					g.FillPath(brush, face);
				var place = image;
				var shown = CentrePicture(picture, button.ImageAlign, button.TextImageRelation, button.Text.Length > 0, bounds.Height, ref place);
				if (shown != null && !place.IsEmpty)
					using (var attributes = new ImageAttributes())
					{
						attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.5f });
						g.DrawImage(shown, place, 0, 0, place.Width, place.Height, GraphicsUnit.Pixel, attributes);
					}
				if (arrow)
					DrawDropDownArrow(g, bounds.Width / 2f, bounds.Height / 2f, DisabledText());
				else
					DrawButtonText(g, button, text, DisabledText(), TextFlags(button));
			}
			else if (button.Enabled && arrow && GetButtonLayout(button, e, out text, out image, out shifted))
			{
				// The text a pixel round, as a pressed button's text is drawn a pixel right of and below it.
				using (var brush = new SolidBrush(FaceColor(button)))
					g.FillRectangle(brush, Rectangle.Inflate(text, 1, 1));
				DrawDropDownArrow(g, bounds.Width / 2f, bounds.Height / 2f, button.ForeColor);
			}
			else if (button.Enabled && picture != null && CheckStateOf(button) == CheckState.Unchecked
				&& GetButtonLayout(button, e, out text, out image, out shifted))
			{
				var drawn = shifted ? new Rectangle(image.X + 1, image.Y + 1, image.Width, image.Height) : image;
				var place = image;
				var shown = CentrePicture(picture, button.ImageAlign, button.TextImageRelation, button.Text.Length > 0, bounds.Height, ref place);
				if (place != drawn || shown != picture)
				{
					using (var brush = new SolidBrush(FaceColor(button)))
						g.FillRectangle(brush, drawn);
					g.DrawImage(shown, place, 0, 0, place.Width, place.Height, GraphicsUnit.Pixel);
					var clip = g.Clip;
					g.SetClip(drawn, CombineMode.Intersect);
					DrawButtonText(g, button, text, button.ForeColor, TextFlags(button) | TextFormatFlags.PreserveGraphicsClipping);
					g.Clip = clip;
				}
			}
			if (button.Focused && (bool)(ShowFocusCuesProperty?.GetValue(button, null) ?? false))
				using (var pen = new Pen(Themed("SelectedBackground", SystemColors.Highlight)))
				using (var outline = RoundedRectangle(new RectangleF(0.5f, 0.5f, button.Width - 1, button.Height - 1), left, right, right, left))
					g.DrawPath(pen, outline);
			g.Restore(saved);
		}

		/// <summary>The colour an enabled push button's face is filled with: pressed, under the mouse, or at rest.</summary>
		static Color FaceColor(ButtonBase button)
		{
			return (bool)(MouseIsDownProperty?.GetValue(button, null) ?? false) ? button.FlatAppearance.MouseDownBackColor
				: (bool)(MouseIsOverProperty?.GetValue(button, null) ?? false) ? button.FlatAppearance.MouseOverBackColor
				: button.BackColor;
		}

		/// <summary>
		/// Whether a push button opens a list or a menu, as the arrow half of a split button does: its text is a triangle
		/// pointing down and nothing else, ▾ or ▼, which Windows draws where the theme does not.
		/// </summary>
		static bool OpensList(ButtonBase button)
		{
			return button.Text == "▾" || button.Text == "▼";
		}

		/// <summary>
		/// Where a button's picture stands as the WPF programs centre one: at its own size, in the middle of the face's height,
		/// as many rows of the face above it as below. A face is as tall as its text and the frame's room above and below it,
		/// so a picture as tall as the text has an even number of rows to share.
		/// </summary>
		/// <param name="picture">The picture, or null.</param>
		/// <param name="align">How the button aligns its picture: only one aligned to the middle of its height is moved.</param>
		/// <param name="relation">How the button places its picture and text: one above the other keeps its place.</param>
		/// <param name="withText">Whether the button shows text.</param>
		/// <param name="height">The height of the face, from nought at its top.</param>
		/// <param name="place">Where the layout puts the picture, at its own size; given back where it is drawn.</param>
		/// <returns>The picture to draw there.</returns>
		static Image CentrePicture(Image picture, System.Drawing.ContentAlignment align, TextImageRelation relation, bool withText, int height, ref Rectangle place)
		{
			var stacked = withText && (relation == TextImageRelation.ImageAboveText || relation == TextImageRelation.TextAboveImage);
			if (picture == null || place.Size != picture.Size || (align & (AnyTop | AnyBottom)) != 0 || stacked)
				return picture;
			place = new Rectangle(place.X, (height - picture.Height) / 2, picture.Width, picture.Height);
			return picture;
		}

		static readonly PropertyInfo AdapterProperty = typeof(ButtonBase).GetProperty("Adapter", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly PropertyInfo MouseIsDownProperty = typeof(ButtonBase).GetProperty("MouseIsDown", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly PropertyInfo MouseIsOverProperty = typeof(ButtonBase).GetProperty("MouseIsOver", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>Whether a check box or radio button is checked; a push button never is.</summary>
		static CheckState CheckStateOf(ButtonBase button)
		{
			return (button as CheckBox)?.CheckState ?? ((button as RadioButton)?.Checked == true ? CheckState.Checked : CheckState.Unchecked);
		}

		/// <summary>
		/// Where Windows Forms paints a button's text, and the place its layout centres the button's image in, from its own
		/// layout; false where the runtime does not show it.
		/// </summary>
		/// <param name="button">The button.</param>
		/// <param name="e">What the button is being painted with.</param>
		/// <param name="text">Where the text is painted.</param>
		/// <param name="image">The part of the image painted, in the place the layout centres it in.</param>
		/// <param name="shifted">
		/// Whether Windows Forms paints the image whole a pixel right of and below that place, as .NET Framework 1.1 laid a
		/// button out, which a push button still is; otherwise it paints it there.
		/// </param>
		/// <remarks>
		/// The layout an adapter gives on its own is the one it measures a button with, pressed, its text a pixel right of and
		/// below where a raised button has it. A button is painted with the layout of its adapter's Paint...Layout, raised unless
		/// it is a checked check box or radio button that has no colour of its own for that.
		/// </remarks>
		static bool GetButtonLayout(ButtonBase button, PaintEventArgs e, out Rectangle text, out Rectangle image, out bool shifted)
		{
			text = image = Rectangle.Empty;
			shifted = false;
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var adapter = AdapterProperty?.GetValue(button, null);
			var up = CheckStateOf(button) == CheckState.Unchecked || !button.FlatAppearance.CheckedBackColor.IsEmpty;
			var paint = adapter?.GetType().GetMethods(flags).FirstOrDefault(x => !x.IsStatic && x.Name.StartsWith("Paint") && x.Name.EndsWith("Layout")
				&& x.GetParameters().Any(p => p.Name == "up"));
			var options = paint != null
				? paint.Invoke(adapter, paint.GetParameters().Select(p =>
					p.ParameterType == typeof(PaintEventArgs) ? e
					: p.Name == "up" ? up
					: p.Name == "borderSize" ? button.FlatAppearance.BorderSize
					// A raised pop-up button has a one-pixel border painted.
					: p.Name == "paintedBorder" ? 1
					: (object)false).ToArray())
				: adapter?.GetType().GetMethod("Layout", flags, null, new[] { typeof(PaintEventArgs) }, null)?.Invoke(adapter, new object[] { e });
			var data = options?.GetType().GetMethod("Layout", flags, null, Type.EmptyTypes, null)?.Invoke(options, null);
			// .NET Framework names the fields in lower case, later Windows Forms in upper.
			var type = data?.GetType();
			var textField = type?.GetField("textBounds", flags) ?? type?.GetField("TextBounds", flags);
			var imageField = type?.GetField("imageBounds", flags) ?? type?.GetField("ImageBounds", flags);
			if (textField == null || imageField == null)
				return false;
			text = (Rectangle)textField.GetValue(data);
			var place = (Rectangle)imageField.GetValue(data);
			var size = button.Image?.Size ?? Size.Empty;
			var compat = options.GetType().GetField("everettButtonCompat", flags) ?? options.GetType().GetField("DotNetOneButtonCompat", flags);
			var start = type.GetField("imageStart", flags) ?? type.GetField("ImageStart", flags);
			shifted = compat != null && start != null && (bool)compat.GetValue(options);
			image = shifted
				? new Rectangle((Point)start.GetValue(data), size)
				: Rectangle.Intersect(new Rectangle(place.Location, size), place);
			return true;
		}

		/// <summary>Whether another button on the same parent stands against this one's left or right side.</summary>
		static void JoinedSides(Control button, out bool left, out bool right)
		{
			left = right = false;
			var parent = button.Parent;
			if (parent == null)
				return;
			var b = button.Bounds;
			foreach (Control other in parent.Controls)
			{
				if (other == button || !other.Visible || !(other is ButtonBase))
					continue;
				var o = other.Bounds;
				if (o.Top >= b.Bottom || o.Bottom <= b.Top)
					continue;
				left |= Math.Abs(o.Right - b.Left) <= 1;
				right |= Math.Abs(o.Left - b.Right) <= 1;
			}
		}

		/// <summary>A rectangle drawn in by the same room on every side, so a line twice that wide drawn on it stays inside the first.</summary>
		static RectangleF Inset(RectangleF r, float room)
		{
			return new RectangleF(r.X + room, r.Y + room, r.Width - room * 2, r.Height - room * 2);
		}

		/// <summary>A rectangle with each corner rounded by its own radius; nought leaves that corner square.</summary>
		static GraphicsPath RoundedRectangle(RectangleF r, float topLeft, float topRight, float bottomRight, float bottomLeft)
		{
			var path = new GraphicsPath();
			if (topLeft > 0)
				path.AddArc(r.Left, r.Top, topLeft * 2, topLeft * 2, 180, 90);
			else
				path.AddLine(r.Left, r.Top, r.Left, r.Top);
			if (topRight > 0)
				path.AddArc(r.Right - topRight * 2, r.Top, topRight * 2, topRight * 2, 270, 90);
			else
				path.AddLine(r.Right, r.Top, r.Right, r.Top);
			if (bottomRight > 0)
				path.AddArc(r.Right - bottomRight * 2, r.Bottom - bottomRight * 2, bottomRight * 2, bottomRight * 2, 0, 90);
			else
				path.AddLine(r.Right, r.Bottom, r.Right, r.Bottom);
			if (bottomLeft > 0)
				path.AddArc(r.Left, r.Bottom - bottomLeft * 2, bottomLeft * 2, bottomLeft * 2, 90, 90);
			else
				path.AddLine(r.Left, r.Bottom, r.Left, r.Bottom);
			path.CloseFigure();
			return path;
		}

		/// <summary>
		/// Draws a menu or a status bar in the dark theme: as Windows Forms' professional renderer draws it, the lines beside a
		/// status bar's labels in the theme's line colour (BorderDark), as every other line of the dark theme, where Windows draws
		/// them in its own light grey.
		/// </summary>
		class DarkToolStripRenderer : ToolStripProfessionalRenderer
		{
			public DarkToolStripRenderer() : base(new ProfessionalColorTable()) { }

			protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
			{
				base.OnRenderToolStripStatusLabelBackground(e);
				var label = e.Item as ToolStripStatusLabel;
				if (label == null || label.BorderSides == ToolStripStatusLabelBorderSides.None)
					return;
				// Windows draws each side two pixels wide, a dark one and a light one: both are covered with what is behind the
				// label, and the outer one drawn again in the line colour, one pixel wide as every other line.
				var sides = label.BorderSides;
				var w = label.Width;
				var h = label.Height;
				using (var back = new SolidBrush(label.BackColor))
				using (var line = new SolidBrush(Themed("BorderDark", SystemColors.ControlDark)))
				{
					if ((sides & ToolStripStatusLabelBorderSides.Left) != 0)
					{
						e.Graphics.FillRectangle(back, 0, 0, 2, h);
						e.Graphics.FillRectangle(line, 0, 0, 1, h);
					}
					if ((sides & ToolStripStatusLabelBorderSides.Right) != 0)
					{
						e.Graphics.FillRectangle(back, w - 2, 0, 2, h);
						e.Graphics.FillRectangle(line, w - 1, 0, 1, h);
					}
					if ((sides & ToolStripStatusLabelBorderSides.Top) != 0)
					{
						e.Graphics.FillRectangle(back, 0, 0, w, 2);
						e.Graphics.FillRectangle(line, 0, 0, w, 1);
					}
					if ((sides & ToolStripStatusLabelBorderSides.Bottom) != 0)
					{
						e.Graphics.FillRectangle(back, 0, h - 2, w, 2);
						e.Graphics.FillRectangle(line, 0, h - 1, w, 1);
					}
				}
			}
		}

		/// <summary>
		/// Draws a tool bar as the WPF programs draw one: plain, without an edge, its buttons filled and rounded, lighter
		/// under the mouse and lighter again pressed. A menu that drops down from it is drawn as before.
		/// </summary>
		class WpfToolStripRenderer : ToolStripProfessionalRenderer
		{
			public WpfToolStripRenderer() : base(new ProfessionalColorTable()) { }

			protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
			{
				if (e.ToolStrip is ToolStripDropDown)
				{
					base.OnRenderToolStripBackground(e);
					return;
				}
				using (var brush = new SolidBrush(e.ToolStrip.BackColor))
					e.Graphics.FillRectangle(brush, e.AffectedBounds);
			}

			protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
			{
				if (e.ToolStrip is ToolStripDropDown)
					base.OnRenderToolStripBorder(e);
			}

			protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
			{
				if (!FillItem(e.Graphics, e.Item))
					base.OnRenderButtonBackground(e);
			}

			protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
			{
				if (!FillItem(e.Graphics, e.Item))
					base.OnRenderDropDownButtonBackground(e);
			}

			protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
			{
				var split = (ToolStripSplitButton)e.Item;
				if (!FillItem(e.Graphics, e.Item))
				{
					base.OnRenderSplitButtonBackground(e);
					return;
				}
				DrawArrow(new ToolStripArrowRenderEventArgs(e.Graphics, split, split.DropDownButtonBounds, split.ForeColor, ArrowDirection.Down));
			}

			/// <summary>
			/// Draws a disabled button's text on the tool bar itself as WPF draws it and the theme draws a disabled push
			/// button's, where Windows Forms draws it in the grey text colour at full strength.
			/// </summary>
			protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
			{
				if (e.Item.Enabled || e.Item.IsOnDropDown || e.TextDirection != ToolStripTextDirection.Horizontal)
				{
					base.OnRenderItemText(e);
					return;
				}
				TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, DisabledText(), e.TextFormat);
			}

			/// <summary>
			/// Draws a button's picture on the tool bar itself in the middle of the button's height, as a push button's, and
			/// a disabled one at half strength, as WPF draws a disabled control, where Windows Forms draws it in grey; a
			/// picture on a drop-down is drawn as before.
			/// </summary>
			protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
			{
				if (e.Item.IsOnDropDown || e.Image == null)
				{
					base.OnRenderItemImage(e);
					return;
				}
				var item = e.Item;
				var place = e.ImageRectangle;
				var withText = (item.DisplayStyle & ToolStripItemDisplayStyle.Text) != 0 && !string.IsNullOrEmpty(item.Text);
				var image = CentrePicture(e.Image, item.ImageAlign, item.TextImageRelation, withText, item.Height, ref place);
				if (item.Enabled && image == e.Image && place == e.ImageRectangle)
				{
					base.OnRenderItemImage(e);
					return;
				}
				using (var attributes = new ImageAttributes())
				{
					if (!item.Enabled)
						attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.5f });
					var key = item.ImageTransparentColor;
					if (key != Color.Empty)
						attributes.SetColorKey(key, key);
					e.Graphics.DrawImage(image, place, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
				}
			}

			/// <summary>
			/// Draws the arrow of a drop-down button on the tool bar itself as a drop-down list's: Default.xaml's triangle,
			/// in the column <see cref="PadToolBarItem"/> leaves for it at the button's right; any other arrow is drawn as before.
			/// </summary>
			protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
			{
				var item = e.Item;
				if (!HasListArrow(item))
				{
					base.OnRenderArrow(e);
					return;
				}
				var g = e.Graphics;
				var color = item.Enabled ? item.ForeColor : DisabledText();
				DrawDropDownArrow(g, item.Width - Line(g) - Dip(g, ComboBoxPadding + ComboBoxArrowColumn / 2f), item.Height / 2f, color);
			}

			/// <summary>
			/// Fills a button on the tool bar itself, at half strength while disabled, as WPF draws a disabled control; false
			/// for one on a drop-down, which is drawn as before.
			/// </summary>
			static bool FillItem(Graphics g, ToolStripItem item)
			{
				if (item.IsOnDropDown)
					return false;
				var key = !item.Enabled ? "BackgroundDark"
					: item.Pressed ? "BackgroundDarkPressed"
					: item.Selected ? "BackgroundDarkSelected"
					: "BackgroundDark";
				var saved = Smooth(g);
				var radius = Dip(g, CornerRadius);
				using (var brush = new SolidBrush(Color.FromArgb(item.Enabled ? 255 : 128, Themed(key, SystemColors.ControlLight))))
				using (var face = RoundedRectangle(new RectangleF(0, 0, item.Width, item.Height), radius, radius, radius, radius))
					g.FillPath(brush, face);
				g.Restore(saved);
				return true;
			}
		}

		/// <summary>The room round each item of a tool bar, in device-independent pixels: Default.xaml's button margin.</summary>
		const int ButtonMargin = 4;

		/// <summary>The room between a button's fill and what is on it, in device-independent pixels: Default.xaml's button padding.</summary>
		const int ButtonPadding = 4;

		/// <summary>The room Windows Forms leaves inside a tool bar button's edge, in pixels.</summary>
		const int WindowsToolBarButtonBorder = 2;

		/// <summary>What a tool bar item had before the theme laid it out as Default.xaml's button.</summary>
		class ItemLook
		{
			public Padding Margin;
			public Padding Padding;
			public System.Drawing.ContentAlignment ImageAlign;
			public System.Drawing.ContentAlignment TextAlign;
			/// <summary>The room a drop-down button's layout keeps either side of its arrow.</summary>
			public Padding ArrowPadding;
		}

		static readonly ConditionalWeakTable<ToolStripItem, ItemLook> ItemLooks = new ConditionalWeakTable<ToolStripItem, ItemLook>();

		/// <summary>The layout of a tool bar item, a property of its own, made again as the item changes.</summary>
		static readonly PropertyInfo ItemLayoutProperty = typeof(ToolStripItem).GetProperty("InternalLayout", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>The layout of a drop-down button, a class of its own.</summary>
		static readonly Type DropDownLayoutType = typeof(ToolStripDropDownButton).GetNestedType("ToolStripDropDownButtonInternalLayout", BindingFlags.NonPublic);

		/// <summary>
		/// The room a drop-down button's layout keeps either side of its arrow, a field of the layout's own: Windows Forms lays the
		/// button's picture and text out clear of it and of the arrow.
		/// </summary>
		static readonly FieldInfo ArrowPaddingField = DropDownLayoutType?.GetField("scaledDropDownArrowPadding", BindingFlags.Instance | BindingFlags.NonPublic)
			?? DropDownLayoutType?.GetField("_dropDownArrowPadding", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>The size of the arrow Windows Forms draws on a drop-down button, a field of the layout's class.</summary>
		static readonly FieldInfo ArrowSizeField = DropDownLayoutType?.GetField("dropDownArrowSize", BindingFlags.Static | BindingFlags.NonPublic)
			?? DropDownLayoutType?.GetField("s_dropDownArrowSize", BindingFlags.Static | BindingFlags.NonPublic);

		/// <summary>The layout of a drop-down button, or null where the runtime does not show it.</summary>
		static object DropDownLayout(ToolStripItem item)
		{
			if (ItemLayoutProperty == null || ArrowPaddingField == null || ArrowSizeField == null || !(item is ToolStripDropDownButton))
				return null;
			var layout = ItemLayoutProperty.GetValue(item, null);
			return DropDownLayoutType.IsInstanceOfType(layout) ? layout : null;
		}

		/// <summary>
		/// Lays an item on a tool bar drawn as the WPF programs draw one out as a control on a WPF tool bar: Default.xaml's
		/// button margin round it, and a button as Default.xaml's button, its picture and text side by side in the middle,
		/// with the button padding at each side, and as tall as Default.xaml's button; gives any other item its own look back.
		/// </summary>
		/// <remarks>
		/// Windows Forms does not keep a tool bar button's padding round what is on it: it adds the padding to the width and
		/// shares it between the picture and the text, each standing in the middle of its share. The picture is put at the
		/// right of its share and the text at the left of its, so the two stay together and the padding falls outside them.
		/// </remarks>
		static void PadToolBarItem(ToolStripItem item, bool wpf)
		{
			ItemLook own;
			if (!ItemLooks.TryGetValue(item, out own))
			{
				if (!wpf)
					return;
				own = new ItemLook { Margin = item.Margin, Padding = item.Padding, ImageAlign = item.ImageAlign, TextAlign = item.TextAlign };
				var layout = DropDownLayout(item);
				if (layout != null)
					own.ArrowPadding = (Padding)ArrowPaddingField.GetValue(layout);
				ItemLooks.Add(item, own);
			}
			var margin = wpf ? new Padding(Pixels(ButtonMargin)) : own.Margin;
			if (item.Margin != margin)
				item.Margin = margin;
			if (!(item is ToolStripButton || item is ToolStripDropDownButton || item is ToolStripSplitButton))
				return;
			var together = wpf && item.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText && item.Image != null
				&& item.TextImageRelation == TextImageRelation.ImageBeforeText;
			var imageAlign = together ? System.Drawing.ContentAlignment.MiddleRight : own.ImageAlign;
			var textAlign = together ? System.Drawing.ContentAlignment.MiddleLeft : own.TextAlign;
			if (item.ImageAlign != imageAlign)
				item.ImageAlign = imageAlign;
			if (item.TextAlign != textAlign)
				item.TextAlign = textAlign;
			// Inside the button's line, the button padding at each side, as tall as Default.xaml's button.
			var side = LineWidth() + Pixels(ButtonPadding);
			var extra = wpf ? side - WindowsToolBarButtonBorder : 0;
			var natural = item.GetPreferredSize(Size.Empty).Height - item.Padding.Vertical + own.Padding.Vertical;
			var taller = wpf ? Math.Max(0, item.Font.Height + side * 2 - natural) : 0;
			var padding = new Padding(own.Padding.Left + extra, own.Padding.Top + taller / 2, own.Padding.Right + extra, own.Padding.Bottom + taller - taller / 2);
			if (item.Padding != padding)
				item.Padding = padding;
			// A drop-down button keeps a drop-down list's arrow column at its right: the room round Windows' arrow is widened, so
			// its picture and text end where a list's text does, the button's padding short of the column.
			var dropDown = DropDownLayout(item);
			if (dropDown != null)
			{
				var room = Math.Max(0, Pixels(ComboBoxPadding * 2 + ComboBoxArrowColumn) - Pixels(ButtonPadding) - ((Size)ArrowSizeField.GetValue(null)).Width);
				var arrowPadding = wpf && HasListArrow(item) ? new Padding(room / 2, 0, room - room / 2, 0) : own.ArrowPadding;
				if ((Padding)ArrowPaddingField.GetValue(dropDown) != arrowPadding)
					ArrowPaddingField.SetValue(dropDown, arrowPadding);
			}
		}

		/// <summary>
		/// Whether a tool bar item shows a drop-down list's arrow: a drop-down button on the tool bar itself with its arrow
		/// shown, its text across and left to right, where the runtime lets the room round the arrow be widened. Any other keeps
		/// the arrow Windows Forms draws.
		/// </summary>
		static bool HasListArrow(ToolStripItem item)
		{
			return item is ToolStripDropDownButton button && button.ShowDropDownArrow && !item.IsOnDropDown
				&& item.RightToLeft != RightToLeft.Yes && item.TextDirection == ToolStripTextDirection.Horizontal
				&& ItemLayoutProperty != null && ArrowPaddingField != null && ArrowSizeField != null;
		}

		/// <summary>
		/// The padding of a tool bar: as the WPF programs draw one, at its sides none but the room a list beside it lent it,
		/// above and below its buttons its own; otherwise its own.
		/// </summary>
		static Padding ToolBarPadding(ToolStrip strip, State state, bool wpf)
		{
			if (!wpf)
				return state.Padding;
			var lent = LentRoom(strip);
			return new Padding(lent.Left, state.Padding.Top, lent.Right, state.Padding.Bottom);
		}

		/// <summary>
		/// Lays the items out again each time the tool bar is: an item added, or a picture enlarged to the screen's scale after
		/// the theme was applied, would otherwise keep a padding worked out for what it had before.
		/// </summary>
		static void ToolBar_Layout(object sender, LayoutEventArgs e)
		{
			foreach (ToolStripItem item in ((ToolStrip)sender).Items)
				PadToolBarItem(item, true);
		}

		/// <summary>Whether a group box is drawn as the WPF programs draw one: one Windows Forms draws, while high contrast is off.</summary>
		static bool PaintsGroupBox(Control control)
		{
			return control is GroupBox group && group.FlatStyle != FlatStyle.System && !SystemInformation.HighContrast;
		}

		/// <summary>
		/// Where a group box's caption breaks its frame, how far its text stands in from there, and how far down its text stands,
		/// in device-independent pixels, as in Default.xaml.
		/// </summary>
		const int GroupBoxCaptionGap = 12;
		const int GroupBoxCaptionPadding = 4;
		const int GroupBoxCaptionTop = 4;

		/// <summary>
		/// The room between a group box's content and the inner line of its frame at the sides and below, and between its
		/// caption's text and its content, in device-independent pixels, as Default.xaml's GroupBox pads its content.
		/// </summary>
		const int GroupBoxRoom = 4;

		/// <summary>
		/// Draws a group box as Default.xaml does, over what Windows Forms drew: its frame rounded and sunken, a shadow line with
		/// a highlight line beside it, a pixel right and down, and its caption in a break in the frame clear of the corner, the
		/// frame's top line through the caption's middle. The caption never comes closer to the box's controls. Disabled, the
		/// caption is at half strength, as a disabled label is.
		/// </summary>
		static void GroupBox_Paint(object sender, PaintEventArgs e)
		{
			var box = (GroupBox)sender;
			var g = e.Graphics;
			var client = box.ClientRectangle;
			if (box.BackColor.A < 255)
				ButtonRenderer.DrawParentBackground(g, client, box);
			using (var brush = new SolidBrush(box.BackColor))
				g.FillRectangle(brush, client);
			var caption = TextRenderer.MeasureText(g, box.Text.Length > 0 ? box.Text : " ", box.Font);
			// Default.xaml's caption stands lower than Windows Forms' does, where a layout moves the box's controls down with
			// it. Controls put where they stand keep the room left above them, and the caption stays where Windows Forms has it.
			var laidOut = box.Controls.Cast<Control>().Any(x => x.Visible && x.Dock != DockStyle.None);
			var captionTop = laidOut ? (int)Math.Round(Dip(g, GroupBoxCaptionTop)) : 0;
			// Through the middle of the caption's line as WPF lays it out, the font's height: Windows' own line can be a row
			// shorter, which would put the line half a row high.
			var top = captionTop + box.Font.Height / 2;
			var saved = Smooth(g);
			var line = Line(g);
			var radius = Dip(g, CornerRadius);
			var frame = new RectangleF(0, top, client.Width - line, client.Height - top - line);
			using (var pen = new Pen(Themed("BorderHighlight", SystemColors.ControlLightLight), line))
			using (var path = RoundedRectangle(Inset(new RectangleF(frame.X + line, frame.Y + line, frame.Width, frame.Height), line / 2), radius, radius, radius, radius))
				g.DrawPath(pen, path);
			using (var pen = new Pen(Themed("BorderShadow", SystemColors.ControlDark), line))
			using (var path = RoundedRectangle(Inset(frame, line / 2), radius, radius, radius, radius))
				g.DrawPath(pen, path);
			g.Restore(saved);
			if (box.Text.Length == 0)
				return;
			const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
			var width = TextRenderer.MeasureText(g, box.Text, box.Font, Size.Empty, flags).Width;
			var gap = (int)Math.Round(Dip(g, GroupBoxCaptionGap));
			var padding = (int)Math.Round(Dip(g, GroupBoxCaptionPadding));
			using (var brush = new SolidBrush(box.BackColor))
				g.FillRectangle(brush, gap, captionTop, width + padding * 2, caption.Height);
			TextRenderer.DrawText(g, box.Text, box.Font, new Rectangle(gap + padding, captionTop, width, caption.Height),
				box.Enabled ? box.ForeColor : DisabledText(), flags);
		}

		#endregion

		#region Painted here

		static readonly MethodInfo SetStyleMethod = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly MethodInfo GetStyleMethod = typeof(Control).GetMethod("GetStyle", BindingFlags.Instance | BindingFlags.NonPublic);

		const ControlStyles PaintedHereStyles = ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw;

		/// <summary>
		/// The slider has no dark Windows style, so in the dark theme it is painted here, double buffered. The tab strip
		/// and the drop-down list are painted here in both themes, as the WPF programs draw them.
		/// </summary>
		/// <remarks>A control no longer painted here gets back the styles it had, flag by flag.</remarks>
		static void SetPaintedHere(Control control, State state, bool paintHere, PaintEventHandler paint)
		{
			if (state.PaintStyles == null)
				state.PaintStyles = Enum.GetValues(typeof(ControlStyles)).Cast<ControlStyles>()
					.Where(x => (PaintedHereStyles & x) == x && (bool)GetStyleMethod.Invoke(control, new object[] { x }))
					.Aggregate((ControlStyles)0, (all, x) => all | x);
			SetStyleMethod.Invoke(control, new object[] { PaintedHereStyles, paintHere });
			if (!paintHere)
				SetStyleMethod.Invoke(control, new object[] { state.PaintStyles.Value, true });
			control.Paint -= paint;
			if (paintHere)
				control.Paint += paint;
			control.Invalidate();
		}

		static void Repaint(object sender, EventArgs e)
		{
			((Control)sender).Invalidate();
		}

		/// <summary>Whether a control is drawn where Windows does not have it: a tab strip or slider painted here.</summary>
		static bool MapsMouse(Control control)
		{
			return PaintsTabs(control) || (control is TrackBar && !SystemInformation.HighContrast);
		}

		/// <summary>
		/// Passes the mouse on a tab strip or slider painted here on to what is drawn under it, and puts the pages of such a strip
		/// inside the frame drawn round them; leaves any other as it is.
		/// </summary>
		static void AttachMouseMap(Control control, State state)
		{
			if (MapsMouse(control))
			{
				if (state.Mouse == null)
					state.Mouse = new MouseMap(control);
				if (state.Mouse.Handle != control.Handle)
				{
					if (state.Mouse.Handle != IntPtr.Zero)
						state.Mouse.ReleaseHandle();
					state.Mouse.AssignHandle(control.Handle);
				}
			}
			else if (state.Mouse != null && state.Mouse.Handle != IntPtr.Zero)
				state.Mouse.ReleaseHandle();
		}

		/// <summary>
		/// The window of a control drawn where Windows does not have it: a tab strip, whose tabs are drawn from the side of
		/// the window, as the WPF programs draw them, where Windows keeps them a little in from it, or a slider, whose thumb
		/// travels as far as WPF's, further than Windows lets it. The mouse is passed on to Windows where Windows has what is
		/// drawn under it, and a tab strip's pages are put where the frame drawn round them has them.
		/// </summary>
		class MouseMap : NativeWindow
		{
			readonly Control Control;

			public MouseMap(Control control)
			{
				Control = control;
			}

			protected override void WndProc(ref Message m)
			{
				// Windows Forms gives the window the strip's pictures, which a strip sizing its tabs as Default.xaml's has sized by
				// blank ones in their place.
				State state;
				if (m.Msg == NativeMethods.TCM_SETIMAGELIST && Control is TabControl strip && States.TryGetValue(strip, out state) && state.TabsSized)
				{
					ImageList replaced;
					m.LParam = TabImageList(strip, state, out replaced);
					base.WndProc(ref m);
					replaced?.Dispose();
					return;
				}
				// Windows Forms asks the window where the pages go, for the window's rectangle.
				if (m.Msg == NativeMethods.TCM_ADJUSTRECT && m.WParam == IntPtr.Zero && Control is TabControl tabs && tabs.TabCount > 0)
				{
					var window = (NativeMethods.RECT)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.RECT));
					Marshal.StructureToPtr(PageArea(tabs, window), m.LParam, false);
					return;
				}
				var mouse = m.Msg >= NativeMethods.WM_MOUSEMOVE && m.Msg <= NativeMethods.WM_MBUTTONDBLCLK;
				if (mouse)
				{
					var lParam = (int)(long)m.LParam;
					var point = MapMouse(Control, new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)));
					m.LParam = (IntPtr)((point.Y << 16) | (point.X & 0xFFFF));
				}
				base.WndProc(ref m);
				// Windows repaints a slider where it has the thumb, so the slider is repainted whole as the mouse lights or moves it.
				if (mouse && Control is TrackBar)
					Control.Invalidate();
			}
		}

		/// <summary>Where Windows has the place of a tab strip or slider that is drawn at the point given.</summary>
		static Point MapMouse(Control control, Point point)
		{
			if (control is TabControl tabs)
			{
				var rects = TabRects(tabs);
				return point.Y < TabsBottom(rects) ? new Point(point.X + TabOffset(rects), point.Y) : point;
			}
			if (control is TrackBar track)
			{
				float nativeFirst, nativeLast, first, last;
				GetSliderTravel(track, out nativeFirst, out nativeLast, out first, out last);
				var across = SliderShift(track);
				if (track.Orientation == Orientation.Vertical)
					return new Point(point.X - across, (int)Math.Round(Remap(point.Y, first, last, nativeFirst, nativeLast)));
				return new Point((int)Math.Round(Remap(point.X, first, last, nativeFirst, nativeLast)), point.Y - across);
			}
			return point;
		}

		/// <summary>A value between the first two numbers given, at the same place between the last two.</summary>
		static float Remap(float value, float from, float to, float newFrom, float newTo)
		{
			return to == from ? newFrom : newFrom + (value - from) * (newTo - newFrom) / (to - from);
		}

		/// <summary>
		/// Makes a list painted here as tall as a push button in the same font, as Default.xaml makes both: its text with 4
		/// device-independent pixels above and below, on a tool bar too, and at least as wide as its widest item, off a tool
		/// bar, a stretched one by its minimum width, and gives the list's window its font.
		/// </summary>
		/// <remarks>
		/// A table lays a list out by the height it was given, so the list is given its height. Windows makes the window as
		/// tall as its field and an edge of its own, which grows with the screen's scale, so the field is made what the height
		/// leaves of it, read from the window. A list painted here draws its items itself (<see cref="DrawMode.OwnerDrawFixed"/>),
		/// so its field is its <see cref="ComboBox.ItemHeight"/>, which Windows Forms gives Windows again whenever it makes the
		/// window. Windows Forms gives its font only to a window it leaves Windows to paint; without it, Windows draws the text
		/// box of an editable list in the system's bitmap font.
		/// </remarks>
		static void FitComboBox(object sender, EventArgs e)
		{
			var combo = (ComboBox)sender;
			State state;
			if (!States.TryGetValue(combo, out state))
				return;
			var height = ComboBoxHeight(combo);
			if (height > 0 && combo.Height != height)
				combo.Height = height;
			const AnchorStyles across = AnchorStyles.Left | AnchorStyles.Right;
			var stretched = combo.Dock != DockStyle.None || (combo.Anchor & across) == across;
			if (!(combo.Parent is ToolStrip) && combo.Items.Count > 0)
			{
				var width = GetComboBoxWidth(combo);
				if (!stretched)
				{
					if (combo.Width < width)
						combo.Width = width;
				}
				// A layout that stretches the list gives it the width of its cell, which only the list can make wide enough:
				// a table's column sized to its content is as wide as the widest thing in it, the list counted at its minimum.
				else if (combo.MinimumSize.Width < width)
					combo.MinimumSize = new Size(width, combo.MinimumSize.Height);
			}
			if (!combo.IsHandleCreated)
				return;
			SetWindowFontMethod?.Invoke(combo, null);
			NativeMethods.RECT r;
			if (height > 0 && NativeMethods.GetWindowRect(combo.Handle, out r))
			{
				var field = (int)NativeMethods.SendMessage(combo.Handle, NativeMethods.CB_GETITEMHEIGHT, (IntPtr)(-1), IntPtr.Zero);
				var itemHeight = field + height - (r.Bottom - r.Top);
				if (field > 0 && itemHeight > 0 && combo.ItemHeight != itemHeight)
					combo.ItemHeight = itemHeight;
			}
			var info = new NativeMethods.COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.COMBOBOXINFO)) };
			if (!NativeMethods.GetComboBoxInfo(combo.Handle, ref info))
				return;
			AttachDropDownList(combo, state, info.hwndList);
			AttachEditField(combo, state, combo.DropDownStyle == ComboBoxStyle.DropDown ? info.hwndItem : IntPtr.Zero);
		}

		/// <summary>The height Default.xaml gives a drop-down list painted here, on a tool bar or off it; 0 for any other list.</summary>
		static int ComboBoxHeight(ComboBox combo)
		{
			return PaintsComboBox(combo) ? ControlHeight(combo) : 0;
		}

		/// <summary>
		/// Opens the list as wide as its widest item, as WPF's grows to what it holds, from the field's left edge, so no item
		/// is cut short.
		/// </summary>
		static void ComboBox_DropDown(object sender, EventArgs e)
		{
			var combo = (ComboBox)sender;
			// The items of the list it opens as tall as Default.xaml's, their text with 4 above and below and no line; Windows
			// Forms gives them the field's height whenever it gives the field its own.
			NativeMethods.SendMessage(combo.Handle, NativeMethods.CB_SETITEMHEIGHT, IntPtr.Zero, (IntPtr)(combo.Font.Height + Pixels(ComboBoxPadding * 2)));
			var width = WidestItem(combo) + Pixels(ComboBoxPadding) * 2;
			if (combo.Items.Count > combo.MaxDropDownItems)
				width += SystemInformation.VerticalScrollBarWidth;
			width = Math.Max(combo.Width, width);
			if (combo.DropDownWidth != width)
				combo.DropDownWidth = width;
		}

		/// <summary>The width of the text of a drop-down list's widest item, as a list draws it.</summary>
		static int WidestItem(ComboBox combo)
		{
			var widest = 0;
			for (var i = 0; i < combo.Items.Count; i++)
				widest = Math.Max(widest, TextRenderer.MeasureText(combo.GetItemText(combo.Items[i]), combo.Font, Size.Empty,
					TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width);
			return widest;
		}

		/// <summary>
		/// The width a drop-down list needs to show its widest item whole, as it is drawn now: here, with Default.xaml's room
		/// before the text and its triangle's column after it, or by Windows, with its arrow button and edge.
		/// </summary>
		/// <remarks>Windows Forms gives a list the width it was made, whatever it holds, so a page that fits a list to its items asks here.</remarks>
		public static int GetComboBoxWidth(ComboBox combo)
		{
			var room = PaintsComboBox(combo)
				? FrameSide() * 2 + Pixels(ComboBoxPadding + ComboBoxArrowColumn)
				: SystemInformation.VerticalScrollBarWidth + 8;
			return WidestItem(combo) + room;
		}

		/// <summary>Draws a drop-down list again in the shape it was given: made editable, or a list again.</summary>
		static void ComboBox_DropDownStyleChanged(object sender, EventArgs e)
		{
			State state;
			if (States.TryGetValue((Control)sender, out state) && state.Dark != null)
				ApplyStyle((Control)sender, state);
		}

		/// <summary>
		/// The list a drop-down list painted here opens, as the WPF programs draw it: an item as Default.xaml's ComboBoxItem
		/// draws one, its text 4 in from the side, on the control colour, and rounded in the hover colour while it is the one the
		/// mouse or the keys stand on. The field is painted with the rest of the drop-down list.
		/// </summary>
		static void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			var combo = (ComboBox)sender;
			if ((e.State & DrawItemState.ComboBoxEdit) != 0)
			{
				combo.Invalidate();
				return;
			}
			if (e.Index < 0 || e.Index >= combo.Items.Count)
				return;
			var g = e.Graphics;
			using (var brush = new SolidBrush(Themed("BackgroundDark", SystemColors.Control)))
				g.FillRectangle(brush, e.Bounds);
			if ((e.State & DrawItemState.Selected) != 0)
			{
				var saved = Smooth(g);
				var radius = Dip(g, CornerRadius);
				using (var brush = new SolidBrush(Themed("BackgroundDarkSelected", SystemColors.Highlight)))
				using (var face = RoundedRectangle(e.Bounds, radius, radius, radius, radius))
					g.FillPath(brush, face);
				g.Restore(saved);
			}
			var padding = Pixels(ComboBoxPadding);
			var text = Rectangle.FromLTRB(e.Bounds.Left + padding, e.Bounds.Top, e.Bounds.Right - padding, e.Bounds.Bottom);
			TextRenderer.DrawText(g, combo.GetItemText(combo.Items[e.Index]), combo.Font, text, Themed("ForegroundBlack", SystemColors.ControlText),
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
		}

		/// <summary>Gives the list a drop-down list painted here opens the WPF programs' shape, and leaves any other list as it is.</summary>
		static void AttachDropDownList(ComboBox combo, State state, IntPtr list)
		{
			if (list == IntPtr.Zero)
				return;
			if (PaintsComboBox(combo))
			{
				if (state.List == null)
					state.List = new DropDownList(combo);
				if (state.List.Handle != list)
				{
					if (state.List.Handle != IntPtr.Zero)
						state.List.ReleaseHandle();
					state.List.AssignHandle(list);
					state.List.Shape();
				}
			}
			else if (state.List != null && state.List.Handle != IntPtr.Zero)
				state.List.ReleaseHandle();
		}

		/// <summary>Puts the text box of an editable drop-down list painted here where Default.xaml has it, and leaves any other where Windows puts it.</summary>
		static void AttachEditField(ComboBox combo, State state, IntPtr edit)
		{
			if (edit != IntPtr.Zero && PaintsComboBox(combo))
			{
				if (state.Edit == null)
					state.Edit = new EditField(combo);
				if (state.Edit.Handle != edit)
				{
					if (state.Edit.Handle != IntPtr.Zero)
						state.Edit.ReleaseHandle();
					state.Edit.AssignHandle(edit);
				}
				state.Edit.Place();
				if (state.Colors == null)
					state.Colors = new EditColors(combo);
				if (state.Colors.Handle != combo.Handle)
				{
					if (state.Colors.Handle != IntPtr.Zero)
						state.Colors.ReleaseHandle();
					state.Colors.AssignHandle(combo.Handle);
				}
				return;
			}
			if (state.Edit != null && state.Edit.Handle != IntPtr.Zero)
				state.Edit.ReleaseHandle();
			if (state.Colors != null && state.Colors.Handle != IntPtr.Zero)
				state.Colors.ReleaseHandle();
		}

		/// <summary>
		/// The window of an editable drop-down list painted here, which gives the text box on it the field's colours. Windows
		/// Forms gives the text box of a list it paints itself no back and no text colour, so it shows whatever was drawn
		/// there before, its text in black.
		/// </summary>
		class EditColors : NativeWindow
		{
			readonly ComboBox Combo;

			public EditColors(ComboBox combo)
			{
				Combo = combo;
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == NativeMethods.WM_CTLCOLOREDIT || m.Msg == NativeMethods.WM_CTLCOLORSTATIC)
				{
					var back = FrameFace(Combo);
					NativeMethods.SetTextColor(m.WParam, ColorTranslator.ToWin32(Combo.Enabled ? Combo.ForeColor : DisabledText()));
					NativeMethods.SetBkColor(m.WParam, ColorTranslator.ToWin32(back));
					m.Result = SolidBrush(back);
					return;
				}
				base.WndProc(ref m);
			}
		}

		/// <summary>A Windows brush of the colour given, made once: the theme has few colours, and a window may use one at any time.</summary>
		static IntPtr SolidBrush(Color color)
		{
			var key = color.ToArgb();
			IntPtr brush;
			lock (Brushes)
			{
				if (!Brushes.TryGetValue(key, out brush))
				{
					brush = NativeMethods.CreateSolidBrush(ColorTranslator.ToWin32(color));
					Brushes.Add(key, brush);
				}
			}
			return brush;
		}
		static readonly Dictionary<int, IntPtr> Brushes = new Dictionary<int, IntPtr>();

		/// <summary>
		/// The text box of an editable drop-down list painted here, where Default.xaml's PART_EditableTextBox stands: 4 in from
		/// the left, clear of the triangle, as tall as its line of text and in the middle of the field. Windows moves it each
		/// time the list is resized, so it is moved back as it goes.
		/// </summary>
		class EditField : DisabledTextWindow
		{
			readonly ComboBox Combo;

			public EditField(ComboBox combo)
			{
				Combo = combo;
			}

			/// <summary>Whether the theme draws the text: the list is disabled and has text.</summary>
			protected override bool DrawsDisabledText()
			{
				return !Combo.Enabled && Combo.Text.Length > 0;
			}

			/// <summary>Draws the list's text again on the field's colour, the colour the list gives its text box.</summary>
			protected override void PaintDisabledText(IntPtr hdc)
			{
				PaintDisabledEdit(Handle, hdc, Combo.Text, Combo.Font, FrameFace(Combo), false, false,
					Combo.RightToLeft == RightToLeft.Yes ? HorizontalAlignment.Right : HorizontalAlignment.Left);
			}

			Rectangle Bounds()
			{
				var client = Combo.ClientRectangle;
				var line = Math.Min(LineHeight(Combo.Font), client.Height);
				var left = FrameSide();
				var right = FrameSide() + Pixels(ComboBoxPadding + ComboBoxArrowColumn);
				return new Rectangle(left, (client.Height - line) / 2, Math.Max(0, client.Width - left - right), line);
			}

			public void Place()
			{
				var r = Bounds();
				NativeMethods.SetWindowPos(Handle, IntPtr.Zero, r.X, r.Y, r.Width, r.Height, NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == NativeMethods.WM_WINDOWPOSCHANGING)
				{
					var pos = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.WINDOWPOS));
					var r = Bounds();
					if ((pos.flags & NativeMethods.SWP_NOMOVE) == 0)
					{
						pos.x = r.X;
						pos.y = r.Y;
					}
					if ((pos.flags & NativeMethods.SWP_NOSIZE) == 0)
					{
						pos.cx = r.Width;
						pos.cy = r.Height;
					}
					Marshal.StructureToPtr(pos, m.LParam, false);
				}
				base.WndProc(ref m);
			}
		}

		/// <summary>
		/// The window of the list a drop-down list painted here opens, shaped as the WPF programs' list: without an edge, on the
		/// control colour, as tall as its items, its corners rounded where Windows rounds them.
		/// </summary>
		class DropDownList : NativeWindow
		{
			readonly ComboBox Combo;

			public DropDownList(ComboBox combo)
			{
				Combo = combo;
			}

			/// <summary>Takes the edge off the window, and has Windows 11 round its corners, without an edge of its own.</summary>
			public void Shape()
			{
				var style = NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_STYLE);
				if ((style & NativeMethods.WS_BORDER) != 0)
				{
					NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_STYLE, style & ~NativeMethods.WS_BORDER);
					NativeMethods.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE
						| NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
				}
				if (WindowsBuild < 22000)
					return;
				var corner = NativeMethods.DWMWCP_ROUNDSMALL;
				NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
				var border = NativeMethods.DWMWA_COLOR_NONE;
				NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ref border, sizeof(int));
			}

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case NativeMethods.WM_ERASEBKGND:
						using (var g = Graphics.FromHdc(m.WParam))
						using (var brush = new SolidBrush(Themed("BackgroundDark", SystemColors.Control)))
							g.FillRectangle(brush, g.VisibleClipBounds);
						m.Result = (IntPtr)1;
						return;
					case NativeMethods.WM_WINDOWPOSCHANGING:
						// Windows leaves room for the edge the list no longer has; the list is as tall as the items it shows.
						var pos = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.WINDOWPOS));
						var count = Combo.Items.Count;
						if ((pos.flags & NativeMethods.SWP_NOSIZE) == 0 && Combo.IntegralHeight && count > 0)
						{
							var height = (int)NativeMethods.SendMessage(Combo.Handle, NativeMethods.CB_GETITEMHEIGHT, IntPtr.Zero, IntPtr.Zero);
							pos.cy = Math.Min(pos.cy, Math.Min(count, Combo.MaxDropDownItems) * height);
							Marshal.StructureToPtr(pos, m.LParam, false);
						}
						break;
				}
				base.WndProc(ref m);
			}
		}

		/// <summary>
		/// Whether a drop-down list is painted here, as the WPF programs draw one: a list that drops down, picked from or typed
		/// in, whose items the program does not draw and which does not edit a grid cell, while high contrast is off.
		/// </summary>
		static bool PaintsComboBox(Control control)
		{
			State state;
			return control is ComboBox combo && combo.DropDownStyle != ComboBoxStyle.Simple
				&& States.TryGetValue(control, out state) && state.DrawMode == DrawMode.Normal
				&& !(control is IDataGridViewEditingControl) && !SystemInformation.HighContrast;
		}

		/// <summary>The width of the column a drop-down list's triangle stands in, in device-independent pixels.</summary>
		const int ComboBoxArrowColumn = 16;

		/// <summary>The room between a drop-down list's edge and what is on it, in device-independent pixels: its border and padding.</summary>
		const int ComboBoxPadding = 4;

		/// <summary>The size of Default.xaml's drop-down triangle, in device-independent pixels: as wide as its column.</summary>
		static readonly SizeF DropDownTriangle = new SizeF(16, 8);

		/// <summary>
		/// Default.xaml's drop-down triangle, pointing down with its middle on the point given: the arrow of a drop-down list,
		/// and of any button that opens a list or a menu.
		/// </summary>
		/// <remarks>
		/// Its top left corner stands on the nearest whole pixel, so its top edge and its sides are as sharp on a face of any
		/// height or width as on any other: on a half pixel, the top row is drawn at half strength and the triangle looks
		/// smaller.
		/// </remarks>
		static void DrawDropDownArrow(Graphics g, float x, float y, Color color)
		{
			var width = Dip(g, DropDownTriangle.Width);
			var height = Dip(g, DropDownTriangle.Height);
			var left = (float)Math.Round(x - width / 2, MidpointRounding.AwayFromZero);
			var top = (float)Math.Round(y - height / 2, MidpointRounding.AwayFromZero);
			var points = new[]
			{
				new PointF(left, top), new PointF(left + width, top), new PointF(left + width / 2, top + height),
			};
			var saved = Smooth(g);
			using (var brush = new SolidBrush(color))
				g.FillPolygon(brush, points);
			g.Restore(saved);
		}

		/// <summary>
		/// A drop-down list as Default.xaml draws one: a rounded field in the control colour, the hover colour under the mouse,
		/// with the chosen item's text and a triangle pointing down; disabled, all at half strength. An editable one is a text
		/// box with a triangle: the text box's frame, with the text box itself on it showing the text.
		/// </summary>
		static void ComboBox_Paint(object sender, PaintEventArgs e)
		{
			var combo = (ComboBox)sender;
			var g = e.Graphics;
			var client = combo.ClientRectangle;
			var behind = combo.Parent?.BackColor ?? SystemColors.Control;
			using (var brush = new SolidBrush(behind))
				g.FillRectangle(brush, client);
			var editable = combo.DropDownStyle == ComboBoxStyle.DropDown;
			var hot = combo.Enabled && client.Contains(combo.PointToClient(Control.MousePosition));
			var alpha = combo.Enabled ? 255 : 128;
			if (editable)
				DrawFrame(g, client, combo);
			var saved = Smooth(g);
			var radius = Dip(g, CornerRadius);
			if (!editable)
				using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed(hot ? "BackgroundDarkSelected" : "BackgroundDark", SystemColors.ControlLight))))
				using (var face = RoundedRectangle(client, radius, radius, radius, radius))
					g.FillPath(brush, face);
			g.Restore(saved);
			// Inside the face's line, the room before the text and round the triangle, which stands in the middle of its column.
			var side = (int)Line(g) + (int)Math.Round(Dip(g, ComboBoxPadding));
			DrawDropDownArrow(g, client.Width - side - Dip(g, ComboBoxArrowColumn / 2f), client.Height / 2f,
				Color.FromArgb(alpha, combo.ForeColor));
			if (editable)
				return;
			var text = Rectangle.FromLTRB(side, 0, client.Width - side - (int)Math.Round(Dip(g, ComboBoxPadding + ComboBoxArrowColumn)), client.Height);
			TextRenderer.DrawText(g, combo.Text, combo.Font, text, combo.Enabled ? combo.ForeColor : DisabledText(),
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
			if (combo.Focused && (bool)(ShowFocusCuesProperty?.GetValue(combo, null) ?? false))
				DrawFocus(g, client, combo.ForeColor);
		}

		/// <summary>The room between two tabs, as Default.xaml leaves it after a TabItem.</summary>
		const int TabGap = 4;

		/// <summary>The room inside a TabItem's edge on every side of its text, in device-independent pixels, as in Default.xaml.</summary>
		const int TabTextRoom = 4;

		/// <summary>How far in from the top of its window Windows puts the tabs, in pixels.</summary>
		const int WindowsTabTop = 2;

		/// <summary>
		/// Gives the tabs of a strip painted here the size of a TabItem of Default.xaml, and the tabs of any other strip
		/// their own padding back. A program's own padding and tab size are kept.
		/// </summary>
		/// <remarks>
		/// Windows makes a tab as wide as its text with the padding on either side, gap included. Its height is given
		/// whole: the TabItem's height less the room Windows keeps above the tabs, which the tabs are drawn over, so the page
		/// starts where Default.xaml's does.
		/// The padding goes to the window, because setting <see cref="TabControl.Padding"/> makes the window again, and
		/// a .NET Framework strip that holds the focus then throws as the focus leaves it. Windows measures the tabs
		/// with the padding when it is given the font, which Windows Forms gives no window painted here.
		/// </remarks>
		static void ApplyTabPadding(TabControl tabs, State state)
		{
			state.TabsPadded = PaintsTabs(tabs);
			var x = tabs.Padding.X;
			var y = tabs.Padding.Y;
			var padded = state.TabsPadded && !TypeDescriptor.GetProperties(tabs)[nameof(TabControl.Padding)].ShouldSerializeValue(tabs);
			if (padded)
				x = TabPaddingX();
			NativeMethods.SendMessage(tabs.Handle, NativeMethods.TCM_SETPADDING, IntPtr.Zero, (IntPtr)((y << 16) | (x & 0xFFFF)));
			SetWindowFontMethod?.Invoke(tabs, null);
			state.TabsSized = padded;
			ImageList replaced;
			NativeMethods.SendMessage(tabs.Handle, NativeMethods.TCM_SETIMAGELIST, IntPtr.Zero, TabImageList(tabs, state, out replaced));
			replaced?.Dispose();
			var itemSize = TypeDescriptor.GetProperties(tabs)[nameof(TabControl.ItemSize)];
			if (padded && !itemSize.ShouldSerializeValue(tabs))
			{
				var height = (int)Math.Round(TabHeight(tabs.Font, ControlsHelper.DpiScale)) - WindowsTabTop;
				NativeMethods.SendMessage(tabs.Handle, NativeMethods.TCM_SETITEMSIZE, IntPtr.Zero, (IntPtr)(height << 16));
			}
			// Setting the tab size makes the strip lay its pages out again, in the room the tabs now leave.
			tabs.ItemSize = itemSize.ShouldSerializeValue(tabs) ? tabs.ItemSize : Size.Empty;
		}

		/// <summary>
		/// The height of a TabItem of Default.xaml with text in the font given, at the scale given, in pixels: its top edge's
		/// line and the room above and below the text.
		/// </summary>
		static float TabHeight(Font font, float scale)
		{
			return font.GetHeight(96f * scale) + LineWidth() + TabTextRoom * 2 * scale;
		}

		/// <summary>
		/// The padding Windows is given at either side of a tab's text: the edge's line, the room and half the gap to the next
		/// tab, which each tab is drawn that much narrower to leave.
		/// </summary>
		static int TabPaddingX()
		{
			return LineWidth() + Pixels(TabTextRoom) + Pixels(TabGap) / 2;
		}

		/// <summary>
		/// The picture list Windows sizes a strip's tabs by: for a strip that sizes its tabs as Default.xaml's, blank pictures as
		/// wide as the strip's own less the padding Windows puts between a tab's picture and its text, so a tab with a picture
		/// is as wide as Default.xaml's, the room before its picture as wide as the room after its text; for any other strip its
		/// own list.
		/// </summary>
		/// <remarks>
		/// Windows makes a tab with a picture as wide as its text, its picture and the padding three times: at either side and
		/// between the two. The tabs are drawn here from the strip's own list, so Windows draws nothing from this one.
		/// </remarks>
		/// <param name="replaced">The blank pictures the strip had before, to dispose of once Windows has the new list.</param>
		static IntPtr TabImageList(TabControl tabs, State state, out ImageList replaced)
		{
			replaced = null;
			var own = tabs.ImageList;
			if (own == null || !state.TabsSized)
			{
				replaced = state.TabSizes;
				state.TabSizes = null;
				return own?.Handle ?? IntPtr.Zero;
			}
			// Line, room, picture, room, text, room, line and the gap after, of which Windows gives the text and the padding.
			var room = Pixels(TabTextRoom);
			var width = Pixels(TabGap) + (LineWidth() + room) * 2 + own.ImageSize.Width + room - TabPaddingX() * 3;
			var size = new Size(Math.Max(1, width), own.ImageSize.Height);
			if (state.TabSizes == null || state.TabSizes.ImageSize != size)
			{
				if (state.TabSizes == null)
					tabs.Disposed += (sender, e) => state.TabSizes?.Dispose();
				replaced = state.TabSizes;
				state.TabSizes = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = size };
			}
			return state.TabSizes.Handle;
		}

		/// <summary>Where Windows puts each tab of a strip.</summary>
		static Rectangle[] TabRects(TabControl tabs)
		{
			var rects = new Rectangle[tabs.TabCount];
			for (var i = 0; i < rects.Length; i++)
				rects[i] = tabs.GetTabRect(i);
			return rects;
		}

		/// <summary>Where the tabs end and the page's frame starts: the bottom of the lowest tab.</summary>
		static int TabsBottom(Rectangle[] rects)
		{
			var bottom = 0;
			foreach (var r in rects)
				bottom = Math.Max(bottom, r.Bottom);
			return bottom;
		}

		/// <summary>
		/// Where the pages of a tab strip painted here stand, for the strip's window rectangle given: inside the frame drawn round
		/// them, edge to edge, as Default.xaml's TabControl holds its content a line in from its border. Windows keeps them a
		/// little further in, more at the sides and the bottom than below the tabs.
		/// </summary>
		static NativeMethods.RECT PageArea(TabControl tabs, NativeMethods.RECT window)
		{
			var line = LineWidth();
			var r = window;
			r.Left += line;
			r.Top += TabsBottom(TabRects(tabs)) + line;
			r.Right = Math.Max(r.Left, r.Right - line);
			r.Bottom = Math.Max(r.Top, r.Bottom - line);
			return r;
		}

		/// <summary>Gives a page the shape of the frame round it again at its new size.</summary>
		static void TabPage_SizeChanged(object sender, EventArgs e)
		{
			ClipTabPage((TabPage)sender);
		}

		/// <summary>
		/// Gives a page of a strip painted here the shape of the inside of the frame round it, rounded at the frame's rounded
		/// corners as Default.xaml's TabControl masks its content, so a list or text that fills the page reaches the frame and
		/// leaves its corners whole; and gives a page of any other strip its own shape back.
		/// </summary>
		static void ClipTabPage(TabPage page)
		{
			Region region = null;
			if (PaintsTabs(page.Parent) && page.Width > 0 && page.Height > 0)
			{
				var radius = CornerRadius * ControlsHelper.DpiScale;
				using (var path = RoundedRectangle(new RectangleF(0, 0, page.Width, page.Height), 0, radius, radius, radius))
					region = new Region(path);
			}
			// Windows Forms disposes of the shape it replaces.
			if (region != null || page.Region != null)
				page.Region = region;
		}

		/// <summary>
		/// How far in from the side of its window Windows starts a row of tabs, which the WPF programs start at the side, in
		/// line with the page's edge.
		/// </summary>
		static int TabOffset(Rectangle[] rects)
		{
			var offset = int.MaxValue;
			foreach (var r in rects)
				if (r.X >= 0 && r.X < offset)
					offset = r.X;
			return offset == int.MaxValue ? 0 : offset;
		}

		static readonly PropertyInfo ShowFocusCuesProperty = typeof(Control).GetProperty("ShowFocusCues", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Whether a tab strip with its tabs on top is painted here, as the WPF programs draw one: in the dark theme every
		/// one, and in the light theme one the program draws no tabs of its own on. High contrast keeps the Windows tabs.
		/// </summary>
		static bool PaintsTabs(Control control)
		{
			return control is TabControl tabs && tabs.Alignment == TabAlignment.Top && !SystemInformation.HighContrast
				&& (IsDark || (tabs.Appearance == TabAppearance.Normal && tabs.DrawMode == TabDrawMode.Normal));
		}

		/// <summary>
		/// A tab strip as the WPF programs draw one: each tab a little apart from the next, its top corners rounded, the
		/// chosen one framed and open into the page, the others filled and lighter under the mouse.
		/// </summary>
		/// <remarks>
		/// The tabs keep the sizes Windows gives them, so nothing on the window moves; each is drawn <see cref="TabGap"/>
		/// narrower to leave the room between, from the side of the window, and as high as a TabItem of Default.xaml.
		/// </remarks>
		static void Tabs_Paint(object sender, PaintEventArgs e)
		{
			var tabs = (TabControl)sender;
			var g = e.Graphics;
			using (var brush = new SolidBrush(tabs.Parent?.BackColor ?? SystemColors.Control))
				g.FillRectangle(brush, tabs.ClientRectangle);
			if (tabs.TabCount == 0)
				return;
			State state;
			var hot = States.TryGetValue(tabs, out state) ? state.HotTab : -1;
			var rects = TabRects(tabs);
			var bottom = TabsBottom(rects);
			var offset = TabOffset(rects);
			var height = (int)Math.Round(TabHeight(tabs.Font, g.DpiY / 96f));
			var saved = Smooth(g);
			var border = Themed("BorderDark", SystemColors.ControlDark);
			var line = Line(g);
			var radius = Dip(g, CornerRadius);
			var gap = (int)Math.Round(Dip(g, TabGap));
			// The page, framed, its top left corner square where the first tab stands.
			var pane = new Rectangle(0, bottom, tabs.ClientSize.Width, tabs.ClientSize.Height - bottom);
			using (var brush = new SolidBrush(Themed("BackgroundLight", SystemColors.Control)))
			using (var fill = RoundedRectangle(pane, 0, radius, radius, radius))
				g.FillPath(brush, fill);
			using (var pen = new Pen(border, line))
			using (var frame = RoundedRectangle(Inset(pane, line / 2), 0, radius, radius, radius))
				g.DrawPath(pen, frame);
			for (var i = 0; i < rects.Length; i++)
			{
				var selected = i == tabs.SelectedIndex;
				// Each tab stands on the page's top line; the chosen one comes down over it, open into the page.
				var r = rects[i];
				var top = Math.Max(0, Math.Min(r.Y, r.Bottom - height));
				var bounds = new Rectangle(r.X - offset, top, Math.Max(1, r.Width - gap), bottom - top + (selected ? (int)line : 0));
				var fill = selected ? Themed("BackgroundTabSelected", SystemColors.Window)
					: i == hot && tabs.Enabled ? Themed("BackgroundDarkSelected", SystemColors.ControlLight)
					: Themed("BackgroundDark", SystemColors.ControlLight);
				using (var brush = new SolidBrush(fill))
				using (var face = RoundedRectangle(bounds, radius, radius, 0, 0))
					g.FillPath(brush, face);
				if (selected)
					using (var pen = new Pen(border, line))
					using (var edge = TabEdge(Inset(bounds, line / 2), radius))
						g.DrawPath(pen, edge);
				// The text and image of every tab sit at the same height, below the tab's top edge: the line the chosen one
				// reaches over into the page is not part of it.
				var content = new Rectangle(bounds.X, top + (int)line, bounds.Width, bottom - top - (int)line);
				var page = tabs.TabPages[i];
				var text = !tabs.Enabled ? SystemColors.GrayText
					: Themed(selected ? "ForegroundTabSelected" : "ForegroundTab", SystemColors.ControlText);
				const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
				var index = GetTabImageIndex(tabs, page);
				if (index >= 0)
				{
					// The picture where the tab's room starts and the text the room after it, as Default.xaml's header; the tab is
					// sized for it (TabImageList). Drawn by the list itself: taking an image out of it copies the image on every
					// paint, and fails for a list read from a resource stream.
					var size = tabs.ImageList.ImageSize;
					var room = (int)Math.Round(Dip(g, TabTextRoom));
					var left = content.X + (int)line + room;
					var textLeft = left + size.Width + room;
					tabs.ImageList.Draw(g, left, content.Y + (content.Height - size.Height) / 2, index);
					TextRenderer.DrawText(g, page.Text, tabs.Font, new Rectangle(textLeft, content.Y, content.Right - textLeft, content.Height), text, flags);
				}
				else
					TextRenderer.DrawText(g, page.Text, tabs.Font, content, text, flags | TextFormatFlags.HorizontalCenter);
			}
			g.Restore(saved);
			// The chosen tab is marked while the strip has the keyboard focus and focus is being shown, as Default.xaml marks
			// a TabItem: the theme's focus mark round the tab, over the line below it.
			if (tabs.SelectedIndex >= 0 && tabs.Focused && (bool)(ShowFocusCuesProperty?.GetValue(tabs, null) ?? false))
			{
				var r = rects[tabs.SelectedIndex];
				var top = Math.Max(0, Math.Min(r.Y, r.Bottom - height));
				DrawFocus(g, new Rectangle(r.X - offset, top, Math.Max(1, r.Width - gap), bottom - top + (int)line), Themed("ForegroundBlack", SystemColors.ControlText));
			}
		}

		/// <summary>The edge of the chosen tab: up its left side, round its top corners and down its right, open at the bottom into the page.</summary>
		static GraphicsPath TabEdge(RectangleF r, float radius)
		{
			var path = new GraphicsPath();
			path.AddLine(r.Left, r.Bottom, r.Left, r.Top + radius);
			path.AddArc(r.Left, r.Top, radius * 2, radius * 2, 180, 90);
			path.AddArc(r.Right - radius * 2, r.Top, radius * 2, radius * 2, 270, 90);
			path.AddLine(r.Right, r.Top + radius, r.Right, r.Bottom);
			return path;
		}

		static void Tabs_MouseMove(object sender, MouseEventArgs e)
		{
			SetHotTab((TabControl)sender, e.Location);
		}

		static void Tabs_MouseLeave(object sender, EventArgs e)
		{
			SetHotTab((TabControl)sender, null);
		}

		/// <summary>Lights the tab under the mouse, as Windows does, and repaints the strip only when that tab changes.</summary>
		static void SetHotTab(TabControl tabs, Point? mouse)
		{
			State state;
			if (!States.TryGetValue(tabs, out state))
				return;
			var hot = -1;
			for (var i = 0; mouse.HasValue && i < tabs.TabCount; i++)
				if (tabs.GetTabRect(i).Contains(mouse.Value))
					hot = i;
			if (hot == state.HotTab)
				return;
			state.HotTab = hot;
			tabs.Invalidate();
		}

		/// <summary>The place of a page's image in its tab control's list, or -1 when it has none.</summary>
		static int GetTabImageIndex(TabControl tabs, TabPage page)
		{
			var list = tabs.ImageList;
			if (list == null)
				return -1;
			if (page.ImageIndex >= 0 && page.ImageIndex < list.Images.Count)
				return page.ImageIndex;
			return string.IsNullOrEmpty(page.ImageKey) ? -1 : list.Images.IndexOfKey(page.ImageKey);
		}

		/// <summary>The colour of a Default.xaml key on a slider: at half strength while it is disabled, as Default.xaml fades one.</summary>
		static Color SliderColor(TrackBar track, string key, Color fallback)
		{
			var color = Themed(key, fallback);
			return track.Enabled ? color : Half(color, track.BackColor);
		}

		/// <summary>The height of WPF's slider track, in device-independent pixels.</summary>
		const int SliderTrackHeight = 4;

		/// <summary>How far in from each end of WPF's slider its track starts, in device-independent pixels.</summary>
		const int SliderTrackMargin = 4;

		/// <summary>The width of WPF's slider thumb, in device-independent pixels.</summary>
		const int SliderThumbWidth = 12;

		/// <summary>The length of WPF's slider thumb, toward its ticks, in device-independent pixels.</summary>
		const int SliderThumbLength = 16;

		/// <summary>How much of WPF's pointed thumb's length is straight before it points at the ticks, in device-independent pixels.</summary>
		const int SliderThumbStraight = 12;

		/// <summary>How far WPF's slider ticks stand from its thumb, and how long the longest are, in device-independent pixels.</summary>
		const int SliderTickGap = 4;
		const int SliderTickLength = 4;

		/// <summary>
		/// A slider as WPF draws one: a thin track, ticks in the theme's control colour and the thumb pointing at them, the
		/// thumb travelling from one end of the slider to the other and whole at both. The thumb of the slider with the
		/// focus is drawn in the selection colours, which show the slider the keys move.
		/// </summary>
		/// <remarks>
		/// Windows keeps its thumb further in, so what it places along the way is drawn as far along WPF's way, and the mouse
		/// is passed on the other way round.
		/// </remarks>
		static void Track_Paint(object sender, PaintEventArgs e)
		{
			var track = (TrackBar)sender;
			var g = e.Graphics;
			var vertical = track.Orientation == Orientation.Vertical;
			using (var brush = new SolidBrush(track.BackColor))
				g.FillRectangle(brush, track.ClientRectangle);
			float nativeFirst, nativeLast, first, last;
			GetSliderTravel(track, out nativeFirst, out nativeLast, out first, out last);
			var thumb = GetTrackRectangle(track, NativeMethods.TBM_GETTHUMBRECT);
			var middle = vertical ? thumb.Y + thumb.Height / 2f : thumb.X + thumb.Width / 2f;
			var across = SliderShift(track);
			thumb.Offset(vertical ? across : 0, vertical ? 0 : across);
			var thumbWidth = Pixels(SliderThumbWidth);
			var thumbLength = Pixels(SliderThumbLength);
			// Where WPF's thumb starts along the way, in whole pixels: its width in whole pixels is within half a pixel of WPF's,
			// so it starts on the slider's first pixel at the lowest value and ends on its last at the highest.
			var start = (int)Math.Round(Remap(middle, nativeFirst, nativeLast, first, last) - thumbWidth / 2f);
			// Across, its middle where SliderShift puts the middle of Windows' thumb.
			var box = vertical
				? new Rectangle(thumb.X + thumb.Width / 2 - thumbLength / 2, start, thumbLength, thumbWidth)
				: new Rectangle(start, thumb.Y + thumb.Height / 2 - thumbLength / 2, thumbWidth, thumbLength);
			// WPF's track: 4 high, in the border colour, 4 in from the ends, through the middle of the thumb.
			var height = (int)Math.Round(Dip(g, SliderTrackHeight));
			var margin = (int)Math.Round(Dip(g, SliderTrackMargin));
			var length = vertical ? track.ClientSize.Height : track.ClientSize.Width;
			var bar = vertical
				? new Rectangle(thumb.X + (thumb.Width - height) / 2, margin, height, length - margin * 2)
				: new Rectangle(margin, thumb.Y + (thumb.Height - height) / 2, length - margin * 2, height);
			using (var brush = new SolidBrush(SliderColor(track, "BorderDark", SystemColors.ControlDark)))
				g.FillRectangle(brush, bar);
			if (track.TickStyle != TickStyle.None)
				DrawTicks(track, g, first, last, thumb, vertical);
			DrawThumb(track, g, box, vertical);
		}

		/// <summary>
		/// Makes a slider that sizes itself as tall across as Default.xaml's: its thumb and the ticks beside it, where Windows
		/// makes one eight thirds of a scroll bar; gives it its own sizing back where Windows draws it.
		/// </summary>
		static void FitSlider(TrackBar track, State state)
		{
			if (!state.AutoSize)
				return;
			if (SystemInformation.HighContrast)
			{
				track.AutoSize = true;
				return;
			}
			var ticks = track.TickStyle == TickStyle.Both ? 2 : track.TickStyle == TickStyle.None ? 0 : 1;
			var across = Pixels(SliderThumbLength) + ticks * Pixels(SliderTickGap + SliderTickLength);
			track.AutoSize = false;
			if (track.Orientation == Orientation.Vertical)
				track.Width = across;
			else
				track.Height = across;
		}

		/// <summary>
		/// How far the thumb and track are drawn across from where Windows has them: where WPF's stand, the thumb and the ticks
		/// beside it together in the middle of the slider's height, as WPF's slider stands in the middle of the room it is
		/// given, where Windows keeps them at the top.
		/// </summary>
		static int SliderShift(TrackBar track)
		{
			var thumb = GetTrackRectangle(track, NativeMethods.TBM_GETTHUMBRECT);
			var vertical = track.Orientation == Orientation.Vertical;
			var room = vertical ? track.ClientSize.Width : track.ClientSize.Height;
			var ticks = Pixels(SliderTickGap + SliderTickLength);
			var before = track.TickStyle == TickStyle.TopLeft || track.TickStyle == TickStyle.Both ? ticks : 0;
			var after = track.TickStyle == TickStyle.BottomRight || track.TickStyle == TickStyle.Both ? ticks : 0;
			var length = Pixels(SliderThumbLength);
			var middle = (room - before - length - after) / 2 + before + length / 2;
			return middle - (vertical ? thumb.Left + thumb.Width / 2 : thumb.Top + thumb.Height / 2);
		}

		static Rectangle GetTrackRectangle(TrackBar track, int message)
		{
			var rect = new NativeMethods.RECT();
			NativeMethods.SendMessage(track.Handle, message, IntPtr.Zero, ref rect);
			return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
		}

		/// <summary>
		/// Where the middle of a slider's thumb stands at the lowest and highest value: as Windows has it, inside its rail, and
		/// as WPF draws it, half the thumb's width in from the slider's ends.
		/// </summary>
		static void GetSliderTravel(TrackBar track, out float nativeFirst, out float nativeLast, out float first, out float last)
		{
			var vertical = track.Orientation == Orientation.Vertical;
			// Windows gives the rail of an upright slider as if it lay flat.
			var rail = GetTrackRectangle(track, NativeMethods.TBM_GETCHANNELRECT);
			var thumb = GetTrackRectangle(track, NativeMethods.TBM_GETTHUMBRECT);
			var half = (vertical ? thumb.Height : thumb.Width) / 2f;
			nativeFirst = rail.Left + half;
			nativeLast = rail.Right - half;
			first = SliderThumbWidth * ControlsHelper.DpiScale / 2;
			last = (vertical ? track.ClientSize.Height : track.ClientSize.Width) - first;
		}

		/// <summary>
		/// Ticks one pixel wide and 4 long, as Default.xaml draws every one, in the colour WPF's are, as far from WPF's thumb
		/// as WPF has them.
		/// </summary>
		static void DrawTicks(TrackBar track, Graphics g, float first, float last, Rectangle thumb, bool vertical)
		{
			// The ends are where the middle of the thumb stands at the lowest and at the highest value, and a
			// tick stands at every step of the tick frequency between them.
			var range = Math.Max(1, track.Maximum - track.Minimum);
			var step = Math.Max(1, track.TickFrequency);
			var places = new List<int> { (int)first, (int)last };
			for (var value = step; value < range; value += step)
				places.Add((int)(first + (last - first) * value / range));
			var after = track.TickStyle != TickStyle.TopLeft;
			var before = track.TickStyle != TickStyle.BottomRight;
			var scale = g.DpiY / 96f;
			// From the middle of the thumb drawn, past its end and the gap; Windows' own thumb is another length.
			var middle = vertical ? thumb.X + thumb.Width / 2 : thumb.Y + thumb.Height / 2;
			var reach = Pixels(SliderThumbLength) / 2 + Pixels(SliderTickGap);
			using (var brush = new SolidBrush(SliderColor(track, "BackgroundDark", SystemColors.ControlDark)))
			{
				for (var i = 0; i < places.Count; i++)
				{
					var length = (int)Math.Round(SliderTickLength * scale);
					if (vertical && after)
						g.FillRectangle(brush, middle + reach, places[i], length, 1);
					if (vertical && before)
						g.FillRectangle(brush, middle - reach - length, places[i], length, 1);
					if (!vertical && after)
						g.FillRectangle(brush, places[i], middle + reach, 1, length);
					if (!vertical && before)
						g.FillRectangle(brush, places[i], middle - reach - length, 1, length);
				}
			}
		}

		/// <summary>
		/// Default.xaml's thumb, 12 by 16, straight for 12 of its 16 and pointing the last 4 at the ticks, or a plain rectangle
		/// without them, filled in the page colour, lit and pressed as a button is, and edged in the grey border colour; while
		/// the slider has the focus, filled as a lit button and edged in the selection colour.
		/// </summary>
		/// <remarks>
		/// The pointed thumb's edge runs through the middle of whole pixels, so each slope is as solid as the straight sides.
		/// It is drawn once pointing down, its left half copied onto its right as in a mirror, so both slopes are the same
		/// pixels, as WPF draws them, and turned to the side the ticks are on.
		/// </remarks>
		/// <param name="thumb">The place and size of the thumb, in whole pixels; its edge is drawn inside it.</param>
		static void DrawThumb(TrackBar track, Graphics g, Rectangle thumb, bool vertical)
		{
			var focused = track.Enabled && track.Focused;
			var key = !track.Enabled ? "BackgroundLight"
				: track.Capture && (Control.MouseButtons & MouseButtons.Left) != 0 ? "BackgroundDarkPressed"
				: focused || thumb.Contains(track.PointToClient(Control.MousePosition)) ? "BackgroundDarkSelected"
				: "BackgroundLight";
			var fill = SliderColor(track, key, SystemColors.Control);
			var edge = focused
				? SliderColor(track, "SelectedBackground", SystemColors.Highlight)
				: SliderColor(track, "BorderGray", SystemColors.ControlDark);
			var line = Line(g);
			if (track.TickStyle != TickStyle.BottomRight && track.TickStyle != TickStyle.TopLeft)
			{
				// Square ended, with an edge one line wide: every side as sharp as the others.
				using (var brush = new SolidBrush(edge))
					g.FillRectangle(brush, thumb);
				var inside = thumb;
				inside.Inflate(-(int)line, -(int)line);
				using (var brush = new SolidBrush(fill))
					g.FillRectangle(brush, inside);
				return;
			}
			// The shape lying flat, pointing down: across the slider as wide as the thumb, along the way to the ticks as long,
			// the line half its width in from the outside.
			var width = vertical ? thumb.Height : thumb.Width;
			var length = vertical ? thumb.Width : thumb.Height;
			var straight = Math.Min(length, Pixels(SliderThumbStraight));
			var half = line / 2;
			// The point ends on the middle of one pixel, or of the two in the middle of an even width.
			var tipLeft = (width - line) % 2 == 0 ? width / 2f : width / 2f - 0.5f;
			var tipRight = width - tipLeft;
			var points = new[]
			{
				new PointF(half, half), new PointF(width - half, half), new PointF(width - half, straight - half),
				new PointF(tipRight, length - half), new PointF(tipLeft, length - half), new PointF(half, straight - half),
			};
			// Not premultiplied, so a pixel copied across reads back as it was drawn.
			using (var shape = new Bitmap(width, length, PixelFormat.Format32bppArgb))
			{
				using (var sg = Graphics.FromImage(shape))
				{
					sg.SmoothingMode = SmoothingMode.AntiAlias;
					sg.PixelOffsetMode = PixelOffsetMode.Half;
					using (var brush = new SolidBrush(fill))
						sg.FillPolygon(brush, points);
					using (var pen = new Pen(edge, line))
						sg.DrawPolygon(pen, points);
				}
				for (var y = 0; y < length; y++)
					for (var x = 0; x < width / 2; x++)
						shape.SetPixel(width - 1 - x, y, shape.GetPixel(x, y));
				var turn = vertical
					? (track.TickStyle == TickStyle.TopLeft ? RotateFlipType.Rotate90FlipNone : RotateFlipType.Rotate270FlipNone)
					: (track.TickStyle == TickStyle.TopLeft ? RotateFlipType.RotateNoneFlipY : RotateFlipType.RotateNoneFlipNone);
				shape.RotateFlip(turn);
				g.DrawImageUnscaled(shape, thumb.Location);
			}
		}

		/// <summary>Whether a progress bar is painted here as WPF draws one: one that shows how far it is, while high contrast is off.</summary>
		static bool PaintsProgress(Control control)
		{
			return control is ProgressBar bar && bar.Style != ProgressBarStyle.Marquee && !SystemInformation.HighContrast;
		}

		/// <summary>
		/// A progress bar as Default.xaml has WPF draw one: its track in a button's colour, edged as a border is, filled flat in
		/// the active colour as far as it has gone, over the edge.
		/// </summary>
		static void Progress_Paint(object sender, PaintEventArgs e)
		{
			var bar = (ProgressBar)sender;
			var g = e.Graphics;
			var client = bar.ClientRectangle;
			using (var brush = new SolidBrush(Themed("BackgroundDark", SystemColors.ControlLight)))
				g.FillRectangle(brush, client);
			using (var pen = new Pen(Themed("BorderDark", SystemColors.ControlDark)))
				g.DrawRectangle(pen, 0, 0, client.Width - 1, client.Height - 1);
			var range = Math.Max(1, bar.Maximum - bar.Minimum);
			var done = (int)Math.Round(client.Width * (double)(bar.Value - bar.Minimum) / range);
			var left = bar.RightToLeftLayout && bar.RightToLeft == RightToLeft.Yes ? client.Right - done : client.X;
			using (var brush = new SolidBrush(Themed("ColorActive", SystemColors.Highlight)))
				g.FillRectangle(brush, left, client.Y, done, client.Height);
		}

		#endregion

		#region Grids

		/// <summary>The room above and below a cell's text, inside the lines between rows, in device-independent pixels.</summary>
		const int GridCellRoom = 4;

		/// <summary>The room above and below a header's text, in device-independent pixels.</summary>
		const int GridHeaderRoom = 4;

		/// <summary>
		/// The room before the header and cells of the first column shown, in device-independent pixels, so its text, picture or
		/// check box does not stand against the grid's edge.
		/// </summary>
		const int GridFirstColumnRoom = 4;

		/// <summary>The room before a cell's check box, in device-independent pixels: CheckBoxCell's margin.</summary>
		const int GridCheckBoxMargin = 4;

		/// <summary>The room at each side of a cell's content, in device-independent pixels: DataGridCell_Default's padding.</summary>
		const int GridCellPadding = 4;

		/// <summary>
		/// Whether a grid is drawn as the WPF programs draw one: one whose headers the program left to Windows, while high
		/// contrast is off. A grid that draws its own headers keeps the look it gives itself.
		/// </summary>
		static bool PaintsGrid(DataGridView grid, State state)
		{
			return state.EnableHeadersVisualStyles && !SystemInformation.HighContrast;
		}

		/// <summary>
		/// Gives a grid Default.xaml's look, or its own back: no edge, the window colour behind its rows, lines only between
		/// rows, in the page colour, or none in a grid made without them, which tells its rows apart by colour, headers without
		/// lines, padded as Default.xaml pads them and with room above and below their text, as tall as that text and its room,
		/// rows at least as tall as Default.xaml makes them, and room before the first column shown.
		/// </summary>
		static void ApplyGridLook(DataGridView grid, State state, bool wpf)
		{
			var look = state.Grid;
			if (look == null)
			{
				if (!wpf)
					return;
				look = state.Grid = new GridLook
				{
					BorderStyle = grid.BorderStyle,
					BackgroundColor = grid.BackgroundColor,
					GridColor = grid.GridColor,
					CellBorderStyle = grid.CellBorderStyle,
					HeadersBorderStyle = grid.ColumnHeadersBorderStyle,
					HeaderPadding = grid.ColumnHeadersDefaultCellStyle.Padding,
					HeadersHeightSizeMode = grid.ColumnHeadersHeightSizeMode,
					HeadersHeight = grid.ColumnHeadersHeight,
					RowHeight = grid.RowTemplate.Height,
					ImagePadding = grid.Columns.OfType<DataGridViewImageColumn>()
						.Select(x => new KeyValuePair<DataGridViewImageColumn, Padding>(x, x.DefaultCellStyle.Padding)).ToList(),
				};
			}
			// The first column's own paddings come back before the pictures' are set, and its room is given again after.
			UnpadFirstColumn(look);
			// A picture stands in from each side of its cell by Default.xaml's cell padding, as text does.
			foreach (var pair in look.ImagePadding)
				pair.Key.DefaultCellStyle.Padding = wpf ? new Padding(Pixels(GridCellPadding), 0, Pixels(GridCellPadding), 0) : pair.Value;
			grid.BorderStyle = wpf ? BorderStyle.None : look.BorderStyle;
			grid.BackgroundColor = wpf ? Themed("BackgroundWhite", SystemColors.Window) : look.BackgroundColor;
			grid.GridColor = wpf ? Themed("BackgroundLight", SystemColors.Control) : look.GridColor;
			grid.CellBorderStyle = wpf && look.CellBorderStyle != DataGridViewCellBorderStyle.None ? DataGridViewCellBorderStyle.SingleHorizontal : look.CellBorderStyle;
			grid.ColumnHeadersBorderStyle = wpf ? DataGridViewHeaderBorderStyle.None : look.HeadersBorderStyle;
			// Default.xaml pads a header 16 on the right for the sort triangle; Windows Forms leaves that room itself. The
			// headers are as tall as their text and the room above and below it, without a line.
			var room = Pixels(GridHeaderRoom);
			grid.ColumnHeadersDefaultCellStyle.Padding = wpf ? new Padding(Pixels(GridHeaderPadding.Left), room, 0, room) : look.HeaderPadding;
			grid.ColumnHeadersHeightSizeMode = wpf ? DataGridViewColumnHeadersHeightSizeMode.DisableResizing : look.HeadersHeightSizeMode;
			if (wpf)
				grid.ColumnHeadersHeight = (grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font).Height + room * 2;
			else if (look.HeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize)
				grid.ColumnHeadersHeight = look.HeadersHeight;
			// A row is as tall as its text, the room above and below it, and the line below it, or as the program made it.
			var cellFont = grid.RowsDefaultCellStyle.Font ?? grid.DefaultCellStyle.Font ?? grid.Font;
			var rowHeight = wpf ? Math.Max(look.RowHeight, cellFont.Height + Pixels(GridCellRoom) * 2 + LineWidth()) : look.RowHeight;
			grid.RowTemplate.Height = rowHeight;
			if (grid.AutoSizeRowsMode == DataGridViewAutoSizeRowsMode.None)
				for (var i = 0; i < grid.Rows.Count; i++)
					grid.Rows[i].Height = rowHeight;
			if (wpf)
				PadFirstColumn(grid, look);
			LendPadding(grid, wpf);
		}

		/// <summary>
		/// Lets a list that fills its container stand edge to edge in it: the container's side padding goes to the controls
		/// docked above and below the list, as room inside them, so they stay where the padding put them, and its padding above
		/// or below the list goes where nothing is docked there; all of it comes back when the list is drawn as Windows draws it.
		/// </summary>
		/// <remarks>
		/// Windows Forms docks every control inside its container's padding, and gives a docked control no margin. A tool bar
		/// takes the room as its padding, before its first button and after its last; any other control as its own padding,
		/// which moves what it holds. A control whose own edge is drawn, a frame or a line, is put in a container of its own to
		/// be moved.
		/// </remarks>
		static void LendPadding(DataGridView grid, bool wpf)
		{
			var parent = grid.Parent;
			State state;
			if (parent == null || !States.TryGetValue(parent, out state))
				return;
			var own = parent.Padding;
			var docked = parent.Controls.Cast<Control>()
				.Where(x => x != grid && (x.Dock == DockStyle.Top || x.Dock == DockStyle.Bottom)).ToList();
			var top = docked.Any(x => x.Dock == DockStyle.Top) ? own.Top : 0;
			var bottom = docked.Any(x => x.Dock == DockStyle.Bottom) ? own.Bottom : 0;
			if (wpf && state.LentPadding == null && grid.Dock == DockStyle.Fill && !(parent is TableLayoutPanel)
				&& !(parent is FlowLayoutPanel) && (own.Left > 0 || own.Right > 0 || top != own.Top || bottom != own.Bottom))
			{
				state.LentPadding = own;
				state.LentTo = docked;
				parent.Padding = new Padding(0, top, 0, bottom);
				foreach (var control in state.LentTo)
					Lend(control, own.Left, own.Right);
			}
			else if (!wpf && state.LentPadding != null)
			{
				own = state.LentPadding.Value;
				var lentTo = state.LentTo;
				state.LentPadding = null;
				state.LentTo = null;
				foreach (var control in lentTo)
					Lend(control, -own.Left, -own.Right);
				parent.Padding = own;
			}
		}

		/// <summary>Gives a control docked beside a list the room lent to it, or takes it back.</summary>
		static void Lend(Control control, int left, int right)
		{
			State state;
			if (control is ToolStrip strip)
			{
				// A tool bar the theme has not reached yet takes the room as it is reached.
				if (!States.TryGetValue(strip, out state))
					return;
				var padding = ToolBarPadding(strip, state, IsWpfToolBar(strip) && state.RenderMode != ToolStripRenderMode.Custom);
				if (strip.Padding != padding)
					strip.Padding = padding;
				return;
			}
			var p = control.Padding;
			control.Padding = new Padding(p.Left + left, p.Top, p.Right + right, p.Bottom);
		}

		/// <summary>The room a list lent a control docked beside it, at its sides; none for any other control.</summary>
		static Padding LentRoom(Control control)
		{
			State state;
			if (control.Parent == null || !States.TryGetValue(control.Parent, out state) || state.LentPadding == null
				|| !state.LentTo.Contains(control))
				return Padding.Empty;
			var lent = state.LentPadding.Value;
			return new Padding(lent.Left, 0, lent.Right, 0);
		}

		/// <summary>
		/// Gives the first column shown room before its header and cells, in its own styles, so Windows Forms widens a column
		/// that sizes itself by that room too; and gives the column that had it its own paddings back.
		/// </summary>
		/// <remarks>
		/// A picture or check box moves right with the room, as text does: the theme's check boxes stand in by the cell's
		/// padding, and Windows Forms puts a picture inside it.
		/// </remarks>
		static void PadFirstColumn(DataGridView grid, GridLook look)
		{
			var column = grid.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
			// While a grid makes its columns from its data, the first may not be wholly the grid's yet: the grid is gone
			// through again once its data is bound.
			if (column != null && column.HeaderCell.DataGridView != grid)
				return;
			if (column == look.First)
				return;
			UnpadFirstColumn(look);
			if (column == null)
				return;
			// Marked first, so the changes below, which the grid may answer by asking again, find it done.
			look.First = column;
			// A column copied from one given the room has its paddings with the room: its own are that column's.
			var own = OwnColumnPaddings.GetValue(column, x =>
			{
				var current = new[] { x.DefaultCellStyle.Padding, x.HeaderCell.Style.Padding };
				return look.LastPadded != null && current[0] == look.LastPadded[0] && current[1] == look.LastPadded[1] ? look.LastOwn : current;
			});
			// The room is added to the padding the column's cells and header have of their own, or the grid's.
			var room = Pixels(GridFirstColumnRoom);
			var cells = own[0] == Padding.Empty ? grid.DefaultCellStyle.Padding : own[0];
			var header = own[1] == Padding.Empty ? grid.ColumnHeadersDefaultCellStyle.Padding : own[1];
			look.LastOwn = own;
			look.LastPadded = new[]
			{
				new Padding(cells.Left + room, cells.Top, cells.Right, cells.Bottom),
				new Padding(header.Left + room, header.Top, header.Right, header.Bottom),
			};
			column.DefaultCellStyle.Padding = look.LastPadded[0];
			column.HeaderCell.Style.Padding = look.LastPadded[1];
		}

		/// <summary>Gives the column that was given the room before its header and cells its own paddings back.</summary>
		static void UnpadFirstColumn(GridLook look)
		{
			var column = look.First;
			Padding[] own;
			if (column == null || !OwnColumnPaddings.TryGetValue(column, out own))
				return;
			// Unmarked last, so the changes below, which the grid may answer by asking again, do not pad it again.
			column.DefaultCellStyle.Padding = own[0];
			column.HeaderCell.Style.Padding = own[1];
			look.First = null;
		}

		/// <summary>
		/// Gives the room before the first column to the column shown first now, as columns come, go, move, show or hide, and
		/// once a grid has made its columns from its data.
		/// </summary>
		static void Grid_ColumnsChanged(object sender, EventArgs e)
		{
			var changed = e as DataGridViewColumnStateChangedEventArgs;
			if (changed != null && changed.StateChanged != DataGridViewElementStates.Visible)
				return;
			var grid = (DataGridView)sender;
			State state;
			if (States.TryGetValue(grid, out state) && state.Grid != null && PaintsGrid(grid, state))
				PadFirstColumn(grid, state.Grid);
		}

		static void LinkGrid_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
		{
			if (e.StateChanged == DataGridViewElementStates.Selected && e.Row.Index >= 0)
				foreach (DataGridViewCell cell in e.Row.Cells)
					ColorSelectedLink(cell);
		}

		static void LinkGrid_CellStateChanged(object sender, DataGridViewCellStateChangedEventArgs e)
		{
			if (e.StateChanged == DataGridViewElementStates.Selected)
				ColorSelectedLink(e.Cell);
		}

		/// <summary>
		/// Draws a link cell in the selection's text colour while it is selected, as the rest of its row, because the link
		/// colour does not read on the selection, and in its column's link colours otherwise.
		/// </summary>
		static void ColorSelectedLink(DataGridViewCell cell)
		{
			var link = cell as DataGridViewLinkCell;
			var column = cell.OwningColumn as DataGridViewLinkColumn;
			if (link == null || column == null || cell.RowIndex < 0)
				return;
			var selected = (cell.InheritedState & DataGridViewElementStates.Selected) != 0;
			var text = cell.InheritedStyle.SelectionForeColor;
			link.LinkColor = selected ? text : column.LinkColor;
			link.ActiveLinkColor = selected ? text : column.ActiveLinkColor;
			link.VisitedLinkColor = selected ? text : column.VisitedLinkColor;
		}

		/// <summary>
		/// Paints a grid's headers as Default.xaml does, from the header colour at the top to the page colour at the bottom
		/// and without lines, and its check boxes as CheckBoxCell does: Default.xaml's check box at the cell's left, in from
		/// it by the cell's padding too.
		/// </summary>
		static void WpfGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var bounds = e.CellBounds;
			if (e.RowIndex < 0)
			{
				if (bounds.Width > 0 && bounds.Height > 0)
					using (var brush = new LinearGradientBrush(new Rectangle(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height + 2),
						Themed("BackgroundHeader", SystemColors.Window), Themed("BackgroundLight", SystemColors.Control), LinearGradientMode.Vertical))
						e.Graphics.FillRectangle(brush, bounds);
				if (e.ColumnIndex >= 0 && grid.RightToLeft != RightToLeft.Yes)
					PaintGridHeader(e, grid.Columns[e.ColumnIndex]);
				else
					e.Paint(bounds, DataGridViewPaintParts.ContentForeground);
				e.Handled = true;
				return;
			}
			if (e.ColumnIndex < 0 || !(grid.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
				|| (e.PaintParts & DataGridViewPaintParts.ContentForeground) == 0)
				return;
			if (!e.Handled)
			{
				e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.ContentForeground);
				e.Handled = true;
			}
			var font = e.CellStyle.Font ?? grid.Font;
			var size = CheckBoxBox(font, false);
			var height = Math.Min(size.Height, bounds.Height);
			var box = new Rectangle(bounds.X + e.CellStyle.Padding.Left + Pixels(GridCheckBoxMargin), bounds.Y + (bounds.Height - height) / 2, size.Width, height);
			// Padding can move the box out of the cell, as a program hides one there.
			if (box.Left >= bounds.Right)
				return;
			var check = e.FormattedValue is CheckState checkState ? checkState
				: e.FormattedValue is bool isChecked && isChecked ? CheckState.Checked : CheckState.Unchecked;
			var hot = grid.Enabled && (HasCellFocus(grid, e.ColumnIndex, e.RowIndex) || bounds.Contains(grid.PointToClient(Control.MousePosition)));
			DrawWpfCheck(e.Graphics, box, CheckScale(font), check, false, hot, grid.Enabled);
		}

		/// <summary>
		/// What Default.xaml's DataGridHeader_Sorting keeps round a header's text, in device-independent pixels: 4 on the left,
		/// and 16 on the right for the sort triangle.
		/// </summary>
		static readonly Padding GridHeaderPadding = new Padding(4, 0, 16, 0);

		/// <summary>The size of the sort triangle in Default.xaml's DataGridHeader_Sorting, and its room from the right, in device-independent pixels.</summary>
		static readonly SizeF GridSortTriangle = new SizeF(8, 4);
		const int GridSortTriangleRight = 4;

		/// <summary>
		/// A header's text and sort triangle where Default.xaml's DataGridHeader_Sorting puts them, which Windows Forms puts
		/// further in: the text in the middle of the header's height, clear of the room kept round it, in from either side by
		/// the header's padding, and the triangle in from the right, pointing down while the column is sorted ascending and up while
		/// sorted descending. Windows Forms leaves no room for a triangle in a column that cannot be sorted, nor does the text.
		/// </summary>
		static void PaintGridHeader(DataGridViewCellPaintingEventArgs e, DataGridViewColumn column)
		{
			var g = e.Graphics;
			var bounds = e.CellBounds;
			var style = e.CellStyle;
			var right = column.SortMode == DataGridViewColumnSortMode.NotSortable ? 0 : Pixels(GridHeaderPadding.Right);
			var text = Rectangle.FromLTRB(bounds.Left + style.Padding.Left, bounds.Top, bounds.Right - right - style.Padding.Right, bounds.Bottom);
			var flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
			// A header whose style wraps its text, in a header row a program made two lines tall or more, breaks it between
			// words, its lines together in the middle of its height.
			var font = style.Font ?? column.DataGridView.Font;
			if (style.WrapMode == DataGridViewTriState.True && text.Width > 0 && text.Height >= font.Height * 2)
			{
				flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
				var lines = TextRenderer.MeasureText(g, e.FormattedValue as string ?? column.HeaderText, font, new Size(text.Width, int.MaxValue), flags).Height;
				text = new Rectangle(text.X, text.Y + Math.Max(0, (text.Height - lines) / 2), text.Width, Math.Min(text.Height, lines));
			}
			var align = style.Alignment;
			if (align == DataGridViewContentAlignment.TopCenter || align == DataGridViewContentAlignment.MiddleCenter || align == DataGridViewContentAlignment.BottomCenter)
				flags |= TextFormatFlags.HorizontalCenter;
			else if (align == DataGridViewContentAlignment.TopRight || align == DataGridViewContentAlignment.MiddleRight || align == DataGridViewContentAlignment.BottomRight)
				flags |= TextFormatFlags.Right;
			TextRenderer.DrawText(g, e.FormattedValue as string ?? column.HeaderText, style.Font ?? column.DataGridView.Font, text, style.ForeColor, flags);
			var sort = column.HeaderCell.SortGlyphDirection;
			if (sort == SortOrder.None)
				return;
			var width = Dip(g, GridSortTriangle.Width);
			var height = Dip(g, GridSortTriangle.Height);
			var x = bounds.Right - Dip(g, GridSortTriangleRight) - width;
			// On a whole pixel, so the triangle's flat side stays sharp.
			var y = (float)Math.Floor(bounds.Top + (bounds.Height - height) / 2);
			var points = sort == SortOrder.Ascending
				? new[] { new PointF(x, y), new PointF(x + width, y), new PointF(x + width / 2, y + height) }
				: new[] { new PointF(x, y + height), new PointF(x + width, y + height), new PointF(x + width / 2, y) };
			var saved = Smooth(g);
			using (var brush = new SolidBrush(style.ForeColor))
				g.FillPolygon(brush, points);
			g.Restore(saved);
		}

		/// <summary>
		/// Carries the header's colours and the lines between rows on past the last column, as Default.xaml's header row and
		/// rows run the width of the grid.
		/// </summary>
		static void WpfGrid_Paint(object sender, PaintEventArgs e)
		{
			var grid = (DataGridView)sender;
			var right = 0;
			for (var i = 0; i < grid.Columns.Count; i++)
				if (grid.Columns[i].Displayed)
					right = Math.Max(right, grid.GetColumnDisplayRectangle(i, false).Right);
			var width = grid.DisplayRectangle.Right - right;
			if (width <= 0)
				return;
			if (grid.ColumnHeadersVisible && grid.ColumnHeadersHeight > 0)
				using (var brush = new LinearGradientBrush(new Rectangle(right, -1, width, grid.ColumnHeadersHeight + 2),
					Themed("BackgroundHeader", SystemColors.Window), Themed("BackgroundLight", SystemColors.Control), LinearGradientMode.Vertical))
					e.Graphics.FillRectangle(brush, right, 0, width, grid.ColumnHeadersHeight);
			using (var pen = new Pen(grid.GridColor))
				for (var i = grid.FirstDisplayedScrollingRowIndex; i >= 0 && i < grid.Rows.Count; i++)
				{
					var row = grid.GetRowDisplayRectangle(i, false);
					if (row.IsEmpty)
						break;
					e.Graphics.DrawLine(pen, right, row.Bottom - 1, right + width - 1, row.Bottom - 1);
				}
		}

		/// <summary>Repaints a check box cell as the mouse comes and goes, so its box shows the hover colour.</summary>
		static void GridCheckCell_Repaint(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
				grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
		}

		/// <summary>Whether a cell has the keyboard: the current cell of a grid that has the focus.</summary>
		static bool HasCellFocus(DataGridView grid, int column, int row)
		{
			var current = grid.CurrentCellAddress;
			return grid.ContainsFocus && current.X == column && current.Y == row;
		}

		#endregion

		#region Frames

		/// <summary>
		/// The height Default.xaml gives a control with a line of text: the text, with its edge's line and 4 device-independent
		/// pixels of room inside the line, above and below.
		/// </summary>
		static int ControlHeight(Control control)
		{
			return control.Font.Height + FrameSide() * 2;
		}

		/// <summary>
		/// The size of text as it is drawn on a window, each letter as wide as it is drawn: its advance width, as WPF lays it
		/// out. Measured without a window's device context, Windows Forms gives letters wider than they are drawn, by about a
		/// letter over a short line. The line has no width limit, so text that may wrap stays on its lines.
		/// </summary>
		static Size MeasureDrawnText(string text, Font font, TextFormatFlags flags)
		{
			using (var bitmap = new Bitmap(1, 1))
			using (var g = Graphics.FromImage(bitmap))
				return TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), flags);
		}

		/// <summary>
		/// The height of a line of text in a font as Windows draws it, which a text box needs whole: a pixel or so more than
		/// the font's own height, which would cut off the bottom of a g or a p.
		/// </summary>
		static int LineHeight(Font font)
		{
			return TextRenderer.MeasureText(" ", font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
		}

		/// <summary>
		/// Whether a control has the frame the WPF programs draw: a text box or number box with an edge, or a list with an
		/// edge whose items the program does not draw, while high contrast is off. The text box inside a framed number box has
		/// one without a line, which keeps its text in the middle.
		/// </summary>
		static bool HasFrame(Control control)
		{
			State state;
			if (SystemInformation.HighContrast || !States.TryGetValue(control, out state))
				return false;
			if (IsInnerBox(control))
				return HasFrame(control.Parent);
			if (state.BorderStyle == BorderStyle.None)
				return false;
			return control is TextBox || control is UpDownBase
				|| (control is ListBox && !(control is CheckedListBox) && state.DrawMode == DrawMode.Normal);
		}

		/// <summary>Whether a control is the text box of a number box.</summary>
		static bool IsInnerBox(Control control)
		{
			return control is TextBox && control.Parent is UpDownBase;
		}

		/// <summary>The room the frame takes at each side of a box's text, in pixels: its line and Default.xaml's room inside it.</summary>
		static int FrameSide()
		{
			return LineWidth() + Pixels(ComboBoxPadding);
		}

		/// <summary>The width of a line Default.xaml draws one device-independent pixel wide, in whole pixels of this program's windows.</summary>
		static int LineWidth()
		{
			return Math.Max(1, (int)Math.Floor(ControlsHelper.DpiScale));
		}

		/// <summary>
		/// Gives a box the program made a width for, in a table column sized to what it holds, the width that leaves its text
		/// the room Windows leaves it: the frame takes more at each side than Windows' own edge, and the column grows to make
		/// room. A box placed by its position, or one a layout stretches, keeps the width it has.
		/// </summary>
		static void FitBoxWidth(Control control, State state, bool framed)
		{
			var table = control.Parent as TableLayoutPanel;
			var stretched = (control.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right);
			if (table == null || control.Dock != DockStyle.None || stretched)
				return;
			// The cell the box was put in, known before the table is first laid out, or the one the table found it.
			var cell = table.GetCellPosition(control);
			if (cell.Column < 0)
				cell = table.GetPositionFromControl(control);
			var span = table.GetColumnSpan(control);
			var sized = false;
			for (var i = cell.Column; i >= 0 && i < cell.Column + span; i++)
				sized |= i >= table.ColumnStyles.Count || table.ColumnStyles[i].SizeType == SizeType.AutoSize;
			if (!sized)
				return;
			var edge = state.BorderStyle == BorderStyle.Fixed3D ? SystemInformation.Border3DSize.Width
				: state.BorderStyle == BorderStyle.FixedSingle ? SystemInformation.BorderSize.Width : 0;
			var width = state.Width + (framed ? 2 * Math.Max(0, FrameSide() - edge) : 0);
			if (control.Width != width)
				control.Width = width;
		}

		/// <summary>
		/// Gives a text box, number box or list Default.xaml's look and height, with the frame drawn in its window's edge, or
		/// its own back.
		/// </summary>
		static void ApplyFrame(Control control, State state)
		{
			var framed = HasFrame(control);
			var border = framed ? BorderStyle.None : state.BorderStyle;
			if (control is TextBox box && !IsInnerBox(box))
			{
				if (box.BorderStyle != border)
					box.BorderStyle = border;
				// A line of text stands as tall as Default.xaml makes it.
				var single = !box.Multiline;
				var autoSize = framed && single ? false : state.AutoSize;
				if (box.AutoSize != autoSize)
					box.AutoSize = autoSize;
				if (framed && single)
					box.Height = ControlHeight(box);
				else if (single && !state.AutoSize)
					box.Height = state.Height;
				if (single)
					FitBoxWidth(box, state, framed);
				box.ReadOnlyChanged -= RedrawFrame;
				if (framed)
					box.ReadOnlyChanged += RedrawFrame;
			}
			if (control is UpDownBase upDown)
			{
				if (upDown.BorderStyle != border)
					upDown.BorderStyle = border;
				var buttons = upDown.Controls.Cast<Control>().FirstOrDefault(x => x.GetType().Name == "UpDownButtons");
				if (buttons != null)
				{
					buttons.Paint -= UpDownButtons_Paint;
					if (framed)
					{
						buttons.Paint += UpDownButtons_Paint;
						// Windows Forms draws its own buttons under these, again each time the mouse moves over them: drawn off the
						// screen first, only the finished buttons are seen.
						SetStyleMethod.Invoke(buttons, new object[] { ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true });
					}
					buttons.Invalidate();
				}
				// The buttons as wide as Default.xaml's, in whole pixels.
				if (ButtonsWidthField != null)
				{
					ButtonsWidthField.SetValue(upDown, framed ? (int)(SpinButtonWidth * ControlsHelper.DpiScale) : state.ButtonsWidth);
					upDown.PerformLayout();
				}
				// Windows Forms makes a number box as high as its font with room for its edge, whatever it is asked for, so,
				// framed, the font is given Default.xaml's height less that room, and the box and the room a layout makes
				// for it take Default.xaml's height.
				FontHeightProperty.SetValue(upDown, -1, null);
				if (framed)
					FontHeightProperty.SetValue(upDown, ControlHeight(upDown) - (upDown.PreferredHeight - upDown.Font.Height), null);
				upDown.Height = framed ? ControlHeight(upDown) : state.Height;
				// A layout sizes a number box by the width it prefers, which counts its padding but not the room the frame takes:
				// the frame's line and the room before the text on one side, and the line and the room before the buttons on the
				// other. Windows Forms puts nothing else of a number box by its padding.
				var padding = framed ? new Padding(FrameSide(), 0, FrameSide(), 0) : state.Padding;
				if (upDown.Padding != padding)
					upDown.Padding = padding;
				if (!upDown.AutoSize)
					FitBoxWidth(upDown, state, framed);
			}
			var room = ItemRoom(control as ListBox);
			if (control is ListBox list)
			{
				if (list.BorderStyle != border)
					list.BorderStyle = border;
				// Items the theme draws stand as tall as their text with the room above and below it.
				var drawMode = room > 0 ? DrawMode.OwnerDrawFixed : state.DrawMode;
				if (list.DrawMode != drawMode)
					list.DrawMode = drawMode;
				list.ItemHeight = room > 0 ? list.Font.Height + Pixels(room * 2) : state.ItemHeight;
				// The list stands the height the program asked for, as WPF's does, not the height of whole items it was cut to.
				var integral = framed ? false : state.IntegralHeight;
				if (list.IntegralHeight != integral)
				{
					list.IntegralHeight = integral;
					var requested = RequestedHeightField?.GetValue(list) as int?;
					if (!integral && list.Dock == DockStyle.None && requested > 0)
						list.Height = requested.Value;
				}
				list.DrawItem -= List_DrawItem;
				if (room > 0)
					list.DrawItem += List_DrawItem;
			}
			// The frame shows the mouse, the focus and whether the control can be changed.
			control.MouseEnter -= RedrawFrame;
			control.MouseLeave -= RedrawFrame;
			control.Enter -= RedrawFrame;
			control.Leave -= RedrawFrame;
			control.EnabledChanged -= RedrawFrame;
			control.FontChanged -= Frame_FontChanged;
			if (framed)
			{
				control.MouseEnter += RedrawFrame;
				control.MouseLeave += RedrawFrame;
				control.Enter += RedrawFrame;
				control.Leave += RedrawFrame;
				control.EnabledChanged += RedrawFrame;
			}
			// The height of a framed box, and of the items the theme draws in a list, follow its font.
			if (framed || room > 0)
				control.FontChanged += Frame_FontChanged;
			SetFrameColor(control);
			if (control.IsHandleCreated)
			{
				AttachFrame(control, state);
				RedrawFrame(control, EventArgs.Empty);
			}
		}

		static void Frame_FontChanged(object sender, EventArgs e)
		{
			State state;
			if (States.TryGetValue((Control)sender, out state))
				ApplyFrame((Control)sender, state);
		}

		/// <summary>Draws the frame in a control's window edge while it has one, and leaves the edge to Windows again after.</summary>
		static void AttachFrame(Control control, State state)
		{
			if (HasFrame(control))
			{
				if (state.Frame == null)
					state.Frame = new Frame(control);
				if (state.Frame.Handle != control.Handle)
				{
					if (state.Frame.Handle != IntPtr.Zero)
						state.Frame.ReleaseHandle();
					state.Frame.AssignHandle(control.Handle);
				}
			}
			else if (state.Frame != null && state.Frame.Handle != IntPtr.Zero)
				state.Frame.ReleaseHandle();
			// Windows works the edge out again.
			NativeMethods.SetWindowPos(control.Handle, IntPtr.Zero, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE
				| NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
		}

		static void RedrawFrame(object sender, EventArgs e)
		{
			var control = (Control)sender;
			SetFrameColor(control);
			if (control.IsHandleCreated)
				NativeMethods.RedrawWindow(control.Handle, IntPtr.Zero, IntPtr.Zero,
					NativeMethods.RDW_FRAME | NativeMethods.RDW_INVALIDATE | NativeMethods.RDW_ERASE | NativeMethods.RDW_ALLCHILDREN);
		}

		/// <summary>Whether a text box or number box can be changed: one that cannot is filled in the control colour, without an edge.</summary>
		static bool IsEditable(Control control)
		{
			return control.Enabled && !(control is TextBoxBase box && box.ReadOnly) && !(control is UpDownBase upDown && upDown.ReadOnly);
		}

		/// <summary>The colour inside a frame: the field colour, or the control colour for a list and for a box that cannot be changed.</summary>
		/// <remarks>A box a program draws itself, such as a chart's, is filled with it too, so it looks as the boxes beside it do.</remarks>
		public static Color FrameFace(Control control)
		{
			return control is ListBox || !IsEditable(control)
				? Themed("BackgroundDark", SystemColors.Control)
				: Themed("BackgroundWhite", SystemColors.Window);
		}

		/// <summary>
		/// Gives a framed control the colour inside its frame, which Windows fills the control with, unless the program gave it
		/// a colour of its own.
		/// </summary>
		static void SetFrameColor(Control control)
		{
			State state;
			if (!States.TryGetValue(control, out state) || !HasFrame(control) || (state.BackColorSet && !state.BackColor.IsSystemColor))
				return;
			var face = FrameFace(control);
			if (control.BackColor != face)
				control.BackColor = face;
		}

		/// <summary>
		/// A window whose text the theme draws again while it is disabled, in the colour of every disabled text, where Windows
		/// draws a disabled edit window's text in its own grey: in one pass with Windows' own paint, so every paint shows it, and
		/// into every picture taken of the window.
		/// </summary>
		abstract class DisabledTextWindow : NativeWindow
		{
			/// <summary>Whether the theme draws the window's text: disabled, with text.</summary>
			protected abstract bool DrawsDisabledText();

			/// <summary>Draws the window's text again, into a device context whose origin is its client area: a paint's or a picture's.</summary>
			protected abstract void PaintDisabledText(IntPtr hdc);

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case NativeMethods.WM_PAINT:
						if (DrawsDisabledText())
						{
							PaintWithDisabledText(ref m);
							return;
						}
						break;
					case NativeMethods.WM_PRINTCLIENT:
						base.WndProc(ref m);
						if (m.WParam != IntPtr.Zero && DrawsDisabledText())
							PaintDisabledText(m.WParam);
						return;
				}
				base.WndProc(ref m);
			}

			/// <summary>
			/// Paints the window in one pass: Windows draws it into the device context of the paint, and its text is drawn again
			/// over it in the same device context.
			/// </summary>
			void PaintWithDisabledText(ref Message m)
			{
				var paint = new NativeMethods.PAINTSTRUCT();
				var hdc = NativeMethods.BeginPaint(Handle, ref paint);
				try
				{
					// An edit control draws into the device context a paint message brings.
					var own = Message.Create(m.HWnd, m.Msg, hdc, IntPtr.Zero);
					base.WndProc(ref own);
					if (hdc != IntPtr.Zero)
						PaintDisabledText(hdc);
					m.Result = own.Result;
				}
				finally
				{
					NativeMethods.EndPaint(Handle, ref paint);
				}
			}
		}

		/// <summary>
		/// Draws the text of a disabled edit window again in the colour of every disabled text, over its background, into the
		/// device context given. A disabled edit window cannot be scrolled or have text chosen, so a line of text stands where
		/// Windows starts it, and the lines of a box of many lines where Windows lays them out, from its first character.
		/// </summary>
		/// <param name="handle">The edit window.</param>
		/// <param name="hdc">A device context whose origin is the window's client area: a paint's or a picture's.</param>
		static void PaintDisabledEdit(IntPtr handle, IntPtr hdc, string shown, Font font, Color back, bool multiline, bool wordWrap, HorizontalAlignment align)
		{
			NativeMethods.RECT r;
			NativeMethods.GetClientRect(handle, out r);
			var client = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
			Rectangle text;
			Rectangle clip;
			var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl | TextFormatFlags.PreserveGraphicsClipping;
			if (multiline)
			{
				var lines = new NativeMethods.RECT();
				NativeMethods.SendMessage(handle, NativeMethods.EM_GETRECT, IntPtr.Zero, ref lines);
				clip = Rectangle.FromLTRB(lines.Left, lines.Top, lines.Right, lines.Bottom);
				// The first character stands above or left of the lines' place when the box was scrolled before it was disabled.
				var first = (int)NativeMethods.SendMessage(handle, NativeMethods.EM_POSFROMCHAR, IntPtr.Zero, IntPtr.Zero);
				var dy = (short)(first >> 16) - clip.Top;
				var dx = wordWrap ? 0 : (short)(first & 0xFFFF) - clip.Left;
				text = Rectangle.FromLTRB(clip.Left + Math.Min(0, dx), clip.Top + Math.Min(0, dy), clip.Right, clip.Bottom);
				if (wordWrap)
					flags |= TextFormatFlags.WordBreak;
			}
			else
			{
				var margins = (int)NativeMethods.SendMessage(handle, NativeMethods.EM_GETMARGINS, IntPtr.Zero, IntPtr.Zero);
				text = Rectangle.FromLTRB(client.Left + (margins & 0xFFFF), client.Top, client.Right - ((margins >> 16) & 0xFFFF), client.Bottom);
				clip = client;
				flags |= TextFormatFlags.SingleLine;
			}
			flags |= align == HorizontalAlignment.Center ? TextFormatFlags.HorizontalCenter : align == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left;
			using (var g = Graphics.FromHdc(hdc))
			{
				using (var brush = new SolidBrush(back))
					g.FillRectangle(brush, client);
				g.SetClip(clip);
				TextRenderer.DrawText(g, shown, font, text, DisabledText(), back, flags);
			}
		}

		/// <summary>
		/// The frame Default.xaml draws round a text box, a number box or a list, drawn in the window's own edge: rounded, with
		/// room inside, in the theme colours, its edge in the hover colour under the mouse and while it has the focus.
		/// </summary>
		/// <remarks>
		/// Windows draws a control's edge outside the part of the window a program paints, so the edge is made as wide as the
		/// frame and the room inside it, and drawn here. Windows fills the inside of a box that cannot be changed, and of a
		/// list, with the colour the frame gives it.
		/// </remarks>
		class Frame : DisabledTextWindow
		{
			readonly Control Control;

			public Frame(Control control)
			{
				Control = control;
			}

			/// <summary>The edge, in pixels, round a window of the size given: the frame's line and the room inside it.</summary>
			Padding Insets(Size window)
			{
				var line = LineWidth();
				var text = LineHeight(Control.Font);
				var top = Math.Max(0, (window.Height - text) / 2);
				var bottom = Math.Max(0, window.Height - text - top);
				// The text box of a number box: its line of text in the middle of the frame round it, and the room Default.xaml
				// leaves before the buttons.
				if (IsInnerBox(Control))
				{
					var room = Pixels(ComboBoxPadding);
					var left = ((UpDownBase)Control.Parent).UpDownAlign == LeftRightAlignment.Left;
					return new Padding(left ? room : 0, top, left ? 0 : room, bottom);
				}
				if (Control is ListBox)
					return new Padding(line);
				// The buttons of a number box fill the inside of its frame on their side, up to the line, without a gap.
				var side = FrameSide();
				if (Control is UpDownBase upDown)
					return upDown.UpDownAlign == LeftRightAlignment.Left
						? new Padding(line, line, side, line)
						: new Padding(side, line, line, line);
				// Default.xaml's edge and padding: the room inside the edge on every side of the text.
				if (Control is TextBox box && box.Multiline)
					return new Padding(side);
				// A single line of text stands in the middle.
				return new Padding(side, Math.Max(line, top), side, Math.Max(line, bottom));
			}

			static NativeMethods.RECT Deflate(NativeMethods.RECT r, Padding insets)
			{
				r.Left += insets.Left;
				r.Top += insets.Top;
				r.Right = Math.Max(r.Left, r.Right - insets.Right);
				r.Bottom = Math.Max(r.Top, r.Bottom - insets.Bottom);
				return r;
			}

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case NativeMethods.WM_NCCALCSIZE:
						CalcSize(ref m);
						return;
					case NativeMethods.WM_NCPAINT:
						base.WndProc(ref m);
						PaintFrame(IntPtr.Zero);
						return;
					case NativeMethods.WM_PRINT:
						// Into the picture taken of the window, where it asks for its edge.
						base.WndProc(ref m);
						if (((int)(long)m.LParam & NativeMethods.PRF_NONCLIENT) != 0)
							PaintFrame(m.WParam);
						return;
				}
				base.WndProc(ref m);
			}

			/// <summary>Whether the theme draws the box's text: a text box that is disabled and has text.</summary>
			protected override bool DrawsDisabledText()
			{
				return Control is TextBox box && !box.Enabled && box.Text.Length > 0;
			}

			/// <summary>Windows works out the edge it leaves, scroll bars included, and the frame takes its own from the rest.</summary>
			void CalcSize(ref Message m)
			{
				if (m.WParam != IntPtr.Zero)
				{
					var p = (NativeMethods.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.NCCALCSIZE_PARAMS));
					var window = new Size(p.rgrc0.Right - p.rgrc0.Left, p.rgrc0.Bottom - p.rgrc0.Top);
					base.WndProc(ref m);
					p = (NativeMethods.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.NCCALCSIZE_PARAMS));
					p.rgrc0 = Deflate(p.rgrc0, Insets(window));
					Marshal.StructureToPtr(p, m.LParam, false);
				}
				else
				{
					var r = (NativeMethods.RECT)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.RECT));
					var window = new Size(r.Right - r.Left, r.Bottom - r.Top);
					base.WndProc(ref m);
					r = (NativeMethods.RECT)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.RECT));
					Marshal.StructureToPtr(Deflate(r, Insets(window)), m.LParam, false);
				}
			}

			/// <summary>Draws the text of a disabled box again, as it shows it: a password box its password characters.</summary>
			protected override void PaintDisabledText(IntPtr hdc)
			{
				var box = (TextBox)Control;
				var align = box.RightToLeft == RightToLeft.Yes
					? (box.TextAlign == HorizontalAlignment.Left ? HorizontalAlignment.Right : box.TextAlign == HorizontalAlignment.Right ? HorizontalAlignment.Left : box.TextAlign)
					: box.TextAlign;
				var shown = box.PasswordChar != '\0' || box.UseSystemPasswordChar
					? new string(box.UseSystemPasswordChar ? '●' : box.PasswordChar, box.Text.Length)
					: box.Text;
				PaintDisabledEdit(Handle, hdc, shown, box.Font, box.BackColor, box.Multiline, box.WordWrap, align);
			}

			/// <summary>Draws the frame in the window's edge, on the window or into the device context given, whose origin is the window's.</summary>
			void PaintFrame(IntPtr target)
			{
				var hdc = target != IntPtr.Zero ? target : NativeMethods.GetWindowDC(Handle);
				if (hdc == IntPtr.Zero)
					return;
				try
				{
					NativeMethods.RECT r;
					NativeMethods.GetWindowRect(Handle, out r);
					var bounds = new Rectangle(0, 0, r.Right - r.Left, r.Bottom - r.Top);
					var insets = Insets(bounds.Size);
					var inner = new Rectangle(insets.Left, insets.Top, bounds.Width - insets.Horizontal, bounds.Height - insets.Vertical);
					using (var g = Graphics.FromHdc(hdc))
					using (var edge = new Region(bounds))
					{
						edge.Exclude(inner);
						g.Clip = edge;
						// The room round the text of a number box, in the box's own colour.
						if (IsInnerBox(Control))
						{
							using (var brush = new SolidBrush(Control.BackColor))
								g.FillRectangle(brush, bounds);
							return;
						}
						using (var brush = new SolidBrush(Control.Parent?.BackColor ?? SystemColors.Control))
							g.FillRectangle(brush, bounds);
						DrawFrame(g, bounds, Control);
					}
				}
				finally
				{
					if (target == IntPtr.Zero)
						NativeMethods.ReleaseDC(Handle, hdc);
				}
			}
		}

		/// <summary>
		/// The frame's line: Default.xaml's, the field edge colour, or the hover colour under the mouse or with the focus; none
		/// round a box that cannot be changed.
		/// </summary>
		static Color FrameEdge(Control control)
		{
			if (control is ListBox)
				return Themed("BorderDark", SystemColors.ControlDark);
			if (!IsEditable(control))
				return Color.Empty;
			NativeMethods.RECT r;
			NativeMethods.GetWindowRect(control.Handle, out r);
			var hot = control.ContainsFocus || Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom).Contains(Control.MousePosition);
			return Themed(hot ? "BackgroundDarkSelected" : "BorderField", SystemColors.ControlDark);
		}

		/// <summary>A control's frame, round its window's bounds given: its face filled and rounded, and its line.</summary>
		static void DrawFrame(Graphics g, Rectangle bounds, Control control)
		{
			var saved = Smooth(g);
			var line = Line(g);
			var radius = Dip(g, CornerRadius);
			using (var face = RoundedRectangle(bounds, radius, radius, radius, radius))
			using (var brush = new SolidBrush(FrameFace(control)))
				g.FillPath(brush, face);
			var color = FrameEdge(control);
			if (!color.IsEmpty)
				using (var pen = new Pen(color, line))
				using (var path = RoundedRectangle(Inset(bounds, line / 2), radius, radius, radius, radius))
					g.DrawPath(pen, path);
			g.Restore(saved);
		}

		/// <summary>The room round each item's text a program gave a list without an edge, in device-independent pixels.</summary>
		static readonly ConditionalWeakTable<ListBox, StrongBox<int>> ItemRooms = new ConditionalWeakTable<ListBox, StrongBox<int>>();

		/// <summary>
		/// Has the theme draw the items of a list without an edge as it draws a framed list's, in both themes: on the list's own
		/// colour, or the selection colour while chosen, with the room given round each item's text, in device-independent
		/// pixels, scaled with the screen. 0 leaves the items to Windows again. A framed list keeps Default.xaml's room, 4.
		/// </summary>
		public static void SetItemRoom(ListBox list, int room)
		{
			ItemRooms.GetOrCreateValue(list).Value = room;
			State state;
			if (States.TryGetValue(list, out state))
				ApplyFrame(list, state);
		}

		/// <summary>
		/// The room round the text of the items the theme draws in a list, in device-independent pixels: Default.xaml's, 4, in a
		/// framed list, and the room the program gave a list without an edge (<see cref="SetItemRoom"/>); 0, which leaves the
		/// items to Windows, in any other list, in a list the program draws itself and while high contrast is on.
		/// </summary>
		static int ItemRoom(ListBox list)
		{
			State state;
			if (list == null || SystemInformation.HighContrast || !States.TryGetValue(list, out state))
				return 0;
			if (HasFrame(list))
				return ComboBoxPadding;
			StrongBox<int> room;
			return state.DrawMode == DrawMode.Normal && !(list is CheckedListBox) && ItemRooms.TryGetValue(list, out room) ? room.Value : 0;
		}

		/// <summary>
		/// A list's item as Default.xaml draws one: on the control colour in a framed list, or the list's own colour in one
		/// without an edge, or the selection colour while chosen, its text in from the side by the list's room; in the colour of
		/// every disabled text while the list is disabled.
		/// </summary>
		static void List_DrawItem(object sender, DrawItemEventArgs e)
		{
			var list = (ListBox)sender;
			if (e.Index < 0 || e.Index >= list.Items.Count)
				return;
			var selected = (e.State & DrawItemState.Selected) != 0;
			var back = selected ? Themed("SelectedBackground", SystemColors.Highlight)
				: HasFrame(list) ? Themed("BackgroundDark", SystemColors.Control) : list.BackColor;
			var fore = selected ? Themed("SelectedForeground", SystemColors.HighlightText) : list.ForeColor;
			if (!list.Enabled)
				fore = DisabledText();
			using (var brush = new SolidBrush(back))
				e.Graphics.FillRectangle(brush, e.Bounds);
			var padding = Pixels(ItemRoom(list));
			var text = Rectangle.FromLTRB(e.Bounds.Left + padding, e.Bounds.Top, e.Bounds.Right - padding, e.Bounds.Bottom);
			TextRenderer.DrawText(e.Graphics, list.GetItemText(list.Items[e.Index]), list.Font, text, fore,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
			if ((e.State & DrawItemState.Focus) != 0 && (e.State & DrawItemState.NoFocusRect) == 0)
				ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, fore, back);
		}

		/// <summary>The width of a number box's buttons in its Default.xaml template, in device-independent pixels.</summary>
		const int SpinButtonWidth = 24;

		/// <summary>The size of the triangle on each of a number box's buttons, in device-independent pixels.</summary>
		static readonly SizeF SpinButtonTriangle = new SizeF(8, 4);

		/// <summary>The height the program asked a list for, before Windows Forms cut it to whole items, a field of its own.</summary>
		static readonly FieldInfo RequestedHeightField = typeof(ListBox).GetField("requestedHeight", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(ListBox).GetField("_requestedHeight", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>The height of a control's font as Windows Forms works out its sizes, a protected property of its own.</summary>
		static readonly PropertyInfo FontHeightProperty = typeof(Control).GetProperty("FontHeight", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>The width Windows Forms lays a number box's buttons out with, a field of its own.</summary>
		static readonly FieldInfo ButtonsWidthField = typeof(UpDownBase).GetField("defaultButtonsWidth", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(UpDownBase).GetField("_defaultButtonsWidth", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// The buttons of a number box as the WPF programs draw them: two filled halves inside the frame's line, rounded on the
		/// outside as the line is, lighter under the mouse and lighter again pressed, each with a triangle.
		/// </summary>
		/// <remarks>The frame's rounded corners reach behind the buttons, so the frame is drawn here too, as it stands round the box.</remarks>
		static void UpDownButtons_Paint(object sender, PaintEventArgs e)
		{
			var buttons = (Control)sender;
			var g = e.Graphics;
			var client = buttons.ClientRectangle;
			var upDown = (UpDownBase)buttons.Parent;
			using (var brush = new SolidBrush(upDown.Parent?.BackColor ?? SystemColors.Control))
				g.FillRectangle(brush, client);
			NativeMethods.RECT box, own;
			NativeMethods.GetWindowRect(upDown.Handle, out box);
			NativeMethods.GetWindowRect(buttons.Handle, out own);
			var origin = g.Save();
			g.TranslateTransform(box.Left - own.Left, box.Top - own.Top);
			DrawFrame(g, new Rectangle(0, 0, box.Right - box.Left, box.Bottom - box.Top), upDown);
			g.Restore(origin);
			var half = client.Height / 2;
			var mouse = buttons.PointToClient(Control.MousePosition);
			var pressed = buttons.Capture && (Control.MouseButtons & MouseButtons.Left) != 0;
			var saved = Smooth(g);
			// The inside of the frame's line is rounded this much.
			var radius = Dip(g, CornerRadius) - Line(g) / 2;
			var left = upDown.UpDownAlign == LeftRightAlignment.Left;
			var up = new Rectangle(0, 0, client.Width, half);
			var down = new Rectangle(0, half, client.Width, client.Height - half);
			using (var face = RoundedRectangle(up, left ? radius : 0, left ? 0 : radius, 0, 0))
				DrawSpinButton(g, buttons, face, up.Contains(mouse), pressed);
			using (var face = RoundedRectangle(down, 0, 0, left ? 0 : radius, left ? radius : 0))
				DrawSpinButton(g, buttons, face, down.Contains(mouse), pressed);
			g.Restore(saved);
			// Default.xaml's triangles. The up one in the middle of its button, on a whole pixel so its flat side stays sharp;
			// the down one the same pixels upside down, across the middle of the buttons, though the buttons may be a pixel
			// apart in height. Drawn once and turned over, as smoothing does not draw a shape and its mirror image alike.
			var width = Dip(g, SpinButtonTriangle.Width);
			var height = Dip(g, SpinButtonTriangle.Height);
			var x = (int)Math.Round(up.X + (up.Width - width) / 2, MidpointRounding.AwayFromZero);
			var upTop = (int)Math.Floor((up.Height - height) / 2);
			var downTop = client.Height - upTop - (int)Math.Ceiling(height);
			using (var triangle = new Bitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), PixelFormat.Format32bppArgb))
			{
				using (var tg = Graphics.FromImage(triangle))
				{
					tg.SmoothingMode = SmoothingMode.AntiAlias;
					tg.PixelOffsetMode = PixelOffsetMode.Half;
					using (var brush = new SolidBrush(Color.FromArgb(buttons.Enabled ? 255 : 128, Themed("ForegroundBlack", SystemColors.ControlText))))
						tg.FillPolygon(brush, new[] { new PointF(0, height), new PointF(width, height), new PointF(width / 2, 0) });
				}
				g.DrawImageUnscaled(triangle, x, upTop);
				triangle.RotateFlip(RotateFlipType.RotateNoneFlipY);
				g.DrawImageUnscaled(triangle, x, downTop);
			}
		}

		/// <summary>One of a number box's buttons: its half of the face.</summary>
		static void DrawSpinButton(Graphics g, Control buttons, GraphicsPath face, bool hot, bool pressed)
		{
			var key = !buttons.Enabled ? "BackgroundDark" : hot && pressed ? "BackgroundDarkPressed" : hot ? "BackgroundDarkSelected" : "BackgroundDark";
			using (var brush = new SolidBrush(Color.FromArgb(buttons.Enabled ? 255 : 128, Themed(key, SystemColors.ControlLight))))
				g.FillPath(brush, face);
		}

		#endregion

		#region Scroll bars

		// The scroll bar parts and states of the Windows visual style.
		const int SBP_ARROWBTN = 1;
		const int SBP_THUMBBTNHORZ = 2;
		const int SBP_THUMBBTNVERT = 3;
		const int SBP_LOWERTRACKHORZ = 4;
		const int SBP_UPPERTRACKHORZ = 5;
		const int SBP_LOWERTRACKVERT = 6;
		const int SBP_UPPERTRACKVERT = 7;
		const int ABS_UPNORMAL = 1;
		const int ABS_UPHOVER = 17;
		const int SCRBS_HOT = 2;
		const int SCRBS_PRESSED = 3;
		const int SCRBS_DISABLED = 4;
		const int SCRBS_HOVER = 5;

		/// <summary>
		/// True when the Windows visual style draws a scroll bar's arrow at rest without its triangle, as Windows 11 does: it
		/// draws a bar the mouse is not over as a thin line, without arrows, and shows the arrows and the whole thumb only under
		/// the mouse. Such bars are drawn whole here.
		/// </summary>
		static bool HidesScrollBarArrows()
		{
			if (_HidesScrollBarArrows == null)
			{
				var hides = false;
				var element = VisualStyleElement.CreateElement("ScrollBar", SBP_ARROWBTN, ABS_UPNORMAL);
				if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element))
				{
					var size = SystemInformation.VerticalScrollBarWidth;
					using (var bitmap = new Bitmap(size, size))
					{
						using (var g = Graphics.FromImage(bitmap))
							new VisualStyleRenderer(element).DrawBackground(g, new Rectangle(0, 0, size, size));
						// Without its triangle the middle of the arrow, where the triangle stands, is one colour.
						var middle = bitmap.GetPixel(size / 2, size / 2);
						hides = true;
						for (var y = size / 4; y < size - size / 4 && hides; y++)
							for (var x = size / 4; x < size - size / 4 && hides; x++)
								hides = bitmap.GetPixel(x, y) == middle;
					}
				}
				_HidesScrollBarArrows = hides;
			}
			return _HidesScrollBarArrows.Value;
		}
		static bool? _HidesScrollBarArrows;

		/// <summary>Whether a control shows scroll bars of its own: a text box of more than one line, a list, a tree, a scroll bar or a container that scrolls.</summary>
		static bool Scrolls(Control control)
		{
			return control is TextBoxBase box ? box.Multiline
				: control is ListBox || control is ListView || control is TreeView || control is ScrollBar
				|| (control is ScrollableControl scrollable && scrollable.AutoScroll);
		}

		/// <summary>
		/// Draws the scroll bars of a control that can scroll, and of the list a drop-down list opens, whole, where the Windows
		/// visual style draws them thin while the mouse is not over them.
		/// </summary>
		static void AttachScrollBars(Control control, State state)
		{
			if (!HidesScrollBarArrows())
				return;
			if (Scrolls(control))
				AttachScrollBars(ref state.ScrollBars, control.Handle, control is ScrollBar);
			if (control is ComboBox)
			{
				var info = new NativeMethods.COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.COMBOBOXINFO)) };
				if (NativeMethods.GetComboBoxInfo(control.Handle, ref info))
					AttachScrollBars(ref state.ListScrollBars, info.hwndList, false);
			}
		}

		/// <summary>Draws the bars of the window given whole, from now on and at once.</summary>
		static void AttachScrollBars(ref WholeScrollBars bars, IntPtr handle, bool isScrollBar)
		{
			if (handle == IntPtr.Zero)
				return;
			if (bars == null)
				bars = new WholeScrollBars(isScrollBar);
			if (bars.Handle == handle)
				return;
			if (bars.Handle != IntPtr.Zero)
				bars.ReleaseHandle();
			bars.AssignHandle(handle);
			bars.Paint(IntPtr.Zero, true);
		}

		/// <summary>
		/// The window of a control that can scroll, or of a scroll bar, whose scroll bars are drawn here whole whenever they are
		/// shown: both arrows and a thumb as wide as the bar, in the window's own visual style, light or dark, as Windows draws
		/// them while the mouse is over them; the part under the mouse lit, and the part held down pressed.
		/// </summary>
		/// <remarks>
		/// Windows draws a bar again as it scrolls or changes, when its window is painted, and as the mouse comes and goes, so
		/// after each message to the window the bar is drawn again over Windows' drawing if what it shows may have changed.
		/// Windows also fades a bar from one look to the next on a timer of the common controls that calls no window;
		/// <see cref="ScrollBarFades"/> ends each such fade before it draws. A mirrored window keeps the bars Windows draws.
		/// </remarks>
		class WholeScrollBars : NativeWindow
		{
			/// <summary>Whether the window is a scroll bar itself, rather than a window with scroll bars in its edge.</summary>
			readonly bool IsScrollBar;
			/// <summary>The bars last drawn, and their parts under the mouse: a window's vertical and horizontal bar, or a scroll bar.</summary>
			NativeMethods.SCROLLBARINFO _first, _second;
			int _firstHot = -1, _secondHot = -1;
			/// <summary>True while the bars are read and drawn: a scroll bar is read with a message to itself.</summary>
			bool _busy;
			/// <summary>The filter that ends the fades of this window's bars, on the window's thread, and the window it knows by.</summary>
			ScrollBarFades _fades;
			IntPtr _registered;

			public WholeScrollBars(bool isScrollBar)
			{
				IsScrollBar = isScrollBar;
			}

			protected override void OnHandleChange()
			{
				_fades?.Painted.Remove(_registered);
				_registered = Handle;
				if (Handle != IntPtr.Zero)
				{
					_fades = ScrollBarFades.Current;
					_fades.Painted[Handle] = this;
				}
				base.OnHandleChange();
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);
				if (_busy || Handle == IntPtr.Zero)
					return;
				if (m.Msg == NativeMethods.WM_PRINT)
				{
					// Into the picture taken of the window, where it asks for the part the bars are in.
					if (((int)(long)m.LParam & (IsScrollBar ? NativeMethods.PRF_CLIENT : NativeMethods.PRF_NONCLIENT)) != 0)
						Paint(m.WParam, true);
					return;
				}
				Paint(IntPtr.Zero, Redraws(m.Msg));
			}

			/// <summary>Whether Windows may have drawn the bars again on a message, even when what they show stayed the same.</summary>
			bool Redraws(int message)
			{
				switch (message)
				{
					case NativeMethods.WM_NCPAINT:
					case NativeMethods.WM_ENABLE:
					case NativeMethods.WM_THEMECHANGED:
					case NativeMethods.WM_SYSCOLORCHANGE:
					case NativeMethods.WM_SETTINGCHANGE:
					case NativeMethods.WM_CAPTURECHANGED:
					case NativeMethods.WM_NCMOUSELEAVE:
					case NativeMethods.WM_MOUSELEAVE:
						return true;
					case NativeMethods.WM_PAINT:
						return IsScrollBar;
				}
				// The mouse over the window's edge, where its bars are, or over a scroll bar.
				return (message >= NativeMethods.WM_NCMOUSEMOVE && message <= NativeMethods.WM_NCMBUTTONDBLCLK)
					|| (IsScrollBar && message >= NativeMethods.WM_MOUSEMOVE && message <= NativeMethods.WM_MBUTTONDBLCLK);
			}

			/// <summary>
			/// Draws the bars shown, on the window or into the device context given, when asked to or when what they show
			/// changed since they were last drawn.
			/// </summary>
			public void Paint(IntPtr target, bool force)
			{
				_busy = true;
				try
				{
					var first = Read(IsScrollBar ? NativeMethods.OBJID_CLIENT : NativeMethods.OBJID_VSCROLL);
					var second = IsScrollBar ? default(NativeMethods.SCROLLBARINFO) : Read(NativeMethods.OBJID_HSCROLL);
					var firstVertical = !IsScrollBar || (NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_STYLE) & NativeMethods.SBS_VERT) != 0;
					var cursor = Control.MousePosition;
					var firstHot = HotPart(first, firstVertical, cursor);
					var secondHot = HotPart(second, false, cursor);
					// Lit only while the mouse is over this window, not over another in front of it.
					if ((firstHot >= 0 || secondHot >= 0) && NativeMethods.WindowFromPoint(cursor) != Handle)
						firstHot = secondHot = -1;
					if (!force && first.Equals(_first) && second.Equals(_second) && firstHot == _firstHot && secondHot == _secondHot)
						return;
					_first = first;
					_second = second;
					_firstHot = firstHot;
					_secondHot = secondHot;
					if ((first.cbSize == 0 && second.cbSize == 0)
						|| (NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_LAYOUTRTL) != 0)
						return;
					// The style the window draws its bars in, which the dark theme sets.
					var theme = NativeMethods.OpenThemeData(Handle, "ScrollBar");
					if (theme == IntPtr.Zero)
						return;
					var hdc = target != IntPtr.Zero ? target : NativeMethods.GetWindowDC(Handle);
					try
					{
						NativeMethods.RECT window;
						if (hdc == IntPtr.Zero || !NativeMethods.GetWindowRect(Handle, out window))
							return;
						if (first.cbSize != 0)
							DrawScrollBar(theme, hdc, window, first, firstVertical, firstHot);
						if (second.cbSize != 0)
							DrawScrollBar(theme, hdc, window, second, false, secondHot);
					}
					finally
					{
						if (target == IntPtr.Zero && hdc != IntPtr.Zero)
							NativeMethods.ReleaseDC(Handle, hdc);
						NativeMethods.CloseThemeData(theme);
					}
				}
				finally
				{
					_busy = false;
				}
			}

			/// <summary>A bar of the window while it is shown; all zero while it is not.</summary>
			NativeMethods.SCROLLBARINFO Read(int bar)
			{
				var info = new NativeMethods.SCROLLBARINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.SCROLLBARINFO)) };
				if (!NativeMethods.GetScrollBarInfo(Handle, bar, ref info)
					|| (info.Bar & (NativeMethods.STATE_SYSTEM_INVISIBLE | NativeMethods.STATE_SYSTEM_OFFSCREEN)) != 0
					|| info.rcScrollBar.Right <= info.rcScrollBar.Left || info.rcScrollBar.Bottom <= info.rcScrollBar.Top)
					return default(NativeMethods.SCROLLBARINFO);
				return info;
			}
		}

		/// <summary>
		/// A bar's parts, from its top-left corner: the first arrow, the track before the thumb, the thumb, the track after it
		/// and the last arrow, in the order Windows gives their states; the thumb empty while it is not shown.
		/// </summary>
		static Rectangle[] ScrollBarParts(NativeMethods.SCROLLBARINFO info, bool vertical)
		{
			var width = info.rcScrollBar.Right - info.rcScrollBar.Left;
			var height = info.rcScrollBar.Bottom - info.rcScrollBar.Top;
			var length = vertical ? height : width;
			var arrow = Math.Min(info.dxyLineButton, length / 2);
			var thumb = (info.Thumb & (NativeMethods.STATE_SYSTEM_INVISIBLE | NativeMethods.STATE_SYSTEM_OFFSCREEN)) == 0
				&& info.xyThumbBottom > info.xyThumbTop;
			var thumbStart = thumb ? info.xyThumbTop : length - arrow;
			var thumbEnd = thumb ? info.xyThumbBottom : length - arrow;
			Rectangle Span(int from, int to) => vertical ? Rectangle.FromLTRB(0, from, width, to) : Rectangle.FromLTRB(from, 0, to, height);
			return new[] { Span(0, arrow), Span(arrow, thumbStart), thumb ? Span(thumbStart, thumbEnd) : Rectangle.Empty, Span(thumbEnd, length - arrow), Span(length - arrow, length) };
		}

		/// <summary>The part of a bar under a point on the screen, numbered as <see cref="ScrollBarParts"/> gives them; -1 for none.</summary>
		static int HotPart(NativeMethods.SCROLLBARINFO info, bool vertical, Point point)
		{
			if (info.cbSize == 0)
				return -1;
			var parts = ScrollBarParts(info, vertical);
			for (var i = 0; i < parts.Length; i++)
				if (parts[i].Contains(point.X - info.rcScrollBar.Left, point.Y - info.rcScrollBar.Top))
					return i;
			return -1;
		}

		/// <summary>
		/// Draws a bar whole in a visual style, as Windows draws it under the mouse: its arrows and thumb in their hover state,
		/// the part under the mouse lit, a part held down pressed and a disabled one disabled.
		/// </summary>
		static void DrawScrollBar(IntPtr theme, IntPtr hdc, NativeMethods.RECT window, NativeMethods.SCROLLBARINFO info, bool vertical, int hot)
		{
			var r = info.rcScrollBar;
			var bar = Rectangle.FromLTRB(r.Left - window.Left, r.Top - window.Top, r.Right - window.Left, r.Bottom - window.Top);
			var parts = ScrollBarParts(info, vertical);
			var states = new[] { info.FirstArrow, info.PageBefore, info.Thumb, info.PageAfter, info.LastArrow };
			var disabled = (info.Bar & NativeMethods.STATE_SYSTEM_UNAVAILABLE) != 0;
			// Drawn off the screen first and put on the window at once, so the bar never shows half drawn.
			using (var bitmap = new Bitmap(bar.Width, bar.Height, PixelFormat.Format24bppRgb))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					var dc = g.GetHdc();
					try
					{
						// The track under every part, as some are drawn partly see-through.
						DrawThemePart(theme, dc, vertical ? SBP_UPPERTRACKVERT : SBP_UPPERTRACKHORZ, disabled ? SCRBS_DISABLED : SCRBS_HOVER,
							new Rectangle(Point.Empty, bar.Size));
						for (var i = 0; i < parts.Length; i++)
						{
							if (parts[i].Width <= 0 || parts[i].Height <= 0)
								continue;
							var state = disabled || (states[i] & NativeMethods.STATE_SYSTEM_UNAVAILABLE) != 0 ? SCRBS_DISABLED
								: (states[i] & NativeMethods.STATE_SYSTEM_PRESSED) != 0 ? SCRBS_PRESSED
								: hot == i ? SCRBS_HOT : SCRBS_HOVER;
							int part;
							if (i == 0 || i == parts.Length - 1)
							{
								// An arrow's states come in fours, up, down, left and right, and its hover states after them.
								var direction = (vertical ? 0 : 2) + (i == 0 ? 0 : 1);
								part = SBP_ARROWBTN;
								state = state == SCRBS_HOVER ? ABS_UPHOVER + direction : state + direction * 4;
							}
							else if (i == 2)
								part = vertical ? SBP_THUMBBTNVERT : SBP_THUMBBTNHORZ;
							else
								part = vertical ? (i == 1 ? SBP_UPPERTRACKVERT : SBP_LOWERTRACKVERT) : (i == 1 ? SBP_UPPERTRACKHORZ : SBP_LOWERTRACKHORZ);
							DrawThemePart(theme, dc, part, state, parts[i]);
						}
					}
					finally
					{
						g.ReleaseHdc(dc);
					}
				}
				using (var g = Graphics.FromHdc(hdc))
					g.DrawImageUnscaled(bitmap, bar.X, bar.Y);
			}
		}

		/// <summary>Draws a part of a visual style, in a state, filling the bounds given.</summary>
		static void DrawThemePart(IntPtr theme, IntPtr hdc, int part, int state, Rectangle bounds)
		{
			var rect = new NativeMethods.RECT { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom };
			NativeMethods.DrawThemeBackground(theme, hdc, part, state, ref rect, IntPtr.Zero);
		}

		/// <summary>
		/// Ends the fades of the scroll bars drawn whole here before they draw: the common controls fade a bar from one look to
		/// the next, and to the thin one as the mouse leaves it, on a timer that calls the common controls without a message to
		/// the window. Each tick of such a timer, on a window whose bars are drawn here, stops it, and the bars are drawn again.
		/// </summary>
		/// <remarks>One listens on each interface thread with such a window, in its Windows Forms message loop.</remarks>
		class ScrollBarFades : IMessageFilter
		{
			[ThreadStatic]
			static ScrollBarFades _current;

			/// <summary>The filter of the calling thread, listening from its first use.</summary>
			public static ScrollBarFades Current
			{
				get
				{
					if (_current == null)
					{
						_current = new ScrollBarFades();
						Application.AddMessageFilter(_current);
					}
					return _current;
				}
			}

			/// <summary>The windows of this thread whose bars are drawn here.</summary>
			public readonly Dictionary<IntPtr, WholeScrollBars> Painted = new Dictionary<IntPtr, WholeScrollBars>();

			/// <summary>Whether the code a timer calls is in the common controls, by its address.</summary>
			readonly Dictionary<IntPtr, bool> InCommonControls = new Dictionary<IntPtr, bool>();

			public bool PreFilterMessage(ref Message m)
			{
				WholeScrollBars bars;
				// A timer that calls code rather than its window.
				if (m.Msg != NativeMethods.WM_TIMER || m.LParam == IntPtr.Zero || !Painted.TryGetValue(m.HWnd, out bars)
					|| bars.Handle != m.HWnd || !IsCommonControls(m.LParam))
					return false;
				NativeMethods.KillTimer(m.HWnd, m.WParam);
				bars.Paint(IntPtr.Zero, true);
				return true;
			}

			/// <summary>Whether code at an address is in the common controls library, of whichever version.</summary>
			bool IsCommonControls(IntPtr address)
			{
				bool inside;
				if (!InCommonControls.TryGetValue(address, out inside))
				{
					IntPtr module;
					var name = new StringBuilder(260);
					inside = NativeMethods.GetModuleHandleEx(NativeMethods.GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS
						| NativeMethods.GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, address, out module)
						&& NativeMethods.GetModuleFileName(module, name, name.Capacity) > 0
						&& string.Equals(System.IO.Path.GetFileName(name.ToString()), "comctl32.dll", StringComparison.OrdinalIgnoreCase);
					InCommonControls.Add(address, inside);
				}
				return inside;
			}
		}

		#endregion

		#region Windows

		static NativeMethods.HookProc _hookProc;
		static IntPtr _hook;

		/// <summary>Themes each window of the interface thread as it first becomes active, dialogs included.</summary>
		static IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
		{
			if (code == NativeMethods.HCBT_ACTIVATE)
			{
				var form = Control.FromHandle(wParam) as Form;
				State state;
				if (form == null)
					// Windows' own dialogs, message boxes among them, get the title bar at least.
					SetTitleBar(wParam, IsDark);
				else if (!States.TryGetValue(form, out state) || state.Dark != IsDark)
					Apply(form);
			}
			return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
		}

		/// <summary>
		/// Takes the hook away as the process ends, while .NET still runs. Windows calls the hook for every window
		/// the interface thread destroys, and some are destroyed later in the exit, by the text input service or a
		/// game input library: a call into .NET after it stopped ends the process with an error (0xC0020001).
		/// </summary>
		static void RemoveHook()
		{
			if (_hook != IntPtr.Zero)
				NativeMethods.UnhookWindowsHookEx(_hook);
			_hook = IntPtr.Zero;
		}

		/// <summary>The Windows build number, or 0 when it cannot be read.</summary>
		static int WindowsBuild
		{
			get
			{
				if (_WindowsBuild < 0)
				{
					using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
					{
						int build;
						_WindowsBuild = int.TryParse(key?.GetValue("CurrentBuildNumber") as string, out build) ? build : 0;
					}
				}
				return _WindowsBuild;
			}
		}
		static int _WindowsBuild = -1;

		/// <summary>Dark or light title bar, on Windows 10 1809 and later.</summary>
		static void SetTitleBar(IntPtr handle, bool dark)
		{
			if (handle == IntPtr.Zero || WindowsBuild < 17763)
				return;
			// The attribute was 19 before Windows 10 2004.
			var attribute = WindowsBuild >= 19041 ? NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE : NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
			var value = dark ? 1 : 0;
			NativeMethods.DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
		}

		static class NativeMethods
		{
			public const int WH_CBT = 5;
			public const int HCBT_ACTIVATE = 5;
			public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
			public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
			public const int TBM_GETTHUMBRECT = 0x0419;
			public const int TBM_GETCHANNELRECT = 0x041A;
			public const int TCM_SETPADDING = 0x132B;
			public const int TCM_SETITEMSIZE = 0x1329;
			public const int TCM_SETIMAGELIST = 0x1303;
			public const int TCM_ADJUSTRECT = 0x1328;
			public const int WM_MOUSEMOVE = 0x0200;
			public const int WM_MBUTTONDBLCLK = 0x0209;
			public const int CB_SETITEMHEIGHT = 0x0153;
			public const int CB_GETITEMHEIGHT = 0x0154;
			public const int WM_NCCALCSIZE = 0x0083;
			public const int WM_NCPAINT = 0x0085;
			public const int WM_PAINT = 0x000F;
			public const int EM_GETMARGINS = 0x00D4;
			public const int EM_GETRECT = 0x00B2;
			public const int EM_POSFROMCHAR = 0x00D6;
			public const int SWP_NOSIZE = 0x0001;
			public const int SWP_NOMOVE = 0x0002;
			public const int SWP_NOZORDER = 0x0004;
			public const int SWP_NOACTIVATE = 0x0010;
			public const int SWP_FRAMECHANGED = 0x0020;
			public const int RDW_INVALIDATE = 0x0001;
			public const int RDW_ERASE = 0x0004;
			public const int RDW_ALLCHILDREN = 0x0080;
			public const int RDW_FRAME = 0x0400;
			public const int WM_ERASEBKGND = 0x0014;
			public const int WM_CTLCOLOREDIT = 0x0133;
			public const int WM_CTLCOLORSTATIC = 0x0138;
			public const int WM_WINDOWPOSCHANGING = 0x0046;
			public const int GWL_STYLE = -16;
			public const int WS_BORDER = 0x00800000;
			public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
			public const int DWMWA_BORDER_COLOR = 34;
			public const int DWMWCP_ROUNDSMALL = 3;
			public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
			public const int WM_ENABLE = 0x000A;
			public const int WM_SYSCOLORCHANGE = 0x0015;
			public const int WM_SETTINGCHANGE = 0x001A;
			public const int WM_NCMOUSEMOVE = 0x00A0;
			public const int WM_NCMBUTTONDBLCLK = 0x00A9;
			public const int WM_TIMER = 0x0113;
			public const int WM_CAPTURECHANGED = 0x0215;
			public const int WM_NCMOUSELEAVE = 0x02A2;
			public const int WM_MOUSELEAVE = 0x02A3;
			public const int WM_PRINT = 0x0317;
			public const int WM_PRINTCLIENT = 0x0318;
			public const int WM_THEMECHANGED = 0x031A;
			public const int PRF_NONCLIENT = 0x0002;
			public const int PRF_CLIENT = 0x0004;
			public const int GWL_EXSTYLE = -20;
			public const int WS_EX_LAYOUTRTL = 0x00400000;
			public const int SBS_VERT = 0x0001;
			public const int OBJID_HSCROLL = unchecked((int)0xFFFFFFFA);
			public const int OBJID_VSCROLL = unchecked((int)0xFFFFFFFB);
			public const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);
			public const int STATE_SYSTEM_UNAVAILABLE = 0x00000001;
			public const int STATE_SYSTEM_PRESSED = 0x00000008;
			public const int STATE_SYSTEM_INVISIBLE = 0x00008000;
			public const int STATE_SYSTEM_OFFSCREEN = 0x00010000;
			public const int GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002;
			public const int GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004;

			/// <summary>A scroll bar: its place on the screen, its arrows' length, its thumb's ends and the states of it and its parts.</summary>
			[StructLayout(LayoutKind.Sequential)]
			public struct SCROLLBARINFO
			{
				public int cbSize;
				public RECT rcScrollBar;
				public int dxyLineButton, xyThumbTop, xyThumbBottom, reserved;
				/// <summary>The states Windows gives as rgstate: of the bar, its first arrow, the track before the thumb, the thumb, the track after it and its last arrow.</summary>
				public int Bar, FirstArrow, PageBefore, Thumb, PageAfter, LastArrow;
			}

			[DllImport("user32.dll")]
			public static extern bool GetScrollBarInfo(IntPtr hwnd, int idObject, ref SCROLLBARINFO info);

			[DllImport("user32.dll")]
			public static extern IntPtr WindowFromPoint(Point point);

			[DllImport("user32.dll")]
			public static extern bool KillTimer(IntPtr hWnd, IntPtr id);

			[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr OpenThemeData(IntPtr hwnd, string classList);

			[DllImport("uxtheme.dll")]
			public static extern int CloseThemeData(IntPtr theme);

			[DllImport("uxtheme.dll")]
			public static extern int DrawThemeBackground(IntPtr theme, IntPtr hdc, int part, int state, ref RECT rect, IntPtr clip);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern bool GetModuleHandleEx(int flags, IntPtr address, out IntPtr module);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern int GetModuleFileName(IntPtr module, StringBuilder name, int size);

			[StructLayout(LayoutKind.Sequential)]
			public struct NCCALCSIZE_PARAMS
			{
				public RECT rgrc0, rgrc1, rgrc2;
				public IntPtr lppos;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct WINDOWPOS
			{
				public IntPtr hwnd, hwndInsertAfter;
				public int x, y, cx, cy, flags;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct COMBOBOXINFO
			{
				public int cbSize;
				public RECT rcItem, rcButton;
				public int stateButton;
				public IntPtr hwndCombo, hwndItem, hwndList;
			}

			[DllImport("user32.dll")]
			public static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO info);

			[DllImport("gdi32.dll")]
			public static extern int SetTextColor(IntPtr hdc, int color);

			[DllImport("gdi32.dll")]
			public static extern int SetBkColor(IntPtr hdc, int color);

			[DllImport("gdi32.dll")]
			public static extern IntPtr CreateSolidBrush(int color);

			[DllImport("user32.dll")]
			public static extern int GetWindowLong(IntPtr hWnd, int index);

			[DllImport("user32.dll")]
			public static extern int SetWindowLong(IntPtr hWnd, int index, int value);

			[DllImport("user32.dll")]
			public static extern IntPtr GetWindowDC(IntPtr hWnd);

			[DllImport("user32.dll")]
			public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

			/// <summary>What a paint of a window is drawn with: its device context, whether to erase it, and the part to draw.</summary>
			[StructLayout(LayoutKind.Sequential)]
			public struct PAINTSTRUCT
			{
				public IntPtr hdc;
				public bool fErase;
				public RECT rcPaint;
				public bool fRestore;
				public bool fIncUpdate;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
				public byte[] rgbReserved;
			}

			[DllImport("user32.dll")]
			public static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT paint);

			[DllImport("user32.dll")]
			public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paint);

			[DllImport("user32.dll")]
			public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

			[DllImport("user32.dll")]
			public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

			[DllImport("user32.dll")]
			public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

			[DllImport("user32.dll")]
			public static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr region, int flags);

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int Left, Top, Right, Bottom;
			}

			public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

			[DllImport("user32.dll")]
			public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool UnhookWindowsHookEx(IntPtr hhk);

			[DllImport("kernel32.dll")]
			public static extern int GetCurrentThreadId();

			[DllImport("dwmapi.dll")]
			public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

			[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
			public static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

			[DllImport("user32.dll")]
			public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);
		}

		#endregion
	}
}
