using JocysCom.ClassLibrary.Controls.IssuesControl;
using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.App.Issues
{
	/// <summary>
	/// The virtual bus driver is installed but does not work. Connecting to it fails, a controller keeps
	/// failing to plug in while its XInput place is free, the bus keeps refusing its reports, or it will not
	/// remove a controller, so no other is made for that tab. A controller whose vibration it refused is said
	/// too, as information, since that controller works.
	/// </summary>
	/// <remarks>
	/// Nothing else says so. Device Manager shows the driver as healthy, the driver issue sees it
	/// installed, and the input thread retries quietly every few seconds. A bus that refuses every
	/// plug even answers "no free slot", which is never reported as a fault. The person sees
	/// controllers that no game detects. Removing the driver and putting it back is what clears it,
	/// and the fix here does that.
	///
	/// Judged from what the input thread and the bus client have recorded, read without a lock.
	/// Nothing here connects to the bus or takes the bus client's lock: the input thread asks the bus
	/// on every pass and must never wait for this check.
	/// </remarks>
	public class VirtualDriverNotWorkingIssue : IssueItem
	{
		/// <summary>Plugs in a row that must fail while the place is free before the bus is called broken.</summary>
		/// <remarks>
		/// After a failed plug the next waits <see cref="DInputHelper.PlugRetryMs"/> (two seconds), so
		/// three in a row span at least two retry gaps: four seconds of refusals. They span fifteen
		/// seconds or more when Windows never builds the controller, since each such plug first waits
		/// five seconds for a place. One failure is explained by a moment: the bus dropping controllers
		/// during a driver update, another program plugging one in at the same instant, or Windows slow
		/// to build one. Two can still be that moment straddling a retry. Three cannot. With checks
		/// every five seconds, the issue appears about ten seconds after a refusing bus starts refusing.
		///
		/// For plugs that are tried again. A controller that was never seen, because Windows never built it or XInput did not
		/// answer, is held back until a controller comes or goes, so it is not tried again and is said at once.
		/// </remarks>
		public const int PlugFailuresToReport = 3;

		/// <summary>Refused reports in one run before the bus is called broken.</summary>
		/// <remarks>
		/// One refused report is the ordinary case: the bus drops a controller when its driver is
		/// updated, and the next pass plugs it in again. Each refusal unplugs and replugs, so three in a
		/// run, each within <see cref="DInputHelper.FeedDropWindowMs"/> of the one before, means two
		/// fresh controllers were refused again.
		///
		/// A second sign only. The bus library shipped here answers a failed report as delivered unless
		/// Windows denied access, so a run is seldom counted. A bus that has stopped working is caught by
		/// its refused plugs and its refused connection, and this only adds to them.
		/// </remarks>
		public const int FeedDropsToReport = 3;

		/// <summary>How long a refused connection stays current.</summary>
		/// <remarks>
		/// One refusal is enough. Nothing works until connecting does, and the input thread asks again
		/// every <see cref="ViGEmClient.ConnectRetryMs"/> while a game uses virtual emulation, so a
		/// refusal that passes is replaced by the next answer within one retry. An answer three retries
		/// old means nothing has asked since, and it no longer describes the bus.
		/// </remarks>
		public const int ConnectErrorFreshMs = 3 * ViGEmClient.ConnectRetryMs;

		public VirtualDriverNotWorkingIssue() : base()
		{
			Name = Title(IssueSeverity.Moderate);
			FixName = "Repair";
			// Removing and putting back the driver is an Administrator action.
			FixNeedsAdmin = true;
		}

		/// <summary>The row's title, named for the worst it says.</summary>
		/// <remarks>
		/// Low is said only of controllers that work without vibration, and a driver whose controllers work
		/// is not "not working".
		/// </remarks>
		public static string Title(IssueSeverity severity)
		{
			return severity == IssueSeverity.Low
				? "Virtual controller has no vibration"
				: "Virtual driver is installed but not working";
		}

		/// <summary>What the input thread has recorded about one controller's use of the bus.</summary>
		public struct PadHealth
		{
			/// <summary>The tab is set to have a virtual controller now.</summary>
			public bool Wanted;
			/// <summary>What the last plug of this controller came to.</summary>
			public VirtualError LastResult;
			/// <summary>The last failure the bus answered this controller with, or VIGEM_ERROR_NONE.</summary>
			public VIGEM_ERROR LastError;
			/// <summary>What the bus answered its controller's registration for vibration, or VIGEM_ERROR_NONE while it takes vibration or is not on the bus.</summary>
			public VIGEM_ERROR RumbleError;
			/// <summary>Plugs in a row that failed while its own XInput place was free.</summary>
			public int PlugFailures;
			/// <summary>What held it back until a controller comes or goes, or None. Held back, it is not tried again and its count does not grow.</summary>
			public VirtualError HeldAs;
			/// <summary>Reports in a row the bus refused, each within <see cref="DInputHelper.FeedDropWindowMs"/> of the one before.</summary>
			public int FeedDrops;
			/// <summary><see cref="Environment.TickCount"/> at the last refused report.</summary>
			public int LastFeedDropTick;
		}

		/// <summary>Everything the decision reads, taken at one moment.</summary>
		public class Health
		{
			/// <summary>The bus driver is on this computer.</summary>
			public bool BusInstalled;
			/// <summary>What the bus answered the last connection, or VIGEM_ERROR_NONE.</summary>
			public VIGEM_ERROR ConnectError = VIGEM_ERROR.VIGEM_ERROR_NONE;
			/// <summary><see cref="Environment.TickCount"/> when <see cref="ConnectError"/> was answered.</summary>
			public int ConnectTick;
			/// <summary><see cref="Environment.TickCount"/> when this was read.</summary>
			public int Now;
			/// <summary>One entry per controller tab, the first tab first.</summary>
			public PadHealth[] Pads = new PadHealth[ViGEmClient.PlaceCount];
		}

		public override void CheckTask()
		{
			// Before the input thread starts nothing has been asked of the bus, so there is nothing to judge.
			var form = MainForm.Current;
			if (form != null && !form.AllowDHelperStart)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			if (!VirtualDeviceDriverIssue.IsRequired(SettingsManager.UserGames.Items))
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			string message;
			var severity = Judge(Read(), out message);
			var name = Title(severity);
			if (Name != name)
				Name = name;
			SetSeverity(severity, 0, message);
		}

		/// <summary>Reads what the input thread and the bus client have recorded, without a lock and without connecting.</summary>
		static Health Read()
		{
			var health = new Health
			{
				BusInstalled = VirtualDriverInstaller.GetInstalledViGEmBusVersion() != null,
				// The answer before its time: the bus client stores the time first, so a new answer read
				// here is never paired with an older time.
				ConnectError = ViGEmClient.LastConnectError,
				ConnectTick = ViGEmClient.LastConnectTick,
			};
			var helper = Global.DHelper;
			if (helper != null)
			{
				var game = SettingsManager.CurrentGame;
				// With no bus client there is no controller, and none goes without vibration.
				var client = ViGEmClient.Current;
				var rumble = client == null ? null : client.RumbleErrors;
				for (var i = 0; i < health.Pads.Length; i++)
				{
					health.Pads[i] = new PadHealth
					{
						Wanted = DInputHelper.WantsVirtual(game, (uint)(i + 1)),
						LastResult = helper.VirtualErrors[i],
						// After the plug's result, so a controller just made is never read with the answer from
						// before it; one still waiting for its place may be named for one check.
						RumbleError = rumble == null ? VIGEM_ERROR.VIGEM_ERROR_NONE : rumble[i],
						LastError = helper.BusErrors[i],
						PlugFailures = helper.PlugFailures[i],
						HeldAs = helper.HeldAs(i),
						// The count before its time: the input thread stores the time first, so a new count
						// read here is never paired with an older time. Volatile.Read makes that order formal.
						FeedDrops = Volatile.Read(ref helper.FeedDrops[i]),
						LastFeedDropTick = Volatile.Read(ref helper.LastFeedDropTick[i]),
					};
				}
			}
			// Read last, so nothing recorded above is newer than it.
			health.Now = Environment.TickCount;
			return health;
		}

		/// <summary>Whether the bus is installed but not working, and the words that say why.</summary>
		/// <param name="health">What is known about the bus at one moment.</param>
		/// <param name="message">Each reason on its own paragraph, then the way out; null when the bus works.</param>
		/// <returns>
		/// Moderate when it does not work, so the checks after this one still run; Low when all it refused is
		/// vibration, since those controllers work; None otherwise.
		/// </returns>
		public static IssueSeverity Judge(Health health, out string message)
		{
			message = null;
			// A driver that is not there is the Virtual Device Driver issue, which offers to install it.
			if (health == null || !health.BusInstalled)
				return IssueSeverity.None;
			var lines = new List<string>();
			if (health.ConnectError != VIGEM_ERROR.VIGEM_ERROR_NONE
				&& DInputHelper.IsRecent(health.ConnectTick, health.Now, ConnectErrorFreshMs))
				lines.Add(string.Format(
					"x360ce cannot connect to the virtual controller driver (driver answer: {0}), so no " +
					"virtual controller can be made.", BusAnswers.Name(health.ConnectError)));
			var pads = health.Pads ?? new PadHealth[0];
			// Lines about controllers that work and have no vibration.
			var quiet = 0;
			for (var i = 0; i < pads.Length; i++)
			{
				var pad = pads[i];
				if (!pad.Wanted)
					continue;
				var number = i + 1;
				var said = lines.Count;
				// Nothing else is made for it until the driver lets go, which Repair or a restart brings about. Said instead of
				// the failed plugs counted before it, whose kind its last result no longer tells.
				if (pad.LastResult == VirtualError.RemovalRefused)
					lines.Add(string.Format(
						"Controller {0}: the driver would not remove its virtual controller, so no other is made for it.",
						number));
				// A pad held back is not tried again while the places stay the same, so its count cannot grow. When the attempt
				// that held it back never saw its controller, it is said at once, in the words of what that attempt found. One
				// Windows put in another place was seen, and the driver works. One tried again is said after enough failures
				// that no passing moment explains.
				else if (pad.HeldAs == VirtualError.NotAnswering)
					lines.Add(string.Format(
						"Controller {0}: XInput did not answer while its virtual controller was being made, so it was taken " +
						"away again. It is tried again when a controller arrives or leaves.", number));
				else if (pad.HeldAs == VirtualError.PlaceNotGiven || pad.PlugFailures >= PlugFailuresToReport)
					lines.Add(pad.HeldAs == VirtualError.PlaceNotGiven || pad.LastResult == VirtualError.PlaceNotGiven
						? pad.PlugFailures <= 1
							? string.Format(
								"Controller {0}: the driver accepted its virtual controller, but Windows never finished building it.",
								number)
							: string.Format(
								"Controller {0}: the driver accepted its virtual controller {1} times in a row, " +
								"but Windows never finished building it.", number, pad.PlugFailures)
						: string.Format(
							"Controller {0}: the driver refused to make its virtual controller {1} times in a " +
							"row while XInput {0} was free (driver answer: {2}).",
							number, pad.PlugFailures, BusAnswers.Name(pad.LastError)));
				if (pad.FeedDrops >= FeedDropsToReport
					&& DInputHelper.IsRecent(pad.LastFeedDropTick, health.Now, DInputHelper.FeedDropWindowMs))
					lines.Add(string.Format(
						"Controller {0}: the driver stopped taking its input {1} times in a row (driver " +
						"answer: {2}), so its virtual controller had to be made again each time.",
						number, pad.FeedDrops, BusAnswers.Name(pad.LastError)));
				// Said of a controller that was made, and only when nothing worse is said of it.
				if (lines.Count == said && pad.LastResult == VirtualError.None
					&& pad.RumbleError != VIGEM_ERROR.VIGEM_ERROR_NONE)
				{
					lines.Add(string.Format(
						"Controller {0} works, but the driver refused its vibration (driver answer: {1}).",
						number, BusAnswers.Name(pad.RumbleError)));
					quiet++;
				}
			}
			if (lines.Count == 0)
				return IssueSeverity.None;
			var severity = lines.Count > quiet ? IssueSeverity.Moderate : IssueSeverity.Low;
			// Says what the button does, not that it will work: on Windows 10 and later the driver's
			// own setup decides, and on older Windows a driver of another version is left alone.
			lines.Add(
				"Repair installs the driver again" + (quiet > 0 ? ", which may restore what the driver refused" : "") +
				"; if its own setup opens, choose Repair there. If this message comes back afterwards, close " +
				"and reopen x360ce, then restart Windows if it is still here.");
			message = string.Join(Environment.NewLine + Environment.NewLine, lines.ToArray());
			return severity;
		}

		public override void FixTask()
		{
			// Runs on the issue panel's worker, never on the interface thread.
			VirtualDriverInstaller.RepairViGEmBusElevated();
		}

	}
}
