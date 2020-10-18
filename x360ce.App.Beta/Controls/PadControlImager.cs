using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class PadControlImager : IDisposable
	{

		public PadControlImager()
		{
			locations.Add(GamepadButtonFlags.Y, new Point(196, 29));
			// Create images.
			var topImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png"));
			var frontImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerFront.png"));
			var topDisabledImage = AppHelper.GetDisabledImage(topImage);
			var frontDisabledImage = AppHelper.GetDisabledImage(frontImage);
			// WPF.
			_TopImage = GetImageSource(topImage);
			_FrontImage = GetImageSource(frontImage);
			_TopDisabledImage = GetImageSource(topDisabledImage);
			_FrontDisabledImage = GetImageSource(frontDisabledImage);
			// Other.
			markB = new Bitmap(EngineHelper.GetResourceStream("Images.MarkButton.png"));
			markA = new Bitmap(EngineHelper.GetResourceStream("Images.MarkAxis.png"));
			markC = new Bitmap(EngineHelper.GetResourceStream("Images.MarkController.png"));
			float rH = topDisabledImage.HorizontalResolution;
			float rV = topDisabledImage.VerticalResolution;
			// Make sure resolution is same everywhere so images won't be resized.
			markB.SetResolution(rH, rV);
			markA.SetResolution(rH, rV);
			markC.SetResolution(rH, rV);
			Recorder = new Recorder(rH, rV);
		}

		public Recorder Recorder;

		// Green round button image.
		public Bitmap markB;
		// Green cross axis image.
		public Bitmap markA;
		// Green round controller/player number image.
		public Bitmap markC;

		ImageSource _TopImage;
		ImageSource _FrontImage;
		ImageSource _TopDisabledImage;
		ImageSource _FrontDisabledImage;

		public XboxImageControl ImageControl;

		Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();

		// Background images.
		public System.Windows.Controls.Image Top;
		public System.Windows.Controls.Image Front;

		// Axis status Images.
		public System.Windows.Controls.ContentControl LeftThumbStatus;
		public System.Windows.Controls.ContentControl RightThumbStatus;
		public System.Windows.Controls.ContentControl LeftTriggerStatus;
		public System.Windows.Controls.ContentControl RightTriggerStatus;

		public void SetImages(bool enabled)
		{
			Top.Source = enabled ? _TopImage : _TopDisabledImage;
			Front.Source = enabled ? _FrontImage : _FrontDisabledImage;
			var show = enabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
			LeftThumbStatus.Visibility = show;
			RightThumbStatus.Visibility = show;
			LeftTriggerStatus.Visibility = show;
			RightTriggerStatus.Visibility = show;
		}

		public ImageSource GetImageSource(Bitmap bitmap)
		{
			var photo = new BitmapImage();
			var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Png);
			photo.BeginInit();
			photo.CacheOption = BitmapCacheOption.OnLoad;
			photo.StreamSource = stream;
			photo.EndInit();
			stream.Dispose();
			return photo;
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

		public void DrawState(ImageInfo ii, Gamepad gp, Control currentCbx)
		{
			bool on;
			// Show trigger axis state -green minus image.
			if (ii.Code == LayoutCode.LeftTrigger || ii.Code == LayoutCode.RightTrigger)
			{
				var isLeft = ii.Code == LayoutCode.LeftTrigger;
				var control = isLeft ? LeftTriggerStatus : RightTriggerStatus;
				var h = (float)(((System.Windows.FrameworkElement)control.Parent).Height - control.Height);
				var y = isLeft ? gp.LeftTrigger : gp.RightTrigger;
				var b = ConvertHelper.ConvertRangeF(byte.MinValue, byte.MaxValue, 0, h, y);
				var m = control.Margin;
				on = y > 0;
				control.Margin = new System.Windows.Thickness(m.Left, m.Top, m.Right, b);
			}
			// Draw thumb axis state - green cross image.
			if (ii.Code == LayoutCode.LeftThumbButton || ii.Code == LayoutCode.RightThumbButton)
			{
				var isLeft = ii.Code == LayoutCode.LeftThumbButton;
				var control = isLeft ? LeftThumbStatus : RightThumbStatus;
				var w = (float)((System.Windows.FrameworkElement)control.Parent).Width / 2F;
				var x = isLeft ? gp.LeftThumbX : gp.RightThumbX;
				var y = isLeft ? gp.LeftThumbY : gp.RightThumbY;
				var l = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, -w, w, x);
				var t = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, w, -w, y);
				var m = control.Margin;
				control.Margin = new System.Windows.Thickness(l, t, m.Right, m.Bottom);
			}
			// If D-Pad.
			if (ii.Code == LayoutCode.DPad)
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
				var t = 2000;
				// This is axis.
				short value = 0;
				if (ii.Code == LayoutCode.LeftThumbAxisX)
					value = gp.LeftThumbX;
				else if (ii.Code == LayoutCode.LeftThumbAxisY)
					value = gp.LeftThumbY;
				else if (ii.Code == LayoutCode.RightThumbAxisX)
					value = gp.RightThumbX;
				else if (ii.Code == LayoutCode.RightThumbAxisY)
					value = gp.RightThumbY;
				// Check when value is on.
				on = value < -t || value > t;
				if (ii.Code == LayoutCode.LeftThumbRight)
					on = gp.LeftThumbX > t;
				if (ii.Code == LayoutCode.LeftThumbLeft)
					on = gp.LeftThumbX < -t;
				if (ii.Code == LayoutCode.LeftThumbUp)
					on = gp.LeftThumbY > t;
				if (ii.Code == LayoutCode.LeftThumbDown)
					on = gp.LeftThumbY < -t;
				if (ii.Code == LayoutCode.RightThumbRight)
					on = gp.RightThumbX > t;
				if (ii.Code == LayoutCode.RightThumbLeft)
					on = gp.RightThumbX < -t;
				if (ii.Code == LayoutCode.RightThumbUp)
					on = gp.RightThumbY > t;
				if (ii.Code == LayoutCode.RightThumbDown)
					on = gp.RightThumbY < -t;
			}
			else
			{
				// Check when value is on.
				on = gp.Buttons.HasFlag(ii.Button);
			}
			// If recording is in progress then...
			if (Recorder.Recording && ii.Control == currentCbx)
			{
				ImageControl.SetImage(ii.Code, NavImageType.Record, Recorder.DrawRecordingImage);
			}
			else if (
				 ShowLeftThumbButtons && SettingsConverter.LeftThumbCodes.Contains(ii.Code) ||
				 ShowRightThumbButtons && SettingsConverter.RightThumbCodes.Contains(ii.Code) ||
				 ShowDPadButtons && SettingsConverter.DPadCodes.Contains(ii.Code) ||
				 ShowMainButtons && SettingsConverter.MainButtonCodes.Contains(ii.Code) ||
				 ShowMenuButtons && SettingsConverter.MenuButtonCodes.Contains(ii.Code) ||
				 ShowTriggerButtons && SettingsConverter.TriggerButtonCodes.Contains(ii.Code) ||
				 ShowShoulderButtons && SettingsConverter.ShoulderButtonCodes.Contains(ii.Code)
			)
			{
				ImageControl.SetImage(ii.Code, NavImageType.Normal, true);
			}
			else
			{
				var isAxisCode = SettingsConverter.AxisCodes.Contains(ii.Code);
				// Axis status will be displayed as image therefore can hide active button indicator.
				ImageControl.SetImage(ii.Code, NavImageType.Active, on && !isAxisCode);
			}
			if (ii.Label != null)
				setLabelColor(on, ii.Label);
		}

		void setLabelColor(bool on, Control label)
		{
			var c = on ? System.Drawing.Color.Green : SystemColors.ControlText;
			if (label.ForeColor != c)
				label.ForeColor = c;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#region IDisposable

		bool IsDisposing;

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				markA.Dispose();
				markB.Dispose();
				markC.Dispose();
				Recorder.Dispose();
			}
		}

		#endregion
	}

}
