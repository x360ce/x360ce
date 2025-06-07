using SharpDX.XInput;
using System;
//using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.Engine;
//using System.Drawing;
//using System.Linq;
//using System.Windows;
//using System.Windows.Media;
//using System.IO;
//using System.Security.Policy;
//using JocysCom.ClassLibrary.Controls;

namespace x360ce.App.Controls
{
	public partial class PadControlImager : IDisposable
	{
        //static Bitmap topImage;
        //static Bitmap frontImage;
        //static Bitmap topDisabledImage;
        //static Bitmap frontDisabledImage;

        //static ImageSource _TopImage;
        //static ImageSource _FrontImage;
        //static ImageSource _TopDisabledImage;
        //static ImageSource _FrontDisabledImage;

        // Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();
        public static object imagesLock = new object();

        public PadControlImager()
		{
			//locations.Add(GamepadButtonFlags.Y, new Point(196, 29));
			lock (imagesLock)
			{
				// var a = GetType().Assembly;
				// Create images.
				//if (topImage == null)
				//{
				//    var keys = JocysCom.ClassLibrary.Helper.GetResourceKeys(a);
				//    var ti = JocysCom.ClassLibrary.Helper.GetResourceValue("images/xboxcontrollertop.png", a);
				//    topImage = new Bitmap(ti);
				//    var ti2 = JocysCom.ClassLibrary.Helper.GetResourceValue("images/xboxcontrollerfront.png", a);
				//    frontImage = new Bitmap(ti2);
				//    topDisabledImage = AppHelper.GetDisabledImage(topImage);
				//    frontDisabledImage = AppHelper.GetDisabledImage(frontImage);
				//    //WPF.
				//   _TopImage = ControlsHelper.GetImageSource(topImage);
				//    _FrontImage = ControlsHelper.GetImageSource(frontImage);
				//    _TopDisabledImage = ControlsHelper.GetImageSource(topDisabledImage);
				//    _FrontDisabledImage = ControlsHelper.GetImageSource(frontDisabledImage);
				//}
				// Other.
				//markB = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkButton.png", a);
				//markA = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkAxis.png", a);
				//markC = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkController.png", a);
				//float rH = topDisabledImage.HorizontalResolution;
				//float rV = topDisabledImage.VerticalResolution;
				// Make sure resolution is same everywhere so images won'stickRDeadzone be resized.
				//markB.SetResolution(rH, rV);
				//markA.SetResolution(rH, rV);
				//markC.SetResolution(rH, rV);
				//Recorder = new Recorder(rH, rV);
				Recorder = new Recorder(0, 0);
			}
		}

		public Recorder Recorder;

        // Green round button image.
        // public Bitmap markB;
        // Green cross axis image.
        // public Bitmap markA;
        // Green round controller/player number image.
        // public Bitmap markC;

        public PadItem_General_XboxImageControl ImageControl;

        // Background Images.
        //public Image Top;
        //public Image Front;

        //      public void SetImages(bool enabled)
        //{
        //	////Top.Source = enabled ? _TopImage : _TopDisabledImage;
        //	////Front.Source = enabled ? _FrontImage : _FrontDisabledImage;
        //	////var show = enabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        //	////LeftThumbAxisStatus.Visibility = show;
        //	////RightThumbAxisStatus.Visibility = show;
        //	////LeftTriggerAxisStatus.Visibility = show;
        //	////RightTriggerAxisStatus.Visibility = show;
        //}

        //public void DrawController(PaintEventArgs e, MapTo mappedTo)
        //{
        //    // Controller (Player) index indicator coordinates.
        //    var pads = new Point[4];
        //    pads[0] = new Point(116, 35);
        //    pads[1] = new Point(139, 35);
        //    pads[2] = new Point(116, 62);
        //    pads[3] = new Point(139, 62);
        //    // Display controller index light.
        //    int mW = -markC.Width / 2;
        //    int mH = -markC.Height / 2;
        //    var index = (int)mappedTo - 1;
        //    e.Graphics.DrawImage(markC, pads[index].X + mW, pads[index].Y + mH);
        //}

