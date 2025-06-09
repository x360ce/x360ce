using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Mail;
using JocysCom.ClassLibrary.Threading;
using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public class CloudClient : IDisposable
	{
		public QueueTimer<CloudItem> TasksTimer;

		object TasksTimerLock = new object();

		public void StartServer(TaskScheduler scheduler, UserControl control = null)
		{
			lock (TasksTimerLock)
			{
				if (TasksTimer != null)
					return;
				TasksTimer = new QueueTimer<CloudItem>(0, 5000, scheduler);
				TasksTimer.SynchronizingObject = null;
				if (control == null)
				{
					TasksTimer.HasHandle = true;

				}
				else
				{
					control.HandleCreated += (sender, e) => { TasksTimer.HasHandle = true; };
					control.HandleDestroyed += (sender, e) => { TasksTimer.HasHandle = false; };
					TasksTimer.HasHandle = control.IsHandleCreated;
				}
				TasksTimer.DoWork += queueTimer_DoWork;
				TasksTimer.Queue.ListChanged += Data_ListChanged;
				// Enable network monitoring.
				// Make sure settings are synchronized when connection is available only.
				NetworkChange.NetworkAvailabilityChanged += NetworkInformation_NetworkAvailabilityChanged;
				NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
				IsNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
			}
		}

		public void StopServer()
		{
			lock (TasksTimerLock)
			{
				if (TasksTimer == null)
					return;
				TasksTimer.Dispose();
				TasksTimer = null;
				// Disable network monitoring.
				NetworkChange.NetworkAvailabilityChanged -= NetworkInformation_NetworkAvailabilityChanged;
				NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
				// Monitoring not available.
				IsNetworkAvailable = false;
			}
		}

		public void Add<T>(CloudAction action, T[] items, Guid[] checksums = null)
		{
			var o = SettingsManager.Options;
			var allow = o.InternetAutoSave;
			if (!allow)
				return;
			var message = new CloudMessage(action);
			// Try to assign list.
			message.Checksums = checksums;
			message.UserGames = items as UserGame[];
			message.UserSettings = items as UserSetting[];
			message.UserDevices = items as UserDevice[];
			message.UserComputers = items as UserComputer[];
			message.UserInstances = items as UserInstance[];
			message.MailMessages = items as MailMessageSerializable[];
			var item = new CloudItem()
			{
				Date = DateTime.Now,
				Message = message,
				State = CloudState.None,
			};
			TasksTimer.DoActionNow(item);
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
			Global.HMan.AddTask(TaskName.CloudCommand);
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
					// Add computer and profile ID.
					item.Message.Values.Add(CloudKey.ComputerId, o.ComputerId, true, true);
					item.Message.Values.Add(CloudKey.ProfileId, o.ProfileId, true, true);
					// Add version so it will be possible distinguish between Library (v3.x) and Virtual (v4.x) settings.
					item.Message.Values.Add(CloudKey.ClientVersion, Application.ProductVersion, false, true);
					// Call web service.
					result = ws.Execute(item.Message);
					if (result.ErrorCode != 0)
					{
						// If unable to decrypt error then...
						if (result.ErrorCode == (int)CloudErrorCode.UnableToDecrypt)
						{
							// Get server's RSA Public key.
							var cloudRsaPublicKey = result.Values.GetValue<string>(CloudKey.RsaPublicKey, null);
							// If key was set then update local key.
							if (!string.IsNullOrEmpty(cloudRsaPublicKey))
								o.CloudRsaPublicKey = cloudRsaPublicKey;
						}
						error = new Exception(result.ErrorMessage);
					}
					else
					{
						ProcessResult(item.Message, result);
					}
				}
				ws.Dispose();
			}
			catch (Exception ex)
			{
				error = ex;
			}
			Global.HMan.RemoveTask(TaskName.CloudCommand);
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

		private void Data_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If item added or deleted then...
			if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
			{
				var label = Global._MainWindow?.MainPanel?.CloudMessagesLabel;
				if (label == null)
					return;
				// update main form status bar.
				ControlsHelper.SetText(label, "M: {0}", TasksTimer.Queue.Count);
			}
		}

		void ProcessResult(CloudMessage command, CloudMessage result)
		{
			// Execute on interface thread.
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(new Action(() => ProcessResult(command, result)));
				return;
			}
			switch (command.Action)
			{
				case CloudAction.Select:
					if (result.UserGames != null)
					{
						Global._MainWindow.UserProgramsPanel.ListPanel.ImportAndBindItems(result.UserGames);
						if (!string.IsNullOrEmpty(result.ErrorMessage))
							if (result.ErrorCode != 0)
								Global.HMan.SetBodyError(result.ErrorMessage);
							else
								Global.HMan.SetBodyInfo(result.ErrorMessage);
					}
					if (result.UserDevices != null)
					{
						Global._MainWindow.MainBodyPanel.DevicesPanel.ImportAndBindItems(result.UserDevices);
						if (!string.IsNullOrEmpty(result.ErrorMessage))
							if (result.ErrorCode != 0)
								Global.HMan.SetBodyError(result.ErrorMessage);
							else
								Global.HMan.SetBodyInfo(result.ErrorMessage);
					}
					break;
				case CloudAction.CheckUpdates:
					Global._MainWindow.ProcessUpdateResults(result);
					break;
			}
		}



		#region ■ Network Availability

		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			IsNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
		}

		public bool IsNetworkAvailable;

		public void NetworkInformation_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			IsNetworkAvailable = JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();
		}

		#endregion

		#region ■ IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool IsDisposing;

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				// Free managed resources.
				StopServer();
			}
		}

		#endregion


	}
}
