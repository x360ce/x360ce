using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Mail;
using JocysCom.ClassLibrary.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using JocysCom.ClassLibrary.Web.Services;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public class CloudClient : IDisposable
	{
		public QueueTimer<CloudItem> TasksTimer;

		object TasksTimerLock = new object();

		public void StartServer(TaskScheduler scheduler, UserControl control)
		{
			lock (TasksTimerLock)
			{
				if (TasksTimer != null)
					return;
				// No sleep interval: the queue sleeps while nothing waits, an item added wakes it, and an item that waits
				// after a failed try sets the wake for when it may be sent (SleepUntilDue).
				TasksTimer = new QueueTimer<CloudItem>(0, 0, scheduler);
				TasksTimer.SynchronizingObject = null;
				control.HandleCreated += (sender, e) => { TasksTimer.HasHandle = true; };
				control.HandleDestroyed += (sender, e) => { TasksTimer.HasHandle = false; };
				TasksTimer.HasHandle = control.IsHandleCreated;
				TasksTimer.DoWork += queueTimer_DoWork;
				TasksTimer.Queue.ListChanged += Data_ListChanged;
				// When the network comes back, an item that waits after a failed try is sent at once.
				NetworkChange.NetworkAvailabilityChanged += NetworkInformation_NetworkAvailabilityChanged;
				NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
				// Listing the network adapters takes a while; the window is not kept waiting for it. A first look
				// finds the network as it is, not come back.
				Task.Run(() => RefreshNetworkAvailable(false));
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

		/// <summary>Why nothing may be sent to the cloud now, or null when it may.</summary>
		/// <remarks>Enable Internet Features switches every call off; Save Settings to Cloud is the one beneath it.</remarks>
		public static string OffReason
		{
			get
			{
				var o = SettingsManager.Options;
				if (!o.InternetFeatures)
					return "The cloud is off: tick Enable Internet Features on the Options tab's Internet page.";
				if (!o.InternetAutoSave)
					return "The cloud is off: tick Save Settings to Cloud on the Options tab's Internet page.";
				return null;
			}
		}

		/// <summary>Sends a message to the cloud, unless <see cref="OffReason"/> says it may not be sent.</summary>
		public void Add<T>(CloudAction action, T[] items, Guid[] checksums = null)
		{
			if (OffReason != null)
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
			var timer = TasksTimer;
			if (timer != null)
				OnQueue(() => timer.DoActionNow(item));
		}

		/// <summary>Sends what waits in the queue now.</summary>
		public void SendNow()
		{
			var timer = TasksTimer;
			if (timer != null)
				OnQueue(() => timer.DoActionNow());
		}

		/// <summary>Removes every message from the queue.</summary>
		public void ClearQueue()
		{
			var timer = TasksTimer;
			if (timer != null)
				OnQueue(() => timer.ClearQueue());
		}

		#region Handing work to the queue

		/// <summary>The last of the requests handed to the queue; each next one runs after it.</summary>
		Task _handOff = Task.FromResult(0);
		readonly object _handOffLock = new object();

		/// <summary>Runs a request of the queue on a thread of its own, after the requests made before it, and returns at once.</summary>
		/// <remarks>
		/// The queue is changed under its own lock, and while the Cloud page shows it, a change made on the sending thread
		/// waits for the interface thread to draw it. An interface thread that waited for that lock would wait for a
		/// sending thread waiting for it. So nothing that asks the queue for something waits for the lock: not the interface
		/// thread, and not a caller from another thread, so the requests keep the order they were made in.
		/// </remarks>
		void OnQueue(Action request)
		{
			lock (_handOffLock)
				_handOff = _handOff.ContinueWith(_ =>
				{
					try
					{
						request();
					}
					catch (Exception ex)
					{
						// On a thread of its own, nobody waits for the request to see it fail.
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					}
				}, System.Threading.CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		#endregion

		#region Sending

		/// <summary>The largest message sent to the cloud, in bytes of XML. A bigger one is not sent and stops.</summary>
		/// <remarks>
		/// Upload To Cloud sends each kind of record in one message, and the biggest is the game list: 1,000 games,
		/// eight times as many as the program's own game list knows, come to 1.3 MB (CloudClientTest). The service
		/// refuses a request over 4 MB without reading it, so a bigger message could only fail.
		/// </remarks>
		public const int MaxMessageBytes = 3 * 1024 * 1024;

		/// <summary>Sends a message and returns the reply: the web service at the address on the Options page, unless a test sets its own.</summary>
		public Func<CloudMessage, CloudMessage> Execute = ExecuteOnWebService;

		/// <summary>Saves the options after the service's key was read: to the options file, unless a test sets its own.</summary>
		public Action SaveOptions = () => SettingsManager.OptionsData.Save();

		/// <summary>The options the queue sends with: the program's, unless a test sets its own.</summary>
		public Func<Options> ReadOptions = () => SettingsManager.Options;

		readonly Random random = new Random();

		static CloudMessage ExecuteOnWebService(CloudMessage message)
		{
			using (var ws = new WebServiceClient())
			{
				ws.Url = SettingsManager.Options.InternetDatabaseUrl;
				return ws.Execute(message);
			}
		}

		/// <summary>
		/// Sends the queue's next item, if one is due. Runs on the queue's own thread, never on the interface's.
		/// </summary>
		void queueTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			// The item sent is not always the first one, which is the one the timer would remove, so it is removed here.
			e.Keep = true;
			var timer = (QueueTimer<CloudItem>)sender;
			try
			{
				var item = Due(timer.Queue.ToArraySynchronized(), DateTime.Now);
				if (item == null)
				{
					// Nothing to send now: the queue sleeps until the item that waits may go, or an item is added.
					e.Cancel = true;
					return;
				}
				var form = MainForm.Current;
				form?.AddTask(TaskName.CloudCommand);
				var result = Send(item, ReadOptions());
				form?.RemoveTask(TaskName.CloudCommand);
				// An item that waits holds up the items behind it, so they keep their order; a stopped one does not.
				e.Cancel = item.State == CloudState.Error;
				if (result == null)
					return;
				timer.Queue.Remove(item);
				ProcessResult(item.Message, result);
			}
			finally
			{
				SleepUntilDue(timer);
			}
		}

		/// <summary>
		/// Sets the queue to wake when the item that holds it may be sent, and not at all while no item waits: an item
		/// added wakes it, and so does the network coming back.
		/// </summary>
		static void SleepUntilDue(QueueTimer<CloudItem> timer)
		{
			// A queue let go of while it sent wakes no more.
			if (timer.IsDisposing)
				return;
			var item = Next(timer.Queue.ToArraySynchronized());
			var wait = item == null ? 0 : Math.Max(1, (int)Math.Ceiling((item.NextTry - DateTime.Now).TotalMilliseconds));
			timer.ChangeSleepInterval(wait);
		}

		/// <summary>The item the queue sends next, or null: the first item not sent or stopped.</summary>
		static CloudItem Next(IEnumerable<CloudItem> items)
		{
			return items.FirstOrDefault(x => x.State != CloudState.Done && x.State != CloudState.Stopped);
		}

		/// <summary>The item to send now, or null: the first item not sent or stopped, once its wait is over.</summary>
		/// <remarks>
		/// An item that waits holds back the items behind it, so that changes to one record arrive in the order they
		/// were made; a stopped item lets them go.
		/// </remarks>
		public static CloudItem Due(IEnumerable<CloudItem> items, DateTime now)
		{
			var item = Next(items);
			return item != null && item.NextTry <= now ? item : null;
		}

		/// <summary>
		/// Tries once to send an item and returns the service's reply, or null when the try failed: the item then waits
		/// for its next try or has stopped, with the reason in <see cref="CloudItem.Error"/>.
		/// </summary>
		/// <remarks>
		/// A Delete is sent like any other message, also for a record without an ID: the program cannot know which of
		/// its records the cloud holds, and the service answers a Delete that matches nothing with success.
		/// </remarks>
		public CloudMessage Send(CloudItem item, Options o)
		{
			item.Try++;
			Exception error = null;
			var transient = false;
			CloudMessage result = null;
			try
			{
				o.CheckAndFixUserRsaKeys();
				if (string.IsNullOrEmpty(o.CloudRsaPublicKey))
				{
					// Step 1: Get Server's Public RSA key for encryption.
					var keyReply = Execute(new CloudMessage(CloudAction.GetPublicRsaKey));
					if (keyReply.ErrorCode == 0)
					{
						o.CloudRsaPublicKey = keyReply.Values.GetValue<string>(CloudKey.RsaPublicKey);
						SaveOptions();
					}
					else
					{
						error = new Exception(keyReply.ErrorMessage);
					}
				}
				if (error == null)
				{
					Secure(item.Message, o);
					var size = MessageBytes(item.Message);
					if (size > MaxMessageBytes)
						error = new Exception(string.Format("Not sent: the message is {0:N0} KB, more than the {1:N0} KB the online database takes.",
							size / 1024, MaxMessageBytes / 1024));
				}
				if (error == null)
				{
					result = Execute(item.Message);
					// The service's key changed: its reply carries the new key, and the message is sent once more with it.
					var newKey = result.ErrorCode == (int)CloudErrorCode.UnableToDecrypt
						? result.Values.GetValue<string>(CloudKey.RsaPublicKey, null)
						: null;
					if (!string.IsNullOrEmpty(newKey) && newKey != o.CloudRsaPublicKey)
					{
						o.CloudRsaPublicKey = newKey;
						SaveOptions();
						Secure(item.Message, o);
						result = Execute(item.Message);
					}
					// The service read the message and refused it: the same message gets the same answer.
					if (result.ErrorCode != 0)
						error = new Exception(result.ErrorMessage);
				}
			}
			catch (Exception ex)
			{
				error = ex;
				transient = IsTransient(ex);
			}
			if (error != null)
			{
				item.Fail(error, transient, DateTime.Now, random.NextDouble());
				return null;
			}
			item.Error = null;
			item.State = CloudState.Done;
			return result;
		}

		/// <summary>
		/// Secures a message and adds what the service needs to know about this program. Done before every try, it
		/// replaces what the try before added.
		/// </summary>
		public static void Secure(CloudMessage message, Options o)
		{
			CloudHelper.ApplySecurity(message, o.UserRsaPublicKey, o.CloudRsaPublicKey, o.Username, o.Password);
			// Add computer and profile ID.
			message.Values.Add(CloudKey.ComputerId, o.ComputerId, true);
			message.Values.Add(CloudKey.ProfileId, o.ProfileId, true);
			AddClientValues(message);
		}

		/// <summary>The size of a message as XML, about what it adds to a request.</summary>
		public static int MessageBytes(CloudMessage message)
		{
			var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(message, indent: false);
			return Encoding.UTF8.GetByteCount(xml);
		}

		/// <summary>True when a try failed in a way that can pass, so that the same message may succeed later.</summary>
		/// <remarks>
		/// No connection, no answer in time, a connection that broke, or a service that is down or busy (HTTP 5xx, 408 or
		/// 429, or a SOAP fault on the service's side). Anything else, such as an address that is not the service, or a
		/// request the service refuses, gets the same result every time.
		/// </remarks>
		public static bool IsTransient(Exception error)
		{
			var web = error as WebException;
			if (web != null)
			{
				switch (web.Status)
				{
					case WebExceptionStatus.NameResolutionFailure:
					case WebExceptionStatus.ProxyNameResolutionFailure:
					case WebExceptionStatus.ConnectFailure:
					case WebExceptionStatus.ConnectionClosed:
					case WebExceptionStatus.KeepAliveFailure:
					case WebExceptionStatus.PipelineFailure:
					case WebExceptionStatus.ReceiveFailure:
					case WebExceptionStatus.SendFailure:
					case WebExceptionStatus.RequestCanceled:
					case WebExceptionStatus.Timeout:
						return true;
					case WebExceptionStatus.ProtocolError:
						var response = web.Response as HttpWebResponse;
						var code = response == null ? 0 : (int)response.StatusCode;
						return code >= 500 || code == 408 || code == 429;
					default:
						return false;
				}
			}
			var soap = error as SoapException;
			if (soap != null)
				return soap.Code == SoapException.ServerFaultCode;
			return error is IOException;
		}

		/// <summary>
		/// Adds to a message what the web service needs to know about this program: its version and the kinds of
		/// device it reads, so that the service returns no device of another kind.
		/// </summary>
		public static void AddClientValues(CloudMessage message)
		{
			// Add version so it will be possible distinguish between Library (v3.x) and Virtual (v4.x) settings.
			message.Values.Add(CloudKey.ClientVersion, typeof(CloudClient).Assembly.GetName().Version.ToString());
			// A number travels as one; the enumeration itself would be sent as XML text.
			message.Values.Add(CloudKey.InputSourceTypes, (int)(InputSourceType.DirectInput | InputSourceType.RawInput));
		}

		#endregion

		private void Data_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If item added or deleted then...
			if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
			{
				var label = MainForm.Current?.CloudMessagesLabel;
				if (label == null)
					return;
				// update main form status bar.
				ControlsHelper.SetText(label, "M: {0}", TasksTimer.Queue.Count);
			}
		}

		void ProcessResult(CloudMessage command, CloudMessage result)
		{
			// No window to show the reply in, as in a test.
			if (MainForm.Current == null)
				return;
			// Execute on interface thread.
			if (MainForm.Current.InvokeRequired)
			{
				MainForm.Current.Invoke(new Action(() => ProcessResult(command, result)));
				return;
			}
			switch (command.Action)
			{
				case CloudAction.Select:
					if (result.UserGames != null)
					{
						MainForm.Current.Main.GameSettingsPanel.ImportAndBindItems(result.UserGames);
						if (!string.IsNullOrEmpty(result.ErrorMessage))
							if (result.ErrorCode != 0)
								MainForm.Current.SetHeaderError(result.ErrorMessage);
							else
								MainForm.Current.SetHeaderInfo(result.ErrorMessage);
					}
					if (result.UserDevices != null)
					{
						MainForm.Current.Main.DevicesPanel.ImportAndBindItems(result.UserDevices);
						if (!string.IsNullOrEmpty(result.ErrorMessage))
							if (result.ErrorCode != 0)
								MainForm.Current.SetHeaderError(result.ErrorMessage);
							else
								MainForm.Current.SetHeaderInfo(result.ErrorMessage);
					}
					break;
			}
		}



		#region Network Availability

		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			RefreshNetworkAvailable(true);
		}

		/// <summary>Whether a network is up, as the Cloud page shows it.</summary>
		public bool IsNetworkAvailable;

		/// <summary>Reads whether a network is up: from Windows' network adapters, unless a test sets its own.</summary>
		public Func<bool> ReadNetworkAvailable = () => JocysCom.ClassLibrary.Network.NetStatInfo.IsNetworkAvailable();

		public void NetworkInformation_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			RefreshNetworkAvailable(true);
		}

		/// <summary>
		/// Reads whether a network is up, keeping the last answer when Windows cannot list its network adapters, and sends
		/// the item that waits after a failed try at once when the network has come back. Windows tells of a change on a
		/// thread of its own.
		/// </summary>
		/// <remarks>
		/// Windows can fail the list while it is changing it ("The pipe is being closed"). The change that follows
		/// asks again.
		/// </remarks>
		/// <param name="changed">True when Windows told of a change; false for the first look, which finds nothing come back.</param>
		void RefreshNetworkAvailable(bool changed)
		{
			bool available;
			try
			{
				available = ReadNetworkAvailable();
			}
			catch (System.Net.NetworkInformation.NetworkInformationException)
			{
				return;
			}
			var back = changed && available && !IsNetworkAvailable;
			IsNetworkAvailable = available;
			if (back)
				SendWaitingNow();
		}

		/// <summary>Makes the items that wait after a failed try due now, and wakes the queue for them. Their tries count as before.</summary>
		void SendWaitingNow()
		{
			var timer = TasksTimer;
			if (timer == null)
				return;
			var waiting = timer.Queue.ToArraySynchronized().Where(x => x.State == CloudState.Error).ToArray();
			if (waiting.Length == 0)
				return;
			var now = DateTime.Now;
			foreach (var item in waiting)
				item.NextTry = now;
			SendNow();
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Free managed resources.
				StopServer();
			}
		}

		#endregion


	}
}
