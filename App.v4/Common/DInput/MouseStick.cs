using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	/// <summary>How a mouse moves a stick.</summary>
	public enum MouseStyle
	{
		/// <summary>The stick follows how far the mouse has moved, and stays there: steering, a flight stick.</summary>
		Position = 0,
		/// <summary>The stick follows how fast the mouse moves, and returns to the centre when it stops: a camera, aiming.</summary>
		Speed = 1,
		/// <summary>The stick follows where the pointer is on the screen: a mouse yoke.</summary>
		Screen = 2,
	}

	/// <summary>When the mouse moves a stick.</summary>
	public enum MouseActive
	{
		Always = 0,
		/// <summary>While the button is held; let go, the stick returns to the centre.</summary>
		HeldCentre = 1,
		/// <summary>While the button is held; let go, the stick stays where it was.</summary>
		HeldStay = 2,
		/// <summary>The button turns it on and off; off, the stick is at the centre.</summary>
		Toggle = 3,
	}

	/// <summary>The Mouse page's settings for one stick.</summary>
	public sealed class MouseStickSettings
	{
		public MouseStyle Style;
		/// <summary>In percent, across and up or down.</summary>
		public int SensitivityX = 100, SensitivityY = 100;
		public int SmoothingMs = 30;
		public int ReturnMs;
		public MouseActive Active;
		/// <summary>The device's button that <see cref="Active"/> uses, counted from 1, or 0 for none.</summary>
		public int Button;

		/// <summary>The settings of one stick of a preset, or null when every one is at its default.</summary>
		/// <remarks>At their defaults a mouse moves the stick as it did before the Mouse page existed, through the engine's own
		/// reading of the position, so nothing a preset saved before does changes.</remarks>
		/// <param name="right">The right stick, or the left.</param>
		public static MouseStickSettings From(PadSetting ps, bool right)
		{
			if (ps == null)
				return null;
			var style = right ? ps.MouseRightThumbStyle : ps.MouseLeftThumbStyle;
			var x = right ? ps.MouseRightThumbSensitivityX : ps.MouseLeftThumbSensitivityX;
			var y = right ? ps.MouseRightThumbSensitivityY : ps.MouseLeftThumbSensitivityY;
			var smoothing = right ? ps.MouseRightThumbSmoothing : ps.MouseLeftThumbSmoothing;
			var back = right ? ps.MouseRightThumbReturn : ps.MouseLeftThumbReturn;
			var active = right ? ps.MouseRightThumbActive : ps.MouseLeftThumbActive;
			var button = right ? ps.MouseRightThumbButton : ps.MouseLeftThumbButton;
			if (IsDefault(style, SettingName.DefaultMouseStyle) && IsDefault(x, SettingName.DefaultMouseSensitivity)
				&& IsDefault(y, SettingName.DefaultMouseSensitivity) && IsDefault(back, SettingName.DefaultMouseReturn)
				&& IsDefault(active, SettingName.DefaultMouseActive))
				// Smoothing and the button do nothing in Position while it is always active.
				return null;
			return new MouseStickSettings
			{
				Style = (MouseStyle)Clamp(Number(style, SettingName.DefaultMouseStyle), 0, 2),
				SensitivityX = Clamp(Number(x, SettingName.DefaultMouseSensitivity), 1, 400),
				SensitivityY = Clamp(Number(y, SettingName.DefaultMouseSensitivity), 1, 400),
				SmoothingMs = Clamp(Number(smoothing, SettingName.DefaultMouseSmoothing), 0, 500),
				ReturnMs = Clamp(Number(back, SettingName.DefaultMouseReturn), 0, 2000),
				Active = (MouseActive)Clamp(Number(active, SettingName.DefaultMouseActive), 0, 3),
				Button = Clamp(Number(button, SettingName.DefaultMouseButton), 0, 128),
			};
		}

		static bool IsDefault(string value, string defaultValue)
		{
			return string.IsNullOrEmpty(value) || value == defaultValue;
		}

		static int Number(string value, string defaultValue)
		{
			int n;
			return int.TryParse(string.IsNullOrEmpty(value) ? defaultValue : value, out n) ? n : int.Parse(defaultValue);
		}

		static int Clamp(int value, int min, int max)
		{
			return value < min ? min : value > max ? max : value;
		}
	}

	/// <summary>Where the pointer is, and the screen it is on.</summary>
	public struct ScreenPoint
	{
		public int X, Y;
		/// <summary>The centre of the screen the pointer is on.</summary>
		public int CentreX, CentreY;
		/// <summary>Half the height or width of that screen, whichever is less: the distance that is the whole stick at 100 %.</summary>
		public int Half;
	}

	/// <summary>
	/// Turns a control that reports movement, such as a mouse's X and Y, into one stick in the style its controller's Mouse page
	/// sets. One for each stick of each row that sets a style, made with the routing, and used by the engine thread only.
	/// </summary>
	/// <remarks>
	/// It reads the device's running totals of counts, which the engine keeps for every moving control, and keeps its own
	/// position, speed and activation for each axis, so nothing is made on a pass. What it answers is an axis reading, 0 to
	/// 65535 with the centre in the middle, which the stick's own dead zone, anti-dead zone, sensitivity and inversion then
	/// shape as they shape any axis.
	/// </remarks>
	public sealed class MouseStick
	{
		public MouseStick(MouseStickSettings settings)
		{
			Settings = settings;
		}

		public readonly MouseStickSettings Settings;

		/// <summary>Counts from the centre to the end in Position at 100 %, as the engine's own reading of a moving control.</summary>
		public const double PositionCounts = 2048;

		/// <summary>Counts a second that reach the end in Speed at 100 %: a moderate move of an 800 to 1,600 DPI mouse.</summary>
		public const double SpeedCounts = 4096;

		/// <summary>Where the pointer is, read once a pass while a stick is in <see cref="MouseStyle.Screen"/>. A test stands in its own.</summary>
		public static Func<ScreenPoint> ReadScreen = ReadPointer;

		// For each axis, 0 across and 1 up and down.
		readonly double[] _value = new double[2];
		readonly double[] _speed = new double[2];
		readonly int[] _last = new int[2];
		readonly long[] _origin = new long[2];
		readonly long[] _time = new long[2];
		readonly bool[] _seen = new bool[2];
		readonly double[] _anchor = new double[2];

		bool _active;
		bool _started;
		bool _buttonWas;
		ScreenPoint _screen;

		/// <summary>True while the mouse moves the stick.</summary>
		public bool IsActive { get { return _active; } }

		/// <summary>Decides whether the mouse moves the stick this pass, and reads the pointer for Screen. Once a pass, before the axes.</summary>
		/// <param name="state">The device's state this pass, whose button <see cref="MouseStickSettings.Button"/> turns it on and off.</param>
		public void BeginPass(SourceState state)
		{
			var button = Settings.Button;
			var pressed = button > 0 && state != null && button <= state.Buttons.Length && state.Buttons[button - 1];
			bool active;
			switch (Settings.Active)
			{
				case MouseActive.HeldCentre:
				case MouseActive.HeldStay:
					active = pressed;
					break;
				case MouseActive.Toggle:
					active = pressed && !_buttonWas ? !_active : _active;
					break;
				default:
					active = true;
					break;
			}
			_buttonWas = pressed;
			_started = active && !_active;
			_active = active;
			if (Settings.Style == MouseStyle.Screen)
				_screen = ReadScreen();
		}

		/// <summary>The stick on one axis this pass, as an axis reads: 0 to 65535, the centre in the middle.</summary>
		/// <param name="axis">0 across, 1 up and down.</param>
		/// <param name="total">The control's running total of counts.</param>
		/// <param name="origin">When the device last started counting again: a total from before that is not a movement.</param>
		/// <param name="ticks">Now, in <see cref="Stopwatch"/> ticks.</param>
		public ushort Value(int axis, int total, long origin, long ticks)
		{
			// The movement since the last pass. The first reading, and the first after the device was taken again, when the
			// totals may start anywhere, move nothing.
			var delta = _seen[axis] && _origin[axis] == origin ? unchecked(total - _last[axis]) : 0;
			_last[axis] = total;
			_origin[axis] = origin;
			_seen[axis] = true;
			var seconds = _time[axis] == 0 ? 0 : Math.Min(0.1, Math.Max(0, (ticks - _time[axis]) / (double)Stopwatch.Frequency));
			_time[axis] = ticks;
			var scale = (axis == 0 ? Settings.SensitivityX : Settings.SensitivityY) / 100.0;
			var value = _value[axis];
			if (!_active)
			{
				// Let go: the centre, or where it was.
				if (Settings.Active != MouseActive.HeldStay)
					value = 0;
				_speed[axis] = 0;
			}
			else if (Settings.Style == MouseStyle.Speed)
			{
				// A mouse reports in bursts: many counts in one pass, none in the next few. The speed is smoothed as it
				// comes, many times the whole stick in a burst included, so the bursts average to the speed the mouse
				// moves at, and only what reaches the stick is held to its ends.
				if (seconds > 0)
					_speed[axis] = Follow(_speed[axis], delta / seconds * scale / SpeedCounts, Settings.SmoothingMs, seconds);
				var speed = Clamp(_speed[axis]);
				// Back to the centre no faster than it returns, when that is set; up to speed as fast as it smooths.
				value = Settings.ReturnMs > 0 && Math.Abs(speed) < Math.Abs(value) ? Follow(value, speed, Settings.ReturnMs, seconds) : speed;
			}
			else if (Settings.Style == MouseStyle.Screen)
			{
				var pointer = axis == 0 ? _screen.X : _screen.Y;
				// Always on: the centre of the screen. Turned on by a button: wherever the pointer was, as a stick on a
				// phone's screen starts where the finger lands.
				if (_started)
					_anchor[axis] = Settings.Active == MouseActive.Always ? (axis == 0 ? _screen.CentreX : _screen.CentreY) : pointer;
				if (Settings.Active == MouseActive.Always)
					_anchor[axis] = axis == 0 ? _screen.CentreX : _screen.CentreY;
				var target = _screen.Half > 0 ? (pointer - _anchor[axis]) / _screen.Half * scale : 0;
				value = Follow(value, Clamp(target), Settings.SmoothingMs, seconds);
			}
			else
			{
				// Position. Turned on by a button, it starts from the centre, unless it stays where it was let go.
				if (_started && Settings.Active != MouseActive.HeldStay)
					value = 0;
				value = Clamp(value + delta * scale / PositionCounts);
				if (Settings.ReturnMs > 0 && seconds > 0)
					value *= Math.Exp(-seconds * 1000 / Settings.ReturnMs);
			}
			if (Math.Abs(value) < 1e-6)
				value = 0;
			_value[axis] = value;
			return (ushort)Math.Round((value + 1) * 32767.5);
		}

		/// <summary>Moves a value towards a target, a fraction of the way that grows with the time passed against how long it takes.</summary>
		static double Follow(double value, double target, int ms, double seconds)
		{
			if (ms <= 0)
				return target;
			if (seconds <= 0)
				return value;
			return value + (target - value) * (1 - Math.Exp(-seconds * 1000 / ms));
		}

		static double Clamp(double value)
		{
			return value < -1 ? -1 : value > 1 ? 1 : value;
		}

		static ScreenPoint ReadPointer()
		{
			var point = new ScreenPoint();
			NativeMethods.POINT p;
			if (!NativeMethods.GetCursorPos(out p))
				return point;
			point.X = p.X;
			point.Y = p.Y;
			var monitor = NativeMethods.MonitorFromPoint(p, NativeMethods.MONITOR_DEFAULTTONEAREST);
			var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };
			if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
				return point;
			var r = info.rcMonitor;
			point.CentreX = (r.Left + r.Right) / 2;
			point.CentreY = (r.Top + r.Bottom) / 2;
			point.Half = Math.Min(r.Right - r.Left, r.Bottom - r.Top) / 2;
			return point;
		}

		static class NativeMethods
		{
			public const uint MONITOR_DEFAULTTONEAREST = 2;

			[StructLayout(LayoutKind.Sequential)]
			public struct POINT
			{
				public int X, Y;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int Left, Top, Right, Bottom;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct MONITORINFO
			{
				public int cbSize;
				public RECT rcMonitor;
				public RECT rcWork;
				public uint dwFlags;
			}

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool GetCursorPos(out POINT point);

			[DllImport("user32.dll")]
			public static extern IntPtr MonitorFromPoint(POINT point, uint flags);

			[DllImport("user32.dll", CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
		}
	}
}
