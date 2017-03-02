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
	public partial class CloudStorageUserControl : UserControl
	{

		public CloudStorageUserControl()
		{
			InitializeComponent();
			data = new SortableBindingList<CloudItem>();
			data.ListChanged += Data_ListChanged;
			TasksDataGridView.AutoGenerateColumns = false;
			TasksDataGridView.DataSource = data;
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

		public void Add<T>(CloudAction action, T[] items)
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
			MainForm.Current.LoadingCircle = true;
			try
			{
				Execute<Game>(CloudAction.Delete);
				Execute<Game>(CloudAction.Insert);
				Execute<UserController>(CloudAction.Delete);
				Execute<UserController>(CloudAction.Insert);
			}
			catch (Exception ex)
			{
				var error = ex.Message;
				if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
				MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, error);
			}
		}

		/// <summary>
		///  Submit changed data to the cloud.
		/// </summary>
		void Execute<T>(CloudAction action)
		{
			MainForm.Current.LoadingCircle = true;
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			CloudResults result = null;
			try
			{
				var items = data.Where(x => x.Action == action).Select(x => x.Item).OfType<T>().ToList();
				if (items.Count > 0)
				{
					var command = new CloudCommand();
					command.Action = action;
					if (typeof(T) == typeof(Game))
					{
						command.Games = items as List<Game>;
					}
					else if (typeof(T) == typeof(UserController))
					{
						command.UserControllers = items as List<UserController>;
					}
					// Add secure credentials.
					var rsa = new JocysCom.ClassLibrary.Security.Encryption("Cloud");
					if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
					{
						var username = rsa.RsaEncrypt("username");
						var password = rsa.RsaEncrypt("password");
						ws.SetCredentials(username, password);
					}
					result = ws.Execute(command);
					MainForm.Current.SetHeaderBody(result.ErrorCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error, result.ErrorMessage);
				}
			}
			catch (Exception ex)
			{
				var error = ex.Message;
				if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
				MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, error);
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
			if (queueTimer != null)
			{
				queueTimer.Dispose();
				queueTimer = null;
			}

			base.Dispose(disposing);
		}


	}
}
