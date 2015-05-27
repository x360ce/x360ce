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

		public void Add(Game game, CloudAction action)
		{
			var item = new CloudItem
			{
				Action = action,
				Date = DateTime.Now,
				Item = game,
				State = CloudState.None,
			};
			data.Add(item);
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
				// If update was successfull then.
				if (string.IsNullOrEmpty(result))
				{
					var gamesToUpdate = data.Where(x => x.Action == CloudAction.Update).Select(x => (Game)x.Item).ToList();
					result = ws.SetGames(CloudAction.Update, gamesToDelete);
				}
			}
			catch (Exception)
			{
			}
		}

		//void GetPrograms()
		//{
		//	ws.SetGamesCompleted += ws_SetGamesCompleted;
		//	System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
		//	{
		//		ws.SetGamesCompleted(enabled, minInstances);
		//	});
		//}

		//void ws_SetGamesCompleted(object sender, ResultEventArgs e)
		//{
		//	// Make sure method is executed on the same thread as this control.
		//	BeginInvoke((MethodInvoker)delegate()
		//	{
		//		MainForm.Current.LoadingCircle = false;
		//		if (e.Error != null)
		//		{
		//			var error = e.Error.Message;
		//			if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
		//			MainForm.Current.UpdateHelpHeader(error, MessageBoxIcon.Error);
		//		}
		//		else if (e.Result == null)
		//		{
		//			MainForm.Current.UpdateHelpHeader("No results were returned by the web service!", MessageBoxIcon.Error);
		//		}
		//		else
		//		{
		//			var result = (List<x360ce.Engine.Data.Program>)e.Result;
		//			ImportAndBindPrograms(result);
		//		}
		//	});
		//}

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
