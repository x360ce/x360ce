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
			checkTimer.Interval = 1000;
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
			if (checkTimer.Interval != 5000) checkTimer.Interval = 5000;
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
					IssueList.Add(new ExeFileIssue());
					IssueList.Add(new DirectXIssue());
					IssueList.Add(new LeakDetectorIssue());
					IssueList.Add(new MdkIssue());
					IssueList.Add(new ArchitectureIssue());
					IssueList.Add(new GdbFileIssue());
					IssueList.Add(new IniFileIssue());
					IssueList.Add(new DllFileIssue());
					foreach (var item in IssueList)
					{
						item.FixApplied += Item_FixApplied;
					}
				}
			}
			bool clearRest = false;
			foreach (var issue in IssueList)
			{
				if (clearRest) issue.Severity = IssueSeverity.None;
				else issue.Check();
				if (issue.Severity == IssueSeverity.Critical) clearRest = true;
				UpdateWarning(issue);
			}
			MainForm.Current.BeginInvoke((MethodInvoker)delegate ()
			{
				var update2 = MainForm.Current.update2Enabled;
				if (Warnings.Count > 0)
				{
					// If not visible and must not ignored then...
					if (!Visible && !IgnoreAll)
					{
						StartPosition = FormStartPosition.CenterParent;
						var result = ShowDialog(MainForm.Current);
						// If ignore button was used then...
						if (IgnoreAll)
						{
							// If critical issues remaining then...
							if (Warnings.Any(x => x.Severity == IssueSeverity.Critical))
							{
								// Close application.
								MainForm.Current.Close();
							}
							// Update 2 haven't ran yet then..
							else if (!update2.HasValue)
							{
								MainForm.Current.update2Enabled = true;
							}
						}
					}
				}
				else
				{
					if (Visible) DialogResult = DialogResult.OK;
					if (!update2.HasValue)
					{
						MainForm.Current.update2Enabled = true;
					}
				}
			});
		}

		private void Item_FixApplied(object sender, EventArgs e)
		{
			// Reset check timer.
			lock (checkTimerLock)
			{
				// If timer is disposed then return;
				if (checkTimer == null) return;
				checkTimer.Stop();
				checkTimer.Interval = 500;
				checkTimer.Start();
			}
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
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
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

		private void WarningsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (WarningItem)row.DataBoundItem;
			if (column == SeverityColumn)
			{
				switch (item.Severity)
				{
					case IssueSeverity.None:
						e.Value = null;
						break;
					case IssueSeverity.Low:
						e.Value = Properties.Resources.MessageBoxIcon_Information_32x32;
						break;
					case IssueSeverity.Important:
						e.Value = Properties.Resources.MessageBoxIcon_Warning_32x32;
						break;
					case IssueSeverity.Moderate:
						e.Value = Properties.Resources.MessageBoxIcon_Warning_32x32;
						break;
					case IssueSeverity.Critical:
						e.Value = Properties.Resources.MessageBoxIcon_Error_32x32;
						break;
					default:
						break;
				}

			}
		}
	}

}
