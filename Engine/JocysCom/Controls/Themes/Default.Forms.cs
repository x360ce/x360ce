#nullable disable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
	/// The light theme is the Windows colours. In both themes, push buttons, tool bars and tab strips are drawn as the
	/// WPF programs draw them with Default.xaml: buttons flat, filled and rounded, tabs rounded and apart. High contrast
	/// keeps the Windows look.
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
			if (dark || changed)
			{
				ToolStripManager.VisualStylesEnabled = !dark;
				// The professional renderer keeps the colours it worked out, so it starts again from the new ones.
				if (ToolStripManager.Renderer.GetType() == typeof(ToolStripProfessionalRenderer))
					ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
			}
			if (!changed && !themeChanged)
				return;
			foreach (var form in Application.OpenForms.Cast<Form>().ToArray())
			{
				Apply(form);
				form.Invalidate(true);
			}
			ThemeChanged?.Invoke(null, EventArgs.Empty);
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
			{ KnownColor.ControlLightLight, "BackgroundWhite" },
			{ KnownColor.ButtonHighlight, "BackgroundWhite" },
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
			{ KnownColor.HotTrack, "ColorBrand" },
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

		/// <summary>The light colours of the Default.xaml keys the buttons, tool bars and tab strips are drawn with.</summary>
		/// <remarks>Copied here because not every program carries Default.xaml; a test holds them equal to it.</remarks>
		public static readonly Dictionary<string, Color> LightPalette = new Dictionary<string, Color>
		{
			{ "BackgroundWhite", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "BackgroundLight", Color.FromArgb(0xF0, 0xF0, 0xF0) },
			{ "BackgroundDark", Color.FromArgb(0xDE, 0xDE, 0xDE) },
			{ "BackgroundDarkSelected", Color.FromArgb(0xC2, 0xE7, 0xFF) },
			{ "BackgroundDarkPressed", Color.FromArgb(0xC2, 0xFF, 0xE7) },
			{ "BackgroundTabSelected", Color.FromArgb(0xFF, 0xFF, 0xFF) },
			{ "BorderDark", Color.FromArgb(0xCC, 0xCC, 0xCC) },
			{ "ForegroundBlack", Color.FromArgb(0x00, 0x00, 0x00) },
			{ "ForegroundGray", Color.FromArgb(0x5A, 0x5A, 0x5A) },
			{ "ForegroundTab", Color.FromArgb(0x5A, 0x5A, 0x5A) },
			{ "ForegroundTabSelected", Color.FromArgb(0x00, 0x00, 0x00) },
			{ "SelectedBackground", Color.FromArgb(0x00, 0x78, 0xD4) },
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
			/// <summary>A check box's own padding, which the larger box of Default.xaml widens.</summary>
			public Padding Padding;
			public bool EnableHeadersVisualStyles;
			public Color LinkColor;
			public Color VisitedLinkColor;
			public ToolStripRenderMode RenderMode;
			public ControlStyles? PaintStyles;
			public List<KeyValuePair<DataGridViewButtonColumn, FlatStyle>> ButtonColumns;
			/// <summary>A property grid's own colours, in the order <see cref="PropertyGridColors"/> reads them.</summary>
			public Color[] PropertyGridColors;
			public bool NativeDark;
			/// <summary>The tab under the mouse on a tab strip painted here, or -1.</summary>
			public int HotTab = -1;
			/// <summary>Whether the window of a tab strip has the taller tabs of a strip painted here.</summary>
			public bool TabsPadded;
		}

		static readonly ConditionalWeakTable<Control, State> States = new ConditionalWeakTable<Control, State>();

		/// <summary>Themes a control and everything on it, and the controls added to it later.</summary>
		/// <remarks>Windows are themed as they first become active, so this is for controls shown some other way.</remarks>
		public static void Apply(Control control)
		{
			if (control == null)
				return;
			ApplyControl(control);
			foreach (Control child in control.Controls)
				Apply(child);
		}

		static void ApplyControl(Control control)
		{
			State state;
			if (!States.TryGetValue(control, out state))
			{
				state = Remember(control);
				States.Add(control, state);
				control.ControlAdded += (sender, e) => Apply(e.Control);
				control.HandleCreated += (sender, e) =>
				{
					// A window made again has no window style yet, and a tab strip its own padding.
					state.NativeDark = false;
					state.TabsPadded = false;
					ApplyNative((Control)sender);
				};
			}
			var highContrast = SystemInformation.HighContrast;
			if (state.Dark == IsDark && state.HighContrast == highContrast)
				return;
			// A control the dark theme never changed is left exactly as it is, but for the buttons, tool bars and tab
			// strips the light theme draws as the WPF programs do.
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
			}
			if (control is ComboBox combo)
				state.FlatStyle = combo.FlatStyle;
			if (control is TabPage page)
				state.UseVisualStyleBackColor = page.UseVisualStyleBackColor;
			if (control is DataGridView grid)
			{
				state.EnableHeadersVisualStyles = grid.EnableHeadersVisualStyles;
				state.ButtonColumns = grid.Columns.OfType<DataGridViewButtonColumn>()
					.Select(x => new KeyValuePair<DataGridViewButtonColumn, FlatStyle>(x, x.FlatStyle)).ToList();
			}
			if (control is ToolStrip strip)
				state.RenderMode = strip.RenderMode;
			if (control is LinkLabel link)
			{
				state.LinkColor = link.LinkColor;
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
				}
				if (!IsWpfButton(button, state) && state.BackColor.IsSystemColor)
				{
					if (state.BackColorSet)
						button.BackColor = state.BackColor;
					else
						button.ResetBackColor();
				}
				// A check box is drawn by Windows Forms, so its box can be drawn again as the WPF programs draw it, with
				// its text moved right to make room for the larger box.
				button.Paint -= CheckBox_Paint;
				if (button is CheckBox check)
				{
					var wpf = IsWpfCheckBox(check, state);
					var extra = wpf ? CheckBoxPixels(CheckBoxSize.Width) - CheckBoxPixels(WindowsCheckBoxSize) : 0;
					var padding = new Padding(state.Padding.Left + extra, state.Padding.Top, state.Padding.Right, state.Padding.Bottom);
					if (check.Padding != padding)
						check.Padding = padding;
					if (wpf)
					{
						if (state.FlatStyle == FlatStyle.System)
							check.FlatStyle = FlatStyle.Standard;
						check.Paint += CheckBox_Paint;
						check.Invalidate();
					}
				}
			}
			if (control is ComboBox combo && state.FlatStyle != FlatStyle.Flat)
				combo.FlatStyle = IsDark ? FlatStyle.Flat : state.FlatStyle;
			if (control is GroupBox group)
			{
				group.Paint -= GroupBox_Paint;
				if (IsDark)
					group.Paint += GroupBox_Paint;
				group.Invalidate();
			}
			// A page of a strip painted here is filled plain, as the WPF programs fill one.
			if (control is TabPage page)
				page.UseVisualStyleBackColor = state.UseVisualStyleBackColor && !PaintsTabs(page.Parent);
			if (control is DataGridView grid)
			{
				grid.EnableHeadersVisualStyles = !IsDark && state.EnableHeadersVisualStyles;
				// Cells draw their buttons in the light visual style; flat, they take the theme colours.
				foreach (var pair in state.ButtonColumns)
					pair.Key.FlatStyle = IsDark ? FlatStyle.Flat : pair.Value;
				grid.CellPainting -= CheckCell_Painting;
				if (IsDark && HasDarkCheckBox())
					grid.CellPainting += CheckCell_Painting;
				grid.Invalidate();
			}
			if (control is LinkLabel link)
			{
				link.LinkColor = GetColor("ColorBrand", state.LinkColor);
				link.VisitedLinkColor = GetColor("ColorBrand", state.VisitedLinkColor);
			}
			// A tool bar without a renderer of its own is drawn as the WPF programs draw one, in both themes. The system
			// renderer draws a checked button with the Windows colours, so in the dark theme every other strip, a menu
			// or a status bar, is drawn by a professional one. Both are made anew because they keep the colours they
			// worked out.
			if (control is ToolStrip strip && state.RenderMode != ToolStripRenderMode.Custom)
			{
				if (IsWpfToolBar(strip))
					strip.Renderer = new WpfToolStripRenderer();
				else if (IsDark)
					strip.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
				else
					strip.RenderMode = state.RenderMode;
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
			if (control is TrackBar)
				SetPaintedHere(control, state, IsDark, Track_Paint);
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
			if (control is TabControl tabs && state.TabsPadded != PaintsTabs(tabs))
				ApplyTabPadding(tabs, state);
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

		const int SBP_UPPERTRACKVERT = 7;

		/// <summary>The buttons of the Windows dark style, named by class so a check box can be drawn in it without a window.</summary>
		const string DarkButtonClass = "DarkMode_Explorer::BUTTON";
		const int BP_CHECKBOX = 3;

		static VisualStyleRenderer _checkRenderer;

		static readonly MethodInfo GetContentBoundsMethod = typeof(DataGridViewCell).GetMethod("GetContentBounds",
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
			// Unchecked 1, checked 5, mixed 9; then hot +1, pressed +2, disabled +3.
			var state = check == CheckState.Checked ? 5 : check == CheckState.Indeterminate ? 9 : 1;
			var hot = box.Contains(grid.PointToClient(Control.MousePosition));
			if (!grid.Enabled)
				state += 3;
			else if (hot && (Control.MouseButtons & MouseButtons.Left) != 0)
				state += 2;
			else if (hot)
				state += 1;
			if (_checkRenderer == null)
				_checkRenderer = new VisualStyleRenderer(DarkButtonClass, BP_CHECKBOX, state);
			else
				_checkRenderer.SetParameters(DarkButtonClass, BP_CHECKBOX, state);
			_checkRenderer.DrawBackground(e.Graphics, box);
		}

		/// <summary>The rounding of a button's corners, a tab's top corners and a tab page's frame, as Default.xaml draws them, in device-independent pixels.</summary>
		const int CornerRadius = 5;

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

		/// <summary>Whether a check box is drawn as the WPF programs draw one: a box before its text the program left in a Windows style, while high contrast is off.</summary>
		static bool IsWpfCheckBox(CheckBox check, State state)
		{
			return check.Appearance == Appearance.Normal && check.CheckAlign == System.Drawing.ContentAlignment.MiddleLeft
				&& (state.FlatStyle == FlatStyle.Standard || state.FlatStyle == FlatStyle.System) && !SystemInformation.HighContrast;
		}

		/// <summary>The size of the box Windows draws a check box with, in device-independent pixels.</summary>
		const int WindowsCheckBoxSize = 13;

		/// <summary>
		/// The size of Default.xaml's check box, in device-independent pixels: its tick, 10 by 9.1, with 4 on every side for
		/// the tick's margin, the box's padding and its edge.
		/// </summary>
		static readonly SizeF CheckBoxSize = new SizeF(18f, 17.1f);

		/// <summary>A check box size in device-independent pixels, in whole pixels of this program's windows.</summary>
		static int CheckBoxPixels(float value)
		{
			return (int)Math.Round(value * ControlsHelper.DpiScale);
		}

		/// <summary>The rounding of a check box's corners in Default.xaml, in device-independent pixels.</summary>
		const int CheckBoxRadius = 3;

		/// <summary>The tick of Default.xaml's check box: its outline, in the 10 by 9.1 units of its own box.</summary>
		static readonly PointF[] CheckMark =
		{
			new PointF(9.97498f, 1.22334f), new PointF(4.6983f, 9.09834f), new PointF(4.52164f, 9.09834f), new PointF(0f, 5.19331f),
			new PointF(1.27664f, 3.52165f), new PointF(4.255f, 6.08833f), new PointF(8.33331f, 0f),
		};

		/// <summary>
		/// Draws Default.xaml's check box over the smaller one Windows drew, from where the program's padding ends: a rounded
		/// box in the window colour, edged in grey or, under the mouse, in the hover colour, with Default.xaml's tick, or a
		/// square while it is neither on nor off. Disabled, WPF draws the whole check box at half strength and its box at
		/// half that again.
		/// </summary>
		static void CheckBox_Paint(object sender, PaintEventArgs e)
		{
			var check = (CheckBox)sender;
			State state;
			if (!States.TryGetValue(check, out state))
				return;
			var g = e.Graphics;
			// A check box in a small font is not as tall as Default.xaml's box, which is then made as tall as it.
			var height = Math.Min(CheckBoxPixels(CheckBoxSize.Height), check.ClientSize.Height);
			var box = new Rectangle(state.Padding.Left, (check.ClientSize.Height - height) / 2, CheckBoxPixels(CheckBoxSize.Width), height);
			using (var brush = new SolidBrush(check.BackColor))
				g.FillRectangle(brush, Rectangle.Inflate(box, 1, 1));
			var hot = check.Enabled && check.ClientRectangle.Contains(check.PointToClient(Control.MousePosition));
			var alpha = check.Enabled ? 255 : 64;
			var saved = Smooth(g);
			var line = Line(g);
			var radius = Dip(g, CheckBoxRadius);
			using (var face = RoundedRectangle(Inset(box, line / 2), radius, radius, radius, radius))
			{
				using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("BackgroundWhite", SystemColors.Window))))
					g.FillPath(brush, face);
				using (var pen = new Pen(Color.FromArgb(alpha, Themed(hot ? "BackgroundDarkSelected" : "ForegroundGray", SystemColors.ControlDark)), line))
					g.DrawPath(pen, face);
			}
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("ForegroundBlack", SystemColors.ControlText))))
			{
				// The tick, or the square, stands 4 in from every side of the box, as in Default.xaml; the tick keeps to the
				// middle of a box made shorter.
				var scale = Dip(g, 1);
				var inset = 4 * scale;
				var top = box.Y + (box.Height - 9.1f * scale) / 2;
				if (check.CheckState == CheckState.Checked)
					g.FillPolygon(brush, CheckMark.Select(p => new PointF(box.X + inset + p.X * scale, top + p.Y * scale)).ToArray());
				else if (check.CheckState == CheckState.Indeterminate)
					g.FillRectangle(brush, Inset(box, inset));
			}
			g.Restore(saved);
		}

		/// <summary>Whether a strip is a tool bar drawn as the WPF programs draw one: not a menu, a status bar or a drop-down, while high contrast is off.</summary>
		static bool IsWpfToolBar(ToolStrip strip)
		{
			return !(strip is MenuStrip || strip is StatusStrip || strip is ToolStripDropDown) && !SystemInformation.HighContrast;
		}

		/// <summary>Whether a control is drawn as the WPF programs draw it, which the light theme does too.</summary>
		static bool DrawnAsWpf(Control control)
		{
			State state;
			if (control is ButtonBase button && States.TryGetValue(control, out state))
				return IsWpfButton(button, state) || (button is CheckBox check && IsWpfCheckBox(check, state));
			return PaintsTabs(control)
				|| (control is TabPage && PaintsTabs(control.Parent))
				|| (control is ToolStrip strip && IsWpfToolBar(strip));
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
			using (var corners = new GraphicsPath(FillMode.Alternate))
			using (var face = RoundedRectangle(bounds, left, right, right, left))
			using (var brush = new SolidBrush(button.Parent?.BackColor ?? SystemColors.Control))
			{
				corners.AddRectangle(bounds);
				corners.AddPath(face, false);
				g.FillPath(brush, corners);
			}
			if (button.Focused && (bool)(ShowFocusCuesProperty?.GetValue(button, null) ?? false))
				using (var pen = new Pen(Themed("SelectedBackground", SystemColors.Highlight)))
				using (var outline = RoundedRectangle(new RectangleF(0.5f, 0.5f, button.Width - 1, button.Height - 1), left, right, right, left))
					g.DrawPath(pen, outline);
			g.Restore(saved);
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
		/// Draws a tool bar as the WPF programs draw one: plain, without an edge, its buttons filled and rounded, lighter
		/// under the mouse and lighter again pressed. A menu that drops down from it is drawn as before.
		/// </summary>
		class WpfToolStripRenderer : ToolStripProfessionalRenderer
		{
			/// <summary>The room left at each side of a tool bar button's fill.</summary>
			const int ToolBarButtonInset = 1;

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

			/// <summary>Fills a button on the tool bar itself; false for one on a drop-down, which is drawn as before.</summary>
			static bool FillItem(Graphics g, ToolStripItem item)
			{
				if (item.IsOnDropDown)
					return false;
				var key = !item.Enabled ? "BackgroundDark"
					: item.Pressed ? "BackgroundDarkPressed"
					: item.Selected ? "BackgroundDarkSelected"
					: "BackgroundDark";
				var saved = Smooth(g);
				// A pixel in from each side, so buttons next to each other stand apart, as WPF's margins keep them.
				var inset = Dip(g, ToolBarButtonInset);
				var radius = Dip(g, CornerRadius);
				using (var brush = new SolidBrush(Themed(key, SystemColors.ControlLight)))
				using (var face = RoundedRectangle(new RectangleF(inset, 0, item.Width - inset * 2, item.Height), radius, radius, radius, radius))
					g.FillPath(brush, face);
				g.Restore(saved);
				return true;
			}
		}

		/// <summary>
		/// The visual style draws a group box frame light and, unless the box sets its own text colour, the
		/// caption in the style's colour; in the dark theme both are drawn again in the theme colours.
		/// </summary>
		static void GroupBox_Paint(object sender, PaintEventArgs e)
		{
			var box = (GroupBox)sender;
			var g = e.Graphics;
			var caption = TextRenderer.MeasureText(g, box.Text.Length > 0 ? box.Text : " ", box.Font);
			var top = caption.Height / 2;
			using (var brush = new SolidBrush(box.BackColor))
				g.FillRectangle(brush, box.ClientRectangle);
			using (var pen = new Pen(GetColor("BorderDark", SystemColors.ControlDark)))
				g.DrawRectangle(pen, 0, top, box.Width - 1, box.Height - top - 1);
			if (box.Text.Length > 0)
				TextRenderer.DrawText(g, box.Text, box.Font, new Point(6, 0),
					box.Enabled ? box.ForeColor : SystemColors.GrayText, box.BackColor);
		}

		#endregion

		#region Painted here

		static readonly MethodInfo SetStyleMethod = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly MethodInfo GetStyleMethod = typeof(Control).GetMethod("GetStyle", BindingFlags.Instance | BindingFlags.NonPublic);

		const ControlStyles PaintedHereStyles = ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw;

		/// <summary>
		/// The slider has no dark Windows style, so in the dark theme it is painted here, double buffered. The tab strip
		/// is painted here in both themes, as the WPF programs draw one.
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

		/// <summary>The room between two tabs, as Default.xaml leaves it after a TabItem.</summary>
		const int TabGap = 3;

		/// <summary>
		/// What a tab strip painted here adds to its tabs' padding on every side, in device-independent pixels, for each tab
		/// to stand as large as a TabItem of Default.xaml.
		/// </summary>
		const int TabExtraPadding = 2;

		/// <summary>
		/// Gives the tabs of a strip painted here the height of a TabItem of Default.xaml, and the tabs of any other
		/// strip their own padding back.
		/// </summary>
		/// <remarks>
		/// The padding goes to the window, because setting <see cref="TabControl.Padding"/> makes the window again, and
		/// a .NET Framework strip that holds the focus then throws as the focus leaves it. Windows measures the tabs
		/// with the padding when it is given the font, which Windows Forms gives no window painted here.
		/// </remarks>
		static void ApplyTabPadding(TabControl tabs, State state)
		{
			state.TabsPadded = PaintsTabs(tabs);
			var extra = state.TabsPadded ? (int)Math.Round(TabExtraPadding * ControlsHelper.DpiScale) : 0;
			var x = tabs.Padding.X + extra;
			var y = tabs.Padding.Y + extra;
			NativeMethods.SendMessage(tabs.Handle, NativeMethods.TCM_SETPADDING, IntPtr.Zero, (IntPtr)((y << 16) | (x & 0xFFFF)));
			SetWindowFontMethod?.Invoke(tabs, null);
			// Setting the tab size makes the strip lay its pages out again, in the room the tabs now leave.
			var itemSize = TypeDescriptor.GetProperties(tabs)[nameof(TabControl.ItemSize)];
			tabs.ItemSize = itemSize.ShouldSerializeValue(tabs) ? tabs.ItemSize : Size.Empty;
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
		/// The tabs keep the places and sizes Windows gives them, so nothing on the window moves; each is drawn
		/// <see cref="TabGap"/> narrower to leave the room between.
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
			var rects = new Rectangle[tabs.TabCount];
			var bottom = 0;
			for (var i = 0; i < rects.Length; i++)
			{
				rects[i] = tabs.GetTabRect(i);
				bottom = Math.Max(bottom, rects[i].Bottom);
			}
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
				var bounds = new Rectangle(r.X, r.Y, Math.Max(1, r.Width - gap), bottom - r.Y + (selected ? (int)line : 0));
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
				// The text and image of every tab sit at the same height: the line the chosen one reaches over into the
				// page is not part of it.
				var content = new Rectangle(bounds.X, bounds.Y, bounds.Width, bottom - r.Y);
				var page = tabs.TabPages[i];
				var index = GetTabImageIndex(tabs, page);
				if (index >= 0)
				{
					// Drawn by the list itself: taking an image out of it copies the image on every paint, and
					// fails for a list read from a resource stream.
					var size = tabs.ImageList.ImageSize;
					tabs.ImageList.Draw(g, content.X + 6, content.Y + (content.Height - size.Height) / 2, index);
					content = new Rectangle(content.X + size.Width + 6, content.Y, content.Width - size.Width - 6, content.Height);
				}
				var text = !tabs.Enabled ? SystemColors.GrayText
					: Themed(selected ? "ForegroundTabSelected" : "ForegroundTab", SystemColors.ControlText);
				TextRenderer.DrawText(g, page.Text, tabs.Font, content, text,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			}
			g.Restore(saved);
			// Windows marks the chosen tab while the strip has the keyboard focus and focus is being shown.
			if (tabs.SelectedIndex >= 0 && tabs.Focused && (bool)(ShowFocusCuesProperty?.GetValue(tabs, null) ?? false))
			{
				var focus = rects[tabs.SelectedIndex];
				focus = new Rectangle(focus.X + 2, focus.Y + 2, Math.Max(1, focus.Width - gap - 4), Math.Max(1, focus.Height - 4));
				ControlPaint.DrawFocusRectangle(g, focus);
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

		/// <summary>
		/// The rail and ticks of a slider in the theme colours, and its thumb as Windows draws it: the accent
		/// colour reads on dark.
		/// </summary>
		/// <remarks>Windows gives the places of the rail and the thumb; the ticks are spaced along the rail.</remarks>
		static void Track_Paint(object sender, PaintEventArgs e)
		{
			var track = (TrackBar)sender;
			var g = e.Graphics;
			var vertical = track.Orientation == Orientation.Vertical;
			using (var brush = new SolidBrush(track.BackColor))
				g.FillRectangle(brush, track.ClientRectangle);
			var rail = GetTrackRectangle(track, NativeMethods.TBM_GETCHANNELRECT);
			// Windows gives the rail of an upright slider as if it lay flat.
			if (vertical)
				rail = new Rectangle(rail.Y, rail.X, rail.Height, rail.Width);
			var thumb = GetTrackRectangle(track, NativeMethods.TBM_GETTHUMBRECT);
			using (var brush = new SolidBrush(GetColor("BackgroundDark", SystemColors.ControlLight)))
				g.FillRectangle(brush, rail);
			if (track.TickStyle != TickStyle.None)
				DrawTicks(track, g, rail, thumb, vertical);
			DrawThumb(track, g, thumb, vertical);
			// The focus shows as Windows shows it: while focused, once the keyboard has been used.
			var uiState = (int)NativeMethods.SendMessage(track.Handle, NativeMethods.WM_QUERYUISTATE, IntPtr.Zero, IntPtr.Zero);
			if (track.Focused && (uiState & NativeMethods.UISF_HIDEFOCUS) == 0)
				ControlPaint.DrawFocusRectangle(g, track.ClientRectangle, track.ForeColor, track.BackColor);
		}

		static Rectangle GetTrackRectangle(TrackBar track, int message)
		{
			var rect = new NativeMethods.RECT();
			NativeMethods.SendMessage(track.Handle, message, IntPtr.Zero, ref rect);
			return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
		}

		/// <summary>Ticks as Windows draws them: one pixel wide and four long, the two ends one longer.</summary>
		static void DrawTicks(TrackBar track, Graphics g, Rectangle rail, Rectangle thumb, bool vertical)
		{
			// The ends are where the middle of the thumb stands at the lowest and at the highest value, and a
			// tick stands at every step of the tick frequency between them.
			var half = (vertical ? thumb.Height : thumb.Width) / 2;
			var first = (vertical ? rail.Top : rail.Left) + half;
			var last = (vertical ? rail.Bottom : rail.Right) - half - 1;
			var range = Math.Max(1, track.Maximum - track.Minimum);
			var step = Math.Max(1, track.TickFrequency);
			var places = new List<int> { first, last };
			// Halves rounded up, as Windows rounds them.
			for (var value = step; value < range; value += step)
				places.Add(first + (int)Math.Round((last - first) * (double)value / range, MidpointRounding.AwayFromZero));
			var after = track.TickStyle != TickStyle.TopLeft;
			var before = track.TickStyle != TickStyle.BottomRight;
			var scale = g.DpiY / 96f;
			using (var brush = new SolidBrush(GetColor("BorderDark", SystemColors.ControlDark)))
			{
				for (var i = 0; i < places.Count; i++)
				{
					var length = (int)Math.Round((i < 2 ? 5 : 4) * scale);
					if (vertical && after)
						g.FillRectangle(brush, thumb.Right, places[i], length, 1);
					if (vertical && before)
						g.FillRectangle(brush, thumb.Left - length, places[i], length, 1);
					if (!vertical && after)
						g.FillRectangle(brush, places[i], thumb.Bottom, 1, length);
					if (!vertical && before)
						g.FillRectangle(brush, places[i], thumb.Top - length, 1, length);
				}
			}
		}

		/// <summary>The thumb Windows draws, pointing at the ticks, in the slider's state.</summary>
		static void DrawThumb(TrackBar track, Graphics g, Rectangle thumb, bool vertical)
		{
			var state = !track.Enabled ? TrackBarThumbState.Disabled
				: track.Capture && (Control.MouseButtons & MouseButtons.Left) != 0 ? TrackBarThumbState.Pressed
				: thumb.Contains(track.PointToClient(Control.MousePosition)) ? TrackBarThumbState.Hot
				: TrackBarThumbState.Normal;
			if (!VisualStyleRenderer.IsSupported)
			{
				ControlPaint.DrawButton(g, thumb, state == TrackBarThumbState.Disabled ? ButtonState.Inactive : ButtonState.Normal);
				return;
			}
			var element = track.TickStyle == TickStyle.BottomRight
				? (vertical ? VisualStyleElement.TrackBar.ThumbRight.Normal : VisualStyleElement.TrackBar.ThumbBottom.Normal)
				: track.TickStyle == TickStyle.TopLeft
				? (vertical ? VisualStyleElement.TrackBar.ThumbLeft.Normal : VisualStyleElement.TrackBar.ThumbTop.Normal)
				: vertical ? VisualStyleElement.TrackBar.ThumbVertical.Normal : VisualStyleElement.TrackBar.Thumb.Normal;
			// Every thumb part numbers its states alike: normal, hot, pressed, focused, disabled.
			new VisualStyleRenderer(element.ClassName, element.Part, (int)state).DrawBackground(g, thumb);
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
			public const int WM_QUERYUISTATE = 0x0129;
			public const int UISF_HIDEFOCUS = 0x1;
			public const int TBM_GETTHUMBRECT = 0x0419;
			public const int TBM_GETCHANNELRECT = 0x041A;
			public const int TCM_SETPADDING = 0x132B;

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
