using JocysCom.ClassLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.App.DInput
{
	/// <summary>What has to happen, in order, to put controllers in the places somebody asked for.</summary>
	/// <remarks>
	/// XInput cannot be asked for a place. It gives one out when a device arrives, and the order of
	/// arrival is the only lever there is. A virtual controller arrives with a hint, which is how many
	/// of them are present: the first is given XInput 2, the second XInput 3, the third XInput 4, if
	/// free. The fourth, and any controller with no hint, such as an Xbox One controller, is given the
	/// first free place. So XInput 1 is reached last, by the fourth to arrive, and the arrivals that
	/// belong to no wanted controller are made by temporary controllers that hold their place on the
	/// way and are taken away before the end.
	///
	/// Everything that holds a place is taken away first, then the arrivals are made one at a time in
	/// an order found by searching that rule, with the fewest temporary controllers. A real controller
	/// is expected to carry no hint; what XInput shows when the order is made is checked.
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
			/// <summary>Make a temporary controller, so the places below the one that arrives next are taken.</summary>
			CreateDecoy,
			/// <summary>Take away a temporary controller. Costs nothing and asks nobody.</summary>
			RemoveDecoy,
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
			/// <summary>The number of the temporary controller, from one, for the steps that make or take one away.</summary>
			public int Decoy;

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
					case StepKind.CreateDecoy: return ExpectedPlace < 0
						? string.Format("Make temporary controller {0}", Decoy)
						: string.Format("Make temporary controller {0}, which holds XInput {1} until the others are in", Decoy, ExpectedPlace + 1);
					case StepKind.RemoveDecoy: return string.Format("Take away temporary controller {0}", Decoy);
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
		/// thinks of it as one thing. None for a controller that <see cref="Entry.TakesNoPart"/>.
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
				if (places == null || !places.TryGetValue(hardware, out place))
					place = XInputPlaces.Unknown;
				DeviceInfo hardwareInfo;
				var name = byId.TryGetValue(hardware, out hardwareInfo) && !string.IsNullOrEmpty(hardwareInfo.Description)
					? hardwareInfo.Description
					: device.Description;
				var entry = new Entry
				{
					HardwareId = hardware,
					Name = name,
					IsVirtual = VirtualDriverInstaller.IsVirtualPad(device, byId),
					IsOurs = VirtualDriverInstaller.IsOneOfOurs(device, byId),
					Pad = PadHolding(place),
					Place = place,
				};
				if (!entry.TakesNoPart)
					entries.Add(entry);
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

			/// <summary>The controller tab whose place this holds, one to four, or zero when it holds none.</summary>
			/// <remarks>
			/// XInput N belongs to Controller N, so a real controller in the first place sits in
			/// Controller 1's place. A waiting virtual controller names the tab it waits for.
			/// </remarks>
			public int Controller
			{
				get { return Place >= 0 && Place < 4 ? Place + 1 : Waiting ? Pad : 0; }
			}

			/// <summary>Whether it takes no part in the order: a virtual controller this program did not make, holding no place.</summary>
			/// <remarks>
			/// Nothing here can move it: letting go of it does nothing, and a controller made in its stead would not be it.
			/// Holding no place, it is in nobody's way either. Given a position, it would be given a step to make it, which
			/// fails at once and stops the order part way, and every controller after it would be expected one place further
			/// along than it gets. So it is not listed, and a plan leaves it out; the Issues tab lists it for removal.
			/// </remarks>
			public bool TakesNoPart
			{
				get { return IsVirtual && !IsOurs && Place < 0; }
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
			// Left out before positions are counted, so the controllers after one are expected in the places they get.
			if (wanted != null)
				wanted = wanted.Where(x => !x.TakesNoPart).ToList();
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
			// Controller N's virtual controller is only ever kept in XInput N: measured, any other order of the
			// controllers comes out in the places a game started later is given as another one, so it is refused
			// now, before a real controller is switched off for an order that cannot be made.
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
			var moves = Arrivals(wanted.Take(PlaceCount).ToList());
			if (moves == null)
			{
				plan.Refusal = "No order of arrivals, with temporary controllers holding places on the way, puts them there.";
				return plan;
			}
			foreach (var entry in wanted.Where(x => x.IsVirtual && x.Place >= 0))
				plan.Steps.Add(new Step { Kind = StepKind.RemoveVirtual, HardwareId = entry.HardwareId, Name = entry.Name, Pad = entry.Pad });
			// Every real one, including one holding no place: switching on what is already on changes
			// nothing, and off and on again is what gets it a place.
			foreach (var entry in wanted.Where(x => !x.IsVirtual))
				plan.Steps.Add(new Step { Kind = StepKind.DisableReal, HardwareId = entry.HardwareId, Name = entry.Name, Pad = entry.Pad });

			// Then back, one at a time. The order of arrival is what decides the order of places.
			foreach (var move in moves)
			{
				if (move.Wanted < 0)
				{
					plan.Steps.Add(new Step
					{
						Kind = move.Leaves ? StepKind.RemoveDecoy : StepKind.CreateDecoy,
						Name = "temporary controller",
						Decoy = move.Decoy,
						ExpectedPlace = move.Place,
					});
					continue;
				}
				var entry = wanted[move.Wanted];
				plan.Steps.Add(new Step
				{
					Kind = entry.IsVirtual ? StepKind.CreateVirtual : StepKind.EnableReal,
					HardwareId = entry.HardwareId,
					Name = entry.Name,
					ExpectedPlace = move.Place,
					Pad = entry.Pad,
				});
			}
			// Past the fourth there is no place left to give, so they come back last.
			foreach (var entry in wanted.Skip(PlaceCount))
				plan.Steps.Add(new Step
				{
					Kind = entry.IsVirtual ? StepKind.CreateVirtual : StepKind.EnableReal,
					HardwareId = entry.HardwareId,
					Name = entry.Name,
					Pad = entry.Pad,
				});
			return plan;
		}

		const int PlaceCount = 4;

		/// <summary>The most temporary controllers one plan makes.</summary>
		const int MaxDecoys = 3;

		/// <summary>Added to a temporary controller's number to tell it from a wanted controller in a place.</summary>
		const int DecoyTag = 100;

		/// <summary>One thing that happens while the order is made: a wanted controller arrives, or a temporary one comes or goes.</summary>
		class Move
		{
			/// <summary>The wanted controller that arrives, or -1 for a temporary one.</summary>
			public int Wanted = -1;
			public int Decoy;
			public bool Leaves;
			/// <summary>The place it takes, or -1 when going away or when none was free.</summary>
			public int Place = -1;
		}

		/// <summary>How XInput gives out places to what arrives, as a thing that can be tried without touching a controller.</summary>
		sealed class Ladder
		{
			/// <summary>What holds each place: -1 free, a wanted controller's position, or <see cref="DecoyTag"/> plus a temporary one's number.</summary>
			public int[] Held = { -1, -1, -1, -1 };
			/// <summary>How many controllers that arrive with a hint are here.</summary>
			public int Hinted;
			/// <summary>A bit for each wanted controller that has arrived.</summary>
			public int Arrived;
			/// <summary>A bit for each temporary controller that is here.</summary>
			public int DecoyOn;
			public int DecoyMade;

			public Ladder Copy()
			{
				var copy = (Ladder)MemberwiseClone();
				copy.Held = (int[])Held.Clone();
				return copy;
			}

			/// <summary>Lets a controller arrive, and says which place it took, or -1 when none was free.</summary>
			public int Arrive(int tag, bool hinted)
			{
				var place = -1;
				if (hinted)
				{
					Hinted++;
					if (Hinted < PlaceCount && Held[Hinted] < 0)
						place = Hinted;
				}
				if (place < 0)
					place = Array.IndexOf(Held, -1);
				if (place >= 0)
					Held[place] = tag;
				return place;
			}

			public void Leave(int tag)
			{
				var place = Array.IndexOf(Held, tag);
				if (place >= 0)
					Held[place] = -1;
				Hinted--;
			}

			public string Key()
			{
				return string.Join(",", Held) + "/" + Hinted + "/" + Arrived + "/" + DecoyOn + "/" + DecoyMade;
			}
		}

		/// <summary>The fewest moves that put each of these controllers in the place of its position, or null when none do.</summary>
		/// <remarks>
		/// Tried with no temporary controller, then with more moves each time. A wanted controller that would land
		/// anywhere but its own place is never let arrive, since nothing moves it afterwards.
		/// </remarks>
		static List<Move> Arrivals(IList<Entry> top)
		{
			for (var limit = top.Count; limit <= top.Count + 2 * MaxDecoys; limit++)
			{
				var path = new List<Move>();
				if (Extend(top, new Ladder(), limit, path, new Dictionary<string, int>()))
					return path;
			}
			return null;
		}

		static bool Extend(IList<Entry> top, Ladder now, int left, List<Move> path, Dictionary<string, int> failed)
		{
			if (now.Arrived == (1 << top.Count) - 1 && now.DecoyOn == 0)
				return true;
			if (left == 0)
				return false;
			int tried;
			var key = now.Key();
			if (failed.TryGetValue(key, out tried) && tried >= left)
				return false;
			// Real controllers last where it makes no difference: each is switched on while the person waits.
			foreach (var i in Enumerable.Range(0, top.Count).OrderBy(x => top[x].IsVirtual ? 0 : 1))
			{
				if ((now.Arrived & (1 << i)) != 0)
					continue;
				var next = now.Copy();
				var place = next.Arrive(i, top[i].IsVirtual);
				if (place != i)
					continue;
				next.Arrived |= 1 << i;
				path.Add(new Move { Wanted = i, Place = place });
				if (Extend(top, next, left - 1, path, failed))
					return true;
				path.RemoveAt(path.Count - 1);
			}
			if (now.DecoyMade < MaxDecoys)
			{
				var next = now.Copy();
				var number = next.DecoyMade++;
				var place = next.Arrive(DecoyTag + number, true);
				next.DecoyOn |= 1 << number;
				path.Add(new Move { Decoy = number + 1, Place = place });
				if (Extend(top, next, left - 1, path, failed))
					return true;
				path.RemoveAt(path.Count - 1);
			}
			for (var number = 0; number < now.DecoyMade; number++)
			{
				if ((now.DecoyOn & (1 << number)) == 0)
					continue;
				var next = now.Copy();
				next.Leave(DecoyTag + number);
				next.DecoyOn &= ~(1 << number);
				path.Add(new Move { Decoy = number + 1, Leaves = true });
				if (Extend(top, next, left - 1, path, failed))
					return true;
				path.RemoveAt(path.Count - 1);
			}
			failed[key] = left;
			return false;
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
		/// moves out of the way instead of pushing every tab's controller one place along. A controller
		/// that <see cref="Entry.TakesNoPart"/> is left out.
		/// </remarks>
		/// <param name="entries">The controllers on the machine, as <see cref="ReadEntries"/> reads them.</param>
		public static List<Entry> ByController(IList<Entry> entries)
		{
			entries = entries.Where(x => !x.TakesNoPart).ToList();
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
			if (Steps.Any(x => x.Kind == StepKind.CreateDecoy))
				text += Environment.NewLine + Environment.NewLine
					+ "A temporary controller is made only to hold a place while the others arrive, and is taken "
					+ "away before the order is done.";
			if (NeedsElevation)
				text += Environment.NewLine + Environment.NewLine
					+ "Windows will ask for Administrator once, before the first real controller is switched "
					+ "off. This program is not restarted: a copy of it switches controllers off and on and "
					+ "closes when the order is done.";
			return text;
		}
	}
}
