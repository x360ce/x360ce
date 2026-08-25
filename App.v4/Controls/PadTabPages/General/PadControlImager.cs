using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
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
			Recorder = new Recorder(rH, rV);
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

		public void DrawState(ImageInfo ii, Gamepad gp, Control currentCbx)
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
			else
			{
				// Check when value is on.
				on = gp.Buttons.HasFlag(ii.Button);
			}
			MapCode recordingCode = ii.Code;

			var isRecordingItem = Recorder.Recording && ii.Code == Recorder.CurrentMap.Code;
			// If recording then...
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
					recordingCode = redirect.Value;
					// Skip if redirected control.
					if (ii.Code == recordingCode)
						return;
				}
			}
			// If recording is in progress then...
			if (isRecordingItem)
			{
				ImageControl.SetImage(recordingCode, NavImageType.Record, Recorder.DrawRecordingImage);
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
				var nit = on ? NavImageType.Active : NavImageType.Normal;
				ImageControl.SetImage(ii.Code, nit, true);
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
