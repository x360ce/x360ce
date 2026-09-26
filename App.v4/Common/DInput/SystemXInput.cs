using JocysCom.ClassLibrary.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	/// <summary>
	/// Windows' own XInput, asked about the places it has given out.
	/// </summary>
	/// <remarks>
	/// Deliberately separate from the XInput wrapper in this solution. That one exists to load
	/// whichever library the emulator wants a game to see, including this program's own - which is
	/// the opposite of what is wanted here. The question is which places Windows has actually handed
	/// out, and only the library Windows ships can answer it. Asking through the wrapper answers
	/// about the emulation instead, and would agree with itself no matter what was true.
	///
	/// The same distinction matters for sending vibration back to a real controller: it has to reach
	/// the real device, not the one being emulated.
	///
	/// The library is loaded by its full path in the system folder that matches this process. Asked
	/// for by name, Windows looks in the program's own folder first, and a copy of the program placed
	/// in a 32-bit game's folder found that game's 32-bit library and ended the device thread. A
	/// library that cannot be loaded is reported once and answers "no controller" from then on.
	/// </remarks>
	public static class SystemXInput
	{
		delegate int GetStateDelegate(int index, out RawState state);
		delegate int SetStateDelegate(int index, ref RawVibration vibration);

		[StructLayout(LayoutKind.Sequential)]
		struct RawState
		{
			public uint PacketNumber;
			public ushort Buttons;
			public byte LeftTrigger;
			public byte RightTrigger;
			public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct RawVibration
		{
			public ushort LeftMotorSpeed;
			public ushort RightMotorSpeed;
		}

		static readonly object LoadLock = new object();
		static bool _Attempted;
		static IntPtr _Library;
		static GetStateDelegate _GetState;
		static SetStateDelegate _SetState;

		/// <summary>Full path of the library in use, or null while none is loaded.</summary>
		public static string LibraryPath { get; private set; }

		/// <summary>Why the last load failed, or null when the library in use loaded cleanly.</summary>
		public static Exception LoadError { get; private set; }

		/// <summary>
		/// Loads the library at this path, or the system's own when the path is null. Returns false,
		/// and says why in <see cref="LoadError"/>, when the file is missing, of the wrong bitness, or
		/// not an XInput library; the probe then answers "no controller" until a load succeeds.
		/// </summary>
		public static bool Load(string fileName)
		{
			lock (LoadLock)
			{
				_Attempted = true;
				Unload();
				var candidates = fileName == null ? SystemCandidates() : new[] { fileName };
				foreach (var candidate in candidates)
				{
					if (!File.Exists(candidate))
					{
						LoadError = new FileNotFoundException("XInput library not found.", candidate);
						continue;
					}
					Exception error;
					var library = NativeMethods.LoadLibrary(candidate, out error);
					if (library == IntPtr.Zero)
					{
						LoadError = error;
						continue;
					}
					var getState = NativeMethods.GetProcAddress(library, "XInputGetState", out error);
					var setState = getState == IntPtr.Zero
						? IntPtr.Zero
						: NativeMethods.GetProcAddress(library, "XInputSetState", out error);
					if (getState == IntPtr.Zero || setState == IntPtr.Zero)
					{
						LoadError = error;
						NativeMethods.FreeLibrary(library, out error);
						continue;
					}
					_Library = library;
					_GetState = (GetStateDelegate)Marshal.GetDelegateForFunctionPointer(getState, typeof(GetStateDelegate));
					_SetState = (SetStateDelegate)Marshal.GetDelegateForFunctionPointer(setState, typeof(SetStateDelegate));
					LibraryPath = candidate;
					LoadError = null;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// The newest XInput in the system folder matching this process, then the one every
		/// Windows since Vista carries. Both by full path, so the program's own folder is never
		/// searched.
		/// </summary>
		static string[] SystemCandidates()
		{
			var newest = EngineHelper.GetMsXInputLocation().FullName;
			var oldest = Path.Combine(Path.GetDirectoryName(newest), "xinput9_1_0.dll");
			return new[] { newest, oldest };
		}

		static void Unload()
		{
			_GetState = null;
			_SetState = null;
			LibraryPath = null;
			if (_Library != IntPtr.Zero)
			{
				Exception error;
				NativeMethods.FreeLibrary(_Library, out error);
				_Library = IntPtr.Zero;
			}
		}

		/// <summary>Lets go of the library until the next question loads it again.</summary>
		/// <remarks>
		/// XInput keeps open every controller it has answered about for as long as it is loaded, and
		/// Windows cannot cleanly switch off a controller that anything holds open. Nothing may ask
		/// while this runs: a question already under way would call into the library as it goes.
		/// </remarks>
		/// <returns>False when the library was being loaded and did not finish in the time given.</returns>
		public static bool Release(TimeSpan limit)
		{
			if (!System.Threading.Monitor.TryEnter(LoadLock, limit))
				return false;
			try
			{
				Unload();
				_Attempted = false;
			}
			finally
			{
				System.Threading.Monitor.Exit(LoadLock);
			}
			return true;
		}

		/// <summary>
		/// Loads the system library on first use. The lock is taken once, on that first call;
		/// every later call from the device thread reads the delegate and takes nothing.
		/// </summary>
		static void EnsureLoaded()
		{
			if (_Attempted)
				return;
			lock (LoadLock)
			{
				if (_Attempted)
					return;
				if (!Load(null))
					System.Diagnostics.Trace.TraceWarning("XInput not loaded: {0}", LoadError);
			}
		}

		/// <summary>Whether Windows reports a controller in this place. Places count from zero.</summary>
		public static bool IsConnected(int place)
		{
			if (place < 0 || place > 3)
				return false;
			EnsureLoaded();
			var getState = _GetState;
			if (getState == null)
				return false;
			RawState state;
			return getState(place, out state) == 0;
		}

		/// <summary>Sends vibration to a real controller in this place. Places count from zero.</summary>
		/// <remarks>
		/// An Xbox controller offers its motors through XInput and nowhere else - its DirectInput face
		/// declares no force feedback at all - so this is the only way to pass a game's rumble back to
		/// the device somebody is holding.
		/// </remarks>
		public static bool SetVibration(int place, ushort leftMotor, ushort rightMotor)
		{
			if (place < 0 || place > 3)
				return false;
			EnsureLoaded();
			var setState = _SetState;
			if (setState == null)
				return false;
			var vibration = new RawVibration { LeftMotorSpeed = leftMotor, RightMotorSpeed = rightMotor };
			return setState(place, ref vibration) == 0;
		}
	}
}
