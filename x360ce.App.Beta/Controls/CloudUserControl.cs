using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Threading;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
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
			if (IsDesignMode) return;
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(TasksDataGridView);
            EngineHelper.EnableDoubleBuffering(TasksDataGridView);
			InitNetworkInformation();
			// Enable task timer.
			TasksTimer = new JocysCom.ClassLibrary.Threading.QueueTimer<CloudItem>(0, 5000, this);
            TasksTimer.DoWork += queueTimer_DoWork;
            TasksTimer.Queue.ListChanged += Data_ListChanged;
            TasksDataGridView.AutoGenerateColumns = false;
            // Suspend errors.
            TasksDataGridView.DataError += TasksDataGridView_DataError;
            // Attach 
            TasksDataGridView.DataSource = TasksTimer.Queue;
            // Force to create handle.
            var handle = this.Handle;
            // Start monitoring tasks queue.
            QueueMonitorTimer.Start();
        }

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		#region Network Availability

		void InitNetworkInformation()
		{
			// Enable network monitoring.
			// Make sure settings are synchronized when connection is available only.
			NetworkChange.NetworkAvailabilityChanged += NetworkInformation_NetworkAvailabilityChanged;
			NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
			isNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
		}

		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			isNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
		}

		bool isNetworkAvailable;

		public void NetworkInformation_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			isNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
		}

		#endregion


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
                var count = TasksTimer.Queue.Count;
                ControlsHelper.SetText(f.CloudMessagesLabel, "M: {0}", count);
            }
        }

        public JocysCom.ClassLibrary.Threading.QueueTimer<CloudItem> TasksTimer;

        void _Add(CloudAction action, object items, Guid[] checksums)
        {
            var message = new CloudMessage(action);
            // Try to assign list.
            message.Checksums = checksums;
            message.UserGames = items as UserGame[];
			message.UserSettings = items as UserSetting[];
            message.UserDevices = items as UserDevice[];
            message.UserComputers = items as UserComputer[];
            message.UserInstances = items as UserInstance[];
            var item = new CloudItem()
            {
                Date = DateTime.Now,
                Message = message,
                State = CloudState.None,

            };
            TasksTimer.DoActionNow(item);
        }

        public void Add(CloudItem item)
        {
            TasksTimer.DoActionNow(item);
        }

        public void Add<T>(CloudAction action, T[] items, bool split = false, Guid[] checksums = null)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                var allow = MainForm.Current.OptionsPanel.InternetAutoSaveCheckBox.Checked;
                if (!allow)
                {
                    return;
                }
                if (split)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        _Add(action, new T[] { items[i] }, checksums);
                    }
                }
                else
                {
                    _Add(action, items, checksums);
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
            Invoke((Action)delegate ()
            {
                MainForm.Current.AddTask(TaskName.CloudCommand);
            });
            Exception error = null;
            try
            {
                var ws = new WebServiceClient();
                ws.Url = SettingsManager.Options.InternetDatabaseUrl;
                CloudMessage result = null;
                var o = SettingsManager.Options;
                // Check if user public keys are present.
                o.CheckAndFixUserRsaKeys();
                // If cloud RSA keys are missing then...
                if (string.IsNullOrEmpty(o.CloudRsaPublicKey))
                {
                    // Step 1: Get Server's Public RSA key for encryption.
                    var msg = new CloudMessage(CloudAction.GetPublicRsaKey);
                    CloudHelper.ApplySecurity(item.Message);
                    msg.Values.Add(CloudKey.RsaPublicKey, o.UserRsaPublicKey);
                    // Retrieve public RSA key.
                    var results = ws.Execute(msg);
                    if (results.ErrorCode == 0)
                    {
                        o.CloudRsaPublicKey = results.Values.GetValue<string>(CloudKey.RsaPublicKey);
                        SettingsManager.OptionsData.Save();
                    }
                    else
                    {
                        error = new Exception(result.ErrorMessage);
                    }
                }
                // If no errors till this point then...
                if (error == null)
                {
                    // Add security.
                    CloudHelper.ApplySecurity(item.Message, o.UserRsaPublicKey, o.CloudRsaPublicKey, o.Username, o.Password);
                    // Add computer Id
                    item.Message.Values.Add(CloudKey.ComputerId, o.ComputerId, true);
					item.Message.Values.Add(CloudKey.ProfileId, o.ProfileId, true);
					result = ws.Execute(item.Message);
                    if (result.ErrorCode > 0)
                    {
                        error = new Exception(result.ErrorMessage);
                    }
                    else
                    {
                        Invoke((Action)delegate ()
                        {
                            ProcessResult(item.Message, result);
                        });
                    }
                }
                ws.Dispose();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Invoke((Action)delegate ()
            {
                MainForm.Current.RemoveTask(TaskName.CloudCommand);
            });
            var success = error == null;
            item.Error = error;
            item.State = success ? CloudState.Done : CloudState.Error;
            // If error or have not finished.
            e.Keep = !success;
            // error and no more retries left then...
            if (!success && item.Try >= item.Retries)
            {
                // Order to remove task.
                e.Keep = false;
            }
            // Exit thread (queue will be processed later)
            e.Cancel = !success;
        }

        void ProcessResult(CloudMessage command, CloudMessage result)
        {
            switch (command.Action)
            {
                case CloudAction.Select:
                    if (result.UserGames != null)
                    {
                        MainForm.Current.GameSettingsPanel.ImportAndBindItems(result.UserGames);
                        if (!string.IsNullOrEmpty(result.ErrorMessage))
                            if (result.ErrorCode > 0)
                                MainForm.Current.SetHeaderError(result.ErrorMessage);
                            else
                                MainForm.Current.SetHeaderInfo(result.ErrorMessage);
                    }
                    if (result.UserDevices != null)
                    {
                        MainForm.Current.DevicesPanel.ImportAndBindItems(result.UserDevices);
                        if (!string.IsNullOrEmpty(result.ErrorMessage))
                            if (result.ErrorCode > 0)
                                MainForm.Current.SetHeaderError(result.ErrorMessage);
                            else
                                MainForm.Current.SetHeaderInfo(result.ErrorMessage);
                    }
                    break;
                case CloudAction.CheckUpdates:
                    MainForm.Current.ProcessUpdateResults(result);
                    break;
            }
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
            if (TasksTimer != null)
            {
                TasksTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Re-upload all data to the cloud.
        /// </summary>
        private void UploadToCloudButton_Click(object sender, EventArgs e)
        {
            TasksTimer.Queue.Clear();
			// Add user configuration data for upload.
			AddInsert(SettingsManager.UserGames.Items.ToArray());
			AddInsert(SettingsManager.UserDevices.Items.ToArray());
            AddInsert(SettingsManager.UserInstances.Items.ToArray());
			AddInsert(SettingsManager.UserSettings.Items.ToArray());
		}

		void AddInsert<T>(T[] items) where T : IChecksum
        {
            var arr = items.ToArray();
            EngineHelper.UpdateChecksums(arr);
            Add(CloudAction.Insert, arr, true);
        }

        /// <summary>
        /// Download all data from the cloud.
        /// </summary>
        private void DownloadFromCloudButton_Click(object sender, EventArgs e)
        {
            var userDeviceChecksums = EngineHelper.UpdateChecksums(SettingsManager.UserDevices.Items.ToArray());
            Add(CloudAction.Select, new UserDevice[0], false, userDeviceChecksums.ToArray());
            var userGameChecksums = EngineHelper.UpdateChecksums(SettingsManager.UserGames.Items.ToArray());
            Add(CloudAction.Select, new UserGame[0], false, userGameChecksums.ToArray());
        }

        private void QueueMonitorTimer_Tick(object sender, EventArgs e)
        {
            var nextRunTime = TasksTimer.NextRunTime;
            TimeSpan remains = new TimeSpan();
            if (nextRunTime.Ticks > 0)
            {
                remains = nextRunTime.Subtract(DateTime.Now);
            }
            var nextRun = string.Format("Next Run {2}: {0:00}:{1:00}", remains.Minutes, remains.Seconds + (remains.Milliseconds / 1000m), isNetworkAvailable ? "ON" : "OFF");
            ControlsHelper.SetText(NextRunLabel, nextRun);
            var lrt = TasksTimer.LastActionDoneTime;
            var lastRun = string.Format("Last Done: {0:00}:{1:00}", lrt.Minutes, lrt.Seconds + (lrt.Milliseconds / 1000m));
            //ControlsHelper.SetText(LastDoneLabel, lastRun);
            var state = TasksTimer.IsRunning ? "↑" : " ";
            ControlsHelper.SetText(RunStateLabel, state);
            //ControlsHelper.SetText(AddCountLabel, string.Format("Add: {0}", queueTimer.AddCount));
            //ControlsHelper.SetText(StartCountLabel, string.Format("Start: {0}", queueTimer.StartCount));
            //ControlsHelper.SetText(ThreadCountLabel, string.Format("Thread: {0}", queueTimer.ThreadCount));
            //ControlsHelper.SetText(ActionCountLabel, string.Format("Action: {0}", queueTimer.ActionCount));
            //ControlsHelper.SetText(ActionNoneCountLabel, string.Format("Action (null): {0}", queueTimer.ActionNoneCount));
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            TasksTimer.Queue.Clear();
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
            var message = JocysCom.ClassLibrary.Runtime.LogHelper.ExceptionToText(error);
            MessageBoxForm.Show(message, error.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        private void TasksDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = TasksDataGridView;
            var item = (CloudItem)grid.Rows[e.RowIndex].DataBoundItem;
            var name = grid.Columns[e.ColumnIndex].Name;
            if (name == TryColumn.Name)
            {
                if (item.Retries != int.MaxValue)
                {
                    e.Value = string.Format("{0}/{1}", item.Try, item.Retries);
                }
            }
        }
    }
}
