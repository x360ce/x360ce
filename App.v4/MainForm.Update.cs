using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>
	/// The start-up look for a newer version. It only tells; installing is the user's click on
	/// the Options tab's Update page.
	/// </summary>
	public partial class MainForm
	{
		Timer _UpdateProbeTimer;

		/// <summary>
		/// Arms one probe at a random moment inside the first hour, at most once a day, and only
		/// when the option is on. Off means the program makes no request at all.
		/// </summary>
		void ScheduleUpdateProbe()
		{
			var o = SettingsManager.Options;
			var delay = UpdateClient.NextProbeDelay(o.CheckForUpdates, o.LastUpdateCheck, DateTime.Now, new Random());
			if (delay == null)
				return;
			_UpdateProbeTimer = new Timer { Interval = Math.Max(1, (int)delay.Value.TotalMilliseconds) };
			_UpdateProbeTimer.Tick += UpdateProbeTimer_Tick;
			_UpdateProbeTimer.Start();
		}

		void UpdateProbeTimer_Tick(object sender, EventArgs e)
		{
			_UpdateProbeTimer.Stop();
			var o = SettingsManager.Options;
			// The option may have been switched off while the timer was waiting.
			if (!o.CheckForUpdates)
				return;
			var client = new UpdateClient(new Version(Application.ProductVersion));
			var etag = o.UpdateEtag;
			Task.Run(() => client.Check(etag)).ContinueWith(t =>
			{
				var check = t.Result;
				o.LastUpdateCheck = DateTime.Now;
				if (check.ETag != null)
					o.UpdateEtag = check.ETag;
				// A failure at start says nothing; the update window reports when asked.
				if (check.Outcome == UpdateOutcome.Available)
					SetHeaderInfo("Version {0} is available. Options → Update installs it.", check.Version);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		void DisposeUpdateProbe()
		{
			if (_UpdateProbeTimer == null)
				return;
			_UpdateProbeTimer.Dispose();
			_UpdateProbeTimer = null;
		}
	}
}
