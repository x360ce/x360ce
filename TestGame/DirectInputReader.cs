using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.DirectInput;

namespace x360ce.TestGame
{
	/// <summary>
	/// Every game controller as DirectInput reports it, read in the background and shared with other programs, as many
	/// games read them, and the keyboard and mouse too when asked. Windows itself keeps some devices quiet while the window
	/// is behind: an Xbox controller read through DirectInput reports only to the window in front.
	/// </summary>
	/// <remarks>
	/// DirectInput reads the keyboard and mouse through Raw Input under the hood, and Windows lets a program register one
	/// window per kind of device for Raw Input, so DirectInput's keyboard and mouse take Raw Input's place in this program
	/// while they are open. That is why they are off unless asked for. Made, read and let go of on one thread, the poller's.
	/// </remarks>
	public sealed class DirectInputReader : IDisposable
	{
		/// <summary>How often the list of game controllers is looked at again, for one plugged in or taken away.</summary>
		const int EnumerateEveryMs = 3000;
		/// <summary>How far an axis moves before it is an event: one hundredth of DirectInput's 0 to 65535.</summary>
		const int AxisStep = 655;
		const int AxisEveryMs = 50;
		/// <summary>How long the mouse's moves are added together before they are one event.</summary>
		const int MouseMoveMs = 100;

		static readonly string[] AxisNames = { "X", "Y", "Z", "RotationX", "RotationY", "RotationZ", "Slider1", "Slider2" };
		static readonly string[] MouseButtonNames = { "LeftButton", "RightButton", "MiddleButton", "XButton1", "XButton2", "Button6", "Button7", "Button8" };

		sealed class Pad
		{
			public Joystick Device;
			public Guid Instance;
			public string Name;
			public JoystickState Last;
			public int[] SentAxes = new int[8];
			public long[] SentAt = new long[8];
		}

		readonly List<Pad> _pads = new List<Pad>();
		readonly HashSet<Guid> _refused = new HashSet<Guid>();
		DirectInput _directInput;
		IntPtr _window;
		Keyboard _keyboard;
		Mouse _mouse;
		HashSet<Key> _keys = new HashSet<Key>();
		bool[] _mouseButtons = new bool[8];
		int _moveX, _moveY;
		long _moveSince;
		long _enumeratedAt = -EnumerateEveryMs;

		/// <summary>Whether the keyboard and mouse are to be read too, taking Raw Input's place. Set from any thread; applied at the next read.</summary>
		public volatile bool KeyboardAndMouse;

		/// <summary>Called on the reading thread after the keyboard and mouse were opened or let go of, with whether they are open now.</summary>
		public Action<bool> KeyboardAndMouseChanged;

		/// <summary>The game controllers open now, by the names their events carry.</summary>
		public string[] Devices { get; private set; } = new string[0];

		/// <summary>Why something could not be opened, or null.</summary>
		public string LastError { get; private set; }

		public void Open(IntPtr window)
		{
			_window = window;
			_directInput = new DirectInput();
		}

		Device Acquire(Device device, string name)
		{
			try
			{
				device.SetCooperativeLevel(_window, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
				device.Acquire();
				return device;
			}
			catch (SharpDXException ex)
			{
				LastError = "DirectInput could not open " + name + ": " + ex.Message;
				device.Dispose();
				return null;
			}
		}

		/// <summary>Reads every device once, and returns what changed as events, each with the time it was read.</summary>
		public List<InputEvent> Poll(Func<long> now, bool inFront)
		{
			var events = new List<InputEvent>();
			if (_directInput == null)
				return events;
			var open = _keyboard != null || _mouse != null;
			if (KeyboardAndMouse != open)
				SwitchKeyboardAndMouse(KeyboardAndMouse);
			if (now() - _enumeratedAt >= EnumerateEveryMs)
			{
				_enumeratedAt = now();
				Enumerate(events);
			}
			ReadKeyboard(events, now);
			ReadMouse(events, now);
			foreach (var pad in _pads.ToArray())
				ReadPad(pad, events, now);
			foreach (var e in events)
			{
				e.Api = "DirectInput";
				e.InFront = inFront;
			}
			return events;
		}

		void SwitchKeyboardAndMouse(bool on)
		{
			if (on)
			{
				_keyboard = (Keyboard)Acquire(new Keyboard(_directInput), "the keyboard");
				_mouse = (Mouse)Acquire(new Mouse(_directInput), "the mouse");
			}
			else
			{
				_keyboard?.Dispose();
				_mouse?.Dispose();
				_keyboard = null;
				_mouse = null;
				_keys.Clear();
				_mouseButtons = new bool[8];
			}
			KeyboardAndMouseChanged?.Invoke(on);
		}

		void Enumerate(List<InputEvent> events)
		{
			IList<DeviceInstance> found;
			try { found = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly); }
			catch (SharpDXException) { return; }
			foreach (var instance in found)
			{
				// One that would not open is not asked again each look; a controller plugged in again has a new chance.
				if (_pads.Any(p => p.Instance == instance.InstanceGuid) || _refused.Contains(instance.InstanceGuid))
					continue;
				var joystick = new Joystick(_directInput, instance.InstanceGuid);
				if (Acquire(joystick, instance.InstanceName.Trim()) == null)
				{
					_refused.Add(instance.InstanceGuid);
					continue;
				}
				var name = instance.InstanceName.Trim();
				// Two devices of one kind are told apart by a number, as the controller tabs do.
				var same = _pads.Count(p => p.Name == name || p.Name.StartsWith(name + " ", StringComparison.Ordinal));
				if (same > 0)
					name += " " + (same + 1);
				_pads.Add(new Pad { Device = joystick, Instance = instance.InstanceGuid, Name = name });
				events.Add(new InputEvent { Source = name, Kind = "Connected", Name = "Device", Ms = 0 });
			}
			_refused.IntersectWith(found.Select(x => x.InstanceGuid));
			Devices = _pads.Select(p => p.Name).ToArray();
		}

