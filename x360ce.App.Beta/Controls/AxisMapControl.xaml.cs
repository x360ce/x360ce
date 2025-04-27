using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AxisMapControl.xaml
	/// </summary>
	public partial class AxisMapControl : UserControl
	{
		PointCollection SensitivityPolylinePointCollection = new PointCollection();

		public AxisMapControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Initialize in constructor and not on "Load" event or it will reset AntiDeadZone value.
			// inside DeadZoneControlsLink(...).
			BackgroundPolyline.Points = SensitivityPolylinePointCollection;
		}

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

		private bool isThumb =>
			TargetType == TargetType.LeftThumbX ||
			TargetType == TargetType.LeftThumbY ||
			TargetType == TargetType.RightThumbX ||
			TargetType == TargetType.RightThumbY;

		TrackBarUpDownTextBoxLink deadzoneLink;
		TrackBarUpDownTextBoxLink antiDeadzoneLink;
		TrackBarUpDownTextBoxLink linearLink;

		void deadzoneLink_ValueChanged(object sender, EventArgs e)
		{
			CreateBackgroundPolyline();
		}

		void UpdateTargetType()
		{
			//if (!ControlsHelper.IsLoaded(this))
			//	return;
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
		}

		PadSetting _ps;


		public void SetBinding(PadSetting ps)
		{
			if (_ps != null) { _ps = ps; }
			// Unbind first.
			SettingsManager.UnLoadMonitor(DeadZoneUpDown);
			SettingsManager.UnLoadMonitor(AntiDeadZoneUpDown);
			SettingsManager.UnLoadMonitor(LinearUpDown);
			if (ps == null) // || ControlcsHelper.IsLoaded(this))
				return;
			// Set binding.
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
				var converter = new Converters.PadSettingToNumericConverter<decimal>();
				SettingsManager.LoadAndMonitor(ps, deadZoneName, DeadZoneUpDown, null, converter);
				SettingsManager.LoadAndMonitor(ps, antiDeadZoneName, AntiDeadZoneUpDown, null, converter);
				SettingsManager.LoadAndMonitor(ps, linearName, LinearUpDown, null, converter);
			}
		}

		private float dInputMax = 65535f;
		private float dInputPolylineStepSize = 65535f / 257; // = 255

		// When Tab is loaded and settings changed.
		private void CreateBackgroundPolyline()
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(new Action(() => CreateBackgroundPolyline()));
				return;
			}
			SensitivityPolylinePointCollection.Clear();
			// Create polyline points. Canvas (65535 x 65535).	
			for (var i = 0f; i <= dInputMax; i += dInputPolylineStepSize)
			{
				// var dInputValue = ConvertHelper.ConvertRangeF(i, 0f, dInputMax, ushort.MinValue, ushort.MaxValue);
				var xI = ConvertHelper.GetThumbValue(i, (float)DeadZoneUpDown.Value, (float)AntiDeadZoneUpDown.Value, (float)LinearUpDown.Value, _invert, _half, isThumb);
				SensitivityPolylinePointCollection.Add(new Point(i, ConvertXInputToCanvasPosition(xI)));
			}

			// Sensitivity tooltip.
			var sensitivity = (int)(LinearUpDown.Value ?? 0);
			SensitivityTooltip.Content =
				sensitivity < 0 ? "Center is more sensitive" :
				sensitivity > 0 ? "Center is less sensitive" :
				string.Empty;
		}

		// Half and Invert values are only in creating XInput path - red line.
		private bool _invert;
		private bool _half;

		// When XInput or DInput value changes.
		public void UpdateGraph(int dInput, int xInput, bool invert, bool half)
		{
			// If properties affecting curve changed then set invert and half properties.
			if (_invert != invert || _half != half)
			{
				_invert = invert;
				_half = half;
			}

			// Labels.
			DInputValueLabel.Content = dInput.ToString();
			XInputValueLabel.Content = xInput.ToString();

			// Canvas (65535 x 65535).
			// var dI = ConvertDInputToCanvasPosition(dInput);
			var xI = ConvertHelper.GetThumbValue(dInput, (float)DeadZoneUpDown.Value, (float)AntiDeadZoneUpDown.Value, (float)LinearUpDown.Value, _invert, _half, isThumb);
			xI = Math.Min(ConvertXInputToCanvasPosition(xI), dInputMax);
			// Current position dot.
			Canvas.SetLeft(DXInputPointEllipse, dInput);
			Canvas.SetTop(DXInputPointEllipse, xI);
			// DInput axis.
			DInputPolylineGeometry.StartPoint = new Point(dInput, 0);
			DInputPolylineGeometry.EndPoint = new Point(dInput, dInputMax);
			// XInput axis.
			XInputPolylineGeometry.StartPoint = new Point(0, xI);
			XInputPolylineGeometry.EndPoint = new Point(dInputMax, xI);
		}

		public float ConvertDInputToCanvasPosition(float v)
		{
			var dI = ConvertHelper.ConvertRangeF(v, 0f, ushort.MaxValue, 0f, dInputMax);
			return dI;
		}

		public float ConvertXInputToCanvasPosition(float v)
		{
			var min = isThumb ? -32768f : 0f;
			var max = isThumb ? 32767f : 255f;
			var xI = ConvertHelper.ConvertRangeF(v, min, max, 0f, dInputMax);
			return xI;
		}

		private /*async*/ void P_X_Y_Z_PresetMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// await Task.Yield(); // Placeholder to make the method awaitable.

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
				//default:
				//	break;
			}

			// P_0_0_0_MenuItem > ConvertRangeF(oldValue, oldMin, oldMax, newMin, newMax).
			var selectedItem = (ComboBoxItem)((ComboBox)sender).SelectedItem;
			var values = selectedItem.Tag.ToString().Split('_');
			DeadZoneUpDown.Value = (int)ConvertHelper.ConvertRangeF(0f, 100f, (float)DeadZoneUpDown.Minimum, (float)DeadZoneUpDown.Maximum, int.Parse(values[1]));
			AntiDeadZoneUpDown.Value = (int)((float)xDeadZone * int.Parse(values[2]) / 100f); //var antiDeadZone = ConvertHelper.ConvertRangeF(xDeadZone, antiDeadZonePercent, 0f, 100f, 0);
			LinearUpDown.Value = (int)ConvertHelper.ConvertRangeF(int.Parse(values[3]), -100f, 100f, (float)LinearUpDown.Minimum, (float)LinearUpDown.Maximum);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			CreateBackgroundPolyline();
			//SetBinding(_ps);
			//UpdateTargetType();
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
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

