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
	/// rounded, tabs rounded and apart, frames rounded, and a disabled control at half strength. High contrast keeps the
	/// Windows look.
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
			/// <summary>A check box's own padding, which the larger box of Default.xaml widens, or a drop-down list's, which its height does.</summary>
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
			/// <summary>A text box, number box or list's own edge, which the frame drawn here replaces.</summary>
			public BorderStyle BorderStyle;
			/// <summary>A text box or number box's own sizing and height, which Default.xaml's height replaces, and a number box's buttons' width.</summary>
			public bool AutoSize;
			public int Height;
			public int ButtonsWidth;
			/// <summary>A list's own drawing, item height and sizing to whole items, which Default.xaml's items replace.</summary>
			public DrawMode DrawMode;
			public int ItemHeight;
			public bool IntegralHeight;
			/// <summary>The frame drawn in the window's edge, while the control has one.</summary>
			public Frame Frame;
			/// <summary>What a grid drawn as the WPF programs draw one had before.</summary>
			public GridLook Grid;
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
			public int HeadersHeight;
			public int RowHeight;
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
			{
				state.FlatStyle = combo.FlatStyle;
				state.Padding = combo.Padding;
			}
			if (control is TabPage page)
				state.UseVisualStyleBackColor = page.UseVisualStyleBackColor;
			if (control is TextBox box)
			{
				state.BorderStyle = box.BorderStyle;
				state.AutoSize = box.AutoSize;
				state.Height = box.Height;
			}
			if (control is UpDownBase upDown)
			{
				state.BorderStyle = upDown.BorderStyle;
				state.Height = upDown.Height;
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
					// A button that sizes itself stands as tall as Default.xaml's: its text, padded 5 above and below.
					var padding = state.Padding;
					if (button.AutoSize)
					{
						var natural = button.GetPreferredSize(Size.Empty).Height - button.Padding.Vertical + state.Padding.Vertical;
						var extra = Math.Max(0, ControlHeight(button) - natural);
						padding = new Padding(padding.Left, padding.Top + extra / 2, padding.Right, padding.Bottom + extra - extra / 2);
					}
					if (button.Padding != padding)
						button.Padding = padding;
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
				}
				if (!IsWpfButton(button, state) && state.BackColor.IsSystemColor)
				{
					if (state.BackColorSet)
						button.BackColor = state.BackColor;
					else
						button.ResetBackColor();
				}
				// A check box or radio button is drawn by Windows Forms, so it can be drawn again as the WPF programs draw it,
				// with its text moved right to make room for the larger box.
				button.Paint -= CheckBox_Paint;
				if ((button is CheckBox || button is RadioButton) && !IsWpfButton(button, state))
				{
					var wpf = IsWpfCheckBox(button, state);
					var extra = wpf ? Pixels(CheckBoxSize.Width) - Pixels(WindowsCheckBoxSize) : 0;
					var padding = new Padding(state.Padding.Left + extra, state.Padding.Top, state.Padding.Right, state.Padding.Bottom);
					if (button.Padding != padding)
						button.Padding = padding;
					if (wpf)
					{
						if (state.FlatStyle == FlatStyle.System)
							button.FlatStyle = FlatStyle.Standard;
						button.Paint += CheckBox_Paint;
						button.Invalidate();
					}
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
				FitComboBox(combo, EventArgs.Empty);
				// Painted here, the list shows the mouse and the choice itself.
				combo.MouseEnter -= Repaint;
				combo.MouseLeave -= Repaint;
				combo.SelectedIndexChanged -= Repaint;
				combo.DropDownClosed -= Repaint;
				combo.GotFocus -= Repaint;
				combo.LostFocus -= Repaint;
				combo.FontChanged -= FitComboBox;
				combo.SizeChanged -= ComboBox_SizeChanged;
				if (paint)
				{
					combo.MouseEnter += Repaint;
					combo.MouseLeave += Repaint;
					combo.SelectedIndexChanged += Repaint;
					combo.DropDownClosed += Repaint;
					combo.GotFocus += Repaint;
					combo.LostFocus += Repaint;
					combo.FontChanged += FitComboBox;
					combo.SizeChanged += ComboBox_SizeChanged;
				}
			}
			if (control is GroupBox group)
			{
				group.Paint -= GroupBox_Paint;
				if (PaintsGroupBox(group))
					group.Paint += GroupBox_Paint;
				group.Invalidate();
			}
			// A page of a strip painted here is filled plain, as the WPF programs fill one.
			if (control is TabPage page)
				page.UseVisualStyleBackColor = state.UseVisualStyleBackColor && !PaintsTabs(page.Parent);
			if (control is DataGridView grid)
			{
				var wpf = PaintsGrid(grid, state);
				grid.EnableHeadersVisualStyles = !IsDark && !wpf && state.EnableHeadersVisualStyles;
				// Cells draw their buttons in the light visual style; flat, they take the theme colours.
				foreach (var pair in state.ButtonColumns)
					pair.Key.FlatStyle = IsDark ? FlatStyle.Flat : pair.Value;
				ApplyGridLook(grid, state, wpf);
				grid.CellPainting -= CheckCell_Painting;
				grid.CellPainting -= WpfGrid_CellPainting;
				grid.Paint -= WpfGrid_Paint;
				grid.CellMouseEnter -= GridCheckCell_Repaint;
				grid.CellMouseLeave -= GridCheckCell_Repaint;
				if (wpf)
				{
					grid.CellPainting += WpfGrid_CellPainting;
					grid.Paint += WpfGrid_Paint;
					grid.CellMouseEnter += GridCheckCell_Repaint;
					grid.CellMouseLeave += GridCheckCell_Repaint;
				}
				else if (IsDark && HasDarkCheckBox())
					grid.CellPainting += CheckCell_Painting;
				grid.Invalidate();
			}
			if (control is TextBox || control is UpDownBase || control is ListBox)
				ApplyFrame(control, state);
			if (control is ProgressBar)
				SetPaintedHere(control, state, PaintsProgress(control), Progress_Paint);
			if (control is LinkLabel link)
			{
				link.LinkColor = GetColor("ColorBrand", state.LinkColor);
				link.VisitedLinkColor = GetColor("ColorBrand", state.VisitedLinkColor);
			}
			// Windows Forms draws a disabled label's text in a shade of its back colour, too dark to read on a dark page, so
			// in the dark theme it is drawn again faded.
			if (control is Label label && !(control is LinkLabel))
			{
				label.Paint -= DisabledLabel_Paint;
				if (IsDark)
					label.Paint += DisabledLabel_Paint;
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
					strip.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
				else
					strip.RenderMode = state.RenderMode;
				strip.ItemAdded -= ToolBar_ItemAdded;
				if (wpf)
					strip.ItemAdded += ToolBar_ItemAdded;
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
			if (control is TrackBar)
				SetPaintedHere(control, state, !SystemInformation.HighContrast, Track_Paint);
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

		/// <summary>The size of the box Windows draws a check box with, in device-independent pixels.</summary>
		const int WindowsCheckBoxSize = 13;

		/// <summary>
		/// The size of Default.xaml's check box, in device-independent pixels: its tick, 10 by 9.1, with 4 on every side for
		/// the tick's margin, the box's padding and its edge.
		/// </summary>
		static readonly SizeF CheckBoxSize = new SizeF(18f, 17.1f);

		/// <summary>A size in device-independent pixels, in whole pixels of this program's windows.</summary>
		static int Pixels(float value)
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

		/// <summary>The room between Default.xaml's check box and its text, in device-independent pixels: the box's margin.</summary>
		const int CheckBoxTextGap = 3;

		/// <summary>The size of Default.xaml's radio button box, in device-independent pixels, and of its dot.</summary>
		const int RadioButtonSize = 18;
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
			var size = radio != null ? new SizeF(RadioButtonSize, RadioButtonSize) : CheckBoxSize;
			// A control in a small font is not as tall as Default.xaml's box, which is then made as tall as it.
			var height = Math.Min(Pixels(size.Height), client.Height);
			var box = new Rectangle(state.Padding.Left, (client.Height - height) / 2, Pixels(size.Width), height);
			var hot = button.Enabled && client.Contains(button.PointToClient(Control.MousePosition));
			var check = radio != null ? (radio.Checked ? CheckState.Checked : CheckState.Unchecked) : ((CheckBox)button).CheckState;
			DrawWpfCheck(g, box, check, radio != null, hot, button.Enabled);
			var text = Rectangle.FromLTRB(box.Right + Pixels(CheckBoxTextGap), button.Padding.Top,
				client.Width - button.Padding.Right, client.Height - button.Padding.Bottom);
			var behind = button.BackColor.A == 255 ? button.BackColor : button.Parent?.BackColor ?? SystemColors.Control;
			TextRenderer.DrawText(g, button.Text, button.Font, text, button.Enabled ? button.ForeColor : Half(button.ForeColor, behind),
				TextFlags(button) | TextFormatFlags.NoPadding);
			if (button.Focused && (bool)(ShowFocusCuesProperty?.GetValue(button, null) ?? false))
				DrawFocus(g, client, button.ForeColor);
		}

		/// <summary>
		/// Draws Default.xaml's check box: a rounded box in the window colour, edged in the control colour or, under the mouse,
		/// in the hover colour, with its tick, or a square while it is neither on nor off; or its radio button, round, with a
		/// dot. Disabled, it is drawn at a quarter strength.
		/// </summary>
		static void DrawWpfCheck(Graphics g, Rectangle box, CheckState check, bool round, bool hot, bool enabled)
		{
			var alpha = enabled ? 255 : 64;
			var saved = Smooth(g);
			var line = Line(g);
			var radius = round ? box.Height / 2f : Dip(g, CheckBoxRadius);
			using (var face = RoundedRectangle(Inset(box, line / 2), radius, radius, radius, radius))
			{
				using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("BackgroundWhite", SystemColors.Window))))
					g.FillPath(brush, face);
				using (var pen = new Pen(Color.FromArgb(alpha, Themed(hot ? "BackgroundDarkSelected" : "BackgroundDark", SystemColors.ControlLight)), line))
					g.DrawPath(pen, face);
			}
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("ForegroundBlack", SystemColors.ControlText))))
			{
				var scale = Dip(g, 1);
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
					// The tick, or the square, stands 4 in from every side of the box, as in Default.xaml; the tick keeps to the
					// middle of a box made shorter.
					var inset = 4 * scale;
					var top = box.Y + (box.Height - 9.1f * scale) / 2;
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
			if (button.Enabled || button.Text.Length == 0 || !GetButtonLayout(button, e, out text, out image))
				return;
			var behind = button.BackColor.A == 255 ? button.BackColor : button.Parent?.BackColor ?? SystemColors.Control;
			using (var brush = new SolidBrush(behind))
				e.Graphics.FillRectangle(brush, Rectangle.Inflate(text, 1, 1));
			TextRenderer.DrawText(e.Graphics, button.Text, button.Font, text, Half(button.ForeColor, behind), TextFlags(button));
		}

		static readonly MethodInfo LabelTextFlagsMethod = typeof(Label).GetMethod("CreateTextFormatFlags", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

		/// <summary>
		/// Draws the text of a disabled label again, faded, where Windows Forms put it and with the flags it uses. A label with
		/// an image, or one drawing its text the older way, is left as it is.
		/// </summary>
		static void DisabledLabel_Paint(object sender, PaintEventArgs e)
		{
			var label = (Label)sender;
			if (label.Enabled || label.Text.Length == 0 || label.Image != null || label.BackgroundImage != null
				|| label.UseCompatibleTextRendering || LabelTextFlagsMethod == null)
				return;
			var g = e.Graphics;
			var client = label.ClientRectangle;
			var face = new Rectangle(client.X + label.Padding.Left, client.Y + label.Padding.Top,
				client.Width - label.Padding.Horizontal, client.Height - label.Padding.Vertical);
			if (label.BackColor.A < 255)
				ButtonRenderer.DrawParentBackground(g, face, label);
			using (var brush = new SolidBrush(label.BackColor))
				g.FillRectangle(brush, face);
			var behind = label.BackColor.A == 255 ? label.BackColor : label.Parent?.BackColor ?? SystemColors.Control;
			TextRenderer.DrawText(g, label.Text, label.Font, face, Half(label.ForeColor, behind),
				(TextFormatFlags)LabelTextFlagsMethod.Invoke(label, null));
		}

		/// <summary>A colour at half strength over the one behind it, as WPF draws a disabled control.</summary>
		static Color Half(Color color, Color behind)
		{
			return Color.FromArgb((color.R + behind.R) / 2, (color.G + behind.G) / 2, (color.B + behind.B) / 2);
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

		/// <summary>Whether a control is drawn as the WPF programs draw it, which the light theme does too.</summary>
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
				|| HasFrame(control);
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
			// face is drawn again at half strength, as WPF draws a disabled control, with the image and text where Windows
			// Forms put them.
			Rectangle text, image;
			if (!button.Enabled && GetButtonLayout(button, e, out text, out image))
			{
				using (var brush = new SolidBrush(Half(button.BackColor, behind)))
				using (var face = RoundedRectangle(bounds, left, right, right, left))
					g.FillPath(brush, face);
				var picture = button.Image;
				if (picture != null)
					using (var attributes = new ImageAttributes())
					{
						attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.5f });
						var place = new Rectangle(image.X + (image.Width - picture.Width) / 2, image.Y + (image.Height - picture.Height) / 2, picture.Width, picture.Height);
						g.DrawImage(picture, place, 0, 0, picture.Width, picture.Height, GraphicsUnit.Pixel, attributes);
					}
				TextRenderer.DrawText(g, button.Text, button.Font, text, Half(button.ForeColor, behind), TextFlags(button));
			}
			if (button.Focused && (bool)(ShowFocusCuesProperty?.GetValue(button, null) ?? false))
				using (var pen = new Pen(Themed("SelectedBackground", SystemColors.Highlight)))
				using (var outline = RoundedRectangle(new RectangleF(0.5f, 0.5f, button.Width - 1, button.Height - 1), left, right, right, left))
					g.DrawPath(pen, outline);
			g.Restore(saved);
		}

		static readonly PropertyInfo AdapterProperty = typeof(ButtonBase).GetProperty("Adapter", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>Where Windows Forms puts a button's text and image, from its own layout; false where the runtime does not show it.</summary>
		static bool GetButtonLayout(ButtonBase button, PaintEventArgs e, out Rectangle text, out Rectangle image)
		{
			text = image = Rectangle.Empty;
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var adapter = AdapterProperty?.GetValue(button, null);
			var options = adapter?.GetType().GetMethod("Layout", flags, null, new[] { typeof(PaintEventArgs) }, null)?.Invoke(adapter, new object[] { e });
			var data = options?.GetType().GetMethod("Layout", flags, null, Type.EmptyTypes, null)?.Invoke(options, null);
			// .NET Framework names the fields in lower case, later Windows Forms in upper.
			var type = data?.GetType();
			var textField = type?.GetField("textBounds", flags) ?? type?.GetField("TextBounds", flags);
			var imageField = type?.GetField("imageBounds", flags) ?? type?.GetField("ImageBounds", flags);
			if (textField == null || imageField == null)
				return false;
			text = (Rectangle)textField.GetValue(data);
			image = (Rectangle)imageField.GetValue(data);
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
				// In from each side, so buttons next to each other stand apart, as WPF's margins keep them.
				var inset = Dip(g, ToolBarButtonInset);
				var radius = Dip(g, CornerRadius);
				using (var brush = new SolidBrush(Color.FromArgb(item.Enabled ? 255 : 128, Themed(key, SystemColors.ControlLight))))
				using (var face = RoundedRectangle(new RectangleF(inset, 0, item.Width - inset * 2, item.Height), radius, radius, radius, radius))
					g.FillPath(brush, face);
				g.Restore(saved);
				return true;
			}
		}

		/// <summary>The room left at each side of a tool bar button's fill, in device-independent pixels: Default.xaml's button margin.</summary>
		const int ToolBarButtonInset = 3;

		/// <summary>
		/// What the theme adds between a tool bar button's fill and what is on it, in device-independent pixels: with the room
		/// Windows Forms leaves round the text, the 5 Default.xaml pads a button with.
		/// </summary>
		const int ToolBarButtonPadding = 2;

		/// <summary>The padding each tool bar button had before the theme widened it.</summary>
		static readonly ConditionalWeakTable<ToolStripItem, StrongBox<Padding>> ItemPaddings = new ConditionalWeakTable<ToolStripItem, StrongBox<Padding>>();

		/// <summary>
		/// Widens a button on a tool bar drawn as the WPF programs draw one by the room round its fill and the padding inside it,
		/// and makes it as tall as Default.xaml's button; gives any other button its own padding back.
		/// </summary>
		static void PadToolBarItem(ToolStripItem item, bool wpf)
		{
			if (!(item is ToolStripButton || item is ToolStripDropDownButton || item is ToolStripSplitButton))
				return;
			StrongBox<Padding> own;
			if (!ItemPaddings.TryGetValue(item, out own))
			{
				if (!wpf)
					return;
				own = new StrongBox<Padding>(item.Padding);
				ItemPaddings.Add(item, own);
			}
			var extra = wpf ? Pixels(ToolBarButtonInset + ToolBarButtonPadding) : 0;
			var natural = item.GetPreferredSize(Size.Empty).Height - item.Padding.Vertical + own.Value.Vertical;
			var taller = wpf ? Math.Max(0, item.Font.Height + Pixels(ComboBoxPadding * 2) - natural) : 0;
			var padding = new Padding(own.Value.Left + extra, own.Value.Top + taller / 2, own.Value.Right + extra, own.Value.Bottom + taller - taller / 2);
			if (item.Padding != padding)
				item.Padding = padding;
		}

		static void ToolBar_ItemAdded(object sender, ToolStripItemEventArgs e)
		{
			PadToolBarItem(e.Item, true);
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
		const int GroupBoxCaptionGap = 10;
		const int GroupBoxCaptionPadding = 5;
		const int GroupBoxCaptionTop = 5;

		/// <summary>
		/// Draws a group box as Default.xaml does, over what Windows Forms drew: its frame rounded, in the border colour, with a
		/// line in the window colour beside it, a pixel right and down, and its caption in a break in the frame clear of the
		/// corner, the frame's top line through the caption's middle. The caption is never covered by the box's controls.
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
			// Default.xaml's caption stands lower than Windows Forms' does, as far as the box's first control leaves room.
			var room = box.Controls.Cast<Control>().Where(x => x.Visible).Select(x => x.Top).DefaultIfEmpty(client.Height).Min() - caption.Height;
			var captionTop = Math.Max(0, Math.Min((int)Math.Round(Dip(g, GroupBoxCaptionTop)), room));
			var top = captionTop + caption.Height / 2;
			var saved = Smooth(g);
			var line = Line(g);
			var radius = Dip(g, CornerRadius);
			var frame = new RectangleF(0, top, client.Width - line, client.Height - top - line);
			using (var pen = new Pen(Themed("BackgroundWhite", SystemColors.Window), line))
			using (var path = RoundedRectangle(Inset(new RectangleF(frame.X + line, frame.Y + line, frame.Width, frame.Height), line / 2), radius, radius, radius, radius))
				g.DrawPath(pen, path);
			using (var pen = new Pen(Themed("BorderDark", SystemColors.ControlDark), line))
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
				box.Enabled ? box.ForeColor : SystemColors.GrayText, flags);
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

		/// <summary>
		/// Gives the window of a drop-down list its font, and makes a list painted here as tall as a push button in the same
		/// font, as Default.xaml makes both: its text with 5 device-independent pixels above and below. One on a tool bar keeps
		/// the bar's height, and any other list gets its own size back.
		/// </summary>
		/// <remarks>
		/// Windows Forms gives its font only to a window it leaves Windows to paint; without it, Windows sizes the field and draws
		/// the list it opens in the system's bitmap font. The padding is what Windows Forms adds to a list's preferred height, so
		/// a layout leaves the room.
		/// </remarks>
		static void FitComboBox(object sender, EventArgs e)
		{
			var combo = (ComboBox)sender;
			State state;
			if (!States.TryGetValue(combo, out state))
				return;
			var height = ComboBoxHeight(combo);
			var natural = combo.PreferredHeight - combo.Padding.Vertical + state.Padding.Vertical;
			var extra = Math.Max(0, height - natural);
			var padding = new Padding(state.Padding.Left, state.Padding.Top + extra / 2, state.Padding.Right, state.Padding.Bottom + extra - extra / 2);
			if (combo.Padding != padding)
				combo.Padding = padding;
			if (!combo.IsHandleCreated)
				return;
			SetWindowFontMethod?.Invoke(combo, null);
			RaiseComboBox(combo, height);
		}

		/// <summary>The height Default.xaml gives a drop-down list painted here, off a tool bar; 0 for any other list.</summary>
		static int ComboBoxHeight(ComboBox combo)
		{
			return PaintsComboBox(combo) && !(combo.Parent is ToolStrip) ? ControlHeight(combo) : 0;
		}

		/// <summary>Makes a drop-down list's field tall enough for the list to stand the height given.</summary>
		static void RaiseComboBox(ComboBox combo, int height)
		{
			var field = (int)NativeMethods.SendMessage(combo.Handle, NativeMethods.CB_GETITEMHEIGHT, (IntPtr)(-1), IntPtr.Zero);
			if (field > 0 && height > combo.Height)
				NativeMethods.SendMessage(combo.Handle, NativeMethods.CB_SETITEMHEIGHT, (IntPtr)(-1), (IntPtr)(field + height - combo.Height));
		}

		/// <summary>Windows fits a drop-down list's field to its font whenever the list is resized, so it is raised again.</summary>
		static void ComboBox_SizeChanged(object sender, EventArgs e)
		{
			var combo = (ComboBox)sender;
			if (combo.IsHandleCreated)
				RaiseComboBox(combo, ComboBoxHeight(combo));
		}

		/// <summary>
		/// Whether a drop-down list is painted here, as the WPF programs draw one: a list the user picks from rather than types
		/// in, whose items the program does not draw and which does not edit a grid cell, while high contrast is off.
		/// </summary>
		static bool PaintsComboBox(Control control)
		{
			return control is ComboBox combo && combo.DropDownStyle == ComboBoxStyle.DropDownList
				&& combo.DrawMode == DrawMode.Normal && !(control is IDataGridViewEditingControl) && !SystemInformation.HighContrast;
		}

		/// <summary>The width of the column a drop-down list's triangle stands in, in device-independent pixels.</summary>
		const int ComboBoxArrowColumn = 16;

		/// <summary>The room between a drop-down list's edge and what is on it, in device-independent pixels: its border and padding.</summary>
		const int ComboBoxPadding = 5;

		/// <summary>
		/// A drop-down list as Default.xaml draws one: a rounded field in the control colour, the hover colour under the mouse,
		/// with the chosen item's text and a triangle pointing down; disabled, all at half strength.
		/// </summary>
		static void ComboBox_Paint(object sender, PaintEventArgs e)
		{
			var combo = (ComboBox)sender;
			var g = e.Graphics;
			var client = combo.ClientRectangle;
			var behind = combo.Parent?.BackColor ?? SystemColors.Control;
			using (var brush = new SolidBrush(behind))
				g.FillRectangle(brush, client);
			var hot = combo.Enabled && client.Contains(combo.PointToClient(Control.MousePosition));
			var alpha = combo.Enabled ? 255 : 128;
			var saved = Smooth(g);
			var radius = Dip(g, CornerRadius);
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed(hot ? "BackgroundDarkSelected" : "BackgroundDark", SystemColors.ControlLight))))
			using (var face = RoundedRectangle(client, radius, radius, radius, radius))
				g.FillPath(brush, face);
			// Default.xaml's triangle, 10 by 5 drawn 1.4 times as large, in the middle of its column.
			var scale = Dip(g, 1);
			var x = client.Width - (ComboBoxPadding + ComboBoxArrowColumn / 2f) * scale;
			var y = client.Height / 2f;
			var points = new[]
			{
				new PointF(x - 7 * scale, y - 3.5f * scale), new PointF(x + 7 * scale, y - 3.5f * scale), new PointF(x, y + 3.5f * scale),
			};
			using (var brush = new SolidBrush(Color.FromArgb(alpha, combo.ForeColor)))
				g.FillPolygon(brush, points);
			g.Restore(saved);
			var padding = (int)Math.Round(ComboBoxPadding * scale);
			var text = Rectangle.FromLTRB(padding, 0, client.Width - (int)Math.Round((ComboBoxPadding * 2 + ComboBoxArrowColumn) * scale), client.Height);
			TextRenderer.DrawText(g, combo.Text, combo.Font, text, combo.Enabled ? combo.ForeColor : Half(combo.ForeColor, behind),
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
			if (combo.Focused && (bool)(ShowFocusCuesProperty?.GetValue(combo, null) ?? false))
				DrawFocus(g, client, combo.ForeColor);
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

		/// <summary>The colours of WPF's own slider and progress bar, which Default.xaml leaves as WPF draws them.</summary>
		static readonly Color SliderTrack = Color.FromArgb(0xE7, 0xEA, 0xEA);
		static readonly Color SliderTrackEdge = Color.FromArgb(0xD6, 0xD6, 0xD6);
		static readonly Color ProgressFill = Color.FromArgb(0x06, 0xB0, 0x25);
		static readonly Color ProgressTrack = Color.FromArgb(0xE6, 0xE6, 0xE6);
		static readonly Color ProgressEdge = Color.FromArgb(0xBC, 0xBC, 0xBC);

		/// <summary>The height of WPF's slider track, in device-independent pixels.</summary>
		const int SliderTrackHeight = 4;

		/// <summary>
		/// A slider as WPF draws one: a thin track, ticks in the theme's control colour and the thumb pointing at them.
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
			// WPF's track: 4 high, edged, through the middle of the thumb.
			var height = (int)Math.Round(Dip(g, SliderTrackHeight));
			var bar = vertical
				? new Rectangle(thumb.X + (thumb.Width - height) / 2, rail.Y, height, rail.Height)
				: new Rectangle(rail.X, thumb.Y + (thumb.Height - height) / 2, rail.Width, height);
			using (var brush = new SolidBrush(SliderTrack))
				g.FillRectangle(brush, bar);
			using (var pen = new Pen(SliderTrackEdge))
				g.DrawRectangle(pen, bar.X, bar.Y, bar.Width - 1, bar.Height - 1);
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

		/// <summary>Ticks where Windows puts them, one pixel wide and four long, the two ends one longer, in the colour WPF's are.</summary>
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
			using (var brush = new SolidBrush(Themed("BackgroundDark", SystemColors.ControlDark)))
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

		/// <summary>
		/// WPF's thumb, 11 by 18, pointing 6 of its 18 at the ticks, or square ended without them, in the colours of the
		/// slider's state.
		/// </summary>
		static void DrawThumb(TrackBar track, Graphics g, Rectangle thumb, bool vertical)
		{
			Color fill, edge;
			if (!track.Enabled)
			{
				fill = Color.FromArgb(0xF0, 0xF0, 0xF0);
				edge = Color.FromArgb(0xD9, 0xD9, 0xD9);
			}
			else if (track.Capture && (Control.MouseButtons & MouseButtons.Left) != 0)
			{
				fill = Color.FromArgb(0xDA, 0xEC, 0xFC);
				edge = Color.FromArgb(0x56, 0x9D, 0xE5);
			}
			else if (thumb.Contains(track.PointToClient(Control.MousePosition)))
			{
				fill = Color.FromArgb(0xDC, 0xEC, 0xFC);
				edge = Color.FromArgb(0x7E, 0xB4, 0xEA);
			}
			else
			{
				fill = Color.FromArgb(0xF0, 0xF0, 0xF0);
				edge = Color.FromArgb(0xAC, 0xAC, 0xAC);
			}
			// The shape lying flat, pointing down, in its own units; turned to the side the ticks are on.
			var tip = track.TickStyle == TickStyle.BottomRight || track.TickStyle == TickStyle.TopLeft ? 12f : 18f;
			var shape = new[] { new PointF(0, 0), new PointF(11, 0), new PointF(11, tip), new PointF(5.5f, 18), new PointF(0, tip) };
			var scale = Dip(g, 1);
			var width = 11 * scale;
			var length = 18 * scale;
			// Across the slider the shape is 11 wide; along the way to the ticks it is 18 long.
			var left = thumb.X + (thumb.Width - (vertical ? length : width)) / 2;
			var top = thumb.Y + (thumb.Height - (vertical ? width : length)) / 2;
			var flip = track.TickStyle == TickStyle.TopLeft;
			var points = shape.Select(p =>
			{
				var across = p.X * scale;
				var along = (flip ? 18 - p.Y : p.Y) * scale;
				return vertical ? new PointF(left + along, top + across) : new PointF(left + across, top + along);
			}).ToArray();
			var saved = Smooth(g);
			using (var brush = new SolidBrush(fill))
				g.FillPolygon(brush, points);
			using (var pen = new Pen(edge, Line(g)))
				g.DrawPolygon(pen, points);
			g.Restore(saved);
		}

		/// <summary>Whether a progress bar is painted here as WPF draws one: one that shows how far it is, while high contrast is off.</summary>
		static bool PaintsProgress(Control control)
		{
			return control is ProgressBar bar && bar.Style != ProgressBarStyle.Marquee && !SystemInformation.HighContrast;
		}

		/// <summary>A progress bar as WPF draws one: its track edged, filled flat in green as far as it has gone.</summary>
		static void Progress_Paint(object sender, PaintEventArgs e)
		{
			var bar = (ProgressBar)sender;
			var g = e.Graphics;
			var client = bar.ClientRectangle;
			using (var brush = new SolidBrush(ProgressTrack))
				g.FillRectangle(brush, client);
			var range = Math.Max(1, bar.Maximum - bar.Minimum);
			var inner = Rectangle.Inflate(client, -1, -1);
			var done = (int)Math.Round(inner.Width * (double)(bar.Value - bar.Minimum) / range);
			if (bar.RightToLeftLayout && bar.RightToLeft == RightToLeft.Yes)
				inner.X = inner.Right - done;
			using (var brush = new SolidBrush(ProgressFill))
				g.FillRectangle(brush, inner.X, inner.Y, done, inner.Height);
			using (var pen = new Pen(ProgressEdge))
				g.DrawRectangle(pen, 0, 0, client.Width - 1, client.Height - 1);
		}

		#endregion

		#region Grids

		/// <summary>The smallest heights of a grid's headers and rows in Default.xaml, in device-independent pixels.</summary>
		const int GridHeaderHeight = 24;
		const int GridRowHeight = 25;

		/// <summary>The room before a cell's check box, in device-independent pixels: CheckBoxCell's margin.</summary>
		const int GridCheckBoxMargin = 3;

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
		/// rows, in the page colour, headers without lines and padded as Default.xaml pads them, and rows and headers at least
		/// as tall as Default.xaml makes them.
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
					HeadersHeight = grid.ColumnHeadersHeight,
					RowHeight = grid.RowTemplate.Height,
				};
			}
			grid.BorderStyle = wpf ? BorderStyle.None : look.BorderStyle;
			grid.BackgroundColor = wpf ? Themed("BackgroundWhite", SystemColors.Window) : look.BackgroundColor;
			grid.GridColor = wpf ? Themed("BackgroundLight", SystemColors.Control) : look.GridColor;
			grid.CellBorderStyle = wpf ? DataGridViewCellBorderStyle.SingleHorizontal : look.CellBorderStyle;
			grid.ColumnHeadersBorderStyle = wpf ? DataGridViewHeaderBorderStyle.None : look.HeadersBorderStyle;
			// Default.xaml pads a header 16 on the right for the sort triangle; Windows Forms leaves that room itself.
			grid.ColumnHeadersDefaultCellStyle.Padding = wpf ? new Padding(Pixels(3), 0, 0, 0) : look.HeaderPadding;
			if (grid.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize)
				grid.ColumnHeadersHeight = wpf ? Math.Max(look.HeadersHeight, Pixels(GridHeaderHeight)) : look.HeadersHeight;
			var rowHeight = wpf ? Math.Max(look.RowHeight, Pixels(GridRowHeight)) : look.RowHeight;
			grid.RowTemplate.Height = rowHeight;
			if (grid.AutoSizeRowsMode == DataGridViewAutoSizeRowsMode.None)
				for (var i = 0; i < grid.Rows.Count; i++)
					grid.Rows[i].Height = rowHeight;
		}

		/// <summary>
		/// Paints a grid's headers as Default.xaml does, from the window colour at the top to the page colour at the bottom
		/// and without lines, and its check boxes as CheckBoxCell does: Default.xaml's check box at the cell's left.
		/// </summary>
		static void WpfGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var bounds = e.CellBounds;
			if (e.RowIndex < 0)
			{
				if (bounds.Width > 0 && bounds.Height > 0)
					using (var brush = new LinearGradientBrush(new Rectangle(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height + 2),
						Themed("BackgroundWhite", SystemColors.Window), Themed("BackgroundLight", SystemColors.Control), LinearGradientMode.Vertical))
						e.Graphics.FillRectangle(brush, bounds);
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
			var height = Math.Min(Pixels(CheckBoxSize.Height), bounds.Height);
			var box = new Rectangle(bounds.X + Pixels(GridCheckBoxMargin), bounds.Y + (bounds.Height - height) / 2, Pixels(CheckBoxSize.Width), height);
			var check = e.FormattedValue is CheckState checkState ? checkState
				: e.FormattedValue is bool isChecked && isChecked ? CheckState.Checked : CheckState.Unchecked;
			var hot = grid.Enabled && bounds.Contains(grid.PointToClient(Control.MousePosition));
			DrawWpfCheck(e.Graphics, box, check, false, hot, grid.Enabled);
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
					Themed("BackgroundWhite", SystemColors.Window), Themed("BackgroundLight", SystemColors.Control), LinearGradientMode.Vertical))
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

		#endregion

		#region Frames

		/// <summary>The height Default.xaml gives a control with a line of text: the text, with 5 device-independent pixels above and below.</summary>
		static int ControlHeight(Control control)
		{
			return control.Font.Height + Pixels(ComboBoxPadding * 2);
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
						buttons.Paint += UpDownButtons_Paint;
					buttons.Invalidate();
				}
				// The buttons as wide as Default.xaml's.
				if (ButtonsWidthField != null)
				{
					ButtonsWidthField.SetValue(upDown, framed ? Pixels(SpinButtonWidth) : state.ButtonsWidth);
					upDown.PerformLayout();
				}
			}
			if (control is ListBox list)
			{
				if (list.BorderStyle != border)
					list.BorderStyle = border;
				var drawMode = framed ? DrawMode.OwnerDrawFixed : state.DrawMode;
				if (list.DrawMode != drawMode)
					list.DrawMode = drawMode;
				list.ItemHeight = framed ? ControlHeight(list) : state.ItemHeight;
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
				if (framed)
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
				control.FontChanged += Frame_FontChanged;
			}
			SetFrameColor(control);
			if (control.IsHandleCreated)
			{
				AttachFrame(control, state);
				RedrawFrame(control, EventArgs.Empty);
			}
			// Unframed, Windows Forms gives a number box its own height again.
			if (control is UpDownBase && !framed)
				control.Height = state.Height;
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
			// Windows works the edge out again, and a framed number box takes its height.
			var flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED;
			if (!(control is UpDownBase && HasFrame(control)))
				flags |= NativeMethods.SWP_NOSIZE;
			NativeMethods.SetWindowPos(control.Handle, IntPtr.Zero, 0, 0, control.Width, control.Height, flags);
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

		/// <summary>The colour inside a frame: the window colour, or the control colour for a list and for a box that cannot be changed.</summary>
		static Color FrameFace(Control control)
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
		/// The frame Default.xaml draws round a text box, a number box or a list, drawn in the window's own edge: rounded, with
		/// room inside, in the theme colours, its edge in the hover colour under the mouse and while it has the focus.
		/// </summary>
		/// <remarks>
		/// Windows draws a control's edge outside the part of the window a program paints, so the edge is made as wide as the
		/// frame and the room inside it, and drawn here. Windows fills the inside of a box that cannot be changed, and of a
		/// list, with the colour the frame gives it.
		/// </remarks>
		class Frame : NativeWindow
		{
			readonly Control Control;

			public Frame(Control control)
			{
				Control = control;
			}

			/// <summary>The edge, in pixels, round a window of the size given: the frame's line and the room inside it.</summary>
			Padding Insets(Size window)
			{
				var line = Math.Max(1, (int)Math.Floor(ControlsHelper.DpiScale));
				var top = Math.Max(0, (window.Height - Control.Font.Height) / 2);
				var bottom = Math.Max(0, window.Height - Control.Font.Height - top);
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
				if (Control is UpDownBase)
				{
					// The buttons stand a pixel in from the frame, as Default.xaml's do.
					var gap = line + Pixels(1);
					return new Padding(line + Pixels(5), gap, gap, gap);
				}
				var side = line + Pixels(5);
				if (Control is TextBox box && box.Multiline)
					return new Padding(side, line + Pixels(4), side, line + Pixels(4));
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
						PaintFrame();
						return;
					case NativeMethods.WM_PAINT:
						base.WndProc(ref m);
						PaintDisabledText();
						return;
					case NativeMethods.WM_WINDOWPOSCHANGING:
						// Windows Forms gives a number box the height of its font and edge whatever it is asked for; the window
						// takes Default.xaml's.
						if (Control is UpDownBase)
						{
							var pos = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.WINDOWPOS));
							if ((pos.flags & NativeMethods.SWP_NOSIZE) == 0)
							{
								pos.cy = ControlHeight(Control);
								Marshal.StructureToPtr(pos, m.LParam, false);
							}
						}
						break;
				}
				base.WndProc(ref m);
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

			/// <summary>The frame's line: Default.xaml's, in the hover colour under the mouse or with the focus; none round a box that cannot be changed.</summary>
			Color Edge(Rectangle screen)
			{
				if (Control is ListBox)
					return Themed("BorderDark", SystemColors.ControlDark);
				if (!IsEditable(Control))
					return Color.Empty;
				var hot = Control.ContainsFocus || screen.Contains(Control.MousePosition);
				return Themed(hot ? "BackgroundDarkSelected" : "BackgroundDark", SystemColors.ControlDark);
			}

			/// <summary>
			/// Draws the text of a disabled line of text again in the text colour, as Default.xaml leaves it: Windows draws it grey.
			/// A disabled box cannot be scrolled or have text chosen, so the text stands where Windows starts it.
			/// </summary>
			void PaintDisabledText()
			{
				var box = Control as TextBox;
				if (box == null || box.Enabled || box.Multiline || box.Text.Length == 0)
					return;
				var margins = (int)NativeMethods.SendMessage(Handle, NativeMethods.EM_GETMARGINS, IntPtr.Zero, IntPtr.Zero);
				var client = box.ClientRectangle;
				var text = Rectangle.FromLTRB(client.Left + (margins & 0xFFFF), client.Top, client.Right - ((margins >> 16) & 0xFFFF), client.Bottom);
				var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
				var align = box.RightToLeft == RightToLeft.Yes
					? (box.TextAlign == HorizontalAlignment.Left ? HorizontalAlignment.Right : box.TextAlign == HorizontalAlignment.Right ? HorizontalAlignment.Left : box.TextAlign)
					: box.TextAlign;
				flags |= align == HorizontalAlignment.Center ? TextFormatFlags.HorizontalCenter : align == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left;
				var shown = box.PasswordChar != '\0' || box.UseSystemPasswordChar
					? new string(box.UseSystemPasswordChar ? '●' : box.PasswordChar, box.Text.Length)
					: box.Text;
				using (var g = Graphics.FromHwnd(Handle))
				{
					using (var brush = new SolidBrush(box.BackColor))
						g.FillRectangle(brush, client);
					TextRenderer.DrawText(g, shown, box.Font, text, box.ForeColor, box.BackColor, flags);
				}
			}

			void PaintFrame()
			{
				var hdc = NativeMethods.GetWindowDC(Handle);
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
						var saved = Smooth(g);
						var line = Line(g);
						var radius = Dip(g, CornerRadius);
						using (var face = RoundedRectangle(bounds, radius, radius, radius, radius))
						using (var brush = new SolidBrush(FrameFace(Control)))
							g.FillPath(brush, face);
						var color = Edge(Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom));
						if (!color.IsEmpty)
							using (var pen = new Pen(color, line))
							using (var path = RoundedRectangle(Inset(bounds, line / 2), radius, radius, radius, radius))
								g.DrawPath(pen, path);
						g.Restore(saved);
					}
				}
				finally
				{
					NativeMethods.ReleaseDC(Handle, hdc);
				}
			}
		}

		/// <summary>
		/// A list's item as Default.xaml draws one: on the control colour, or the selection colour while chosen, its text 5 in
		/// from the side; at half strength while the list is disabled.
		/// </summary>
		static void List_DrawItem(object sender, DrawItemEventArgs e)
		{
			var list = (ListBox)sender;
			if (e.Index < 0 || e.Index >= list.Items.Count)
				return;
			var selected = (e.State & DrawItemState.Selected) != 0;
			var back = selected ? Themed("SelectedBackground", SystemColors.Highlight) : Themed("BackgroundDark", SystemColors.Control);
			var fore = selected ? Themed("SelectedForeground", SystemColors.HighlightText) : list.ForeColor;
			if (!list.Enabled)
				fore = Half(fore, back);
			using (var brush = new SolidBrush(back))
				e.Graphics.FillRectangle(brush, e.Bounds);
			var padding = Pixels(ComboBoxPadding);
			var text = Rectangle.FromLTRB(e.Bounds.Left + padding, e.Bounds.Top, e.Bounds.Right - padding, e.Bounds.Bottom);
			TextRenderer.DrawText(e.Graphics, list.GetItemText(list.Items[e.Index]), list.Font, text, fore,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
				| TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
			if ((e.State & DrawItemState.Focus) != 0 && (e.State & DrawItemState.NoFocusRect) == 0)
				ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, fore, back);
		}

		/// <summary>The rounding of a number box button's outer corner in its Default.xaml template, in device-independent pixels.</summary>
		const int SpinButtonRadius = 4;

		/// <summary>The width of a number box's buttons in its Default.xaml template, in device-independent pixels.</summary>
		const int SpinButtonWidth = 25;

		/// <summary>The height the program asked a list for, before Windows Forms cut it to whole items, a field of its own.</summary>
		static readonly FieldInfo RequestedHeightField = typeof(ListBox).GetField("requestedHeight", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(ListBox).GetField("_requestedHeight", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>The width Windows Forms lays a number box's buttons out with, a field of its own.</summary>
		static readonly FieldInfo ButtonsWidthField = typeof(UpDownBase).GetField("defaultButtonsWidth", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(UpDownBase).GetField("_defaultButtonsWidth", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// The buttons of a number box as the WPF programs draw them: two filled halves rounded on the outside, lighter under the
		/// mouse and lighter again pressed, each with a triangle.
		/// </summary>
		static void UpDownButtons_Paint(object sender, PaintEventArgs e)
		{
			var buttons = (Control)sender;
			var g = e.Graphics;
			var client = buttons.ClientRectangle;
			var upDown = buttons.Parent;
			using (var brush = new SolidBrush(upDown != null && IsEditable(upDown) ? Themed("BackgroundWhite", SystemColors.Window) : Themed("BackgroundDark", SystemColors.Control)))
				g.FillRectangle(brush, client);
			var half = client.Height / 2;
			var mouse = buttons.PointToClient(Control.MousePosition);
			var pressed = buttons.Capture && (Control.MouseButtons & MouseButtons.Left) != 0;
			var saved = Smooth(g);
			var radius = Dip(g, SpinButtonRadius);
			var up = new Rectangle(0, 0, client.Width, half);
			var down = new Rectangle(0, half, client.Width, client.Height - half);
			DrawSpinButton(g, buttons, up, radius, true, up.Contains(mouse), pressed);
			DrawSpinButton(g, buttons, down, radius, false, down.Contains(mouse), pressed);
			g.Restore(saved);
		}

		static void DrawSpinButton(Graphics g, Control buttons, Rectangle bounds, float radius, bool up, bool hot, bool pressed)
		{
			var alpha = buttons.Enabled ? 255 : 128;
			var key = !buttons.Enabled ? "BackgroundDark" : hot && pressed ? "BackgroundDarkPressed" : hot ? "BackgroundDarkSelected" : "BackgroundDark";
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed(key, SystemColors.ControlLight))))
			using (var face = RoundedRectangle(bounds, 0, up ? radius : 0, up ? 0 : radius, 0))
				g.FillPath(brush, face);
			// Default.xaml's triangle, made as large as the button's padding allows: 4 and 3 in from the sides, 3 from top and bottom.
			var scale = Dip(g, 1);
			var content = RectangleF.FromLTRB(bounds.Left + 4 * scale, bounds.Top + 3 * scale, bounds.Right - 3 * scale, bounds.Bottom - 3 * scale);
			var width = Math.Min(content.Width, content.Height * 2);
			var height = width / 2;
			var x = content.X + (content.Width - width) / 2;
			var y = content.Y + (content.Height - height) / 2;
			var points = up
				? new[] { new PointF(x, y + height), new PointF(x + width, y + height), new PointF(x + width / 2, y) }
				: new[] { new PointF(x, y), new PointF(x + width, y), new PointF(x + width / 2, y + height) };
			using (var brush = new SolidBrush(Color.FromArgb(alpha, Themed("ForegroundBlack", SystemColors.ControlText))))
				g.FillPolygon(brush, points);
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
			public const int CB_SETITEMHEIGHT = 0x0153;
			public const int CB_GETITEMHEIGHT = 0x0154;
			public const int WM_NCCALCSIZE = 0x0083;
			public const int WM_NCPAINT = 0x0085;
			public const int WM_PAINT = 0x000F;
			public const int EM_GETMARGINS = 0x00D4;
			public const int WM_WINDOWPOSCHANGING = 0x0046;
			public const int SWP_NOSIZE = 0x0001;
			public const int SWP_NOMOVE = 0x0002;
			public const int SWP_NOZORDER = 0x0004;
			public const int SWP_NOACTIVATE = 0x0010;
			public const int SWP_FRAMECHANGED = 0x0020;
			public const int RDW_INVALIDATE = 0x0001;
			public const int RDW_ERASE = 0x0004;
			public const int RDW_ALLCHILDREN = 0x0080;
			public const int RDW_FRAME = 0x0400;

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

			[DllImport("user32.dll")]
			public static extern IntPtr GetWindowDC(IntPtr hWnd);

			[DllImport("user32.dll")]
			public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

			[DllImport("user32.dll")]
			public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

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
