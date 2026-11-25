using JocysCom.ClassLibrary.Controls.Themes;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class PadControlImager : IDisposable
	{

		public PadControlImager()
		{
			locations.Add(GamepadButtonFlags.Y, new Point(196, 29));
			// Create images.
			markB = new Bitmap(EngineHelper.GetResourceStream("Images.MarkButton.png"));
			markA = new Bitmap(EngineHelper.GetResourceStream("Images.MarkAxis.png"));
			markC = new Bitmap(EngineHelper.GetResourceStream("Images.MarkController.png"));
			// Only the resolution of the controller picture is wanted here. The picture itself
			// belongs to the control that draws it, so this copy is released straight away.
			float rH, rV;
			using (var topImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png")))
			{
				rH = topImage.HorizontalResolution;
				rV = topImage.VerticalResolution;
			}
			// Make sure resolution is same everywhere so images won't be resized.
			markB.SetResolution(rH, rV);
			markA.SetResolution(rH, rV);
			markC.SetResolution(rH, rV);
			Recorder = new Recorder();
		}

		public Recorder Recorder;

		// Green round button image.
		public Bitmap markB;
		// Green cross axis image.
		public Bitmap markA;
		// Green round controller/player number image.
		public Bitmap markC;

		public XboxImageUserControl ImageControl;

		Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();

		public void SetImages(bool enabled)
		{
			if (ImageControl != null)
				ImageControl.SetEnabled(enabled);
		}

		public void DrawController(PaintEventArgs e, MapTo mappedTo)
		{
			// Controller (Player) index indicator coordinates.
			var pads = new Point[4];
			pads[0] = new Point(116, 35);
			pads[1] = new Point(139, 35);
			pads[2] = new Point(116, 62);
			pads[3] = new Point(139, 62);
			// Display controller index light.
			int mW = -markC.Width / 2;
			int mH = -markC.Height / 2;
			var index = (int)mappedTo - 1;
			e.Graphics.DrawImage(markC, pads[index].X + mW, pads[index].Y + mH);
		}

		public bool ShowRightThumbButtons;
		public bool ShowLeftThumbButtons;
		public bool ShowDPadButtons;
		public bool ShowMainButtons;
		public bool ShowMenuButtons;
		public bool ShowTriggerButtons;
		public bool ShowShoulderButtons;

		#region Lights

		/// <summary>The mappings this picture and its boxes show: each with its place on the picture and its box.</summary>
		public ImageInfos Infos;

		/// <summary>Shows every mark on the picture, to check where they stand.</summary>
		public bool ShowAllMarks;

		/// <summary>A chip of the Source Input panel is being dragged: every box it may land in is lit green, as a moving one is.</summary>
		public bool Dropping;

		/// <summary>The mapping whose box the pointer is on, or null.</summary>
		public MapCode? PointedBox;

		/// <summary>The mark the pointer is on, as the code it is drawn for (<see cref="NavImages.GetNameCode"/>), or null.</summary>
		public MapCode? PointedMark;

		/// <summary>Whether the control a mapping's box names is lit in the Source Input panel: moving, or pressed.</summary>
		public Func<ImageInfo, bool> IsMoving;

		/// <summary>
		/// The one rule for how a mapping is lit, the same on its box and on its mark on the picture: recording outranks the
		/// pointer, the pointer outranks movement, and movement outranks a mark shown only because the pointer is near it.
		/// </summary>
		/// <param name="recording">The mapping is being recorded.</param>
		/// <param name="pointed">The pointer is on its box or on its mark.</param>
		/// <param name="moving">The control its box names is moving or pressed.</param>
		/// <param name="shown">Its mark is shown, the pointer being near it on the picture.</param>
		/// <returns>The state that wins, or null when the mapping is not lit.</returns>
		public static NavImageType? Light(bool recording, bool pointed, bool moving, bool shown)
		{
			if (recording)
				return NavImageType.Record;
			if (pointed)
				return NavImageType.Over;
			if (moving)
				return NavImageType.Active;
			if (shown)
				return NavImageType.Normal;
			return null;
		}

		/// <summary>The theme's colour of a state, which its mark and its box are drawn in.</summary>
		/// <remarks>
		/// Default.xaml's colours in the light theme and Default_DarkTheme.xaml's in the dark. The class library's copy of the
		/// light colours holds no ColorOver or ColorRecord, so Default.xaml's are given here.
		/// </remarks>
		public static Color LightColor(NavImageType type)
		{
			switch (type)
			{
				case NavImageType.Record:
					return FormsTheme.GetColor("ColorRecord", LightRecord);
				case NavImageType.Over:
					return FormsTheme.GetColor("ColorOver", LightOver);
				case NavImageType.Active:
					return FormsTheme.GetColor("ColorActive");
				default:
					return FormsTheme.GetColor("ColorNormal");
			}
		}

		/// <summary>Default.xaml's ColorRecord and ColorOver.</summary>
		static readonly Color LightRecord = Color.FromArgb(0xFF, 0x6B, 0x66);
		static readonly Color LightOver = Color.FromArgb(0xFF, 0xCC, 0x66);

		/// <summary>The colour a box is lit in: its state's, but none for a mark only shown.</summary>
		public static Color BoxColor(NavImageType? light)
		{
			return light.HasValue && light.Value != NavImageType.Normal ? LightColor(light.Value) : Color.Empty;
		}

		/// <summary>The state each mark is drawn in, by the code it is drawn for: kept to be filled again on each update.</summary>
		readonly Dictionary<MapCode, NavImageType?> _Marks = new Dictionary<MapCode, NavImageType?>();

		/// <summary>
		/// Lights every box and every mark from the state now, each by <see cref="Light"/>: what is recorded, where the pointer
		/// is, and what moves in the Source Input panel.
		/// </summary>
		/// <remarks>
		/// On the interface thread, from what the interface already holds: the recorder, the pointer and the panel's chips,
		/// which read the state the engine last published. The engine is not asked anything. Two mappings that share a mark,
		/// such as a stick's X axis and its right, light it in the stronger of their two states.
		/// </remarks>
		public void UpdateLights()
		{
			if (Infos == null || ImageControl == null)
				return;
			var recorded = Recorder.Recording ? Recorder.CurrentMap : null;
			_Marks.Clear();
			foreach (var ii in Infos)
			{
				var mark = NavImages.GetNameCode(ii.Code);
				var light = Light(
					recorded != null && recorded.Code == ii.Code,
					PointedBox == ii.Code || PointedMark == mark,
					IsMoving != null && IsMoving(ii),
					ShowAllMarks || InShownGroup(ii.Code));
				var box = BoxColor(light);
				SetBoxColor(ii.Control as ComboBox, box.IsEmpty && Dropping ? LightColor(NavImageType.Active) : box);
				NavImageType? other;
				if (!_Marks.TryGetValue(mark, out other) || light.HasValue && (!other.HasValue || light.Value > other.Value))
					_Marks[mark] = light;
			}
			foreach (var pair in _Marks)
			{
				var light = pair.Value;
				// A mark being recorded blinks.
				var show = light.HasValue && (light.Value != NavImageType.Record || Recorder.DrawRecordingImage);
				ImageControl.SetImage(pair.Key, light ?? NavImageType.Normal, show);
			}
		}

		/// <summary>Whether a mapping's mark is shown because the pointer is in its group on the picture.</summary>
		bool InShownGroup(MapCode code)
		{
			return ShowLeftThumbButtons && SettingsConverter.LeftThumbCodes.Contains(code) ||
				ShowRightThumbButtons && SettingsConverter.RightThumbCodes.Contains(code) ||
				ShowDPadButtons && SettingsConverter.DPadCodes.Contains(code) ||
				ShowMainButtons && SettingsConverter.MainButtonCodes.Contains(code) ||
				ShowMenuButtons && SettingsConverter.MenuButtonCodes.Contains(code) ||
				ShowTriggerButtons && SettingsConverter.TriggerButtonCodes.Contains(code) ||
				ShowShoulderButtons && SettingsConverter.ShoulderButtonCodes.Contains(code);
		}

		readonly Dictionary<ComboBox, BoxPainter> _Boxes = new Dictionary<ComboBox, BoxPainter>();

		/// <summary>Lights a mapping box in a colour, or puts it back to the theme's look with an empty one.</summary>
		void SetBoxColor(ComboBox box, Color color)
		{
			if (box == null)
				return;
			BoxPainter painter;
			if (!_Boxes.TryGetValue(box, out painter))
			{
				if (color.IsEmpty)
					return;
				painter = new BoxPainter(box);
				_Boxes.Add(box, painter);
			}
			painter.Face = color;
		}

		/// <summary>The colour a box is lit in now, or empty; what the box shows.</summary>
		public Color GetBoxColor(ComboBox box)
		{
			BoxPainter painter;
			return box != null && _Boxes.TryGetValue(box, out painter) ? painter.Face : Color.Empty;
		}

		/// <summary>
		/// Paints a mapping box in the colour it is lit in, over the theme's drawing of it.
		/// </summary>
		/// <remarks>
		/// The theme fills a drop-down list's face in its own colour and leaves the box's back colour unused, and a handler of
		/// the box's Paint event runs before or after the theme's depending on which was added last. So the lit face is drawn
		/// here, after the box has drawn itself: on the screen as the box paints, and on a bitmap as it is printed. It is drawn
		/// as the theme draws the face, its text and its triangle, in the light theme's text colour on every lit colour, as the
		/// lit colours are as light in both themes. A box holding a formula is a text field and is left to the theme; so is
		/// every box in high contrast, which keeps the Windows colours.
		/// </remarks>
		sealed class BoxPainter : NativeWindow
		{
			const int WM_PAINT = 0x000F;
			const int WM_PRINTCLIENT = 0x0318;

			/// <summary>Default.xaml's rounding of a field's corners, the room inside its edge, and the column and size of its triangle, in device-independent pixels.</summary>
			const float CornerRadius = 4;
			const float Room = 4;
			const float ArrowColumn = 16;
			const float ArrowWidth = 16;
			const float ArrowHeight = 8;

			static readonly System.Reflection.PropertyInfo ShowFocusCuesProperty = typeof(Control).GetProperty("ShowFocusCues",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

			readonly ComboBox Box;
			Color _Face;

			public BoxPainter(ComboBox box)
			{
				Box = box;
				// Windows makes a box's window again when its style changes, as when it is switched to a formula and back.
				box.HandleCreated += (sender, e) => AssignHandle(Box.Handle);
				box.HandleDestroyed += (sender, e) => ReleaseHandle();
				if (box.IsHandleCreated)
					AssignHandle(box.Handle);
			}

			/// <summary>The colour the box is lit in, or empty for the theme's look.</summary>
			public Color Face
			{
				get { return _Face; }
				set
				{
					if (_Face.ToArgb() == value.ToArgb() && _Face.IsEmpty == value.IsEmpty)
						return;
					_Face = value;
					Box.Invalidate();
				}
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);
				if (_Face.IsEmpty || Box.DropDownStyle != ComboBoxStyle.DropDownList || SystemInformation.HighContrast)
					return;
				if (m.Msg == WM_PRINTCLIENT || m.Msg == WM_PAINT && m.WParam != IntPtr.Zero)
					using (var g = Graphics.FromHdc(m.WParam))
						Draw(g);
				else if (m.Msg == WM_PAINT)
					using (var g = Box.CreateGraphics())
						Draw(g);
			}

			void Draw(Graphics g)
			{
				var client = Box.ClientRectangle;
				var dip = g.DpiX / 96f;
				var alpha = Box.Enabled ? 255 : 128;
				using (var brush = new SolidBrush(Box.Parent == null ? SystemColors.Control : Box.Parent.BackColor))
					g.FillRectangle(brush, client);
				var text = Color.FromArgb(alpha, FormsTheme.LightPalette["ForegroundBlack"]);
				var saved = g.Save();
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				var radius = (int)Math.Round(CornerRadius * dip);
				using (var brush = new SolidBrush(Color.FromArgb(alpha, _Face)))
				using (var face = AppHelper.RoundedRectangle(client, radius))
					g.FillPath(brush, face);
				var line = Math.Max(1f, (float)Math.Floor(dip));
				var side = (int)line + (int)Math.Round(Room * dip);
				// The triangle points down, its top left corner on a whole pixel so its edges are sharp.
				var left = (float)Math.Round(client.Width - side - ArrowColumn * dip / 2 - ArrowWidth * dip / 2, MidpointRounding.AwayFromZero);
				var top = (float)Math.Round(client.Height / 2f - ArrowHeight * dip / 2, MidpointRounding.AwayFromZero);
				using (var brush = new SolidBrush(text))
					g.FillPolygon(brush, new[]
					{
						new PointF(left, top), new PointF(left + ArrowWidth * dip, top), new PointF(left + ArrowWidth * dip / 2, top + ArrowHeight * dip),
					});
				if (Box.Focused && (bool)(ShowFocusCuesProperty?.GetValue(Box, null) ?? false))
					using (var pen = new Pen(text, line) { DashPattern = new[] { 1f, 2f } })
					using (var focus = AppHelper.RoundedRectangle(new Rectangle(0, 0, client.Width - 1, client.Height - 1), radius))
						g.DrawPath(pen, focus);
				g.Restore(saved);
				var room = Rectangle.FromLTRB(side, 0, client.Width - side - (int)Math.Round((Room + ArrowColumn) * dip), client.Height);
				TextRenderer.DrawText(g, Box.Text, Box.Font, room, text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter
					| TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
			}
		}

		#endregion

		/// <summary>Shows what the controller is given for one part: a trigger's bar, a stick's dot, and its name lit while it is used.</summary>
		public void DrawState(ImageInfo ii, Gamepad gp)
		{
			bool on;
			// Show trigger axis state -green minus image.
			if (ii.Code == MapCode.LeftTrigger || ii.Code == MapCode.RightTrigger)
			{
				var isLeft = ii.Code == MapCode.LeftTrigger;
				var y = isLeft ? gp.LeftTrigger : gp.RightTrigger;
				on = y > 0;
				ImageControl.SetTriggerLevel(isLeft, y / (float)byte.MaxValue);
			}
			// Draw thumb axis state - green cross image.
			if (ii.Code == MapCode.LeftThumbButton || ii.Code == MapCode.RightThumbButton)
			{
				var isLeft = ii.Code == MapCode.LeftThumbButton;
				var x = isLeft ? gp.LeftThumbX : gp.RightThumbX;
				var y = isLeft ? gp.LeftThumbY : gp.RightThumbY;
				ImageControl.SetThumbPosition(isLeft, x / (float)short.MaxValue, y / (float)short.MaxValue);
			}
			// If D-Pad.
			if (ii.Code == MapCode.DPad)
			{
				on =
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadUp) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadRight) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadDown);
			}
			// If button is not specified then...
			else if (ii.Button == GamepadButtonFlags.None)
			{
				// The triggers come first, because they are neither buttons nor thumb axes and fell through
				// to the thumb reading below - which looks at four axes, none of them a trigger. The value
				// read was therefore always nothing, so a trigger held all the way down never lit its name.
				//
				// Pressed past the point XInput itself counts as pressed, so what the label says agrees with
				// what a game receives.
				if (ii.Code == MapCode.LeftTrigger || ii.Code == MapCode.RightTrigger)
				{
					const byte pressed = 30;
					var amount = ii.Code == MapCode.LeftTrigger ? gp.LeftTrigger : gp.RightTrigger;
					on = amount > pressed;
				}
				else
				{
				var t = 2000;
				// This is axis.
				short value = 0;
				if (ii.Code == MapCode.LeftThumbAxisX)
					value = gp.LeftThumbX;
				else if (ii.Code == MapCode.LeftThumbAxisY)
					value = gp.LeftThumbY;
				else if (ii.Code == MapCode.RightThumbAxisX)
					value = gp.RightThumbX;
				else if (ii.Code == MapCode.RightThumbAxisY)
					value = gp.RightThumbY;
				// Check when value is on.
				on = value < -t || value > t;
				if (ii.Code == MapCode.LeftThumbRight)
					on = gp.LeftThumbX > t;
				if (ii.Code == MapCode.LeftThumbLeft)
					on = gp.LeftThumbX < -t;
				if (ii.Code == MapCode.LeftThumbUp)
					on = gp.LeftThumbY > t;
				if (ii.Code == MapCode.LeftThumbDown)
					on = gp.LeftThumbY < -t;
				if (ii.Code == MapCode.RightThumbRight)
					on = gp.RightThumbX > t;
				if (ii.Code == MapCode.RightThumbLeft)
					on = gp.RightThumbX < -t;
				if (ii.Code == MapCode.RightThumbUp)
					on = gp.RightThumbY > t;
				if (ii.Code == MapCode.RightThumbDown)
					on = gp.RightThumbY < -t;
				}
			}
			else
			{
				// Check when value is on.
				on = gp.Buttons.HasFlag(ii.Button);
			}
			if (ii.Label != null)
				setLabelColor(on, ii.Label);
		}

		void setLabelColor(bool on, Control label)
		{
			var c = on ? JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetColor("ForegroundActive") : SystemColors.ControlText;
			if (label.ForeColor != c)
				label.ForeColor = c;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#region IDisposable

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				markA.Dispose();
				markB.Dispose();
				markC.Dispose();
				Recorder.Dispose();
			}
		}

		#endregion
	}

}
