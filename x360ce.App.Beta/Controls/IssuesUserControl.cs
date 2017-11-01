using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.App.Issues;

namespace x360ce.App.Controls
{
	public partial class IssuesUserControl : UserControl
	{
		public IssuesUserControl()
		{
			InitializeComponent();
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(IssuesDataGridView);
			if (IsDesignMode) return;
			CheckTimer = new System.Timers.Timer();
			CheckTimer.Interval = 1000;
			CheckTimer.AutoReset = false;
			CheckTimer.SynchronizingObject = this;
			CheckTimer.Elapsed += CheckTimer_Elapsed;
			IssuesDataGridView.AutoGenerateColumns = false;
			IssuesDataGridView.DataSource = Warnings;
			Text = EngineHelper.GetProductFullName() + " - Warnings";
		}

	internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

	private void CheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock (checkTimerLock)
			{
				// If timer is disposed then return;
				if (CheckTimer == null) return;
				CheckAll();
			}
			if (CheckTimer.Interval != 5000) CheckTimer.Interval = 5000;
			CheckTimer.Start();
		}

		BindingList<WarningItem> Warnings = new BindingList<WarningItem>();
		object checkTimerLock = new object();
		public System.Timers.Timer CheckTimer;
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
			MainForm.Current.BeginInvoke((MethodInvoker)CheckAllAsync);
		}

		void CheckAllAsync()
		{
			var update2 = MainForm.Current.update2Enabled;
			if (Warnings.Count > 0)
			{
				// If not visible and must not ignored then...
				if (!Visible && !IgnoreAll)
				{
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
				if (!update2.HasValue)
				{
					MainForm.Current.update2Enabled = true;
				}
			}
		}

		private void Item_FixApplied(object sender, EventArgs e)
		{
			// Reset check timer.
			lock (checkTimerLock)
			{
				// If timer is disposed then return;
				if (CheckTimer == null) return;
				CheckTimer.Stop();
				CheckTimer.Interval = 500;
				CheckTimer.Start();
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
					if (CheckTimer != null)
					{
						CheckTimer.Dispose();
						CheckTimer = null;
					}
				}
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void IssuesDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
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
		}

		private void IssuesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex == -1) return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[SeverityColumn.Name];
			var item = (WarningItem)row.DataBoundItem;
			if (e.ColumnIndex == grid.Columns[SeverityColumn.Name].Index)
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
