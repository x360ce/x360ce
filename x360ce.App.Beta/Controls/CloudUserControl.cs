using JocysCom.ClassLibrary.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(TasksDataGridView);
			EngineHelper.EnableDoubleBuffering(TasksDataGridView);
			queueTimer = new JocysCom.ClassLibrary.Threading.QueueTimer<CloudItem>(0, 5000, this);
			queueTimer.DoWork += queueTimer_DoWork;
			queueTimer.Queue.ListChanged += Data_ListChanged;
			TasksDataGridView.AutoGenerateColumns = false;
			// Suspend errors.
			TasksDataGridView.DataError += TasksDataGridView_DataError;
			// Attach 
			TasksDataGridView.DataSource = queueTimer.Queue;
			// Force to create handle.
			var handle = this.Handle;
			QueueMonitorTimer.Start();
		}

		private void TasksDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			e.ThrowException = false;
		}

		private void Data_ListChanged(object sender, ListChangedEventArgs e)
		{
			if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
			{
				var f = MainForm.Current;
				if (f == null) return;
				var count = queueTimer.Queue.Count;
				AppHelper.SetText(f.CloudMessagesLabel, "M: {0}", count);
			}
		}

		JocysCom.ClassLibrary.Threading.QueueTimer<CloudItem> queueTimer;

		public void Add<T>(CloudAction action, T[] items = null)
		{
			BeginInvoke((MethodInvoker)delegate ()
			{
				var allow = MainForm.Current.OptionsPanel.InternetAutoSaveCheckBox.Checked;
				if (!allow)
				{
					return;
				}
				for (int i = 0; i < items.Length; i++)
				{
					var item = new CloudItem()
					{
						Action = action,
						Date = DateTime.Now,
						Item = items[i],
						State = CloudState.None,
					};
					queueTimer.DoActionNow(item);
				}
			});
		}

		/// <summary>
		/// This function will run on different thread than UI. Make sure to use Invoke for interface update.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		void queueTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			var item = e.Item as CloudItem;
			if (item == null)
				return;
			item.Try++;
			MainForm.Current.Invoke((Action)delegate ()
			{
				MainForm.Current.AddTask(TaskName.SaveToCloud);
			});
			Exception error = null;
			try
			{
				var ws = new WebServiceClient();
				ws.Url = SettingsManager.Options.InternetDatabaseUrl;
				CloudMessage result = null;
				// Add security.
				var o = SettingsManager.Options;
				var command = CloudHelper.NewMessage(item.Action, o.UserRsaPublicKey, o.CloudRsaPublicKey, o.Username, o.Password);
				command.Values.Add(CloudKey.ComputerId, o.ComputerId, true);
				//// Add secure credentials.
				//var rsa = new JocysCom.ClassLibrary.Security.Encryption("Cloud");
				//if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
				//{
				//	var username = rsa.RsaEncrypt("username");
				//	var password = rsa.RsaEncrypt("password");
				//	ws.SetCredentials(username, password);
				//}
				// Add changes.
				if (item.Item.GetType() == typeof(UserGame))
				{
					command.UserGames = new List<UserGame>() { (UserGame)item.Item };
				}
				else if (item.Item.GetType() == typeof(UserDevice))
				{
					command.UserDevices = new List<UserDevice>() { (UserDevice)item.Item };
				}
				result = ws.Execute(command);
				if (result.ErrorCode > 0)
				{
					error = new Exception(result.ErrorMessage);
				}
				ws.Dispose();
			}
			catch (Exception ex)
			{
				error = ex;
			}
			MainForm.Current.Invoke((Action)delegate ()
			{
				MainForm.Current.RemoveTask(TaskName.SaveToCloud);
			});
			item.Error = error;
			item.State = error == null ? CloudState.Done : CloudState.Error;
			e.Keep = error != null;
			e.Break = error != null;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			if (queueTimer != null)
			{
				queueTimer.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Reupload all data to the cloud.
		/// </summary>
		private void UploadToCloudButton_Click(object sender, EventArgs e)
		{
			queueTimer.Queue.Clear();
			//queueTimer.ChangeSleepInterval(1000);
			// For test purposes take only one record for processing.
			var allControllers = SettingsManager.UserDevices.Items.Take(1).ToArray();
			Add(CloudAction.Insert, allControllers);
			//Add(CloudAction.Insert, allControllers);
			//var allGames = SettingsManager.UserGames.Items.ToArray();
			//Add(CloudAction.Insert, allGames);
		}

		/// <summary>
		/// Download all data from the cloud.
		/// </summary>
		private void DownloadFromCloudButton_Click(object sender, EventArgs e)
		{
			var device = new UserDevice();
			Add(CloudAction.Select, new UserDevice[] { device });
		}

		private void QueueMonitorTimer_Tick(object sender, EventArgs e)
		{
			var nextRunTime = queueTimer.NextRunTime;
			TimeSpan remains = new TimeSpan();
			if (nextRunTime.Ticks > 0)
			{
				remains = nextRunTime.Subtract(DateTime.Now);
			}
			var nextRun = string.Format("Next Run: {0:00}:{1:00}", remains.Minutes, remains.Seconds + (remains.Milliseconds / 1000m));
			AppHelper.SetText(NextRunLabel, nextRun);
			var lrt = queueTimer.LastActionDoneTime;
			var lastRun = string.Format("Last Done: {0:00}:{1:00}", lrt.Minutes, lrt.Seconds + (lrt.Milliseconds / 1000m));
			//AppHelper.SetText(LastDoneLabel, lastRun);
			var state = queueTimer.IsRunning ? "↑" : " ";
			AppHelper.SetText(RunStateLabel, state);
			//AppHelper.SetText(AddCountLabel, string.Format("Add: {0}", queueTimer.AddCount));
			//AppHelper.SetText(StartCountLabel, string.Format("Start: {0}", queueTimer.StartCount));
			//AppHelper.SetText(ThreadCountLabel, string.Format("Thread: {0}", queueTimer.ThreadCount));
			//AppHelper.SetText(ActionCountLabel, string.Format("Action: {0}", queueTimer.ActionCount));
			//AppHelper.SetText(ActionNoneCountLabel, string.Format("Action (null): {0}", queueTimer.ActionNoneCount));
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			queueTimer.Queue.Clear();
		}

		private void TasksDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex < 0 || e.RowIndex < 0)
				return;
			var item = TasksDataGridView.SelectedRows.Cast<DataGridViewRow>().Select(x => (CloudItem)x.DataBoundItem).FirstOrDefault();
			if (item == null)
				return;
			var error = item.Error;
			if (error == null)
				return;
			MessageBoxForm.Show(error.ToString(), error.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