        //public bool ShowRightThumbButtons;
        //public bool ShowLeftThumbButtons;
        //public bool ShowDPadButtons;
        //public bool ShowMainButtons;
        //public bool ShowMenuButtons;
        //public bool ShowTriggerButtons;
        //public bool ShowShoulderButtons;

        // Axis status Borders.
        public Border LeftTriggerAxisStatus;
        public Border RightTriggerAxisStatus;
        public Border LeftThumbAxisStatus;
		public Border RightThumbAxisStatus;

        // public System.Windows.Shapes.Path DPadUpStatus;

		// Set green background if axis or button exceeds deadzone value.
		bool on = false;

        private void UpdateXAMLTriggerElements(byte triggerValue, Label valueLabel, Border circleBorder)
		{
            // Get circle's [•] parent available heigh.
            var height = (float)(((System.Windows.FrameworkElement)circleBorder.Parent).Height - circleBorder.Height);
            // Convert trigger value to circle's [•] position.
            var position = ConvertHelper.ConvertRangeF(triggerValue, byte.MinValue, byte.MaxValue, 0, height);
            // Set circle [•] position.
            circleBorder.RenderTransform = new TranslateTransform { Y = -position };
            // Set XInput value below TextBox name.
            valueLabel.Content = triggerValue;
            // Check if trigger value exceeds deadzone value (will set green background color).
            on = triggerValue > Gamepad.TriggerThreshold;
        }

        private void UpdateXAMLStickElements(short axisValue, Label valueLabel, Border circleBorder, MapCode mapCode, short deadzone)
        {
            // Get circle's (•) parent available heigh.
            var height = (float)(((System.Windows.FrameworkElement)circleBorder.Parent).Height - circleBorder.Height);
            // Range depending on axis X or Y.
            bool isX = mapCode == MapCode.LeftThumbAxisX || mapCode == MapCode.RightThumbAxisX;
            float from = isX ? -height : height;
            float to = isX ? height : -height;
            // Convert stick value to circle's (•) position.
            var newMargin = ConvertHelper.ConvertRangeF(axisValue, short.MinValue, short.MaxValue, from, to);
            // Current circle margin.
            var margin = circleBorder.Margin;
            circleBorder.Margin = isX
                ? new System.Windows.Thickness(newMargin, margin.Top, margin.Right, margin.Bottom)
                : new System.Windows.Thickness(margin.Left, newMargin, margin.Right, margin.Bottom);
            // Set XInput value below TextBox name.
            valueLabel.Content = axisValue;
            // Check if stick value exceeds deadzone value (will set green background color).
            on = axisValue > deadzone || axisValue < -deadzone;
        }

        private void UpdateXAMLStickElementsDetailed(short axisValue, Label valueLabel, MapCode mapCode, short deadzone)
        {
			switch (mapCode) { 
				case MapCode.LeftThumbRight:
                case MapCode.LeftThumbUp:
                case MapCode.RightThumbRight:
                case MapCode.RightThumbUp:
                    on = axisValue > deadzone;
                    break;
                case MapCode.LeftThumbLeft:
                case MapCode.LeftThumbDown:
                case MapCode.RightThumbLeft:
                case MapCode.RightThumbDown:
                    on = axisValue < -deadzone;
                    break;
            }
            valueLabel.Content = on ? axisValue : 0;
        }

        //private Dictionary<MapCode, object> previousGpValues = new Dictionary<MapCode, object>();
        //private bool IsValueChanged<T>(MapCode code, T currentValue)
        //{
        //    if (previousGpValues.TryGetValue(code, out var prevValue) && EqualityComparer<T>.Default.Equals((T)prevValue, currentValue))
        //    {
        //        return false; // No change.
        //    }
        //    previousGpValues[code] = currentValue;
        //    return true; // Value has changed.
        //}

