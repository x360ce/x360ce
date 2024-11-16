using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for CloudControl.xaml
	/// </summary>
	public partial class CloudControl : UserControl
	{
		public CloudControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			//TasksTimer.Queue.AsynchronousInvoke = true;
			MainDataGrid.AutoGenerateColumns = false;
		}

		private void QueueMonitorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ControlsHelper.Invoke(() =>
			{
				var t = Global.CloudClient.TasksTimer;
				if (t == null)
					return;
				var nextRunTime = t.NextRunTime;
				TimeSpan remains = new TimeSpan();
				if (nextRunTime.Ticks > 0)
					remains = nextRunTime.Subtract(DateTime.Now);
				var nextRun = string.Format("Next Run {2}: {0:00}:{1:00}", remains.Minutes,
					remains.Seconds + (remains.Milliseconds / 1000m), Global.CloudClient.IsNetworkAvailable ? "ON" : "OFF");
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
			});
		}

		System.Timers.Timer QueueMonitorTimer;

		public void EnableDataSource(bool enable)
		{
			if (Global.CloudClient == null)
				return;
			MainDataGrid.ItemsSource = enable
				? Global.CloudClient.TasksTimer.Queue
				: null;
			Global.CloudClient.TasksTimer.Queue.SynchronizingObject = enable
				? ControlsHelper.MainTaskScheduler
				: null;
		}

		/// <summary>
		/// Re-upload all data to the cloud.
		/// </summary>
		private void UploadToCloudButton_Click(object sender, RoutedEventArgs e)
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
		private void DownloadFromCloudButton_Click(object sender, RoutedEventArgs e)
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

		private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var cell = (DataGridCell)sender;
			var item = MainDataGrid.SelectedItems.Cast<CloudItem>().FirstOrDefault();
			if (item == null)
				return;
			var error = item.Error;
			if (error == null)
				return;
			var message = JocysCom.ClassLibrary.Runtime.LogHelper.ExceptionToText(error);
			MessageBoxWindow.Show(message, error.Message, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons();
		}

		void UpdateButtons()
		{
			var grid = MainDataGrid;
			DeleteButton.IsEnabled = grid.SelectedItems.Count > 0;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			if (Global.CloudClient == null)
				return;
			// Enable task timer.
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			Global.CloudClient.StartServer(scheduler);
			// Display cloud queue results.
			EnableDataSource(true);
			// Start monitoring tasks queue.
			QueueMonitorTimer = new System.Timers.Timer();
			QueueMonitorTimer.Interval = 500;
			QueueMonitorTimer.Elapsed += QueueMonitorTimer_Elapsed;
			QueueMonitorTimer.Start();
			UpdateButtons();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			// Disable in reverse order.
			EnableDataSource(false);
			if (QueueMonitorTimer != null)
			{
				QueueMonitorTimer.Elapsed -= QueueMonitorTimer_Elapsed;
				QueueMonitorTimer.Stop();
				QueueMonitorTimer.Dispose();
			}
			Global.CloudClient?.StopServer();
		}
	}
}
