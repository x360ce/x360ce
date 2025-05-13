//using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
//using System.IO;
//using System.Security.Policy;
using System.Windows.Controls;
//using System.Windows.Media;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class PadControlImager : IDisposable
	{

		public static object imagesLock = new object();

		//static Bitmap topImage;
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
			short stickLDeadzone = Gamepad.LeftThumbDeadZone;
			short stickRDeadzone = Gamepad.RightThumbDeadZone;
			//Trigger axis state with "•" yellow circle.
			switch (ii.Code)
			{
				case MapCode.LeftTrigger:
				case MapCode.RightTrigger:
					var isLeft = ii.Code == MapCode.LeftTrigger;
					var y = isLeft ? gp.LeftTrigger : gp.RightTrigger;
					var control = isLeft ? LeftTriggerAxisStatus : RightTriggerAxisStatus;
					var h = (float)(((System.Windows.FrameworkElement)control.Parent).Height - control.Height);
					var b = ConvertHelper.ConvertRangeF(y, byte.MinValue, byte.MaxValue, 0, h);
					var m = control.Margin;
					control.Margin = new System.Windows.Thickness(m.Left, m.Top, m.Right, b);
					// Deadzone.
					on = (ii.Code == MapCode.LeftTrigger && y > Gamepad.TriggerThreshold) || (ii.Code == MapCode.RightTrigger && y > Gamepad.TriggerThreshold);
					// XInput value.
					((Label)ii.ControlXI).Content = ii.Code == MapCode.LeftTrigger ? gp.LeftTrigger : gp.RightTrigger;
				break;
				case MapCode.LeftThumbAxisX:
				case MapCode.LeftThumbAxisY:
				case MapCode.RightThumbAxisX:
				case MapCode.RightThumbAxisY:
					var isLeft2 = ii.Code == MapCode.LeftThumbAxisX || ii.Code == MapCode.LeftThumbAxisY;
					var x2 = isLeft2 ? gp.LeftThumbX : gp.RightThumbX;
					var y2 = isLeft2 ? gp.LeftThumbY : gp.RightThumbY;
					var control2 = isLeft2 ? LeftThumbAxisStatus : RightThumbAxisStatus;
					var w = (float)(((System.Windows.FrameworkElement)control2.Parent).Width - control2.Width);
					var l = ConvertHelper.ConvertRangeF(x2, short.MinValue, short.MaxValue, -w, w);
					var t = ConvertHelper.ConvertRangeF(y2, short.MinValue, short.MaxValue, w, -w);
					var m2 = control2.Margin;
					control2.Margin = new System.Windows.Thickness(l, t, m2.Right, m2.Bottom);
					// Deadzone.
					on = (ii.Code == MapCode.LeftThumbAxisX && (x2 > stickLDeadzone || x2 < -stickLDeadzone)) ||
						 (ii.Code == MapCode.LeftThumbAxisY && (y2 > stickLDeadzone || y2 < -stickLDeadzone)) ||
						 (ii.Code == MapCode.RightThumbAxisX && (x2 > stickRDeadzone || x2 < -stickRDeadzone)) ||
						 (ii.Code == MapCode.RightThumbAxisY && (y2 > stickRDeadzone || y2 < -stickRDeadzone));
					// XInput value.
					switch (ii.Code)
					{
						case MapCode.LeftThumbAxisX: ((Label)ii.ControlXI).Content = gp.LeftThumbX; break;
						case MapCode.LeftThumbAxisY: ((Label)ii.ControlXI).Content = gp.LeftThumbY; break;
						case MapCode.RightThumbAxisX: ((Label)ii.ControlXI).Content = gp.RightThumbX; break;
						case MapCode.RightThumbAxisY: ((Label)ii.ControlXI).Content = gp.RightThumbY; break;
					}
				break;
				// Axis detailed deadzones...
				case MapCode.LeftThumbRight:
					on = gp.LeftThumbX > stickLDeadzone;
					break;
				case MapCode.LeftThumbLeft:
					on = gp.LeftThumbX < -stickLDeadzone;
					break;
				case MapCode.LeftThumbUp:
					on = gp.LeftThumbY > stickLDeadzone;
					break;
				case MapCode.LeftThumbDown:
					on = gp.LeftThumbY < -stickLDeadzone;
					break;
				case MapCode.RightThumbRight:
					on = gp.RightThumbX > stickRDeadzone;
					break;
				case MapCode.RightThumbLeft:
					on = gp.RightThumbX < -stickRDeadzone;
					break;
				case MapCode.RightThumbUp:
					on = gp.RightThumbY > stickRDeadzone;
					break;
				case MapCode.RightThumbDown:
					on = gp.RightThumbY < -stickRDeadzone;
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
					((Label)ii.ControlXI).Content = on ? 1 : 0;
					break;
				// D-Pad.
				case MapCode.DPad:
					on =
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadUp) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadRight) ||
					gp.Buttons.HasFlag(GamepadButtonFlags.DPadDown);
					break;
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
				// If record is in progress then...
				if (ii.Code == Recorder.CurrentMap.Code)
				{
					on = true;
				}
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

			if (ii.ControlName is ContentControl)
				padItem_General_XboxImageControl.setNormalOverActiveRecordColor(ii, on ? padItem_General_XboxImageControl.colorActive : padItem_General_XboxImageControl.colorNormalPath);
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
				// markA.Dispose();
				// markB.Dispose();
				// markC.Dispose();
				// markB = null;
				// markA = null;
				// markC = null;
				Recorder.Dispose();
				Recorder = null;
				locations.Clear();
				locations = null;
			}
		}

		#endregion
	}
}
