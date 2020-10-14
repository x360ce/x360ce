using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.App.Controls.PadControl;

namespace x360ce.App.Controls
{
	public partial class PadControlImager: IDisposable
	{

		public PadControlImager()
		{
			locations.Add(GamepadButtonFlags.Y, new Point(196, 29));
			// Create images.
			TopImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png"));
			FrontImage = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerFront.png"));
			TopDisabledImage = AppHelper.GetDisabledImage(TopImage);
			FrontDisabledImage = AppHelper.GetDisabledImage(FrontImage);
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

		// Green roubnd button image.
		public Bitmap markB;
		// Green cross axis image.
		public Bitmap markA;
		// Green round controller/player number image.
		public Bitmap markC;
		Bitmap TopImage;
		Bitmap FrontImage;
		Bitmap TopDisabledImage;
		Bitmap FrontDisabledImage;

		Dictionary<GamepadButtonFlags, Point> locations = new Dictionary<GamepadButtonFlags, Point>();

		public void SetImages(PictureBox top, PictureBox front, bool enabled)
		{
			top.Image = enabled ? TopImage : TopDisabledImage;
			front.Image = enabled ? FrontImage : FrontDisabledImage;
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

		public void DrawState(ImageInfo ii, PaintEventArgs e, Gamepad gp, Control currentCbx)
		{
			// Draw axis state - green cross image.
			if (ii.Code == LayoutCode.LeftThumbButton || ii.Code == LayoutCode.RightThumbButton)
			{
				var mWA = -markB.Width / 2;
				var mHA = -markB.Height / 2;
				var padSize = 22F / (float)(ushort.MaxValue);
				var tX = ii.Code == LayoutCode.LeftThumbButton
					? gp.LeftThumbX
					: gp.RightThumbX;
				var tY = ii.Code == LayoutCode.LeftThumbButton
					? gp.LeftThumbY
					: gp.RightThumbY;
				e.Graphics.DrawImage(markA, (float)(ii.X + mWA + (tX * padSize)), (float)(ii.Y + mHA + (-tY * padSize)));
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
				if (on)
					e.Graphics.DrawImage(markB, (float)ii.X + mW, (float)ii.Y + mH);
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
					e.Graphics.DrawImage(markB, (float)ii.X + mW, (float)ii.Y + mH);
			}
			// If recording is in progress and processing current recording control then...
			// Draw recording image.
			if (Recorder.drawRecordingImage && ii.Control == currentCbx)
				Recorder.drawMarkR(e, new Point((int)ii.X, (int)ii.Y));
			if (ii.Label != null)
				setLabelColor(on, ii.Label);
		}

		void setLabelColor(bool on, Control label)
		{
			var c = on ? Color.Green : SystemColors.ControlText;
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
