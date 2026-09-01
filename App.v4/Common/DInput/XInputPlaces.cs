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
				var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
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
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
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
		static DateTime _cachedAt = DateTime.MinValue;

		/// <summary>How long an answer is reused before the machine is read again.</summary>
		/// <remarks>
		/// Working the places out walks every device Windows has, which is far too much to do while
		/// painting a row of a table. Devices come and go in seconds rather than milliseconds, so an
		/// answer a moment old is still true, and a table can ask about every row without noticing.
		/// </remarks>
		static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(2);

		/// <summary>Reads the machine again, so the next question gets a fresh answer.</summary>
		public static void Invalidate()
		{
			lock (SyncRoot)
				_cachedAt = DateTime.MinValue;
		}

		/// <summary>The place held by whichever of these devices is known, or <see cref="Unknown"/>.</summary>
		/// <remarks>
		/// A controller is offered under more than one identifier - the face DirectInput reads and the
		/// face XInput reads - and a row usually holds one of them without knowing which. Both are
		/// tried, because both lead to the same piece of hardware and so to the same place.
		/// <summary>Whether any of these devices was made rather than plugged in.</summary>
		public static bool IsMadeNotPluggedIn(params string[] deviceIds)
		{
			return Known(deviceIds, _madeNotPluggedIn);
		}

		/// <summary>Whether any of these devices was made by this program, which can take it away.</summary>
		public static bool IsOneOfOurs(params string[] deviceIds)
		{
			return Known(deviceIds, _madeByUs);
		}

		static bool Known(string[] deviceIds, HashSet<string> set)
		{
			Refresh();
			if (deviceIds == null)
				return false;
			lock (SyncRoot)
				foreach (var id in deviceIds)
					if (!string.IsNullOrEmpty(id) && set.Contains(id))
						return true;
			return false;
		}

		/// <summary>Reads the machine again when the last answer has gone stale.</summary>
		static void Refresh()
		{
			lock (SyncRoot)
			{
				if (DateTime.UtcNow - _cachedAt <= CacheFor)
					return;
				try
				{
					var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
					var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
					_cache = Resolve(all, byId);
					// What each one is, gathered in the same reading. A row asks about a device by name and
					// cannot walk the tree itself: doing that while painting a cell would read every device
					// Windows has, many times a second.
					_madeNotPluggedIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					_madeByUs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (var device in all.Where(IsXInputCapable))
					{
						if (!VirtualDriverInstaller.IsVirtualPad(device, byId))
							continue;
						_madeNotPluggedIn.Add(device.DeviceId);
						if (VirtualDriverInstaller.IsOneOfOurs(device, byId))
							_madeByUs.Add(device.DeviceId);
					}
				}
				catch (Exception ex) { JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex); }
				_cachedAt = DateTime.UtcNow;
			}
		}

		/// </remarks>
		public static int PlaceFor(params string[] deviceIds)
		{
			Refresh();
			Dictionary<string, int> places;
			lock (SyncRoot)
				places = _cache;
			if (deviceIds == null)
				return Unknown;
			foreach (var id in deviceIds)
			{
				int place;
				if (!string.IsNullOrEmpty(id) && places.TryGetValue(id, out place) && place >= 0)
					return place;
			}
			return Unknown;
		}

		/// <summary>What is holding a place, in one word.</summary>
		/// <remarks>
		/// Three kinds, from two questions: is it virtual, and is it ours. A virtual controller this
		/// program did not make is called a leftover, which is the word the Devices page already uses for
		/// them - so the label names the thing and the button that removes it at the same time.
		/// </remarks>
		public static string Holder(bool isVirtual, bool isOurs)
		{
			return !isVirtual ? "Real" : isOurs ? "Virtual" : "Leftover";
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
