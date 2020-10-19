using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class CloudUserControl : UserControl
	{

		public CloudUserControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			ControlsHelper.ApplyBorderStyle(TasksDataGridView);
			EngineHelper.EnableDoubleBuffering(TasksDataGridView);
			//TasksTimer.Queue.AsynchronousInvoke = true;
			TasksDataGridView.AutoGenerateColumns = false;
			// Suspend errors.
			TasksDataGridView.DataError += TasksDataGridView_DataError;
			// Enable task timer.
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			Global.CloudClient.StartServer(scheduler, this);
			// Display cloud queue results.
			EnableDataSource(true);
			// Force to create handle.
			var handle = this.Handle;
			// Start monitoring tasks queue.
			QueueMonitorTimer.Start();
		}

		public void EnableDataSource(bool enable)
		{
			TasksDataGridView.DataSource = enable ? Global.CloudClient.TasksTimer.Queue : null;
			Global.CloudClient.TasksTimer.Queue.SynchronizingObject = enable ? ControlsHelper.MainTaskScheduler : null;
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		private void TasksDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			e.ThrowException = false;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>
		/// Re-upload all data to the cloud.
		/// </summary>
		private void UploadToCloudButton_Click(object sender, EventArgs e)
		{
			Task.Run(new Action(() =>
			{
				Global.CloudClient.TasksTimer.Queue.Clear();
				// Add user configuration data for upload.
				AddInsert(SettingsManager.UserGames.Items.ToArray());
				AddInsert(SettingsManager.UserDevices.Items.ToArray());
				AddInsert(SettingsManager.UserInstances.Items.ToArray());
				AddInsert(SettingsManager.UserSettings.Items.ToArray());
			}));
		}

		private void AddInsert<T>(T[] items) where T : IChecksum
		{
			var arr = items.ToArray();
			EngineHelper.UpdateChecksums(arr);
			Global.CloudClient.Add(CloudAction.Insert, arr);
		}

		/// <summary>
		/// Download all data from the cloud.
		/// </summary>
		private void DownloadFromCloudButton_Click(object sender, EventArgs e)
		{
			Task.Run(new Action(() =>
			{
				List<Guid> userDeviceChecksums;
				lock (SettingsManager.UserDevices.SyncRoot)
					userDeviceChecksums = EngineHelper.UpdateChecksums(SettingsManager.UserDevices.Items.ToArray());
				Global.CloudClient.Add(CloudAction.Select, new UserDevice[0], userDeviceChecksums.ToArray());
				var userGameChecksums = EngineHelper.UpdateChecksums(SettingsManager.UserGames.Items.ToArray());
				Global.CloudClient.Add(CloudAction.Select, new UserGame[0], userGameChecksums.ToArray());
			}));
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			Task.Run(new Action(() =>
			{
				Global.CloudClient.TasksTimer.Queue.Clear();
			}));
		}

		private void QueueMonitorTimer_Tick(object sender, EventArgs e)
		{
			var t = Global.CloudClient.TasksTimer;
			var nextRunTime = t.NextRunTime;
			TimeSpan remains = new TimeSpan();
			if (nextRunTime.Ticks > 0)
				remains = nextRunTime.Subtract(DateTime.Now);
			var nextRun = string.Format("Next Run {2}: {0:00}:{1:00}", remains.Minutes, remains.Seconds + (remains.Milliseconds / 1000m), Global.CloudClient.IsNetworkAvailable ? "ON" : "OFF");
			ControlsHelper.SetText(NextRunLabel, nextRun);
			var lrt = t.LastActionDoneTime;
			var lastRun = string.Format("Last Done: {0:00}:{1:00}", lrt.Minutes, lrt.Seconds + (lrt.Milliseconds / 1000m));
			//ControlsHelper.SetText(LastDoneLabel, lastRun);
			var state = t.IsRunning ? "↑" : " ";
			ControlsHelper.SetText(RunStateLabel, state);
			//ControlsHelper.SetText(AddCountLabel, string.Format("Add: {0}", queueTimer.AddCount));
			//ControlsHelper.SetText(StartCountLabel, string.Format("Start: {0}", queueTimer.StartCount));
			//ControlsHelper.SetText(ThreadCountLabel, string.Format("Thread: {0}", queueTimer.ThreadCount));
			//ControlsHelper.SetText(ActionCountLabel, string.Format("Action: {0}", queueTimer.ActionCount));
			//ControlsHelper.SetText(ActionNoneCountLabel, string.Format("Action (null): {0}", queueTimer.ActionNoneCount));
		}

		private void TasksDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var item = TasksDataGridView.SelectedRows.Cast<DataGridViewRow>().Select(x => (CloudItem)x.DataBoundItem).FirstOrDefault();
			if (item == null)
				return;
			var error = item.Error;
			if (error == null)
				return;
			var message = JocysCom.ClassLibrary.Runtime.LogHelper.ExceptionToText(error);
			MessageBoxForm.Show(message, error.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

		}

		private void TasksDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = TasksDataGridView;
			var item = (CloudItem)grid.Rows[e.RowIndex].DataBoundItem;
			var column = grid.Columns[e.ColumnIndex];
			if (column == TryColumn && item.Retries != int.MaxValue)
				e.Value = string.Format("{0}/{1}", item.Try, item.Retries);
		}
	}
}
