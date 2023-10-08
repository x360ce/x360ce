using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Policy;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.Engine;

namespace x360ce.App.Controls
{
    public partial class PadControlImager : IDisposable
    {

        public static object imagesLock = new object();

        static Bitmap topImage;
        //static Bitmap frontImage;
        //static Bitmap topDisabledImage;
        //static Bitmap frontDisabledImage;

        //static ImageSource _TopImage;
        //static ImageSource _FrontImage;
        //static ImageSource _TopDisabledImage;
        //static ImageSource _FrontDisabledImage;

        public PadControlImager()
        {
            locations.Add(GamepadButtonFlags.Y, new Point(196, 29));
            lock (imagesLock)
            {
                var a = GetType().Assembly;
                // Create images.
                if (topImage == null)
                {
                    var keys = JocysCom.ClassLibrary.Helper.GetResourceKeys(a);
                    //var ti = JocysCom.ClassLibrary.Helper.GetResourceValue("images/xboxcontrollertop.png", a);
                    //topImage = new Bitmap(ti);
                    //var ti2 = JocysCom.ClassLibrary.Helper.GetResourceValue("images/xboxcontrollerfront.png", a);
                    //frontImage = new Bitmap(ti2);
                    //topDisabledImage = AppHelper.GetDisabledImage(topImage);
                    //frontDisabledImage = AppHelper.GetDisabledImage(frontImage);
                    // WPF.
                    //_TopImage = ControlsHelper.GetImageSource(topImage);
                    //_FrontImage = ControlsHelper.GetImageSource(frontImage);
                    //_TopDisabledImage = ControlsHelper.GetImageSource(topDisabledImage);
                    //_FrontDisabledImage = ControlsHelper.GetImageSource(frontDisabledImage);
                }
                // Other.
                markB = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkButton.png", a);
                markA = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkAxis.png", a);
                markC = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.MarkController.png", a);
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
        public Bitmap markB;
        // Green cross axis image.
        public Bitmap markA;
        // Green round controller/player number image.
        public Bitmap markC;

        public PadItem_General_XboxImageControl ImageControl;

        Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();

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
		public Border LeftThumbAxisStatus;
		public Border RightThumbAxisStatus;
		public Border LeftTriggerAxisStatus;
		public Border RightTriggerAxisStatus;

		public System.Windows.Shapes.Path DPadUpStatus;

		bool on = false;

		public void DrawState(ImageInfo ii, Gamepad gp)
        {
			// Trigger axis state with "•" yellow circle.
			if (ii.Code == MapCode.LeftTrigger || ii.Code == MapCode.RightTrigger)
            {
                var isLeft = ii.Code == MapCode.LeftTrigger;
				var y = isLeft ? gp.LeftTrigger : gp.RightTrigger;
				var control = isLeft ? LeftTriggerAxisStatus : RightTriggerAxisStatus;
				var h = (float)(((System.Windows.FrameworkElement)control.Parent).Height - control.Height);
                var b = ConvertHelper.ConvertRangeF(y, byte.MinValue, byte.MaxValue, 0, h);
                var m = control.Margin;
                control.Margin = new System.Windows.Thickness(m.Left, m.Top, m.Right, b);
				var triggerLDeadzone = 0;
				var triggerRDeadzone = 0;
				on = (ii.Code == MapCode.LeftTrigger && y > triggerLDeadzone) || (ii.Code == MapCode.RightTrigger && y > triggerRDeadzone);
			}
            // Show stick axis state with "•" yellow circle.
			else if (ii.Code == MapCode.LeftThumbAxisX || ii.Code == MapCode.LeftThumbAxisY || ii.Code == MapCode.RightThumbAxisX || ii.Code == MapCode.RightThumbAxisY)
			{
				var isLeft = ii.Code == MapCode.LeftThumbAxisX || ii.Code == MapCode.LeftThumbAxisY;
				var x = isLeft ? gp.LeftThumbX : gp.RightThumbX;
				var y = isLeft ? gp.LeftThumbY : gp.RightThumbY;
				var control = isLeft ? LeftThumbAxisStatus : RightThumbAxisStatus;
				var w = (float)(((System.Windows.FrameworkElement)control.Parent).Width - control.Width);
				var l = ConvertHelper.ConvertRangeF(x, short.MinValue, short.MaxValue, -w, w);
				var t = ConvertHelper.ConvertRangeF(y, short.MinValue, short.MaxValue, w, -w);
				var m = control.Margin;
				control.Margin = new System.Windows.Thickness(l, t, m.Right, m.Bottom);
                var stickLDeadzone = 2000;
				var stickRDeadzone = 2000;
				on = (ii.Code == MapCode.LeftThumbAxisX && (x > stickLDeadzone || x < -stickLDeadzone)) ||
					  (ii.Code == MapCode.LeftThumbAxisY && (y > stickLDeadzone || y < -stickLDeadzone)) ||
					  (ii.Code == MapCode.RightThumbAxisX && (x > stickRDeadzone || x < -stickRDeadzone)) ||
					  (ii.Code == MapCode.RightThumbAxisY && (y > stickRDeadzone || y < -stickRDeadzone));
			}
			// If D-Pad.
			else if (ii.Code == MapCode.DPad)
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
                var deadzone = 2000;
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
                on = value < -deadzone || value > deadzone;
                if (ii.Code == MapCode.LeftThumbRight)
                    on = gp.LeftThumbX > deadzone;
                if (ii.Code == MapCode.LeftThumbLeft)
                    on = gp.LeftThumbX < -deadzone;
                if (ii.Code == MapCode.LeftThumbUp)
                    on = gp.LeftThumbY > deadzone;
                if (ii.Code == MapCode.LeftThumbDown)
                    on = gp.LeftThumbY < -deadzone;
                if (ii.Code == MapCode.RightThumbRight)
                    on = gp.RightThumbX > deadzone;
                if (ii.Code == MapCode.RightThumbLeft)
                    on = gp.RightThumbX < -deadzone;
                if (ii.Code == MapCode.RightThumbUp)
                    on = gp.RightThumbY > deadzone;
                if (ii.Code == MapCode.RightThumbDown)
                    on = gp.RightThumbY < -deadzone;
            }
            else
            {
                // Check when value is on.
                on = gp.Buttons.HasFlag(ii.Button);
            }

            // If record then...
            if (Recorder.Recording)
            {
                MapCode? redirect = null;
                if (Recorder.CurrentMap.Code == MapCode.RightThumbAxisX)
                    redirect = MapCode.RightThumbRight;
                if (Recorder.CurrentMap.Code == MapCode.RightThumbAxisY)
                    redirect = MapCode.RightThumbUp;
                if (Recorder.CurrentMap.Code == MapCode.LeftThumbAxisX)
                    redirect = MapCode.LeftThumbRight;
                if (Recorder.CurrentMap.Code == MapCode.LeftThumbAxisY)
                    redirect = MapCode.LeftThumbUp;
                if (redirect.HasValue)
                {
                    MapCode recordingCode = ii.Code;
                    recordingCode = redirect.Value;
                    // Skip if redirected control.
                    if (ii.Code == recordingCode)
                        return;
                }
            }

			// If record is in progress then...
			if (Recorder.Recording && ii.Code == Recorder.CurrentMap.Code)
            {
                // ImageControl.SetImage(recordingCode, NavImageType.Record, Recorder.DrawRecordingImage);
                on = true;
            }
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

            if (ii.Label is ContentControl)
                padItem_General_XboxImageControl.setNormalOverActiveRecordColor(ii, on ? padItem_General_XboxImageControl.colorActive : padItem_General_XboxImageControl.colorNormalPath);
			    //padItem_GeneralControl.setNormalActiveColor(ii, on ? padItem_General_XboxImageControl.colorActive : padItem_General_XboxImageControl.colorNormalPath);
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
                ImageControl = null;
                // Dispose other.
                markA.Dispose();
                markB.Dispose();
                markC.Dispose();
                markB = null;
                markA = null;
                markC = null;
                Recorder.Dispose();
                Recorder = null;
                locations.Clear();
                locations = null;
            }
        }

        #endregion
    }

}
