using JocysCom.ClassLibrary.Controls.Themes;
using SharpDX.XInput;
using System;
using System.Drawing;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The square of a device's X and Y axes as Axis to D-Pad reads them: the middle, where nothing is pressed, the four
	/// sides past the press point, where a direction is, the device's reading as a dot, and the sides it presses lit.
	/// </summary>
	/// <remarks>
	/// The press points and the offset are the engine's own (<see cref="ConvertHelper.GetAxisToDPadButtons"/>), so the chart
	/// lights exactly the directions the controller gets. As tall as seven lines of its text, and square.
	/// </remarks>
	public class AxisToDPadChart : Control
	{
		public AxisToDPadChart()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
				| ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
			AutoSize = true;
		}

		int _pressPoint = ConvertHelper.GetAxisToDPadPressPoint(256);
		int _shift;
		int _x = short.MaxValue;
		int _y = short.MaxValue;
		bool _hasReading;
		GamepadButtonFlags _pressed;

		/// <summary>The settings the chart is drawn for, as they are stored: 0 to 256 for none to the whole of half the travel.</summary>
		public void SetSettings(int deadZone, int offset)
		{
			var pressPoint = ConvertHelper.GetAxisToDPadPressPoint(deadZone);
			var shift = ConvertHelper.GetAxisToDPadShift(offset);
			if (pressPoint == _pressPoint && shift == _shift)
				return;
			_pressPoint = pressPoint;
			_shift = shift;
			_pressed = _hasReading ? ConvertHelper.GetAxisToDPadButtons(_x, _y, _pressPoint, _shift, GamepadButtonFlags.None) : GamepadButtonFlags.None;
			Invalidate();
		}

		/// <summary>The device's X and Y readings, 0 to 65535; drawn again only when they or what they press change.</summary>
		public void SetReading(int x, int y)
		{
			var pressed = ConvertHelper.GetAxisToDPadButtons(x, y, _pressPoint, _shift, _pressed);
			if (_hasReading && x == _x && y == _y && pressed == _pressed)
				return;
			_x = x;
			_y = y;
			_pressed = pressed;
			_hasReading = true;
			Invalidate();
		}

		/// <summary>No device to read: the dot is taken away.</summary>
		public void ClearReading()
		{
			if (!_hasReading)
				return;
			_hasReading = false;
			_pressed = GamepadButtonFlags.None;
			Invalidate();
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			var side = Font.Height * 7;
			return new Size(side, side);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			AppHelper.PaintChartBox(g, this, Enabled);
			var chart = AppHelper.ChartBounds(this);
			var side = Math.Min(chart.Width, chart.Height) - 1;
			if (side <= 0)
				return;
			var area = new RectangleF(chart.X + (chart.Width - 1 - side) / 2f, chart.Y + (chart.Height - 1 - side) / 2f, side, side);
			// Where a reading stands across the square, 0 at the left or the top to 65535 at the right or the bottom.
			Func<int, float, float, float> at = (value, start, length) => start + value * length / ushort.MaxValue;
			// The first readings that press each side, as the engine's half-axis buttons count them.
			var right = at(short.MaxValue + 1 + _shift + _pressPoint, area.Left, area.Width);
			var left = at(_shift + short.MaxValue - _pressPoint, area.Left, area.Width);
			var down = at(short.MaxValue + 1 + _shift + _pressPoint, area.Top, area.Height);
			var up = at(_shift + short.MaxValue - _pressPoint, area.Top, area.Height);
			var brand = FormsTheme.GetColor("ColorBrand", Color.Blue);
			var alpha = Enabled ? 1f : 0.5f;
			using (var side1 = new SolidBrush(Color.FromArgb((int)(32 * alpha), brand)))
			using (var lit = new SolidBrush(Color.FromArgb((int)(128 * alpha), brand)))
			{
				g.FillRectangle(Pressed(GamepadButtonFlags.DPadLeft) ? lit : side1, area.Left, area.Top, Math.Max(0, left - area.Left), area.Height);
				g.FillRectangle(Pressed(GamepadButtonFlags.DPadRight) ? lit : side1, right, area.Top, Math.Max(0, area.Right - right), area.Height);
				g.FillRectangle(Pressed(GamepadButtonFlags.DPadUp) ? lit : side1, area.Left, area.Top, area.Width, Math.Max(0, up - area.Top));
				g.FillRectangle(Pressed(GamepadButtonFlags.DPadDown) ? lit : side1, area.Left, down, area.Width, Math.Max(0, area.Bottom - down));
			}
			using (var line = new Pen(AppHelper.ChartLineColor, 1f))
			{
				// The frame and the centre, where the offset puts it.
				g.DrawRectangle(line, area.X, area.Y, area.Width, area.Height);
				var centreX = at(short.MaxValue + _shift, area.Left, area.Width);
				var centreY = at(short.MaxValue + _shift, area.Top, area.Height);
				g.DrawLine(line, centreX, area.Top, centreX, area.Bottom);
				g.DrawLine(line, area.Left, centreY, area.Right, centreY);
			}
			if (!_hasReading || !Enabled)
				return;
			var smoothing = g.SmoothingMode;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			var radius = 2.5f * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			var x = at(_x, area.Left, area.Width);
			var y = at(_y, area.Top, area.Height);
			using (var dot = new SolidBrush(SystemColors.WindowText))
				g.FillEllipse(dot, x - radius, y - radius, radius * 2f, radius * 2f);
			g.SmoothingMode = smoothing;
		}

		bool Pressed(GamepadButtonFlags flag)
		{
			return _hasReading && (_pressed & flag) != 0;
		}
	}
}
