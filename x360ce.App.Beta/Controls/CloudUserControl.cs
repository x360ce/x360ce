using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine.Data;
using x360ce.Engine;
using JocysCom.ClassLibrary.ComponentModel;

namespace x360ce.App.Controls
{
	public partial class CloudUserControl : UserControl
	{

		public CloudUserControl()
		{
			InitializeComponent();
			ControlHelper.ApplyBorderStyle(TasksDataGridView);
			EngineHelper.EnableDoubleBuffering(TasksDataGridView);
			data = new SortableBindingList<CloudItem>();
			data.ListChanged += Data_ListChanged;
			TasksDataGridView.AutoGenerateColumns = false;
			TasksDataGridView.DataSource = data;
			// Force to create handle.
			var handle = this.Handle;
			queueTimer = new JocysCom.ClassLibrary.Threading.QueueTimer(500, 1000);
			queueTimer.SynchronizingObject = this;
			queueTimer.DoAction = DoAction;
			queueTimer.DoActionNow();
		}

		private void Data_ListChanged(object sender, ListChangedEventArgs e)
		{
			if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
			{
				var f = MainForm.Current;
				if (f == null) return;
				AppHelper.SetText(f.CloudMessagesLabel, "M: {0}", data.Count);
			}
		}

		JocysCom.ClassLibrary.Threading.QueueTimer queueTimer;
		SortableBindingList<CloudItem> data;

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
					data.Add(item);
				}
			});
		}

		void DoAction(object state)
		{
			if (data.Count == 0) return;
			if (MainForm.Current.InvokeRequired)
			{
				Invoke(new Action(() =>
				{
					MainForm.Current.AddTask(TaskName.SaveToCloud);
				}));
			}
			Exception error;
			try
			{
				error = Execute<UserGame>(CloudAction.Delete);
				if (error == null)
					error = Execute<UserGame>(CloudAction.Insert);
				if (error == null)
					error = Execute<UserDevice>(CloudAction.Delete);
				if (error == null)
					error = Execute<UserDevice>(CloudAction.Insert);
			}
			catch (Exception ex)
			{
				error = ex;
			}
			Invoke(new Action(() =>
			{
				MainForm.Current.RemoveTask(TaskName.SaveToCloud);
				if (error == null)
				{
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Information);
				}
				else
				{
					var body = string.Format("Cloud Error: {0}", error.Message);
					if (error.InnerException != null) body += "\r\n" + error.InnerException.Message;
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, body);
				}
			}));
		}

		/// <summary>
		///  Submit changed data to the cloud.
		/// </summary>
		Exception Execute<T>(CloudAction action)
		{
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			CloudMessage result = null;
			try
			{
				var citems = data.Where(x => x.Action == action);
				var items = citems.Select(x => x.Item).OfType<T>().ToList();
				if (items.Count > 0)
				{
					// Add security.
					var o = SettingsManager.Options;
					var command = CloudHelper.NewMessage(action, o.UserRsaPublicKey, o.CloudRsaPublicKey, o.Username, o.Password);
					command.Values.Add(CloudKey.HashedDiskId, o.HashedDiskId, true);
					//// Add secure credentials.
					//var rsa = new JocysCom.ClassLibrary.Security.Encryption("Cloud");
					//if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
					//{
					//	var username = rsa.RsaEncrypt("username");
					//	var password = rsa.RsaEncrypt("password");
					//	ws.SetCredentials(username, password);
					//}
					// Add changes.
					if (typeof(T) == typeof(UserGame))
					{
						command.UserGames = items as List<UserGame>;
					}
					else if (typeof(T) == typeof(UserDevice))
					{
						command.UserControllers = items as List<UserDevice>;
					}
					result = ws.Execute(command);
					if (result.ErrorCode > 0)
					{
						queueTimer.ChangeSleepInterval(5 * 60 * 1000);
						return new Exception(result.ErrorMessage);
					}
					foreach (var item in citems)
					{
						data.Remove(item);
					}
				}
			}
			catch (Exception ex)
			{
				// Sleep for 5 minutes;
				queueTimer.ChangeSleepInterval(5 * 60 * 1000);
				return ex;
			}
			return null;
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
				queueTimer = null;
			}

			base.Dispose(disposing);
		}

		private void UploadToCloudButton_Click(object sender, EventArgs e)
		{
			data.Clear();
			queueTimer.ChangeSleepInterval(1000);
			var allControllers = SettingsManager.UserDevices.Items.ToArray();
			Add(CloudAction.Insert, allControllers);
			var allGames = SettingsManager.UserGames.Items.ToArray();
			Add(CloudAction.Insert, allGames);
		}

		private void DownloadFromCloudButton_Click(object sender, EventArgs e)
		{
			var device = new UserDevice();
			Add(CloudAction.Select, new UserDevice[] { device });
		}
	}
}
