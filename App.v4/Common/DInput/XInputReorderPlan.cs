using JocysCom.ClassLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.App.DInput
{
	/// <summary>What has to happen, in order, to put controllers in the places somebody asked for.</summary>
	/// <remarks>
	/// XInput cannot be asked for a place. It gives one out when a device arrives, and that is the only
	/// lever there is. So an order is achieved by making devices arrive in that order: take away
	/// everything that would be in the way, then bring things back one at a time.
	///
	/// The plan is worked out before anything is touched, so it can be shown to somebody and refused
	/// before a controller is switched off rather than after.
	/// </remarks>
	public class XInputReorderPlan
	{
		public enum StepKind
		{
			/// <summary>Take away a controller this program made. Costs nothing and asks nobody.</summary>
			RemoveVirtual,
			/// <summary>Switch off a real controller. Needs Administrator, and the person sees it go.</summary>
			DisableReal,
			/// <summary>Make a controller. Windows gives it a place as it arrives.</summary>
			CreateVirtual,
			/// <summary>Switch a real controller back on. Windows gives it a place as it arrives.</summary>
			EnableReal,
		}

		public class Step
		{
			public StepKind Kind;
			/// <summary>The hardware this step acts on.</summary>
			public string HardwareId;
			/// <summary>What it is called, for saying what is happening.</summary>
			public string Name;
			/// <summary>The place it should hold once the step is done, or -1 when going away.</summary>
			public int ExpectedPlace = -1;
			/// <summary>The controller tab this acts on, one to four, or zero when it is not ours.</summary>
			public int Pad;

			/// <summary>What this acts on, in words: the tab and what is mapped to it for a virtual controller of ours.</summary>
			/// <remarks>
			/// Every virtual controller is an Xbox 360 Controller for Windows, so its own name made every
			/// step about one read like every other, and a report about one could not be told from a
			/// report about the next.
			/// </remarks>
			string Subject
			{
				get
				{
					if ((Kind != StepKind.RemoveVirtual && Kind != StepKind.CreateVirtual) || Pad < 1 || Pad > 4)
						return Name;
					var names = ProductNames(Pad);
					return string.Format("the virtual controller of Controller {0}{1}", Pad,
						names.Length == 0 ? "" : " (" + names + ")");
				}
			}

			public override string ToString()
			{
				switch (Kind)
				{
					case StepKind.RemoveVirtual: return string.Format("Take away {0}", Subject);
					case StepKind.DisableReal: return string.Format("Switch off {0}", Name);
					case StepKind.CreateVirtual: return string.Format("Make {0}, expecting XInput {1}", Subject, ExpectedPlace + 1);
					default: return ExpectedPlace < 0
						? string.Format("Switch on {0}, which gets no XInput place while four are taken", Name)
						: string.Format("Switch on {0}, expecting XInput {1}", Name, ExpectedPlace + 1);
				}
			}
		}

		/// <summary>One controller, as the person sees it in the list.</summary>
		/// <summary>One row per controller on the machine, in the order XInput has them, unplaced ones last.</summary>
		/// <remarks>
		/// Reads the device tree, so it runs on a worker or the device thread, never on the interface.
		/// One row per piece of hardware, not per face: a controller is several devices and a person
		/// thinks of it as one thing.
		/// </remarks>
		public static List<Entry> ReadEntries()
		{
			var entries = new List<Entry>();
			var all = XInputPlaces.ReadMachine();
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			var places = XInputPlaces.Resolve(all, byId);
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var device in all.Where(XInputPlaces.IsXInputCapable))
			{
				var hardware = XInputPlaces.HardwareOf(device, byId);
				if (!seen.Add(hardware))
					continue;
				int place;
				if (!places.TryGetValue(hardware, out place))
					place = XInputPlaces.Unknown;
				DeviceInfo hardwareInfo;
				var name = byId.TryGetValue(hardware, out hardwareInfo) && !string.IsNullOrEmpty(hardwareInfo.Description)
					? hardwareInfo.Description
					: device.Description;
				entries.Add(new Entry
				{
					HardwareId = hardware,
					SwitchId = SwitchedPart(device, hardware, byId),
					Name = name,
					IsVirtual = VirtualDriverInstaller.IsVirtualPad(device, byId),
					IsOurs = VirtualDriverInstaller.IsOneOfOurs(device, byId),
					Pad = PadHolding(place),
					Place = place,
				});
			}
			// The order XInput has them is the order a game sees, which is the order worth arguing with.
			entries.Sort((a, b) =>
			{
				var pa = a.Place < 0 ? int.MaxValue : a.Place;
				var pb = b.Place < 0 ? int.MaxValue : b.Place;
				return pa != pb ? pa.CompareTo(pb) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			});
			// A tab set to have a virtual controller that has none yet, because its place is taken, is
			// listed too. Without a row it could not be ordered, and nothing could be moved past it.
			var game = SettingsManager.CurrentGame;
			for (var pad = 1; pad <= 4; pad++)
				if (DInputHelper.WantsVirtual(game, (uint)pad) && !entries.Any(x => x.IsOurs && x.Pad == pad))
					entries.Add(new Entry
					{
						HardwareId = "Controller " + pad,
						Name = "Xbox 360 Controller for Windows",
						IsVirtual = true,
						IsOurs = true,
						Pad = pad,
						Waiting = true,
					});
			return entries;
		}

		/// <summary>The class of an Xbox 360 controller, which XInput reads through the controller itself.</summary>
		static readonly Guid Xbox360Class = new Guid("d61ca365-5af4-4486-998b-9db4734c6ca3");

		/// <summary>The part of a real controller that is switched off and on to give up its place.</summary>
		/// <remarks>
		/// Only the part XInput reads. For an Xbox 360 controller that is the controller itself. For any
		/// other, such as an Xbox One controller, it is the input part below it: switching the whole
		/// controller off turned it off, and switched back on it stayed off - present to Windows, with no
		/// XInput place and no DirectInput device - until it was plugged in again.
		/// </remarks>
		public static string SwitchedPart(DeviceInfo face, string hardware, Dictionary<string, DeviceInfo> byId)
		{
			DeviceInfo hardwareInfo;
			if (face == null || byId.TryGetValue(hardware, out hardwareInfo) && hardwareInfo.ClassGuid == Xbox360Class)
				return hardware;
			// Up from the face XInput reads to the highest part that is still an input part, just below
			// the controller.
			var part = face;
			DeviceInfo parent;
			while (!string.IsNullOrEmpty(part.ParentDeviceId) && byId.TryGetValue(part.ParentDeviceId, out parent)
				&& (VirtualDriverInstaller.CarriesInputGroup(parent.DeviceId) || VirtualDriverInstaller.CarriesInputGroup(parent.HardwareIds)))
				part = parent;
			return part.DeviceId;
		}

		/// <summary>The product names of the devices mapped to this controller tab for the current game.</summary>
		/// <remarks>
		/// Named from the mapping, which keeps the names, so a mapped device that is not connected is still
		/// named - and said to be not connected - rather than the tab being shown with nothing mapped.
		/// </remarks>
		public static string ProductNames(int pad)
		{
			var names = SettingsManager.GetSettings(SettingsManager.CurrentGame?.FileName, (Engine.MapTo)pad)
				.Select(setting =>
				{
					var device = SettingsManager.GetDevice(setting.InstanceGuid);
					var name = device != null && !string.IsNullOrEmpty(device.ProductName)
						? device.ProductName
						: !string.IsNullOrEmpty(setting.ProductName) ? setting.ProductName : setting.InstanceName;
					return device != null && device.IsOnline ? name : name + " (not connected)";
				})
				.ToArray();
			return string.Join(", ", names);
		}

		/// <summary>Which controller tab has its controller in this place, or zero.</summary>
		/// <remarks>
		/// Asked of what was watched rather than worked out from the number: the place a tab's controller
		/// is in is not the tab's own number, which is the whole reason the Virtual Device page exists.
		/// </remarks>
		static int PadHolding(int place)
		{
			var helper = Global.DHelper;
			if (helper == null || place < 0)
				return 0;
			for (var pad = 0; pad < helper.XiPlaceForPad.Length; pad++)
				if (helper.XiPlaceForPad[pad] == place)
					return pad + 1;
			return 0;
		}

		public class Entry
		{
			public string HardwareId;
			public string Name;
			public bool IsVirtual;
			/// <summary>Whether this program made it and can take it away again.</summary>
			/// <remarks>
			/// A virtual controller left behind by an earlier run is virtual and is not ours. Letting go of
			/// it does nothing, because this program is not holding it.
			/// </remarks>
			public bool IsOurs;
			/// <summary>Which controller tab made it, one to four, or zero when it is not ours.</summary>
			/// <remarks>
			/// A controller of ours belongs to a tab, and that is what carries its mappings. Which place it
			/// is in is a separate fact and changes; the tab it belongs to does not. Working the tab out from
			/// the place - as this did - meant that reordering handed each tab whichever place came next,
			/// leaving every tab pointing at another tab's controller.
			/// </remarks>
			public int Pad;
			/// <summary>Where it is now, or -1 when unknown or nowhere.</summary>
			public int Place = -1;
			/// <summary>A virtual controller a tab is set to have, not made yet because its place is taken.</summary>
			public bool Waiting;
			/// <summary>The part of a real controller switched off and on to move it, or null to switch the whole.</summary>
			public string SwitchId;

			/// <summary>The controller tab whose place this holds, one to four, or zero when it holds none.</summary>
			/// <remarks>
			/// XInput N belongs to Controller N, so a real controller in the first place sits in
			/// Controller 1's place. A waiting virtual controller names the tab it waits for.
			/// </remarks>
			public int Controller
			{
				get { return Place >= 0 && Place < 4 ? Place + 1 : Waiting ? Pad : 0; }
			}
		}

		public List<Step> Steps = new List<Step>();

		/// <summary>Why the plan cannot be carried out, or null when it can.</summary>
		public string Refusal;

		/// <summary>Whether anything in the plan needs Administrator.</summary>
		public bool NeedsElevation
		{
			get { return Steps.Any(x => x.Kind == StepKind.DisableReal || x.Kind == StepKind.EnableReal); }
		}

		/// <summary>Works out the steps that would put <paramref name="wanted"/> in places one upward.</summary>
		/// <param name="wanted">
		/// The controllers in the order somebody asked for, first taking XInput place one.
		/// </param>
		public static XInputReorderPlan For(IList<Entry> wanted)
		{
			var plan = new XInputReorderPlan();
			if (wanted == null || wanted.Count == 0)
			{
				plan.Refusal = "Nothing was asked for.";
				return plan;
			}
			if (wanted.Select(x => x.HardwareId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != wanted.Count)
			{
				plan.Refusal = "The same controller was asked for twice.";
				return plan;
			}
			// Controller N's virtual controller is only ever kept in XInput N, so any other place for it is
			// refused now, before a real controller is switched off for an order that cannot be made.
			for (var i = 0; i < wanted.Count; i++)
			{
				var entry = wanted[i];
				if (entry.IsVirtual && entry.IsOurs && entry.Pad >= 1 && entry.Pad <= 4 && entry.Pad != i + 1)
				{
					plan.Refusal = string.Format(
						"The virtual controller of Controller {0} can only take XInput {0}, and this order puts "
						+ "it in XInput {1}. Use Auto-Order.", entry.Pad, i + 1);
					return plan;
				}
			}

			// Already right, so nothing to do. Saying so beats switching a controller off to arrive at
			// where it already was.
			var already = wanted.Select((e, i) => e.Place == PlaceFor(i)).All(x => x);
			if (already)
				return plan;

			// Everything that has a place has to give it up, because a place is only handed out on
			// arrival and the ones below it must be filled first. Ours go first: they cost nothing,
			// and every one taken away is a real controller that may not need switching off.
			// A virtual controller this program did not make cannot be let go of - it is held by whatever
			// made it, or by nothing at all if that has since stopped. Saying so beats switching real
			// controllers off to arrive at an order that was never reachable.
			var strays = wanted.Where(x => x.IsVirtual && !x.IsOurs && x.Place >= 0).ToArray();
			if (strays.Length > 0)
			{
				plan.Refusal = string.Format(
					"{0} is a virtual controller this program did not make, so it cannot be moved out of "
					+ "the way. Remove it with Remove Leftover Pads on the Devices page, then try again.",
					strays[0].Name);
				return plan;
			}
			foreach (var entry in wanted.Where(x => x.IsVirtual && x.Place >= 0))
				plan.Steps.Add(new Step { Kind = StepKind.RemoveVirtual, HardwareId = entry.HardwareId, Name = entry.Name, Pad = entry.Pad });
			// Every real one, including one holding no place: switching on what is already on changes
			// nothing, and off and on again is what gets it a place.
			foreach (var entry in wanted.Where(x => !x.IsVirtual))
				plan.Steps.Add(new Step { Kind = StepKind.DisableReal, HardwareId = entry.SwitchId ?? entry.HardwareId, Name = entry.Name, Pad = entry.Pad });

			// Then back, one at a time, in the order asked for. The order of arrival is what decides
			// the order of places.
			for (var i = 0; i < wanted.Count; i++)
			{
				var entry = wanted[i];
				plan.Steps.Add(new Step
				{
					Kind = entry.IsVirtual ? StepKind.CreateVirtual : StepKind.EnableReal,
					HardwareId = entry.IsVirtual ? entry.HardwareId : entry.SwitchId ?? entry.HardwareId,
					Name = entry.Name,
					ExpectedPlace = PlaceFor(i),
					Pad = entry.Pad,
				});
			}
			return plan;
		}

		/// <summary>The place the controller at this position in the list gets: none past the fourth.</summary>
		static int PlaceFor(int position)
		{
			return position < 4 ? position : -1;
		}

		/// <summary>The order that gives each controller tab the place of its own number.</summary>
		/// <remarks>
		/// Controller N's virtual controller goes to place N, because a game reading place N gets what is
		/// mapped on Controller N only then. Everything else fills the places no tab needs, first free
		/// first, in the order it is in now. So a real controller that arrived first and took place one
		/// moves out of the way instead of pushing every tab's controller one place along.
		/// </remarks>
		/// <param name="entries">The controllers on the machine, as <see cref="ReadEntries"/> reads them.</param>
		public static List<Entry> ByController(IList<Entry> entries)
		{
			var byPad = entries.Where(x => x.IsVirtual && x.IsOurs && x.Pad >= 1 && x.Pad <= 4)
				.GroupBy(x => x.Pad).ToDictionary(x => x.Key, x => x.First());
			var rest = new Queue<Entry>(entries.Where(x => !byPad.Values.Contains(x)));
			var ordered = new List<Entry>();
			var last = byPad.Count == 0 ? 0 : byPad.Keys.Max();
			for (var pad = 1; pad <= last; pad++)
			{
				Entry entry;
				if (byPad.TryGetValue(pad, out entry))
					ordered.Add(entry);
				else if (rest.Count > 0)
					ordered.Add(rest.Dequeue());
			}
			ordered.AddRange(rest);
			return ordered;
		}

		/// <summary>The plan in words, for showing before anything is done.</summary>
		public override string ToString()
		{
			if (Refusal != null)
				return Refusal;
			if (Steps.Count == 0)
				return "The controllers are already in that order.";
			var lines = Steps.Select((s, i) => string.Format("{0}. {1}", i + 1, s)).ToArray();
			var text = string.Join(Environment.NewLine, lines);
			if (NeedsElevation)
				text += Environment.NewLine + Environment.NewLine
					+ "Windows will ask for Administrator once, before the first real controller is switched "
					+ "off. This program is not restarted: a copy of it switches controllers off and on and "
					+ "closes when the order is done.";
			return text;
		}
	}
}
