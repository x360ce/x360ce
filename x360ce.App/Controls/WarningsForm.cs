using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.App.Issues;

namespace x360ce.App
{
	public partial class WarningsForm : Form
	{
		public WarningsForm()
		{
			InitializeComponent();
			checkTimer = new System.Timers.Timer();
			checkTimer.Interval = 5000;
			checkTimer.AutoReset = false;
			checkTimer.SynchronizingObject = this;
			checkTimer.Elapsed += CheckTimer_Elapsed;
			WarningsDataGridView.AutoGenerateColumns = false;
			WarningsDataGridView.DataSource = Warnings;
			Text = EngineHelper.GetProductFullName() + " - Warnings";
		}

		static object currentLock = new object();
		static WarningsForm _Current;
		public static WarningsForm Current
		{
			get
			{
				lock (currentLock)
				{
					if (_Current == null)
					{
						_Current = new WarningsForm();
					}
					return _Current;
				}
			}
		}

		public static void CheckAndOpen()
		{
			Current.CheckAll();
			Current.checkTimer.Start();
		}

		private void CheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock (checkTimerLock)
			{
				// If timer is disposed then return;
				if (checkTimer == null) return;
				CheckAll();

			}
			checkTimer.Start();
		}

		BindingList<WarningItem> Warnings = new BindingList<WarningItem>();
		object checkTimerLock = new object();
		System.Timers.Timer checkTimer;
		bool IgnoreAll;

		List<WarningItem> IssueList;
		object IssueListLock = new object();

		void CheckAll()
		{
			lock (IssueListLock)
			{
				if (IssueList == null)
				{
					IssueList = new List<WarningItem>();
					IssueList.Add(new DirectXIssue());
					IssueList.Add(new LeakDetectorIssue());
					IssueList.Add(new MdkIssue());
					IssueList.Add(new ArchitectureIssue());
					IssueList.Add(new IniFileIssue());
                }
			}
			foreach (var issue in IssueList)
			{
				issue.Check();
				UpdateWarning(issue);
            }
			MainForm.Current.BeginInvoke((MethodInvoker)delegate ()
			{
				if (Warnings.Count > 0 && !Visible && !IgnoreAll)
				{

					StartPosition = FormStartPosition.CenterScreen;
					ShowDialog(MainForm.Current);
				}
				else if (Warnings.Count == 0 && Visible)
				{
					DialogResult = DialogResult.OK;
				}
			});
		}

		void UpdateWarning(WarningItem result)
		{
			var item = Warnings.FirstOrDefault(x => x.Name == result.Name);
			if (result.Severity == IssueSeverity.None && item != null) Warnings.Remove(item);
			else if (result.Severity != IssueSeverity.None && item == null) Warnings.Add(result);
		}


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (checkTimerLock)
				{
					if (checkTimer != null)
					{
						checkTimer.Dispose();
						checkTimer = null;
					}
				}
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void Closebutton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void WarningsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (WarningItem)row.DataBoundItem;
				item.Fix();
			}
		}

		private void IgnoreButton_Click(object sender, EventArgs e)
		{
			IgnoreAll = true;
			DialogResult = DialogResult.Cancel;
		}
	}

}
