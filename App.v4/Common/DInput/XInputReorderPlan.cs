using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.App.DInput
{
	/// <summary>What has to happen, in order, to put controllers in the places somebody asked for.</summary>
	/// <remarks>
	/// XInput cannot be asked for a place. It gives out the lowest free one when a device arrives, and
	/// that is the only lever there is. So an order is achieved by making devices arrive in that
	/// order: take away everything that would be in the way, then bring things back one at a time.
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
			/// <summary>Make a controller. It will take the lowest free place.</summary>
			CreateVirtual,
			/// <summary>Switch a real controller back on. It will take the lowest free place.</summary>
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

			public override string ToString()
			{
				switch (Kind)
				{
					case StepKind.RemoveVirtual: return string.Format("Take away {0}", Name);
					case StepKind.DisableReal: return string.Format("Switch off {0}", Name);
					case StepKind.CreateVirtual: return string.Format("Make {0}, expecting XInput {1}", Name, ExpectedPlace + 1);
					default: return string.Format("Switch on {0}, expecting XInput {1}", Name, ExpectedPlace + 1);
				}
			}
		}

		/// <summary>One controller, as the person sees it in the list.</summary>
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
			if (wanted.Count > 4)
			{
				plan.Refusal = string.Format(
					"XInput has four places and {0} controllers were asked for. The ones past the "
					+ "fourth would get no place at all, and no game would see them.", wanted.Count);
				return plan;
			}
			if (wanted.Select(x => x.HardwareId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != wanted.Count)
			{
				plan.Refusal = "The same controller was asked for twice.";
				return plan;
			}

			// Already right, so nothing to do. Saying so beats switching a controller off to arrive at
			// where it already was.
			var already = wanted.Select((e, i) => e.Place == i).All(x => x);
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
			foreach (var entry in wanted.Where(x => !x.IsVirtual && x.Place >= 0))
				plan.Steps.Add(new Step { Kind = StepKind.DisableReal, HardwareId = entry.HardwareId, Name = entry.Name, Pad = entry.Pad });

			// Then back, one at a time, in the order asked for. Each takes the lowest free place, so
			// the order of arrival is the order of places.
			for (var i = 0; i < wanted.Count; i++)
			{
				var entry = wanted[i];
				plan.Steps.Add(new Step
				{
					Kind = entry.IsVirtual ? StepKind.CreateVirtual : StepKind.EnableReal,
					HardwareId = entry.HardwareId,
					Name = entry.Name,
					ExpectedPlace = i,
					Pad = entry.Pad,
				});
			}
			return plan;
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
					+ "Windows will ask for Administrator before each real controller is switched off and "
					+ "on again. This program is not restarted: a copy of it does that one step and closes.";
			return text;
		}
	}
}
