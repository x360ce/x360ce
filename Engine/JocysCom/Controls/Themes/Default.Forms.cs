#nullable disable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
	/// The light theme is the Windows colours.
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

		/// <summary>Works out light or dark, changes the system colours to it and brings the open windows along.</summary>
		/// <param name="themeChanged">True when the choice changed, so the open windows are themed again even if dark stayed dark.</param>
		static void UpdateColors(bool themeChanged)
		{
			var dark = Theme == ThemeType.Dark || (Theme == ThemeType.Auto && !WindowsAppsUseLightTheme());
			dark &= !SystemInformation.HighContrast && Palette.Count > 0;
			if (!SetSystemColors(dark))
				dark = false;
			var changed = dark != IsDark;
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
			public Color BackColor;
			public bool BackColorSet;
			public Color ForeColor;
			public bool ForeColorSet;
			public FlatStyle FlatStyle;
			public int FlatBorderSize;
			public Color FlatMouseOverBackColor;
			public Color FlatMouseDownBackColor;
			public bool UseVisualStyleBackColor;
			public bool EnableHeadersVisualStyles;
			public Color LinkColor;
			public Color VisitedLinkColor;
			public ToolStripRenderMode RenderMode;
			public ControlStyles? PaintStyles;
			public List<KeyValuePair<DataGridViewButtonColumn, FlatStyle>> ButtonColumns;
			public bool NativeDark;
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
					// A window made again has no window style yet.
					state.NativeDark = false;
					ApplyNative((Control)sender);
				};
			}
			if (state.Dark == IsDark)
				return;
			// A control the dark theme never changed is left exactly as it is.
			if (state.Dark == null && !IsDark)
			{
				state.Dark = false;
				return;
			}
			state.Dark = IsDark;
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
			return state;
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
			// The visual style draws a button light, so in the dark theme Windows Forms draws it.
			var button = control as ButtonBase;
			if (button != null)
			{
				var push = button is Button
					|| (button is CheckBox check && check.Appearance == Appearance.Button)
					|| (button is RadioButton radio && radio.Appearance == Appearance.Button);
				button.Paint -= Button_Paint;
				if (IsDark)
				{
					// Windows draws the text of a system button or check box black.
					if (state.FlatStyle == FlatStyle.System)
						button.FlatStyle = FlatStyle.Standard;
					button.UseVisualStyleBackColor = false;
					// A standard button draws its edges from the Windows colours, so a push button is made
					// flat. Its border is drawn here: a flat border takes room from the text and wraps
					// text that fits the standard button.
					if (push && state.FlatStyle != FlatStyle.Flat && state.FlatStyle != FlatStyle.Popup)
					{
						button.FlatStyle = FlatStyle.Flat;
						button.FlatAppearance.BorderSize = 0;
						button.FlatAppearance.MouseOverBackColor = GetColor("MouseOverBackground", SystemColors.ControlLight);
						button.FlatAppearance.MouseDownBackColor = GetColor("BackgroundDarkPressed", SystemColors.ControlDark);
						button.Paint += Button_Paint;
					}
				}
				else
				{
					button.FlatStyle = state.FlatStyle;
					button.FlatAppearance.BorderSize = state.FlatBorderSize;
					button.FlatAppearance.MouseOverBackColor = state.FlatMouseOverBackColor;
					button.FlatAppearance.MouseDownBackColor = state.FlatMouseDownBackColor;
					button.UseVisualStyleBackColor = state.UseVisualStyleBackColor;
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
			if (control is TabPage page)
				page.UseVisualStyleBackColor = !IsDark && state.UseVisualStyleBackColor;
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
			// The system renderer draws a checked button with the Windows colours, so in the dark theme
			// every strip without a renderer of its own is drawn by a professional one, made anew because
			// it keeps the colours it worked out.
			if (control is ToolStrip strip && state.RenderMode != ToolStripRenderMode.Custom)
			{
				if (IsDark)
					strip.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
				else
					strip.RenderMode = state.RenderMode;
			}
			if (control is TabControl tabs && tabs.Alignment == TabAlignment.Top)
				SetPaintedHere(tabs, state, IsDark, Tabs_Paint);
			if (control is TrackBar)
				SetPaintedHere(control, state, IsDark, Track_Paint);
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
			// Windows Forms gives no font to the window of a control painted here, and a tab strip
			// without one measures its tabs in the larger system font.
			if (control is TabControl && IsDark)
				SetWindowFontMethod?.Invoke(control, null);
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

		/// <summary>The border of a push button in the dark theme, in the accent colour while it has the focus.</summary>
		static void Button_Paint(object sender, PaintEventArgs e)
		{
			var button = (ButtonBase)sender;
			var color = button.Focused
				? GetColor("SelectedBackground", SystemColors.Highlight)
				: GetColor("BorderDark", SystemColors.ControlDark);
			using (var pen = new Pen(color))
				e.Graphics.DrawRectangle(pen, 0, 0, button.Width - 1, button.Height - 1);
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
		/// The tab strip and the slider have no dark Windows style, so in the dark theme they are painted here,
		/// double buffered.
		/// </summary>
		/// <remarks>The light theme puts back the styles the control had, flag by flag.</remarks>
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

		static void Tabs_Paint(object sender, PaintEventArgs e)
		{
			var tabs = (TabControl)sender;
			var g = e.Graphics;
			var back = tabs.Parent?.BackColor ?? SystemColors.Control;
			var border = GetColor("BorderDark", SystemColors.ControlDark);
			using (var brush = new SolidBrush(back))
				g.FillRectangle(brush, tabs.ClientRectangle);
			if (tabs.TabCount == 0)
				return;
			// The page area, framed, joined by the open tab.
			var strip = tabs.GetTabRect(0);
			var pane = new Rectangle(0, strip.Bottom, tabs.ClientSize.Width - 1, tabs.ClientSize.Height - strip.Bottom - 1);
			using (var pen = new Pen(border))
				g.DrawRectangle(pen, pane);
			for (var i = 0; i < tabs.TabCount; i++)
			{
				var selected = i == tabs.SelectedIndex;
				var bounds = tabs.GetTabRect(i);
				if (selected)
					bounds = new Rectangle(bounds.X - 2, bounds.Y - 2, bounds.Width + 4, bounds.Height + 3);
				var fill = GetColor(selected ? "BackgroundTabSelected" : "BackgroundDark", SystemColors.Control);
				var text = GetColor(selected ? "ForegroundTabSelected" : "ForegroundTab", SystemColors.ControlText);
				using (var brush = new SolidBrush(fill))
					g.FillRectangle(brush, bounds);
				using (var pen = new Pen(border))
				{
					g.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Left, bounds.Top);
					g.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right - 1, bounds.Top);
					g.DrawLine(pen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom - 1);
				}
				var page = tabs.TabPages[i];
				var content = bounds;
				var index = GetTabImageIndex(tabs, page);
				if (index >= 0)
				{
					// Drawn by the list itself: taking an image out of it copies the image on every paint, and
					// fails for a list read from a resource stream.
					var size = tabs.ImageList.ImageSize;
					tabs.ImageList.Draw(g, content.X + 6, content.Y + (content.Height - size.Height) / 2, index);
					content = new Rectangle(content.X + size.Width + 6, content.Y, content.Width - size.Width - 6, content.Height);
				}
				TextRenderer.DrawText(g, page.Text, tabs.Font, content, text,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			}
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
