using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>The shape the X and Y axes move in.</summary>
	public enum TestMotion
	{
		[Description("Swing and ramp")]
		Swing,
		[Description("Circle")]
		Circle,
	}

	/// <summary>A pretend controller that moves by itself, for trying x360ce without hardware.</summary>
	/// <remarks>
	/// The template for a new plugin: one file with a class for the device, settings x360ce shows on the device's tab of
	/// the controller page, and a control of its own placed under them. It is embedded in x360ce.exe, so it is always
	/// there; built on its own it loads from the plugins folder like any other.
	///
	/// x360ce sets the settings on the thread that calls <see cref="Read"/>, so they need no lock. The control works on
	/// the interface thread, so what it shares with <see cref="Read"/> is volatile.
	/// </remarks>
	[Description("A pretend controller that moves by itself, so a mapping can be tried without hardware. "
		+ "By default, in every six seconds it moves for four and rests for two. The even axes (X, Z and Y Rotation) swing like a sine "
		+ "from the middle to each end and back, reversing every other cycle; the odd axes (Y and X Rotation) run from one end "
		+ "to the other and back; the hat turns once round and back. One button is held at a time, a new one each second. "
		+ "Its tab on the controller page sets the moving and resting times, the shape (Circle takes X and Y round), whether "
		+ "the axes, the hat and the buttons move, and how long each button is held; a square on the tab steers X and Y by hand.")]
	public class TestController : IPluginDevice
	{
		/// <summary>The product of the Test Device this controller replaces, so its saved mappings keep working.</summary>
		static readonly Guid Product = new Guid("a10ef011-41ab-4460-8c45-c1ad2dbf7ede");

		readonly Stopwatch _watch = Stopwatch.StartNew();

		public string Name { get { return "Test Controller"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 5; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 1; } }
		public int ButtonCount { get { return 10; } }

		#region Settings

		[Category("Movement"), DisplayName("Move the axes")]
		[Description("Moves the axes by themselves. Off, they rest: the even ones in the middle, the odd ones at their start.")]
		public bool MoveAxes { get; set; } = true;

		[Category("Movement"), DisplayName("Turn the hat")]
		[Description("Turns the hat once round and back while the axes move. Off, it stays released.")]
		public bool TurnHat { get; set; } = true;

		[Category("Movement"), DisplayName("Shape")]
		[Description("Swing and ramp: the even axes swing like a sine, the odd ones run from one end to the other. Circle: X and Y go round, as a stick does when drawn in a circle.")]
		public TestMotion Shape { get; set; }

		[Category("Movement"), DisplayName("Moving time (s)")]
		[Description("How long the axes and the hat move before they rest.")]
		[Range(0.5, 20.0)]
		public double MoveSeconds { get; set; } = 4;

		[Category("Movement"), DisplayName("Resting time (s)")]
		[Description("How long the axes and the hat rest between movements.")]
		[Range(0.0, 20.0)]
		public double RestSeconds { get; set; } = 2;

		[Category("Buttons"), DisplayName("Press the buttons")]
		[Description("Holds one button at a time, the next one after each press. Off, none is pressed.")]
		public bool PressButtons { get; set; } = true;

		[Category("Buttons"), DisplayName("Press time (ms)")]
		[Description("How long each button is held before the next one.")]
		[Range(50, 5000)]
		public int PressMilliseconds { get; set; } = 1000;

		[DisplayName("Reads per second")]
		[Description("How many times x360ce read the controller in the last second.")]
		public int ReadsPerSecond { get { return _readsPerSecond; } }

		volatile int _readsPerSecond;
		int _reads;
		long _readsSince;

		#endregion

		#region Steering by hand

		volatile bool _steering;
		volatile int _steerX = PluginState.Middle;
		volatile int _steerY = PluginState.Middle;
		volatile int _x;
		volatile int _y;

		/// <summary>Where X is now, 0 to 65535, for the controller's panel.</summary>
		[Browsable(false)]
		public int X { get { return _x; } }

		/// <summary>Where Y is now, 0 to 65535, for the controller's panel.</summary>
		[Browsable(false)]
		public int Y { get { return _y; } }

		/// <summary>True while X and Y follow the hand.</summary>
		[Browsable(false)]
		public bool Steering { get { return _steering; } }

		/// <summary>Holds X and Y where the hand puts them, until <see cref="Release"/>.</summary>
		public void Steer(int x, int y)
		{
			_steerX = Math.Max(0, Math.Min(PluginState.Max, x));
			_steerY = Math.Max(0, Math.Min(PluginState.Max, y));
			_steering = true;
		}

		/// <summary>Lets X and Y move by themselves again.</summary>
		public void Release()
		{
			_steering = false;
		}

		#endregion

		public void Read(PluginState state)
		{
			var elapsed = _watch.ElapsedMilliseconds;
			_reads++;
			if (elapsed - _readsSince >= 1000)
			{
				_readsPerSecond = _reads;
				_reads = 0;
				_readsSince = elapsed;
			}
			// Held inside the ranges the settings give, in case a value from elsewhere is not.
			var busy = Math.Max(500, (int)(MoveSeconds * 1000));
			var idle = Math.Max(0, (int)(RestSeconds * 1000));
			var half = busy / 2;
			var ms = (int)(elapsed % int.MaxValue);
			// One button at a time, a new one after each press.
			var press = elapsed / Math.Max(50, PressMilliseconds);
			for (var i = 0; i < ButtonCount; i++)
				state.Buttons[i] = PressButtons && press % ButtonCount == i;
			// Moving for the moving time, then resting for the resting time.
			var time = ms % (busy + idle);
			var moving = time < busy;
			var invert = ms % ((busy + idle) * 2) > busy + idle;
			// The hat turns once round and back, starting from up.
			var degree = PluginState.PovReleased;
			if (moving && TurnHat)
			{
				var value = (time + half) % busy;
				degree = value < half
					? Range(0, half - 1, 0, 35999, value)
					: Range(half, busy - 1, 35999, 0, value);
			}
			state.Povs[0] = degree;
			for (var i = 0; i < AxisCount; i++)
			{
				var isEven = i % 2 == 0;
				// At rest an even axis is in the middle and an odd one at its start.
				var position = isEven ? PluginState.Middle + 1 : 0;
				if (moving && MoveAxes)
				{
					if (Shape == TestMotion.Circle && i < 2)
					{
						var angle = time * 2 * Math.PI / busy;
						var round = i == 0 ? Math.Sin(angle) : -Math.Cos(angle);
						position = (int)Math.Round(PluginState.Middle + round * PluginState.Middle);
					}
					else if (isEven)
					{
						var sine = Math.Sin(time * 2 * Math.PI / busy);
						if (invert)
							sine = -sine;
						position = (short)RangeF(-1f, 1f, short.MinValue, short.MaxValue, (float)sine) - short.MinValue;
					}
					else
					{
						position = time < half
							? Range(0, half - 1, 0, PluginState.Max, time)
							: Range(half, busy - 1, PluginState.Max, 0, time);
					}
				}
				state.Axes[i] = position;
			}
			if (_steering)
			{
				state.Axes[0] = _steerX;
				state.Axes[1] = _steerY;
			}
			_x = state.Axes[0];
			_y = state.Axes[1];
		}

		/// <summary>Maps a value from one range onto another, held inside the new range, and rounded.</summary>
		static int Range(int oldMin, int oldMax, int newMin, int newMax, int value)
		{
			return (int)Math.Round(RangeF(oldMin, oldMax, newMin, newMax, value), 0);
		}

		static float RangeF(float oldMin, float oldMax, float newMin, float newMax, float value)
		{
			var newValue = newMin + (value - oldMin) * ((newMax - newMin) / (oldMax - oldMin));
			var low = Math.Min(newMin, newMax);
			var high = Math.Max(newMin, newMax);
			return newValue < low ? low : newValue > high ? high : newValue;
		}
	}

	/// <summary>The Test Controller's own control on its tab: a square that shows where X and Y are, and steers them by hand.</summary>
	/// <remarks>
	/// x360ce finds it by its constructor, which takes the device, and places it under the settings. Drawn in the system
	/// colours, so it follows the program's theme.
	/// </remarks>
	public class TestControllerPad : UserControl
	{
		public TestControllerPad(TestController device)
		{
			_device = device;
			Name = "TestControllerPad";
			AccessibleName = "Stick";
			AccessibleDescription = "Shows where X and Y are. Hold the left mouse button in the square to move them by hand; let go and they move by themselves again.";
			DoubleBuffered = true;
			SetMinimumSize();
			_timer = new Timer { Interval = 50 };
			_timer.Tick += (s, e) => Invalidate();
			_timer.Start();
		}

		readonly TestController _device;
		readonly Timer _timer;

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			SetMinimumSize();
		}

		/// <summary>Big enough to aim at, at any zoom.</summary>
		void SetMinimumSize()
		{
			MinimumSize = new Size(Font.Height * 8, Font.Height * 8);
		}

		/// <summary>The dot's radius, from the font, so it scales with the zoom.</summary>
		int Radius { get { return Math.Max(2, Font.Height / 2); } }

		/// <summary>The square, as big as the control allows, at its left, inset by the dot's radius so the dot stays whole at an edge.</summary>
		Rectangle Square
		{
			get
			{
				var radius = Radius;
				var side = Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height) - 1 - radius * 2);
				return new Rectangle(radius, radius, side, side);
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
				Steer(e.Location);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if ((e.Button & MouseButtons.Left) != 0)
				Steer(e.Location);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button == MouseButtons.Left)
				_device.Release();
		}

		void Steer(Point point)
		{
			var square = Square;
			_device.Steer((point.X - square.Left) * PluginState.Max / square.Width, (point.Y - square.Top) * PluginState.Max / square.Height);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var square = Square;
			var middleX = square.Left + square.Width / 2;
			var middleY = square.Top + square.Height / 2;
			using (var pen = new Pen(SystemColors.ControlDark))
			{
				e.Graphics.DrawRectangle(pen, square);
				e.Graphics.DrawLine(pen, middleX, square.Top, middleX, square.Bottom);
				e.Graphics.DrawLine(pen, square.Left, middleY, square.Right, middleY);
			}
			var radius = Radius;
			var x = square.Left + (int)((long)_device.X * square.Width / PluginState.Max);
			var y = square.Top + (int)((long)_device.Y * square.Height / PluginState.Max);
			using (var brush = new SolidBrush(_device.Steering ? SystemColors.Highlight : SystemColors.ControlText))
				e.Graphics.FillEllipse(brush, x - radius, y - radius, radius * 2, radius * 2);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_timer.Dispose();
				_device.Release();
			}
			base.Dispose(disposing);
		}
	}
}
