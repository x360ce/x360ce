using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AxisMapControl.xaml
	/// </summary>
	public partial class AxisMapControl : UserControl
	{
		public AxisMapControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			InitPaintObjects();
			// Initialize in constructor and not on "Load" event or it will reset AntiDeadZone value
			// inside DeadZoneControlsLink(...).
			updateTimer = new System.Timers.Timer();
			updateTimer.AutoReset = false;
			updateTimer.Interval = 500;
		}

		void UpdateTimerReset()
		{
			updateTimer.Stop();
			updateTimer.Start();
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			CreateBackgroundPicture();
		}

		void deadzoneLink_ValueChanged(object sender, EventArgs e)
		{
			UpdateTimerReset();
		}

		TrackBarUpDownTextBoxLink deadzoneLink;
		TrackBarUpDownTextBoxLink antiDeadzoneLink;
		TrackBarUpDownTextBoxLink linearLink;

		System.Timers.Timer updateTimer;

		[Category("Appearance"), DefaultValue(0)]
		public string HeaderText
		{
			get => (string)MainGroupBox.Header;
			set => MainGroupBox.Header = value;
		}

		TargetType _TargetType;

		[Category("Appearance"), DefaultValue(TargetType.None)]
		public TargetType TargetType
		{
			get => _TargetType;
			set { _TargetType = value; UpdateTargetType(); }
		}

		void UpdateTargetType()
		{
			if (IsUnloading)
				return;
			var maxValue = isThumb ? short.MaxValue : byte.MaxValue;
			deadzoneLink = new TrackBarUpDownTextBoxLink(DeadZoneTrackBar, DeadZoneUpDown, DeadZoneTextBox, 0, maxValue);
			deadzoneLink.ValueChanged -= deadzoneLink_ValueChanged;
			deadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			antiDeadzoneLink = new TrackBarUpDownTextBoxLink(AntiDeadZoneTrackBar, AntiDeadZoneUpDown, AntiDeadZoneTextBox, 0, maxValue);
			antiDeadzoneLink.ValueChanged -= deadzoneLink_ValueChanged;
			antiDeadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			linearLink = new TrackBarUpDownTextBoxLink(LinearTrackBar, LinearUpDown, LinearTextBox, -100, 100);
			linearLink.ValueChanged -= deadzoneLink_ValueChanged;
			linearLink.ValueChanged += deadzoneLink_ValueChanged;
			UpdateTimerReset();
		}

		private bool isThumb =>
				TargetType == TargetType.LeftThumbX ||
				TargetType == TargetType.LeftThumbY ||
				TargetType == TargetType.RightThumbX ||
				TargetType == TargetType.RightThumbY;

		// Half and Invert values are only in creating XInput path - red line.
		private bool _invert;
		private bool _half;

		private float w = 150f;
		private float h = 150f;

		public void SetBinding(PadSetting ps)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(DeadZoneUpDown);
			SettingsManager.UnLoadMonitor(AntiDeadZoneUpDown);
			SettingsManager.UnLoadMonitor(LinearUpDown);
			if (ps == null || IsUnloading)
				return;
			// Set binding.
			var converter = new Converters.PadSettingToIntegerConverter();
			string deadZoneName = null;
			string antiDeadZoneName = null;
			string linearName = null;

			switch (TargetType)
			{
				case TargetType.LeftTrigger:
					deadZoneName = nameof(ps.LeftTriggerDeadZone);
					antiDeadZoneName = nameof(ps.LeftTriggerAntiDeadZone);
					linearName = nameof(ps.LeftTriggerLinear);
					break;
				case TargetType.RightTrigger:
					deadZoneName = nameof(ps.RightTriggerDeadZone);
					antiDeadZoneName = nameof(ps.RightTriggerAntiDeadZone);
					linearName = nameof(ps.RightTriggerLinear);
					break;
				case TargetType.LeftThumbX:
					deadZoneName = nameof(ps.LeftThumbDeadZoneX);
					antiDeadZoneName = nameof(ps.LeftThumbAntiDeadZoneX);
					linearName = nameof(ps.LeftThumbLinearX);
					break;
				case TargetType.LeftThumbY:
					deadZoneName = nameof(ps.LeftThumbDeadZoneY);
					antiDeadZoneName = nameof(ps.LeftThumbAntiDeadZoneY);
					linearName = nameof(ps.LeftThumbLinearY);
					break;
				case TargetType.RightThumbX:
					deadZoneName = nameof(ps.RightThumbDeadZoneX);
					antiDeadZoneName = nameof(ps.RightThumbAntiDeadZoneX);
					linearName = nameof(ps.RightThumbLinearX);
					break;
				case TargetType.RightThumbY:
					deadZoneName = nameof(ps.RightThumbDeadZoneY);
					antiDeadZoneName = nameof(ps.RightThumbAntiDeadZoneY);
					linearName = nameof(ps.RightThumbLinearY);
					break;
				default:
					break;
			}
			if (deadZoneName != null)
			{
				SettingsManager.LoadAndMonitor(ps, deadZoneName, DeadZoneUpDown, null, converter);
				SettingsManager.LoadAndMonitor(ps, antiDeadZoneName, AntiDeadZoneUpDown, null, converter);
				SettingsManager.LoadAndMonitor(ps, linearName, LinearUpDown, null, converter);
			}
		}

		public void DrawPoint(int dInput, int xInput, bool invert, bool half)
		{
			// If properties affecting curve changed then...
			if (_invert != invert || _half != half)
				UpdateTimerReset();
			// Set invert and half properties.
			_invert = invert;
			_half = half;
			// Draw.
			DInputValueLabel.Content = dInput.ToString();
			XInputValueLabel.Content = xInput.ToString();
			var di = ConvertDInputToImagePosition(dInput);
			var xi = ConvertXInputToImagePosition(xInput);
			// Draw vertical DirectInput line in the middle.
			// If line goes outside of the image then...
			if (di >= w)
				di = w - 1f;
			DInputLineGeometry.StartPoint = new Point(di, 0);
			DInputLineGeometry.EndPoint = new Point(di, h);
			if (xi >= h)
				xi = h - 1f;
			// If line goes outside of the image then...
			XInputLineGeometry.StartPoint = new Point(0, h - xi);
			XInputLineGeometry.EndPoint = new Point(w, h - xi);
			// Draw dots.
			XInputEllipse.Center = new Point(di, h - xi);
			DInputEllipse.Center = new Point(di, h - di);
		}

		public float ConvertDInputToImagePosition(float v)
		{
			var di = ConvertHelper.ConvertRangeF(0f, ushort.MaxValue, 0f, w, v);
			return di;
		}

		public float ConvertXInputToImagePosition(float v)
		{
			var min = isThumb ? -32768f : 0f;
			var max = isThumb ? 32767f : 255f;
			var xi = ConvertHelper.ConvertRangeF(min, max, 0f, h, v);
			return xi;
		}

		public void InitPaintObjects()
		{
			xInputPath = new SolidColorBrush(Colors.Red);
			// Create thin lines.
			var xInputLineBrush = new SolidColorBrush(Colors.Blue);
			xInputLineBrush.Opacity = 0.125f;
			xInputLine = new Pen(xInputLineBrush, 1f);
			var dInputLineBrush = new SolidColorBrush(Colors.Green);
			dInputLineBrush.Opacity = 0.125f;
			dInputLine = new Pen(dInputLineBrush, 1f);
			var nInputLineBrush = new SolidColorBrush(Colors.Gray);
			nInputLineBrush.Opacity = 0.125f;
			nInputLine = new Pen(nInputLineBrush, 1f);
		}

		private SolidColorBrush xInputPath;
		private Pen xInputLine;
		private Pen dInputLine;
		private Pen nInputLine;

		private void CreateBackgroundPicture()
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(new Action(() => CreateBackgroundPicture()));
				return;
			}
			var deadZone = DeadZoneUpDown.Value;
			var antiDeadZone = AntiDeadZoneUpDown.Value;
			var sensitivity = LinearUpDown.Value;
			var visual = new DrawingVisual();
			// Retrieve the DrawingContext in order to create new drawing content.
			var g = visual.RenderOpen();
			// Persist the drawing content.
			//g.DrawRectangle(Brushes.White, null, new Rect(new Point(0, 0), new Size(w, h)));
			// Draw vertical DInput line in the middle.
			//var dim = (float)Math.Floor(w / 2);
			//g.DrawLine(dInputLine, new Point(dim, 0), new Point(dim, h));
			// Draw vertical XInput line in the middle.
			//var xim = (float)Math.Floor(h / 2);
			//g.DrawLine(xInputLine, new Point(0, xim), new Point(w, xim));
			// Draw grey line from bottom-left to top-right.
			//g.DrawLine(nInputLine, new Point(0f, h - 1f), new Point(w - 1f, 0f));
			for (var i = 0f; i <= w; i += 0.125f)
			{
				// Convert Image X position [0;w] to DInput position [0;65535].
				var dInputValue = ConvertHelper.ConvertRangeF(0f, w, ushort.MinValue, ushort.MaxValue, i);
				var xInputValue = ConvertHelper.GetThumbValue(dInputValue, (float)deadZone, (float)antiDeadZone, (float)sensitivity, _invert, _half, isThumb);
				//var rounded = result >= -1f && result <= 1f;
				var di = ConvertDInputToImagePosition(dInputValue);
				var xi = ConvertXInputToImagePosition(xInputValue);
				// Put red dot where XInput dot must travel. Use radius to fix eclipse position.
				DrawDot(g, di, xi, xInputPath);
			}
			// Finish rendering.
			g.Close();
			var bmp = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);
			bmp.Render(visual);
			// Encoding the RenderBitmapTarget as a PNG file.
			var png = new PngBitmapEncoder();
			png.Frames.Add(BitmapFrame.Create(bmp));
			var ms = new MemoryStream();
			png.Save(ms);
			var imageSource = new BitmapImage();
			imageSource.BeginInit();
			imageSource.StreamSource = ms;
			imageSource.EndInit();
			if (IsEnabled)
				MainPictureBox.Source = imageSource;
			else
				MainPictureBox.Source = null;
		}

		void DrawDot(DrawingContext g, float x, float y, Brush brush, bool snap = false)
		{
			// Half pixel.
			var p = 0.5f;
			// If snap all.
			if (snap)
			{
				// Snap to pixels.
				x = (float)Math.Floor(x);
				// Make sure last line is not snapped outside.
				if (x == w)
					x -= 1f;
				x += p;
			}
			else
			{
				var wm = (w / 2f);
				var hm = (h / 2f);
				// Snap X to start, center and end.
				if (x < 1f)
					x = p;
				if (x >= wm - p && x <= wm + p)
					x = wm;
				if (x > w - 1f)
					x = w - p;
				// Snap Y to top, middle and bottom.
				if (y < 1f)
					y = p;
				if (y >= hm - p && y <= hm + p)
					y = hm;
				if (y > h - 1f)
					y = h - p;
			}
			g.DrawEllipse(brush, null, new Point(x, h - y), 0.5f, 0.5f);
		}

		private void P_X_Y_Z_MenuItem_Click(object sender, EventArgs e)
		{
			var c = (MenuItem)sender;
			var values = c.Name.Split('_');
			float xDeadZone = 0;
			switch (TargetType)
			{
				case TargetType.LeftTrigger:
				case TargetType.RightTrigger:
					xDeadZone = Controller.XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
					break;
				case TargetType.LeftThumbX:
				case TargetType.LeftThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE;
					break;
				case TargetType.RightThumbX:
				case TargetType.RightThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
					break;
				default:
					break;
			}
			var deadZonePercent = int.Parse(values[1]);
			var antiDeadZonePercent = int.Parse(values[2]);
			var linearPercent = int.Parse(values[3]);

			var deadZone = ConvertHelper.ConvertRangeF(0f, 100f, (float)DeadZoneUpDown.Minimum, (float)DeadZoneUpDown.Maximum, deadZonePercent);
			var antiDeadZone = ConvertHelper.ConvertRangeF(0f, 100f, 0, xDeadZone, antiDeadZonePercent);
			var linear = ConvertHelper.ConvertRangeF(-100f, 100f, (float)LinearUpDown.Minimum, (float)LinearUpDown.Maximum, linearPercent);
			// Move focus away from below controls, so that their value can be changed.
			ApplyPresetMenuItem.Focus();
			DeadZoneUpDown.Value = (int)deadZone;
			AntiDeadZoneUpDown.Value = (int)antiDeadZone;
			LinearUpDown.Value = (int)linear;
		}

		private void LinearUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var text = "Sensitivity";
			if ((int)(e.NewValue ?? 0) < 0)
				text = "Sensitivity - more sensitive in the center:";
			if ((int)(e.NewValue ?? 0) > 0)
				text = "Sensitivity - less sensitive in the center:";
			ControlsHelper.SetText(SensitivityLabel, text);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			updateTimer.Elapsed += UpdateTimer_Elapsed;
		}

		bool IsUnloading;

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			IsUnloading = true;
			if (updateTimer != null)
			{
				updateTimer.Elapsed -= UpdateTimer_Elapsed;
				updateTimer.Dispose();
			}
			SetBinding(null);
			deadzoneLink?.Dispose();
			deadzoneLink = null;
			antiDeadzoneLink?.Dispose();
			antiDeadzoneLink = null;
			linearLink?.Dispose();
			linearLink = null;
		}
	}
}

