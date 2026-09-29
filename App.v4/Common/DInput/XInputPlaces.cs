using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.App.DInput
{
	/// <summary>
	/// Which XInput place each controller holds, as far as that can be known.
	/// </summary>
	/// <remarks>
	/// XInput hands out four places when devices arrive and never says which device got which. The
	/// two lists - controllers Windows knows about, and places XInput reports - share no key, so the
	/// answer has to be built rather than looked up.
	///
	/// Three things make it possible. Controllers this program makes are made one at a time, so the
	/// place each took was watched as it filled and is simply known. Every remaining taken place
	/// therefore belongs to something else. And a controller is a small family of devices with one
	/// piece of hardware underneath, so the faces can be gathered into the thing a person would point
	/// at.
	///
	/// Where that leaves one real controller and one unexplained place, they name each other. Where
	/// it leaves two of either, the pairing is open, and this says it does not know. A place stated
	/// wrongly is worse than a place left blank: somebody would map a controller against it.
	/// </remarks>
	public static class XInputPlaces
	{
		/// <summary>A place nobody could work out.</summary>
		public const int Unknown = -1;

		/// <summary>Places our own controllers took, kept against the controller itself.</summary>
		/// <remarks>
		/// Against the piece of hardware, because that is the only name for a controller that means the
		/// same thing to everybody looking at it.
		///
		/// It was kept against the number the bus gave the controller, and found again by reading the
		/// number off the end of a device name. That number is not a serial: it is whatever follows the
		/// last ampersand, and it belongs to no particular kind of thing. A USB hub two steps above a
		/// real controller ends in "&amp;2", so a real controller was handed the place of controller two.
		/// And the bus numbers controllers in the order they arrive across every program using it, while
		/// each program numbers its own from one - so with another program holding a controller, ours
		/// were looked up under names belonging to somebody else's.
		///
		/// So nothing is deduced from a name. The controller that appeared is watched for directly, at
		/// the one moment it can be: between asking for it and it arriving, it is the one that was not
		/// there before.
		/// </remarks>
		static readonly Dictionary<string, int> OursByHardware =
			new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		static readonly object SyncRoot = new object();

		/// <summary>The controller family as the machine reports it. Replaced by tests that have no machine.</summary>
		public static Func<DeviceInfo[]> ReadMachine = () => VirtualDriverInstaller.ReadControllerTree();

		/// <summary>Every controller on the bus right now, named by the hardware each belongs to.</summary>
		/// <remarks>
		/// Taken before a controller is made and again after, so the one that appeared in between is
		/// known by difference rather than by guessing at a name.
		/// </remarks>
		public static HashSet<string> VirtualHardwareNow()
		{
			var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				var all = ReadMachine();
				var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
				foreach (var device in all.Where(IsXInputCapable))
					if (VirtualDriverInstaller.IsVirtualPad(device, byId))
						found.Add(HardwareOf(device, byId));
			}
			catch (Exception ex) { JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex); }
			return found;
		}

		/// <summary>Remembers where a controller this program made was put.</summary>
		public static void Remember(string hardwareId, int place)
		{
			if (string.IsNullOrEmpty(hardwareId))
				return;
			lock (SyncRoot)
				OursByHardware[hardwareId] = place;
		}

		/// <summary>Forgets one controller, for when it is taken away.</summary>
		public static void Forget(string hardwareId)
		{
			if (string.IsNullOrEmpty(hardwareId))
				return;
			lock (SyncRoot)
				OursByHardware.Remove(hardwareId);
		}

		/// <summary>Forgets everything, for when the controllers are taken away.</summary>
		public static void Forget()
		{
			lock (SyncRoot)
				OursByHardware.Clear();
		}

		/// <summary>The place recorded for a controller of ours, or <see cref="Unknown"/>.</summary>
		static int RecordedPlace(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			// Only a controller this program could have made. Nothing else can have a place recorded.
			if (!VirtualDriverInstaller.IsVirtualPad(device, byId))
				return Unknown;
			var hardware = HardwareOf(device, byId);
			lock (SyncRoot)
			{
				int place;
				return OursByHardware.TryGetValue(hardware, out place) ? place : Unknown;
			}
		}

		/// <summary>The piece of hardware a controller device belongs to.</summary>
		/// <remarks>
		/// One controller appears as several devices: the thing itself, and a face for each way of
		/// reading it. Only the faces carry the XInput marker in their identifier, so the first
		/// ancestor without it is the controller a person would point at. Gathering by it means the
		/// DirectInput face of a controller and its XInput face are recognised as one device.
		/// </remarks>
		public static string HardwareOf(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			// A thing that carries no marker is already the controller, so it is its own answer. Walking
			// up from it reaches whatever made it - for a virtual controller, the bus - and every
			// controller on that bus would then be gathered under one name, as though they were one thing.
			if (device != null && byId != null
				&& !VirtualDriverInstaller.CarriesInputGroup(device.DeviceId)
				&& !VirtualDriverInstaller.CarriesInputGroup(device.HardwareIds))
				return device.DeviceId;
			if (device == null || byId == null)
				return device == null ? null : device.DeviceId;
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var current = device;
			while (current != null && !string.IsNullOrEmpty(current.ParentDeviceId) && seen.Add(current.ParentDeviceId))
			{
				DeviceInfo parent;
				if (!byId.TryGetValue(current.ParentDeviceId, out parent))
					break;
				if (!VirtualDriverInstaller.CarriesInputGroup(parent.DeviceId)
					&& !VirtualDriverInstaller.CarriesInputGroup(parent.HardwareIds))
					return parent.DeviceId;
				current = parent;
			}
			return device.DeviceId;
		}

		/// <summary>Whether XInput could ever see this device.</summary>
		public static bool IsXInputCapable(DeviceInfo device)
		{
			return device != null
				&& (VirtualDriverInstaller.CarriesInputGroup(device.HardwareIds)
					|| VirtualDriverInstaller.CarriesInputGroup(device.DeviceId));
		}

		/// <summary>
		/// The XInput place each piece of hardware holds, or <see cref="Unknown"/> where it cannot
		/// be worked out.
		/// </summary>
		public static Dictionary<string, int> Resolve()
		{
			var all = ReadMachine();
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			return Resolve(all, byId);
		}

		/// <summary>The same, against a device list already gathered.</summary>
		public static Dictionary<string, int> Resolve(DeviceInfo[] all, Dictionary<string, DeviceInfo> byId)
		{
			var answer = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			if (all == null || byId == null)
				return answer;

			var taken = new bool[4];
			for (var i = 0; i < 4; i++)
				taken[i] = SystemXInput.IsConnected(i);

			// Gather the faces into the hardware each belongs to, and sort them by what can be known.
			//
			// There are three kinds, not two. A controller this program made and watched arrive: its place
			// was noted at the one moment it could be. A controller somebody plugged in. And a virtual
			// controller this program did not make - left behind by a run that did not shut down cleanly,
			// or made by another program on the same bus. That third kind was counted as ours, which was
			// wrong twice over: it was named as ours on screen, and its place was entered as unknown while
			// the place it holds was counted as taken, so a real controller could no longer be named either.
			//
			// So the question is not who made it but whether we watched it arrive. That is the only thing
			// that yields a place directly; everything else has to be worked out by elimination, and a
			// virtual controller we did not make is exactly as unknown as a real one.
			var known = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var unnamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var device in all.Where(IsXInputCapable))
			{
				var hardware = HardwareOf(device, byId);
				// Any face of it will do; the first that carries a number we recorded answers.
				var place = RecordedPlace(device, byId);
				// And only while we are still holding it. A note that outlives its controller would hand a
				// place to something that no longer has it, and two claims on one place cannot both be true.
				// Asked of the bus, which knows exactly what it is holding this moment.
				if (place >= 0 && !VirtualDriverInstaller.IsOneOfOurs(device, byId))
					place = Unknown;
				if (place >= 0)
				{
					known[hardware] = place;
					// One face answering settles the whole controller, whatever its other faces said.
					unnamed.Remove(hardware);
				}
				else if (!known.ContainsKey(hardware))
					unnamed.Add(hardware);
			}

			// Ours are known, because the place each took was noted as it arrived.
			// A place can hold one controller. Two notes pointing at one place means a note is wrong and
			// there is no way to tell which, so neither is used: a blank says "not known", and that is what
			// this is. Showing both was showing something that cannot happen, which is worse than showing
			// nothing, because it invites somebody to map a controller against it.
			foreach (var place in known.Values.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToArray())
				foreach (var hardware in known.Where(x => x.Value == place).Select(x => x.Key).ToArray())
					known[hardware] = Unknown;
			var accounted = new bool[4];
			foreach (var pair in known)
			{
				answer[pair.Key] = pair.Value;
				if (pair.Value >= 0 && pair.Value <= 3)
					accounted[pair.Value] = true;
			}

			// What is left holds the rest. One controller and one place name each other; more of either
			// and nothing can be said about which is which.
			var spare = Enumerable.Range(0, 4).Where(i => taken[i] && !accounted[i]).ToList();
			if (unnamed.Count == 1 && spare.Count == 1)
				answer[unnamed.First()] = spare[0];
			else
				foreach (var hardware in unnamed)
					answer[hardware] = Unknown;

			// Answer for each face as well as for the hardware, because a list shows faces: a row
			// holds the identifier of the controller as DirectInput sees it, and asking about that
			// should not require the caller to walk the tree again.
			foreach (var device in all.Where(IsXInputCapable))
			{
				int place;
				if (answer.TryGetValue(HardwareOf(device, byId), out place))
					answer[device.DeviceId] = place;
			}
			return answer;
		}

		static Dictionary<string, int> _cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		static HashSet<string> _madeNotPluggedIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		static HashSet<string> _madeByUs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		/// <summary>Raised by every <see cref="Invalidate"/>, with no lock.</summary>
		static int _generation;

		/// <summary>The <see cref="_generation"/> the last finished read started at; -1 before the first read, so the answers start out of date.</summary>
		static int _readGeneration = -1;

		/// <summary>1 while a read started by <see cref="ReadWhenStale"/> is under way.</summary>
		static int _reading;

		/// <summary>The read <see cref="ReadWhenStale"/> starts on a worker, made once.</summary>
		static readonly Action ReadOnWorker = () =>
		{
			try { Read(); }
			finally { System.Threading.Volatile.Write(ref _reading, 0); }
		};

		/// <summary>Marks the answers as out of date, so the device thread reads the machine on its next pass.</summary>
		/// <remarks>
		/// Working the places out walks every device Windows has. That must never happen on the
		/// interface thread: Windows delivers the question "may this device go" to this program's main
		/// window, and while that thread is itself inside the device tree the question is never
		/// answered, the removal never finishes, and the program never comes back. The removal of a
		/// leftover controller from the Issues page froze the whole program this way. So the
		/// interface only ever reads the last answer, and only <see cref="Read"/> touches the machine,
		/// from the device thread or a worker.
		/// </remarks>
		public static void Invalidate()
		{
			System.Threading.Interlocked.Increment(ref _generation);
		}

		/// <summary>Whether the last answer is older than the machine.</summary>
		public static bool IsStale
		{
			get { return System.Threading.Volatile.Read(ref _readGeneration) != System.Threading.Volatile.Read(ref _generation); }
		}

		/// <summary>Reads the machine now when the answers are out of date, on the calling thread.</summary>
		public static void ReadIfStale()
		{
			if (IsStale)
				Read();
		}

		/// <summary>
		/// Starts one read on a worker when the answers are out of date, and returns at once. For the
		/// device thread: reading the machine takes long enough to drop its rate from a thousand
		/// passes a second to a few, and while it is that slow a wheel's force feedback is fed in
		/// jerks and swings from side to side. So the device thread only asks; it never waits.
		/// </summary>
		public static void ReadWhenStale()
		{
			// On every pass: two field reads and a compare while the answers are current, and no lock the
			// interface takes.
			if (!IsStale)
				return;
			if (System.Threading.Interlocked.CompareExchange(ref _reading, 1, 0) != 0)
				return;
			System.Threading.Tasks.Task.Run(ReadOnWorker);
		}

		/// <summary>Whether any of these devices was made rather than plugged in.</summary>
		public static bool IsMadeNotPluggedIn(params string[] deviceIds)
		{
			return Known(deviceIds, System.Threading.Volatile.Read(ref _madeNotPluggedIn));
		}

		/// <summary>Whether any of these devices was made by this program, which can take it away.</summary>
		public static bool IsOneOfOurs(params string[] deviceIds)
		{
			return Known(deviceIds, System.Threading.Volatile.Read(ref _madeByUs));
		}

		/// <summary>Whether any of these ids is in the set.</summary>
		/// <remarks>The sets are published whole and never changed, so they are read without a lock.</remarks>
		static bool Known(string[] deviceIds, HashSet<string> set)
		{
			if (deviceIds == null)
				return false;
			foreach (var id in deviceIds)
				if (!string.IsNullOrEmpty(id) && set.Contains(id))
					return true;
			return false;
		}

		/// <summary>Reads the machine now. Never from the interface thread; see <see cref="Invalidate"/>.</summary>
		public static void Read()
		{
			var generation = System.Threading.Volatile.Read(ref _generation);
			// The machine is read outside the lock, so nothing that takes it waits for the read; the
			// answers are swapped in whole once they are ready.
			Dictionary<string, int> cache = null;
			HashSet<string> madeNotPluggedIn = null;
			HashSet<string> madeByUs = null;
			try
			{
				var all = ReadMachine();
				var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
				cache = Resolve(all, byId);
				madeNotPluggedIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				madeByUs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var device in all.Where(IsXInputCapable))
				{
					if (!VirtualDriverInstaller.IsVirtualPad(device, byId))
						continue;
					madeNotPluggedIn.Add(device.DeviceId);
					if (VirtualDriverInstaller.IsOneOfOurs(device, byId))
						madeByUs.Add(device.DeviceId);
				}
			}
			catch (Exception ex) { JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex); }
			lock (SyncRoot)
			{
				if (cache != null)
				{
					_cache = cache;
					_madeNotPluggedIn = madeNotPluggedIn;
					_madeByUs = madeByUs;
					// After the table, so whoever reads the new version reads the new table.
					System.Threading.Interlocked.Increment(ref _version);
				}
				// Current unless something invalidated the answers while they were read, which the generation it
				// started at shows. Written under the same lock as the publish, so two overlapping reads cannot
				// pair an older table with the newer generation.
				System.Threading.Volatile.Write(ref _readGeneration, generation);
			}
		}

		/// <summary>Counts the place tables <see cref="Read"/> has published.</summary>
		static int _version;

		/// <summary>The version of <see cref="Current"/>: one more with each table <see cref="Read"/> publishes. Read without a lock.</summary>
		/// <remarks>
		/// For the engine, which works out where force is passed on to only when this or its routing changes.
		/// Read it before <see cref="Current"/>. The version is raised after the table is replaced, so a reader
		/// that sees the new version sees the new table, and one that pairs the old version with the new table
		/// works the places out again on its next pass.
		/// </remarks>
		public static int Version { get { return System.Threading.Volatile.Read(ref _version); } }

		/// <summary>The place table <see cref="Read"/> last published: the XInput place of each device, as <see cref="Resolve()"/> answers. Read without a lock.</summary>
		/// <remarks>Replaced whole and never changed after it is published, so it is read while the next one is made.</remarks>
		public static IReadOnlyDictionary<string, int> Current { get { return System.Threading.Volatile.Read(ref _cache); } }

		/// <summary>The place held by whichever of these devices is known, or <see cref="Unknown"/>.</summary>
		/// <remarks>
		/// A controller is offered under more than one identifier - the face DirectInput reads and the
		/// face XInput reads - and a row usually holds one of them without knowing which. Both are
		/// tried, because both lead to the same piece of hardware and so to the same place.
		/// </remarks>
		public static int PlaceFor(params string[] deviceIds)
		{
			var places = Current;
			if (deviceIds == null)
				return Unknown;
			foreach (var id in deviceIds)
			{
				var place = PlaceOf(places, id);
				if (place >= 0)
					return place;
			}
			return Unknown;
		}

		/// <summary>The place a table gives either of a device's two ids, or <see cref="Unknown"/>.</summary>
		/// <remarks>
		/// For the engine. The two ids are named rather than passed as a list, so the call makes nothing, and
		/// the table is handed in, so it is the one the caller read with its <see cref="Version"/>. Both ids
		/// are tried for the reason <see cref="PlaceFor"/> gives.
		/// </remarks>
		/// <param name="places">The place table, as <see cref="Current"/> holds it.</param>
		/// <param name="hidDeviceId">The id of the device's HID face, or null.</param>
		/// <param name="devDeviceId">The id of the device itself, or null.</param>
		public static int PlaceOf(IReadOnlyDictionary<string, int> places, string hidDeviceId, string devDeviceId)
		{
			var place = PlaceOf(places, hidDeviceId);
			return place >= 0 ? place : PlaceOf(places, devDeviceId);
		}

		/// <summary>The place a table gives one id, or <see cref="Unknown"/>.</summary>
		static int PlaceOf(IReadOnlyDictionary<string, int> places, string deviceId)
		{
			int place;
			return places != null && !string.IsNullOrEmpty(deviceId) && places.TryGetValue(deviceId, out place) && place >= 0
				? place
				: Unknown;
		}

		/// <summary>What <see cref="Holder"/> calls a controller somebody plugged in.</summary>
		public const string HolderReal = "Real";

		/// <summary>What <see cref="Holder"/> calls a controller this program made.</summary>
		public const string HolderVirtual = "Virtual";

		/// <summary>What <see cref="Holder"/> calls a virtual controller this program did not make.</summary>
		public const string HolderLeftover = "Leftover";

		/// <summary>What is holding a place, in one word.</summary>
		/// <remarks>
		/// Three kinds, from two questions: is it virtual, and is it ours. A virtual controller this
		/// program did not make is called a leftover, which is the word the Devices page already uses for
		/// them - so the label names the thing and the button that removes it at the same time.
		/// </remarks>
		public static string Holder(bool isVirtual, bool isOurs)
		{
			return !isVirtual ? HolderReal : isOurs ? HolderVirtual : HolderLeftover;
		}

		/// <summary>What the last reading of the machine found holding a place, as <see cref="Holder"/> names it, or null when nothing it could name holds it.</summary>
		/// <remarks>
		/// For the interface, on every tick. It reads only the answers <see cref="Read"/> last swapped in,
		/// never the machine, and takes no lock: the answers are replaced whole rather than changed, so reading
		/// the three references is enough. They can come from two readings a moment apart, which the next tick
		/// puts right.
		/// </remarks>
		/// <param name="place">The place, counting from zero.</param>
		public static string HolderOf(int place)
		{
			return HolderOf(place,
				System.Threading.Volatile.Read(ref _cache),
				System.Threading.Volatile.Read(ref _madeNotPluggedIn),
				System.Threading.Volatile.Read(ref _madeByUs));
		}

		/// <summary>The same, from given answers, so it can be asked without a machine.</summary>
		/// <remarks>
		/// A controller is listed under its piece of hardware and under each of its faces, and only the
		/// faces are in the lists of made controllers. So one face found virtual settles it, and a place
		/// whose every entry was plugged in is held by a real controller.
		/// </remarks>
		/// <param name="place">The place, counting from zero.</param>
		/// <param name="places">The place of each device, as <see cref="Resolve()"/> answers.</param>
		/// <param name="madeNotPluggedIn">Faces of controllers that were made rather than plugged in.</param>
		/// <param name="madeByUs">Faces of controllers this program made.</param>
		public static string HolderOf(int place, Dictionary<string, int> places, HashSet<string> madeNotPluggedIn, HashSet<string> madeByUs)
		{
			if (places == null || place < 0 || place > 3)
				return null;
			string holder = null;
			foreach (var pair in places)
			{
				if (pair.Value != place)
					continue;
				if (madeNotPluggedIn != null && madeNotPluggedIn.Contains(pair.Key))
					return Holder(true, madeByUs != null && madeByUs.Contains(pair.Key));
				holder = HolderReal;
			}
			return holder;
		}

		/// <summary>What is holding a place, in words a person can act on.</summary>
		/// <param name="holder">What <see cref="HolderOf(int)"/> answered, or null when that is not known.</param>
		public static string HolderWords(string holder)
		{
			switch (holder)
			{
				case HolderReal:
					return "a real controller";
				case HolderVirtual:
					return "another of this program's own virtual controllers";
				case HolderLeftover:
					return "a virtual controller this program did not make, such as one from DS4Windows or one left behind (the Devices page lists it as Leftover)";
				default:
					return "another controller";
			}
		}

		/// <summary>How one place reads to a person, with what is holding it.</summary>
		/// <remarks>
		/// What is holding it comes first and the place second, the way somebody would say it out loud.
		/// The places carry no "XInput" of their own: every column that shows them is headed with it
		/// already, and repeating it in each cell says the same word down the whole column while the
		/// values it is there to compare sit behind it.
		/// </remarks>
		/// <param name="place">The place, counting from zero, or <see cref="Unknown"/>.</param>
		/// <param name="isVirtual">Whether the thing in the place was made rather than plugged in.</param>
		/// <param name="isOurs">Whether this program made it and can take it away again.</param>
		public static string Describe(int place, bool isVirtual, bool isOurs)
		{
			return place >= 0 && place <= 3
				? string.Format("{0} {1}", Holder(isVirtual, isOurs), place + 1)
				: string.Empty;
		}

		/// <summary>How the places a device reaches read to a person, as one line.</summary>
		/// <remarks>
		/// A device can reach a game in more than one place at once, and by two different routes. It
		/// holds at most one place itself - the face XInput reads, which only an Xbox controller and a
		/// virtual pad have - and it reaches one more for every controller tab it is mapped to. Naming
		/// only one of them would hide the rest, and the rest are the ones somebody has forgotten about.
		///
		/// Each is marked, because they mean different things to somebody deciding what to change. Real
		/// is the device itself sitting in that place, which a game reads whether this program is running
		/// or not. Virtual is this program carrying it there, and it stops when this program does.
		/// </remarks>
		/// <param name="ownPlace">The place the device holds itself, or <see cref="Unknown"/>.</param>
		/// <param name="ownIsVirtual">Whether the device itself was made rather than plugged in.</param>
		/// <param name="ownIsOurs">Whether this program made the device itself.</param>
		/// <param name="carried">The places of the controllers this device is mapped to.</param>
		public static string Describe(int ownPlace, bool ownIsVirtual, bool ownIsOurs, IEnumerable<int> carried)
		{
			// Carried places are always controllers this program made: that is what carrying means here.
			var named = new SortedDictionary<int, string>();
			if (carried != null)
				foreach (var place in carried.Where(x => x >= 0 && x <= 3))
					named[place] = Holder(true, true);
			// Written second, so a place reached both ways is called what it is. The device being there
			// itself is the stronger fact: a game reads it with this program switched off.
			if (ownPlace >= 0 && ownPlace <= 3)
				named[ownPlace] = Holder(ownIsVirtual, ownIsOurs);
			var parts = named.Select(x => string.Format("{0} {1}", x.Value, x.Key + 1)).ToArray();
			return string.Join(", ", parts);
		}
	}
}