		void ReadKeyboard(List<InputEvent> events, Func<long> now)
		{
			if (_keyboard == null)
				return;
			HashSet<Key> keys;
			try { keys = new HashSet<Key>(_keyboard.GetCurrentState().PressedKeys); }
			catch (SharpDXException) { return; }
			var at = now();
			// DirectInput names its keys as Windows Forms does where it matters: Space, Return, Up, Left.
			foreach (var k in keys.Except(_keys))
				events.Add(new InputEvent { Source = "Keyboard", Kind = "Down", Name = k.ToString(), Ms = at });
			foreach (var k in _keys.Except(keys))
				events.Add(new InputEvent { Source = "Keyboard", Kind = "Up", Name = k.ToString(), Ms = at });
			_keys = keys;
		}

		void ReadMouse(List<InputEvent> events, Func<long> now)
		{
			if (_mouse == null)
				return;
			MouseState state;
			try { state = _mouse.GetCurrentState(); }
			catch (SharpDXException) { return; }
			var at = now();
			if (state.X != 0 || state.Y != 0)
			{
				if (_moveX == 0 && _moveY == 0)
					_moveSince = at;
				_moveX += state.X;
				_moveY += state.Y;
			}
			if ((_moveX != 0 || _moveY != 0) && at - _moveSince >= MouseMoveMs)
			{
				events.Add(new InputEvent { Source = "Mouse", Kind = "Move", Name = "Move", X = _moveX, Y = _moveY, Ms = _moveSince });
				_moveX = _moveY = 0;
			}
			for (var i = 0; i < Math.Min(MouseButtonNames.Length, state.Buttons.Length); i++)
			{
				if (state.Buttons[i] != _mouseButtons[i])
					events.Add(new InputEvent { Source = "Mouse", Kind = state.Buttons[i] ? "Down" : "Up", Name = MouseButtonNames[i], Ms = at });
			}
			_mouseButtons = state.Buttons.ToArray();
			if (state.Z != 0)
				events.Add(new InputEvent { Source = "Mouse", Kind = "Wheel", Name = "Wheel", Value = state.Z, Ms = at });
		}

		void ReadPad(Pad pad, List<InputEvent> events, Func<long> now)
		{
			JoystickState state;
			try
			{
				pad.Device.Poll();
				state = pad.Device.GetCurrentState();
			}
			catch (SharpDXException)
			{
				// Taken away, or its driver went: found again by the next look if it comes back.
				events.Add(new InputEvent { Source = pad.Name, Kind = "Disconnected", Name = "Device", Ms = now() });
				pad.Device.Dispose();
				_pads.Remove(pad);
				Devices = _pads.Select(p => p.Name).ToArray();
				return;
			}
			var at = now();
			var last = pad.Last;
			if (last != null)
			{
				for (var i = 0; i < state.Buttons.Length; i++)
				{
					if (state.Buttons[i] != last.Buttons[i])
						events.Add(new InputEvent { Source = pad.Name, Kind = state.Buttons[i] ? "Down" : "Up", Name = "b" + (i + 1), Ms = at });
				}
				for (var i = 0; i < state.PointOfViewControllers.Length; i++)
				{
					if (state.PointOfViewControllers[i] != last.PointOfViewControllers[i])
						events.Add(new InputEvent { Source = pad.Name, Kind = "Hat", Name = "p" + (i + 1), Value = state.PointOfViewControllers[i], Ms = at });
				}
			}
			var axes = new[] { state.X, state.Y, state.Z, state.RotationX, state.RotationY, state.RotationZ, state.Sliders[0], state.Sliders[1] };
			for (var a = 0; a < axes.Length; a++)
			{
				if (last == null)
				{
					pad.SentAxes[a] = axes[a];
					continue;
				}
				if (Math.Abs(axes[a] - pad.SentAxes[a]) >= AxisStep && at - pad.SentAt[a] >= AxisEveryMs)
				{
					pad.SentAxes[a] = axes[a];
					pad.SentAt[a] = at;
					events.Add(new InputEvent { Source = pad.Name, Kind = "Axis", Name = AxisNames[a], Value = axes[a], Ms = at });
				}
			}
			pad.Last = state;
		}

		public void Dispose()
		{
			foreach (var pad in _pads)
				pad.Device.Dispose();
			_pads.Clear();
			_keyboard?.Dispose();
			_mouse?.Dispose();
			_directInput?.Dispose();
		}
	}
}
