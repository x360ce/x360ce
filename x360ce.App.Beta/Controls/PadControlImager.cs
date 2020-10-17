using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
			TopImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png"));
			FrontImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerFront.png"));
			TopDisabledImage = AppHelper.GetDisabledImage(TopImage);
			FrontDisabledImage = AppHelper.GetDisabledImage(FrontImage);
			// WPF.
			_TopImage = GetImageSource(TopImage);
			_FrontImage = GetImageSource(FrontImage);
			_TopDisabledImage = GetImageSource(TopDisabledImage);
			_FrontDisabledImage = GetImageSource(FrontDisabledImage);
			// Other.
			markB = new Bitmap(EngineHelper.GetResourceStream("Images.MarkButton.png"));
			markA = new Bitmap(EngineHelper.GetResourceStream("Images.MarkAxis.png"));
			markC = new Bitmap(EngineHelper.GetResourceStream("Images.MarkController.png"));
			float rH = TopDisabledImage.HorizontalResolution;
			float rV = TopDisabledImage.VerticalResolution;
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
		Bitmap TopImage;
		Bitmap FrontImage;
		Bitmap TopDisabledImage;
		Bitmap FrontDisabledImage;

		ImageSource _TopImage;
		ImageSource _FrontImage;
		ImageSource _TopDisabledImage;
		ImageSource _FrontDisabledImage;

		public XboxImageControl ImageControl;

		Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();

		public System.Windows.Controls.Image Top;
		public System.Windows.Controls.Image Front;

		public System.Windows.Controls.ContentControl LeftThumb;
		public System.Windows.Controls.ContentControl RightThumb;

		public void SetImages(bool enabled)
		{
			Top.Source = enabled ? _TopImage : _TopDisabledImage;
			Front.Source = enabled ? _FrontImage : _FrontDisabledImage;




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

		public void DrawState(ImageInfo ii, Gamepad gp, Control currentCbx)
		{
			// Draw axis state - green cross image.
			if (ii.Code == LayoutCode.LeftThumbButton || ii.Code == LayoutCode.RightThumbButton)
			{
				var control = ii.Code == LayoutCode.LeftThumbButton
					? LeftThumb
					: RightThumb;
				var w = (float)((System.Windows.FrameworkElement)control.Parent).ActualWidth / 2F;
				var x = ii.Code == LayoutCode.LeftThumbButton
					? gp.LeftThumbX
					: gp.RightThumbX;
				var y = ii.Code == LayoutCode.LeftThumbButton
					? gp.LeftThumbY
					: gp.RightThumbY;
				var l = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, -w, w, x);
				var t = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, w, -w, y);
				control.Margin = new System.Windows.Thickness(l, t, 0, 0);
			}
			bool on;
			// If triggers then...
			if (ii.Code == LayoutCode.LeftTrigger || ii.Code == LayoutCode.RightTrigger)
			{
				// This is axis.
				short value = 0;
				if (ii.Code == LayoutCode.LeftTrigger)
					value = gp.LeftTrigger;
				else if (ii.Code == LayoutCode.RightTrigger)
					value = gp.RightTrigger;
				// Check when value is on.
				on = value > 0;
				// Draw button image, thou some slider image would be better.
				var mW = -markB.Width / 2;
				var mH = -markB.Height / 2;
				//if (on)
				//	e.Graphics.DrawImage(markB, (float)ii.X + mW, (float)ii.Y + mH);
			}
			// If D-Pad.
			else if (ii.Code == LayoutCode.DPad)
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
				// This is button.
				var mW = -markB.Width / 2;
				var mH = -markB.Height / 2;
				// Check when value is on.
				on = gp.Buttons.HasFlag(ii.Button);
				if (on)
				{
					//e.Graphics.DrawImage(markB, (float)ii.X + mW, (float)ii.Y + mH);
				}
			}
			if (Recorder.Recording)
			{
				if (ii.Control == currentCbx)
				{
					// If recording is in progress and processing current recording control then...
					// Draw recording image.
					if (Recorder.drawRecordingImage)
					{
						//Recorder.drawMarkR(e, new Point((int)ii.X, (int)ii.Y));
					}
					ImageControl.SetImage(ii.Code, NavImageType.Record, Recorder.drawRecordingImage);
				}
			}
			else if (ii.Label != null)
			{
				setLabelColor(on, ii.Label);
				ImageControl.SetImage(ii.Code, NavImageType.Active, on);
			}
		}

		void setLabelColor(bool on, Control label)
		{
			var c = on ? System.Drawing.Color.Green : SystemColors.ControlText;
			if (label.ForeColor != c)
				label.ForeColor = c;
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

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
