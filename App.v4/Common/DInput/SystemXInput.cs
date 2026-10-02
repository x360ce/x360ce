using JocysCom.ClassLibrary.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
	/// library that cannot be loaded is reported once and answers "no controller" from then on. The
	/// one exception is a read of the places that throws, as a load does when the system folder could
	/// not be listed: that read answers as XInput not answering, and the next read loads again.
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

		/// <summary>What XInputGetCapabilitiesEx, the library's export number 108, answers: the controller's identity.</summary>
		[StructLayout(LayoutKind.Sequential)]
		struct RawCapabilitiesEx
		{
			public byte Type, SubType;
			public ushort Flags;
			public ushort Buttons;
			public byte LeftTrigger, RightTrigger;
			public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
			public ushort LeftMotor, RightMotor;
			public ushort VendorId, ProductId, VersionNumber, Unknown;
			public uint Unknown2;
		}

		delegate int GetCapabilitiesExDelegate(int one, int index, int flags, out RawCapabilitiesEx caps);

		/// <summary>The export is not named in the library, so it is found by its number.</summary>
		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
		static extern IntPtr GetProcAddressByNumber(IntPtr module, IntPtr number);

		const int CapabilitiesExNumber = 108;

		static readonly object LoadLock = new object();

		/// <summary>Above 0 while a thread holds <see cref="LoadLock"/> or waits for it, so the input thread passes it by without trying it.</summary>
		static int _loadLockBusy;

		/// <summary>Takes <see cref="LoadLock"/> within a limit, saying so to the input thread. Every holder takes it this way.</summary>
		static bool EnterLoadLock(TimeSpan limit)
		{
			return DInputHelper.EnterPublished(LoadLock, ref _loadLockBusy, limit);
		}

		/// <summary>Lets go of <see cref="LoadLock"/>.</summary>
		static void ExitLoadLock()
		{
			DInputHelper.ExitPublished(LoadLock, ref _loadLockBusy);
		}

		static bool _Attempted;

		static IntPtr _Library;
		static GetStateDelegate _GetState;
		static SetStateDelegate _SetState;
		/// <summary>Null when the library has no such export, as the oldest one has not.</summary>
		static GetCapabilitiesExDelegate _GetCapabilitiesEx;

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
			EnterLoadLock(Timeout.InfiniteTimeSpan);
			try
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
					var capabilitiesEx = GetProcAddressByNumber(library, (IntPtr)CapabilitiesExNumber);
					_GetCapabilitiesEx = capabilitiesEx == IntPtr.Zero ? null
						: (GetCapabilitiesExDelegate)Marshal.GetDelegateForFunctionPointer(capabilitiesEx, typeof(GetCapabilitiesExDelegate));
					LibraryPath = candidate;
					LoadError = null;
					return true;
				}
				return false;
			}
			finally
			{
				ExitLoadLock();
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
			_GetCapabilitiesEx = null;
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
		/// Windows cannot cleanly switch off a controller that anything holds open, nor give its place
		/// to another controller while a program still holds the one that went. XInput also keeps a
		/// reference to itself, so letting go of the ones taken here leaves it loaded, with every
		/// controller still open. Its own is let go of as well, which unloads it.
		/// Nothing may ask while this runs: a question already under way would call into the library as it goes.
		/// A read of the places or a send of vibration under way holds the same lock, so this gives up while one has not
		/// come back.
		/// </remarks>
		/// <returns>False when the library was being loaded and did not finish in the time given.</returns>
		public static bool Release(TimeSpan limit)
		{
			if (!EnterLoadLock(limit))
				return false;
			try
			{
				Unload();
				_Attempted = false;
				UnloadOwnReference();
			}
			finally
			{
				ExitLoadLock();
			}
			return true;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr GetModuleHandle(string moduleName);

		/// <summary>Lets go of the one reference the system library keeps to itself, which unloads it when nothing else holds it.</summary>
		/// <remarks>
		/// Only one, so a reference somebody else in this process holds keeps the library loaded for them. Asked for by
		/// full path, so a copy of the library this program loads for a game from another folder is left alone. Called
		/// with <see cref="LoadLock"/> held, after every reference this program took has been let go of, the one
		/// <see cref="SharpDX.XInput.Controller"/> took included.
		/// </remarks>
		static void UnloadOwnReference()
		{
			string path;
			try { path = SystemCandidates()[0]; }
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { return; }
			var module = GetModuleHandle(path);
			if (module == IntPtr.Zero)
				return;
			Exception error;
			NativeMethods.FreeLibrary(module, out error);
		}

		/// <summary>
		/// Loads the system library on first use. The lock is taken once, on that first call;
		/// every later call reads one field and takes nothing.
		/// </summary>
		static void EnsureLoaded()
		{
			if (_Attempted)
				return;
			EnterLoadLock(Timeout.InfiniteTimeSpan);
			try
			{
				if (_Attempted)
					return;
				if (!Load(null))
					System.Diagnostics.Trace.TraceWarning("XInput not loaded: {0}", LoadError);
			}
			finally
			{
				ExitLoadLock();
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

		/// <summary>The product number of the controller in this place, or 0 when there is none or the library cannot say.</summary>
		/// <remarks>Called by the places reader, under <see cref="LoadLock"/>, for a place that answered as connected.</remarks>
		static ushort ProductOf(int place)
		{
			var capabilities = _GetCapabilitiesEx;
			RawCapabilitiesEx caps;
			return capabilities != null && capabilities(1, place, 0, out caps) == 0 ? caps.ProductId : (ushort)0;
		}

		/// <summary>Sends vibration to a real controller in this place. Places count from zero.</summary>
		/// <remarks>
		/// An Xbox controller offers its motors through XInput and nowhere else - its DirectInput face
		/// declares no force feedback at all - so this is the only way to pass a game's rumble back to
		/// the device somebody is holding.
		///
		/// Sent under <see cref="LoadLock"/>, so the library is never let go of under a send. The lock is taken within
		/// <paramref name="limit"/>: a read of the places that has not come back holds it, and nothing waits for that past
		/// the limit. The input thread passes zero and sends on a later pass. With zero the lock is not even tried while
		/// another thread holds it, and the library is not loaded: the places reader loads it.
		/// </remarks>
		/// <param name="limit">How long to wait for the library to be free.</param>
		/// <returns>What the driver answered; true for a place outside 0-3; null when the library was not free, or, for a zero limit, not loaded yet, and nothing was sent.</returns>
		public static bool? SetVibration(int place, ushort leftMotor, ushort rightMotor, TimeSpan limit)
		{
			if (place < 0 || place > 3)
				return true;
			// Before the load, which takes the same lock while nothing is loaded. The input thread never waits for it, and
			// does not try it while another thread holds it or waits for it.
			var noWait = limit == TimeSpan.Zero;
			if (noWait ? !DInputHelper.TryEnterWhenFree(LoadLock, ref _loadLockBusy) : !EnterLoadLock(limit))
				return null;
			try
			{
				// Loading lists the system folder, which can throw; the places reader loads it and catches that.
				if (noWait && !_Attempted)
					return null;
				EnsureLoaded();
				var setState = _SetState;
				if (setState == null)
					return false;
				var vibration = new RawVibration { LeftMotorSpeed = leftMotor, RightMotorSpeed = rightMotor };
				return setState(place, ref vibration) == 0;
			}
			finally
			{
				ExitLoadLock();
			}
		}

		#region Places read with a limit

		/// <summary>One question about the places at a time.</summary>
		static readonly object PlacesAskLock = new object();

		/// <summary>Wakes the places reader for one read.</summary>
		static readonly AutoResetEvent PlacesAsked = new AutoResetEvent(false);

		/// <summary>Set while no read is under way: the last one has come back.</summary>
		static readonly ManualResetEvent PlacesAnswered = new ManualResetEvent(true);

		/// <summary>What the reader read last: reserved here, written by the reader, copied by the asker once it has come back.</summary>
		static readonly bool[] PlacesRead = new bool[4];

		/// <summary>The product number of the controller in each place the reader found one in, written with <see cref="PlacesRead"/>; 0 for none or not known.</summary>
		static readonly ushort[] ProductsRead = new ushort[4];

		/// <summary>The thread the places are read on, made on the first question.</summary>
		static Thread _placesReader;

		/// <summary>What the reader's last read threw, loading the library or reading it, or null when it did not throw.</summary>
		/// <remarks>Written by the reader under <see cref="LoadLock"/> before it answers, read by the asker once it has.</remarks>
		static Exception _placesLoadFault;

		/// <summary>Reads which of the four places Windows reports a controller in, waiting no longer than it is given.</summary>
		/// <param name="places">Four places, filled when XInput answered in time, and left as they were when it did not.</param>
		/// <param name="limit">How long XInput has to answer.</param>
		/// <returns>False when XInput did not answer in time, or the read threw.</returns>
		/// <remarks>
		/// XInput stops answering while Windows takes controllers away and builds them again, and a call into it cannot be
		/// called off. So the places are read on a thread of their own, and whoever asks waits no longer than it was given.
		/// A read that has not come back is waited for again by the next question, never joined by a second one, and time
		/// spent behind another question counts against the limit. The reader holds <see cref="LoadLock"/> while it reads, so
		/// the library is not let go of under a read that has not come back: <see cref="Release"/> gives up instead. Nothing
		/// is made per read.
		/// </remarks>
		public static bool ReadPlaces(bool[] places, TimeSpan limit)
		{
			return ReadPlaces(places, null, limit);
		}

		/// <summary>The same, and says which controller is in each place by its product number.</summary>
		/// <param name="products">Four numbers, or null: 0 for a place with no controller, or when the library cannot say.</param>
		public static bool ReadPlaces(bool[] places, ushort[] products, TimeSpan limit)
		{
			// One limit for the whole question: time spent behind another question counts against it.
			var started = Environment.TickCount;
			var limitMs = (int)limit.TotalMilliseconds;
			if (!Monitor.TryEnter(PlacesAskLock, limitMs))
				return false;
			try
			{
				// A new read only once the last one has come back.
				if (PlacesAnswered.WaitOne(0))
				{
					PlacesAnswered.Reset();
					if (_placesReader == null)
					{
						_placesReader = new Thread(ReadPlacesLoop);
						_placesReader.IsBackground = true;
						_placesReader.Name = "XInputPlacesRead";
						_placesReader.Start();
					}
					PlacesAsked.Set();
				}
				var left = limitMs - unchecked(Environment.TickCount - started);
				if (!PlacesAnswered.WaitOne(left > 0 ? left : 0))
					return false;
				// A read that threw said nothing about the places.
				if (_placesLoadFault != null)
					return false;
				Array.Copy(PlacesRead, places, PlacesRead.Length);
				if (products != null)
					Array.Copy(ProductsRead, products, ProductsRead.Length);
				return true;
			}
			finally
			{
				Monitor.Exit(PlacesAskLock);
			}
		}

		static void ReadPlacesLoop()
		{
			while (true)
			{
				PlacesAsked.WaitOne();
				// Held, and said to be held, for as long as the read takes, which is as long as XInput does not answer.
				EnterLoadLock(Timeout.InfiniteTimeSpan);
				try
				{
					try
					{
						// After a read that threw, the library is loaded again by the next read, here and never on the input thread.
						if (_placesLoadFault != null)
							Load(null);
						for (var i = 0; i < PlacesRead.Length; i++)
						{
							PlacesRead[i] = IsConnected(i);
							ProductsRead[i] = PlacesRead[i] ? ProductOf(i) : (ushort)0;
						}
						_placesLoadFault = null;
					}
					// Loading the library lists the system folder, which can fail. The places are then not known, and the
					// asker hears it the way it hears a read that does not come back.
					catch (IOException ex) { NotePlacesLoadFault(ex); }
					catch (UnauthorizedAccessException ex) { NotePlacesLoadFault(ex); }
					// Anything else, such as a driver that faults inside the read, would end this thread and the program with
					// it. It is heard the same way, written as a fault once per change, and the next read loads again.
					catch (Exception ex)
					{
						if (_placesLoadFault == null || _placesLoadFault.GetType() != ex.GetType())
							JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
						_placesLoadFault = ex;
					}
				}
				finally
				{
					ExitLoadLock();
				}
				PlacesAnswered.Set();
			}
		}

		/// <summary>Keeps what loading the library threw in the reader, and writes it once until a load no longer throws.</summary>
		static void NotePlacesLoadFault(Exception ex)
		{
			LoadError = ex;
			if (_placesLoadFault == null)
				System.Diagnostics.Trace.TraceWarning("XInput not loaded: {0}", ex);
			_placesLoadFault = ex;
		}

		#endregion
	}
}
