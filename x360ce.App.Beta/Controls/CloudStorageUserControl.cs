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
			TasksDataGridView.DataSource = data;
			queueTimer = new JocysCom.ClassLibrary.Threading.QueueTimer(500, 1000);
			queueTimer.DoAction = DoAction;
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

		public void Add<T>(CloudAction action, params T[] items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				var item = new CloudItem
				{
					Action = action,
					Date = DateTime.Now,
					Item = items[i],
					State = CloudState.None,
				};
				data.Add(item);
			}
		}

		bool GamesAction<T>(CloudAction action)
		{
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			var items = data
				.Where(x => x.Action == action)
				.Select(x => x.Item)
				.OfType<T>()
				.ToList();
			var success = true;
			// If there is data to submit.
			if (items.Count > 0)
			{
				string result = null;
				if (typeof(T) == typeof(Game))
				{
					result = ws.SetGames(action, items.Cast<Game>().ToList());
				}
				success = string.IsNullOrEmpty(result);
				if (!success)
				{
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, result);
				}
			}
			ws.Dispose();
			return success;
		}


		void DoAction(object state)
		{
			MainForm.Current.LoadingCircle = true;
			try
			{
				// If update failed then exit.
				if (!GamesAction<Game>(CloudAction.Delete))
					return;
				else if (!GamesAction<Game>(CloudAction.Update))
					return;
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
