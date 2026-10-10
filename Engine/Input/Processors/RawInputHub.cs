using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Engine
{
	/// <summary>Receives the reports of every Raw Input game controller on a thread of its own and keeps each one's newest state for the engine.</summary>
	/// <remarks>
	/// <para>
	/// Windows delivers a top-level collection's input to one window in a process, the one registered for it last, so
	/// the hub is the only code in the program that registers Raw Input. Its thread owns a message-only window,
	/// registered for joysticks (1:04), gamepads (1:05), multi-axis controllers (1:08) and the vendor pages of
	/// <see cref="RawInputDescription.Known"/> with RIDEV_INPUTSINK, which
	/// delivers input while another program is in front, and RIDEV_DEVNOTIFY, which reports each device arriving and
	/// leaving, those attached at registration included.
	/// </para>
	/// <para>
	/// The window is a WinForms <see cref="NativeWindow"/>, which registers the window class, keeps the window
	/// procedure alive and catches an exception thrown in it; the hub keeps that exception in <see cref="Error"/>. The
	/// loop is GetMessage and DispatchMessage, so the thread carries none of Application.Run's OLE, idle or
	/// Application.ThreadException handling.
	/// </para>
	/// <para>
	/// Threads: <see cref="Start"/> and <see cref="Stop"/> from any thread but the hub's. <see cref="TryCopyState"/>
	/// from one thread, the engine's. <see cref="SetLayout"/>, <see cref="Devices"/> and the diagnostics from any.
	/// <see cref="Add"/>, <see cref="Remove"/> and <see cref="ReadInput(byte[])"/> are the hub thread's own steps; while the
	/// hub is stopped, one other thread may stand in for it, as the tests do.
	/// </para>
	/// </remarks>
	public sealed class RawInputHub : IDisposable
	{
		const ushort GenericDesktopPage = 0x01;

		/// <summary>Joystick, gamepad and multi-axis controller: the top-level collections <see cref="RawInputDevice.GetGameControllers"/> lists.</summary>
		static readonly ushort[] Usages = { 0x04, 0x05, 0x08 };

		/// <summary>Posted by <see cref="Stop"/>: ends the loop.</summary>
		const int StopMessage = RawInputNative.WM_APP + 1;

		/// <summary>Posted by <see cref="SetLayout"/>: the hub thread takes each device's pending layout.</summary>
		const int LayoutMessage = RawInputNative.WM_APP + 2;

		readonly object startLock = new object();
		Thread thread;
		IntPtr windowHandle;
		Exception error;
		Exception startFailure;

		/// <summary>Each RAWINPUT is copied here; it grows when a larger one arrives. Hub thread only.</summary>
		byte[] input = new byte[256];

		/// <summary>The devices by Raw Input handle, as <see cref="IntPtr.ToInt64"/>. Hub thread only.</summary>
		readonly Dictionary<long, RawInputHubDevice> byHandle = new Dictionary<long, RawInputHubDevice>();

		/// <summary>The devices by instance GUID. Replaced, never changed, by the hub thread when a device arrives or leaves.</summary>
		Dictionary<Guid, RawInputHubDevice> devices = new Dictionary<Guid, RawInputHubDevice>();

		/// <summary>The last layout <see cref="SetLayout"/> gave each device, by instance GUID, kept after the device leaves and while the hub is stopped.</summary>
		/// <remarks>Written by <see cref="SetLayout"/> on any thread and read by <see cref="Add"/> on the hub thread, under <see cref="knownLayoutsLock"/>; the engine never touches it.</remarks>
		readonly Dictionary<Guid, RawInputLayout> knownLayouts = new Dictionary<Guid, RawInputLayout>();
		readonly object knownLayoutsLock = new object();

		/// <summary>Raised on the hub thread after a device arrives or leaves.</summary>
		/// <remarks>
		/// A handler must return at once, scheduling its work rather than doing it: while it runs the hub reads no
		/// reports, and a handler that waits for a thread that is calling <see cref="Stop"/> never returns.
		/// </remarks>
		public event EventHandler DevicesChanged;

		/// <summary>The devices the hub reads, by <see cref="RawInputDevice.InstanceGuid"/>. Any thread.</summary>
		/// <remarks>A snapshot that never changes: an arrival or a removal makes a new one. Read it once and keep it for the whole of a look-up.</remarks>
		public IReadOnlyDictionary<Guid, RawInputHubDevice> Devices { get { return Volatile.Read(ref devices); } }

		/// <summary>True while the hub thread runs.</summary>
		public bool IsRunning
		{
			get
			{
				var t = Volatile.Read(ref thread);
				return t != null && t.IsAlive;
			}
		}

		/// <summary>The hub's message-only window while it runs; zero otherwise.</summary>
		public IntPtr WindowHandle { get { return Volatile.Read(ref windowHandle); } }

		/// <summary>The last failure: Windows refusing the window or the registration, an exception thrown while handling a message, or the loop ending on an error. Null when there is none. <see cref="Start"/> clears it.</summary>
		public Exception Error { get { return Volatile.Read(ref error); } }

		#region Start and stop

		/// <summary>Starts the hub thread and returns once its window is registered for Raw Input. Does nothing while the hub runs.</summary>
		/// <exception cref="Win32Exception">Windows refused the window or the registration. The hub stays stopped, and <see cref="Error"/> holds the same exception.</exception>
		public void Start()
		{
			lock (startLock)
			{
				if (thread != null && thread.IsAlive)
					return;
				thread = null;
				Volatile.Write(ref error, null);
				startFailure = null;
				// Not disposed: the hub thread may still be inside Set when Wait returns.
				var ready = new ManualResetEventSlim(false);
				var t = new Thread(Run) { IsBackground = true, Name = "x360ce Raw Input" };
				t.Start(ready);
				ready.Wait();
				var failure = Volatile.Read(ref startFailure);
				if (failure != null)
				{
					t.Join();
					ExceptionDispatchInfo.Capture(failure).Throw();
				}
				Volatile.Write(ref thread, t);
			}
		}

		/// <summary>Stops the hub: unregisters, destroys the window, ends the loop, waits for the thread to end and disposes the devices. Does nothing while the hub is stopped, apart from disposing the devices a stand-in thread added.</summary>
		/// <exception cref="InvalidOperationException">Called on the hub thread, which cannot wait for itself.</exception>
		/// <exception cref="Win32Exception">Windows did not take the message that stops the hub; it keeps running.</exception>
		public void Stop()
		{
			lock (startLock)
			{
				var t = thread;
				if (t == null)
				{
					RemoveAll();
					return;
				}
				if (t == Thread.CurrentThread)
					throw new InvalidOperationException("The Raw Input hub cannot be stopped from its own thread.");
				var hwnd = Volatile.Read(ref windowHandle);
				if (hwnd != IntPtr.Zero && !RawInputNative.PostMessage(hwnd, StopMessage, IntPtr.Zero, IntPtr.Zero))
				{
					var code = Marshal.GetLastWin32Error();
					// A window gone by now belongs to a loop that has ended on its own, so there is a thread to wait for.
					if (Volatile.Read(ref windowHandle) != IntPtr.Zero)
						throw new Win32Exception(code);
				}
				t.Join();
				Volatile.Write(ref thread, null);
			}
		}

		public void Dispose()
		{
			Stop();
		}

		/// <summary>The hub thread: makes and registers the window, signals <see cref="Start"/>, then runs the loop until <see cref="Stop"/>.</summary>
		void Run(object state)
		{
			var ready = (ManualResetEventSlim)state;
			var window = new HubWindow(this);
			var registered = false;
			try
			{
				try
				{
					window.CreateHandle(new CreateParams { Caption = "x360ce Raw Input", Parent = RawInputNative.HWND_MESSAGE });
					if (!Register(window.Handle, RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY))
						throw new Win32Exception(Marshal.GetLastWin32Error());
					registered = true;
				}
				catch (Exception ex)
				{
					// The thread's boundary: Start throws it on the caller's thread, with this stack.
					Volatile.Write(ref error, ex);
					Volatile.Write(ref startFailure, ex);
					return;
				}
				Volatile.Write(ref windowHandle, window.Handle);
				ready.Set();
				RawInputNative.MSG msg;
				int result;
				while ((result = RawInputNative.GetMessage(out msg, IntPtr.Zero, 0, 0)) > 0)
					RawInputNative.DispatchMessage(ref msg);
				if (result < 0)
					Volatile.Write(ref error, new Win32Exception(Marshal.GetLastWin32Error()));
			}
			finally
			{
				Volatile.Write(ref windowHandle, IntPtr.Zero);
				if (registered && !Register(IntPtr.Zero, RawInputNative.RIDEV_REMOVE))
					Volatile.Write(ref error, new Win32Exception(Marshal.GetLastWin32Error()));
				if (window.Handle != IntPtr.Zero)
					window.DestroyHandle();
				RemoveAll();
				ready.Set();
			}
		}

		/// <summary>Registers, or with <see cref="RawInputNative.RIDEV_REMOVE"/> and no window unregisters, the game controller usages.</summary>
		/// <remarks>
		/// A device of <see cref="RawInputDescription.Known"/> reports on its vendor's page, whose usage Windows will not
		/// take alone, so the page is registered whole; what the other collections on it send is ignored, as input from
		/// any device the hub does not read is.
		/// </remarks>
		static bool Register(IntPtr hwnd, uint flags)
		{
			var list = new List<RawInputNative.RAWINPUTDEVICE>();
			foreach (var usage in Usages)
				list.Add(new RawInputNative.RAWINPUTDEVICE { usUsagePage = GenericDesktopPage, usUsage = usage, dwFlags = flags, hwndTarget = hwnd });
			foreach (var description in RawInputDescription.Known)
				if (!list.Exists(x => x.usUsagePage == description.UsagePage))
					list.Add(new RawInputNative.RAWINPUTDEVICE { usUsagePage = (ushort)description.UsagePage, dwFlags = flags | RawInputNative.RIDEV_PAGEONLY, hwndTarget = hwnd });
			var array = list.ToArray();
			return RawInputNative.RegisterRawInputDevices(array, (uint)array.Length, (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICE)));
		}

		/// <summary>The hub's message-only window: hands each message to the hub, on the hub thread.</summary>
		sealed class HubWindow : NativeWindow
		{
			readonly RawInputHub hub;

			public HubWindow(RawInputHub hub)
			{
				this.hub = hub;
			}

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case RawInputNative.WM_INPUT:
						hub.ReadInput(m.LParam);
						break;
					case RawInputNative.WM_INPUT_DEVICE_CHANGE:
						if (m.WParam.ToInt64() == RawInputNative.GIDC_ARRIVAL)
							hub.OnArrival(m.LParam);
						else if (m.WParam.ToInt64() == RawInputNative.GIDC_REMOVAL)
							hub.OnRemoval(m.LParam);
						break;
					case LayoutMessage:
						hub.ApplyLayouts();
						return;
					case StopMessage:
						RawInputNative.PostQuitMessage(0);
						return;
				}
				// WM_INPUT needs DefWindowProc, which frees what Windows keeps for the input.
				base.WndProc(ref m);
			}

			/// <summary>Keeps an exception thrown while a message was handled, which NativeWindow catches, where the program sees it.</summary>
			protected override void OnThreadException(Exception e)
			{
				Volatile.Write(ref hub.error, e);
			}
		}

		#endregion

		#region Hub thread

		/// <summary>Starts reading a device that arrived, when it is a HID game controller the hub does not read yet.</summary>
		void OnArrival(IntPtr handle)
		{
			if (byHandle.ContainsKey(handle.ToInt64()))
				return;
			var device = RawInputDevice.FromHandle(handle);
			if (device == null)
				return;
			Add(device);
			DevicesChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Drops a device that left. Its handle is no longer valid, so nothing asks Windows about it.</summary>
		void OnRemoval(IntPtr handle)
		{
			if (Remove(handle))
				DevicesChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Starts reading <paramref name="device"/>, at rest, with its controls where the last layout <see cref="SetLayout"/> gave it puts them, or where their usages put them when it had none. Hub thread only.</summary>
		/// <remarks>
		/// <para>
		/// A device that arrives again, after a reconnect or while the hub was stopped, reads by its last layout from its
		/// first report on, so it never reads in the wrong slots while the device list finds its twin again. A layout made
		/// for other controls (see <see cref="RawInputLayout.Fits"/>) is not used.
		/// </para>
		/// <para>
		/// The hub owns the device from here and disposes it. A device read under the same instance GUID, whose removal
		/// Windows did not report, or under the same handle is dropped first.
		/// </para>
		/// </remarks>
		public RawInputHubDevice Add(RawInputDevice device)
		{
			RawInputHubDevice old;
			if (devices.TryGetValue(device.InstanceGuid, out old))
				Remove(old.Device.Handle);
			Remove(device.Handle);
			RawInputLayout known;
			lock (knownLayoutsLock)
				knownLayouts.TryGetValue(device.InstanceGuid, out known);
			var entry = new RawInputHubDevice(device, known != null && known.Fits(device.Controls) ? known : null);
			byHandle[device.Handle.ToInt64()] = entry;
			var next = new Dictionary<Guid, RawInputHubDevice>(devices);
			next[device.InstanceGuid] = entry;
			Volatile.Write(ref devices, next);
			return entry;
		}

		/// <summary>Stops reading the device with <paramref name="handle"/> and disposes it. Hub thread only.</summary>
		/// <returns>False when the hub reads no device with that handle.</returns>
		public bool Remove(IntPtr handle)
		{
			var key = handle.ToInt64();
			RawInputHubDevice entry;
			if (!byHandle.TryGetValue(key, out entry))
				return false;
			byHandle.Remove(key);
			var next = new Dictionary<Guid, RawInputHubDevice>(devices);
			next.Remove(entry.Device.InstanceGuid);
			Volatile.Write(ref devices, next);
			entry.Device.Dispose();
			return true;
		}

		/// <summary>Stops reading every device and disposes them. The hub thread's, or a stand-in's while no hub thread runs.</summary>
		void RemoveAll()
		{
			Volatile.Write(ref devices, new Dictionary<Guid, RawInputHubDevice>());
			foreach (var entry in byHandle.Values)
				entry.Device.Dispose();
			byHandle.Clear();
		}

		void ApplyLayouts()
		{
			foreach (var entry in byHandle.Values)
				entry.ApplyLayout();
		}

		/// <summary>Copies the RAWINPUT a WM_INPUT carries into the hub's buffer and reads it.</summary>
		void ReadInput(IntPtr rawInput)
		{
			var header = (uint)RawInputNative.RAWINPUTHEADER_Size;
			uint size = 0;
			if (RawInputNative.GetRawInputData(rawInput, RawInputNative.RID_INPUT, null, ref size, header) != 0 || size == 0)
				return;
			if (size > input.Length)
				input = new byte[size];
			size = (uint)input.Length;
			if (RawInputNative.GetRawInputData(rawInput, RawInputNative.RID_INPUT, input, ref size, header) == uint.MaxValue)
				return;
			ReadInput(input);
		}

		/// <summary>Reads one RAWINPUT into the state of the device it came from and hands that state to the engine. Hub thread only.</summary>
		/// <remarks>Input from a device the hub does not read is ignored. Makes nothing, takes no lock and throws nothing.</remarks>
		/// <param name="rawInput">A RAWINPUT as GetRawInputData copies it; the header's hDevice names the device.</param>
		/// <returns>The number of reports read.</returns>
		public int ReadInput(byte[] rawInput)
		{
			if (rawInput == null || rawInput.Length < RawInputNative.RAWINPUTHEADER_Size)
				return 0;
			// hDevice follows dwType and dwSize.
			var handle = IntPtr.Size == 8 ? BitConverter.ToInt64(rawInput, 8) : BitConverter.ToInt32(rawInput, 8);
			RawInputHubDevice entry;
			if (!byHandle.TryGetValue(handle, out entry))
				return 0;
			return entry.Read(rawInput);
		}

		#endregion

		#region Any thread

		/// <summary>Asks for the controls of the device with <paramref name="instanceGuid"/> to go where <paramref name="layout"/> puts them. Any thread.</summary>
		/// <remarks>
		/// The hub thread takes the layout as soon as it handles the message this posts, and before the device's next
		/// report at the latest, and starts the device's state again at rest, unless the layout puts every control where
		/// it is already read. The layout is kept for the device: one that leaves and comes back starts with it again
		/// (see <see cref="Add"/>), and <see cref="DevicesChanged"/> says it came back.
		/// </remarks>
		/// <param name="layout">Made from the device's own <see cref="RawInputDevice.Controls"/>.</param>
		/// <returns>False when the hub reads no device with that GUID.</returns>
		public bool SetLayout(Guid instanceGuid, RawInputLayout layout)
		{
			lock (knownLayoutsLock)
				knownLayouts[instanceGuid] = layout;
			RawInputHubDevice entry;
			if (!Volatile.Read(ref devices).TryGetValue(instanceGuid, out entry))
				return false;
			entry.SetLayout(layout);
			var hwnd = Volatile.Read(ref windowHandle);
			if (hwnd != IntPtr.Zero)
				RawInputNative.PostMessage(hwnd, LayoutMessage, IntPtr.Zero, IntPtr.Zero);
			return true;
		}

		/// <summary>Copies the newest state of the device with <paramref name="instanceGuid"/> into <paramref name="into"/>. One thread only, the engine's.</summary>
		/// <remarks>
		/// Makes nothing, takes no lock and never waits for the hub thread: one read of the device snapshot, one
		/// dictionary look-up, at most one index swap, and four array copies. A device no report has reached yet reads at
		/// rest, as DirectInput reads a control a device lacks.
		/// </remarks>
		/// <param name="fresh">True when the hub has published a state for the device since the last copy: a report, or a new layout.</param>
		/// <returns>False when the hub reads no device with that GUID, which leaves <paramref name="into"/> as it was.</returns>
		public bool TryCopyState(Guid instanceGuid, SourceState into, out bool fresh)
		{
			RawInputHubDevice entry;
			if (!Volatile.Read(ref devices).TryGetValue(instanceGuid, out entry))
			{
				fresh = false;
				return false;
			}
			fresh = entry.CopyState(into);
			return true;
		}

		#endregion
	}
}
