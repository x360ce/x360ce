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
			TasksDataGridView.DataSource = data;
			queueTimer = new JocysCom.ClassLibrary.Threading.QueueTimer(500, 1000);
			queueTimer.DoAction = DoAction;
		}

		JocysCom.ClassLibrary.Threading.QueueTimer queueTimer;
		SortableBindingList<CloudItem> data;

		public void Add(CloudAction action, params Game[] games)
		{
			for (int i = 0; i < games.Length; i++)
			{
				var item = new CloudItem
				{
					Action = action,
					Date = DateTime.Now,
					Item = games[i],
					State = CloudState.None,
				};
				data.Add(item);
			}
		}

		void DoAction(object state)
		{
			MainForm.Current.LoadingCircle = true;
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			var gamesToDelete = data.Where(x => x.Action == CloudAction.Delete).Select(x => (Game)x.Item).ToList();
			try
			{
				var result = ws.SetGames(CloudAction.Delete, gamesToDelete);
				// If update was successful then.
				if (string.IsNullOrEmpty(result))
				{
					var gamesToUpdate = data.Where(x => x.Action == CloudAction.Update).Select(x => (Game)x.Item).ToList();
					result = ws.SetGames(CloudAction.Update, gamesToDelete);
				}
				if (!string.IsNullOrEmpty(result))
				{
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, result);
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