        public void DrawState(ImageInfo ii, Gamepad gp, CustomDiState ds)
		{
			short stickLDeadzone = Gamepad.LeftThumbDeadZone;
			short stickRDeadzone = Gamepad.RightThumbDeadZone;

            switch (ii.Code)
			{
                // Trigger axis state visual representation with yellow circle position [•].
                case MapCode.LeftTrigger:
                    //if (!IsValueChanged(MapCode.LeftTrigger, gp.LeftTrigger)) break;
                    UpdateXAMLTriggerElements(gp.LeftTrigger, (Label)ii.ControlStackPanel, LeftTriggerAxisStatus);
					break;
                case MapCode.RightTrigger:
                    //if (!IsValueChanged(MapCode.RightTrigger, gp.RightTrigger)) break;
                    UpdateXAMLTriggerElements(gp.RightTrigger, (Label)ii.ControlStackPanel, RightTriggerAxisStatus);
                    break;
                // Trigger axis state visual representation with yellow circle position (•).
                case MapCode.LeftThumbAxisX:
                    //if (!IsValueChanged(MapCode.LeftThumbAxisX, gp.LeftThumbX)) break;
                    UpdateXAMLStickElements(gp.LeftThumbX, (Label)ii.ControlStackPanel, LeftThumbAxisStatus, ii.Code, stickLDeadzone);
					break;
                case MapCode.LeftThumbAxisY:
                    //if (!IsValueChanged(MapCode.LeftThumbAxisY, gp.LeftThumbY)) break;
                    UpdateXAMLStickElements(gp.LeftThumbY, (Label)ii.ControlStackPanel, LeftThumbAxisStatus, ii.Code, stickLDeadzone);
					break;
                case MapCode.RightThumbAxisX:
                    //if (!IsValueChanged(MapCode.RightThumbAxisX, gp.RightThumbX)) break;
                    UpdateXAMLStickElements(gp.RightThumbX, (Label)ii.ControlStackPanel, RightThumbAxisStatus, ii.Code, stickRDeadzone);
                    break;
                case MapCode.RightThumbAxisY:
                    //if (!IsValueChanged(MapCode.RightThumbAxisY, gp.RightThumbY)) break;
                    UpdateXAMLStickElements(gp.RightThumbY, (Label)ii.ControlStackPanel, RightThumbAxisStatus, ii.Code, stickRDeadzone);
                    break;
                // Stick axis detailed deadzones.
                case MapCode.LeftThumbRight:
                case MapCode.LeftThumbLeft:
                    UpdateXAMLStickElementsDetailed(gp.LeftThumbX, (Label)ii.ControlStackPanel, ii.Code, stickLDeadzone);
                    break;
                case MapCode.LeftThumbUp:
                case MapCode.LeftThumbDown:
                    UpdateXAMLStickElementsDetailed(gp.LeftThumbY, (Label)ii.ControlStackPanel, ii.Code, stickLDeadzone);
                    break;
                case MapCode.RightThumbRight:
                case MapCode.RightThumbLeft:
                    UpdateXAMLStickElementsDetailed(gp.RightThumbX, (Label)ii.ControlStackPanel, ii.Code, stickRDeadzone);
                    break;
                case MapCode.RightThumbUp:
                case MapCode.RightThumbDown:
                    UpdateXAMLStickElementsDetailed(gp.RightThumbY, (Label)ii.ControlStackPanel, ii.Code, stickRDeadzone);
                    break;
                // Buttons.
                case MapCode.ButtonA:
				case MapCode.ButtonB:
				case MapCode.ButtonX:
				case MapCode.ButtonY:
				case MapCode.ButtonGuide:
				case MapCode.ButtonBack:
				case MapCode.ButtonStart:
				case MapCode.LeftShoulder:
				case MapCode.RightShoulder:
				case MapCode.LeftThumbButton:
				case MapCode.RightThumbButton:
				case MapCode.DPadUp:
				case MapCode.DPadLeft:
				case MapCode.DPadDown:	
				case MapCode.DPadRight:
                    on = gp.Buttons.HasFlag(ii.Button);
                    // Set XInput value below TextBox name.
                    ((Label)ii.ControlStackPanel).Content = on ? 1 : 0;
					break;
				// D-Pad.
				case MapCode.DPad:
					on =
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadUp) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadRight) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadDown);
                    ((Label)ii.ControlStackPanel).Content = on ? 1 : 0;
                    break;
			}

            if (ii.ControlName is ContentControl)
                padItem_General_XboxImageControl.setNormalOverActiveRecordColor(ii, on ? padItem_General_XboxImageControl.colorActive : padItem_General_XboxImageControl.colorNormalPath);

            // If record then...
            //if (Recorder.Recording)
            //{
            //	MapCode? redirect = null;
            //	if (Recorder.CurrentMap.Code == MapCode.RightThumbAxisX)
            //		redirect = MapCode.RightThumbRight;
            //	if (Recorder.CurrentMap.Code == MapCode.RightThumbAxisY)
            //		redirect = MapCode.RightThumbUp;
            //	if (Recorder.CurrentMap.Code == MapCode.LeftThumbAxisX)
            //		redirect = MapCode.LeftThumbRight;
            //	if (Recorder.CurrentMap.Code == MapCode.LeftThumbAxisY)
            //		redirect = MapCode.LeftThumbUp;
            //	if (redirect.HasValue)
            //	{
            //		MapCode recordingCode = ii.Code;
            //		recordingCode = redirect.Value;
            //		// Skip if redirected control.
            //		if (ii.Code == recordingCode)
            //			return;
            //	}
            //	// If record is in progress then...
            //	if (ii.Code == Recorder.CurrentMap.Code)
            //	{
            //		on = true;
            //	}
            //}

            //else if (
            //	 ShowLeftThumbButtons && SettingsConverter.LeftThumbCodes.Contains(ii.Code) ||
            //	 ShowRightThumbButtons && SettingsConverter.RightThumbCodes.Contains(ii.Code) ||
            //	 ShowDPadButtons && SettingsConverter.DPadCodes.Contains(ii.Code) ||
            //	 ShowMainButtons && SettingsConverter.MainButtonCodes.Contains(ii.Code) ||
            //	 ShowMenuButtons && SettingsConverter.MenuButtonCodes.Contains(ii.Code) ||
            //	 ShowTriggerButtons && SettingsConverter.TriggerButtonCodes.Contains(ii.Code) ||
            //	 ShowShoulderButtons && SettingsConverter.ShoulderButtonCodes.Contains(ii.Code)
            //)
            //{
            //	var nit = on ? NavImageType.Active : NavImageType.Normal;
            //	ImageControl.SetImage(ii.Code, nit, true);
            //}
            //else
            //{
            //	var isAxisCode = SettingsConverter.AxisCodes.Contains(ii.Code);
            //	// Axis status will be displayed as image therefore can hide active button indicator.
            //	ImageControl.SetImage(ii.Code, NavImageType.Active, on && !isAxisCode);
            //}
        }

		PadItem_General_XboxImageControl padItem_General_XboxImageControl = new PadItem_General_XboxImageControl();
		// PadItem_GeneralControl padItem_GeneralControl = new PadItem_GeneralControl();


		#region ■ IDisposable

		public bool IsDisposing;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				//Top = null;
				//Front = null;
				LeftThumbAxisStatus = null;
				RightThumbAxisStatus = null;
				LeftTriggerAxisStatus = null;
				RightTriggerAxisStatus = null;
				//ImageControl = null;
				// Dispose other.
				// markA.Dispose();
				// markB.Dispose();
				// markC.Dispose();
				// markB = null;
				// markA = null;
				// markC = null;
				Recorder.Dispose();
				Recorder = null;
				//locations.Clear();
				//locations = null;
			}
		}

		#endregion
	}
}
