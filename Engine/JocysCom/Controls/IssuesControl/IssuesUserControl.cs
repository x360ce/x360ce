using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Threading;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	public partial class IssuesUserControl : UserControl
	{
		public IssuesUserControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			// Set tool strip to the same font.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = this.Font);
			NoIssuesPanel.Visible = false;
			LinePanel.Visible = false;
			ExceptionInfoButton.Visible = false;
			// List which contains all issues.
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			IssueList = new BindingListInvoked<IssueItem>();
			UpdateIgnoreAllButton();
			// List which is bound to the grid and displays issues, which needs user attention.
			Warnings = new BindingListInvoked<IssueItem>();
			Warnings.SynchronizingObject = scheduler;
			// Configure data grid.
			ControlsHelper.ApplyBorderStyle(WarningsDataGridView);
			WarningsDataGridView.CellPainting += WarningsDataGridView_CellPainting;
			WarningsDataGridView.AutoGenerateColumns = false;
			WarningsDataGridView.DataSource = Warnings;
			UpdateIgnoreButton();
			// Timer which checks for the issues.
			var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			var title = ai.GetTitle(true, true, true, true, false) + " - Issues";
			Text = title;
			TasksTimer = new QueueTimer<object>(0, 0);
			TasksTimer.DoWork += queueTimer_DoWork;
			TasksTimer.Queue.ListChanged += Data_ListChanged;
			// Start monitoring tasks queue.
			QueueMonitorTimer.Start();
		}

		void UpdateNoIssuesPanel()
		{
			var items = IssueList.Where(x => x.IsEnabled).ToArray();
			var noIssues =
				// List contains enabled issues.
				items.Length > 0 &&
				// There are no unchecked issues or issues with the problem.
				!items.Any(x => !x.Severity.HasValue || x.Severity.Value != IssueSeverity.None);
			// Panel is visible only if all tests are complete and no issues were found.
			ControlsHelper.SetVisible(NoIssuesPanel, noIssues);
			ControlsHelper.SetVisible(LinePanel, noIssues);
		}

		public JocysCom.ClassLibrary.Threading.QueueTimer<object> TasksTimer;

		private void Data_ListChanged(object sender, ListChangedEventArgs e)
		{
			if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
			{
				var count = TasksTimer.Queue.Count;
				//ControlsHelper.SetText(f.CloudMessagesLabel, "M: {0}", count);
			}
		}

		/// <summary>
		/// This function will run on different thread than UI. Make sure to use Invoke for interface update.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		void queueTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			if (IsSuspended())
				return;
			CheckAll();
			// UpdateDisplayedList() writes to the Warnings list bound to the grid and to
			// the status controls, therefore run it on the same thread as the interface.
			ControlsHelper.Invoke(() => UpdateDisplayedList());
		}

		private void UpdateDisplayedList()
		{
			var list = IssueList.ToArray();
			foreach (var item in list)
			{
				// If issue is unchecked or no longer a problem then...
				if (Warnings.Contains(item) && (!item.IsEnabled || !item.Severity.HasValue || item.Severity == IssueSeverity.None))
					// Remove from warnings list.
					Warnings.Remove(item);
				// If issue not found and problem found then...
				else if (!Warnings.Contains(item) && item.IsEnabled && item.Severity.HasValue && item.Severity.Value != IssueSeverity.None)
					// Add to warnings list.
					Warnings.Add(item);
			}
			// Get issues in progress.
			list = list.Where(x => x.Status != IssueStatus.Idle).ToArray();
			var sb = new StringBuilder();
			foreach (var item in list)
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.AppendFormat("{0}/{1} {2}: {3}", IssueList.IndexOf(item), IssueList.Count, item.GetType().Name, item.Status);
			}
			StatusLabel.Text = sb.ToString();
			UpdateIgnoreAllButton();
			UpdateNoIssuesPanel();
		}

		internal bool IsDesignMode { get { return ControlsHelper.IsDesignMode(this); } }

		void CheckAll()
		{
			// Make sure that issues are checked in correct order.
			var issues = IssueList.OrderBy(x => x.OrderId).ToArray();
			foreach (var issue in issues)
			{
				if (IsDisposing)
					return;
				issue.Check();
				if (IsDisposing)
					return;
				// If issue is critical then...
				if (issue.IsEnabled && issue.Severity.HasValue && issue.Severity.Value >= IssueSeverity.Critical)
					// Skip checking other issues.
					break;
			}
			// Assign result to property, because results will be read on a different i.e. main Thread.
			CriticalIssuesCount = IssueList.Count(x => x.IsEnabled && x.Severity.HasValue && x.Severity.Value >= IssueSeverity.Critical);
			ModerateIssuesCount = IssueList.Count(x => x.IsEnabled && x.Severity.HasValue && x.Severity.Value >= IssueSeverity.Moderate);
			var ev = CheckCompleted;
			if (ev != null)
				CheckCompleted(this, new EventArgs());
		}


		public Func<bool> IsSuspended;

		// List of warnings to show.
		public BindingListInvoked<IssueItem> Warnings;
		object checkTimerLock = new object();

		BindingListInvoked<IssueItem> IssueList;
		object IssueListLock = new object();

		public void AddIssues(params IssueItem[] items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				IssueList.Add(items[i]);
				// Assign run order of the issue.
				items[i].OrderId = i;
				items[i].Checking += Item_Checking;
				items[i].Checked += Item_Checked;
				items[i].Fixing += Item_Fixing;
				items[i].Fixed += Item_Fixed;
			}
		}

		public int? CriticalIssuesCount;
		public int? ModerateIssuesCount;

		public event EventHandler CheckCompleted;

		public Exception LastException;

		private void Item_Checking(object sender, EventArgs e)
		{

		}

		private void Item_Checked(object sender, EventArgs e)
		{
			var ii = (IssueItem)sender;
			if (ii.LastException != null)
			{
				LastException = ii.LastException;
				ExceptionInfoButton.Visible = true;
			}
		}

		private void Item_Fixed(object sender, EventArgs e)
		{
			var ii = (IssueItem)sender;
			if (ii.LastException != null)
			{
				LastException = ii.LastException;
				ExceptionInfoButton.Visible = true;
			}
			TasksTimer.DoActionNow();
			TasksTimer.SleepTimerStart();
		}
		private void Item_Fixing(object sender, EventArgs e)
		{
			TasksTimer.SleepTimerStop();
		}

		public bool IsDisposing;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				if (TasksTimer != null)
					TasksTimer.Dispose();
				// Clear list.
				var items = IssueList.ToArray();
				IssueList.Clear();
				// Remove events.
				foreach (var item in items)
				{
					item.Checking -= Item_Checking;
					item.Checked -= Item_Checked;
					item.Fixing -= Item_Fixing;
					item.Fixed -= Item_Fixed;
				}
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void WarningsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var column = grid.Columns[e.ColumnIndex];
			var row = grid.Rows[e.RowIndex];
			var item = (IssueItem)row.DataBoundItem;
			if (column == SolutionColumn)
			{
				Task.Factory.StartNew(new Action(() =>
				{
					item.Fix();
				}));
			}
			if (column == MoreColumn)
			{
				if (item.MoreInfo != null)
					ControlsHelper.OpenUrl(item.MoreInfo.AbsoluteUri);
			}
		}

		private void IgnoreAllButton_Click(object sender, EventArgs e)
		{
			var ignoreAll = IsIgnoreAll();
			var items = IssueList.ToArray();
			foreach (var item in items)
				item.IsEnabled = ignoreAll;
			TasksTimer.DoActionNow();
		}

		private void IgnoreButton_Click(object sender, EventArgs e)
		{
			var viewRows = WarningsDataGridView.SelectedRows.Cast<DataGridViewRow>();
			var items = viewRows.Select(x => (IssueItem)x.DataBoundItem).ToArray();
			foreach (var item in items)
				item.IsEnabled = false;
			TasksTimer.DoActionNow();
		}

		/// <summary>Side of the shield, which is drawn rather than scaled.</summary>
		const int ShieldSize = 16;

		/// <summary>Space between the shield and the label it belongs to.</summary>
		const int ShieldGap = 3;

		/// <summary>Where the shield goes on a button whose label is centred.</summary>
		/// <remarks>
		/// Worked out from where the label actually lands, not from the edge of the cell. The button
		/// centres its own label, so the only place certain to be free is immediately beside it: an icon
		/// at a fixed distance from the left is under the label as soon as the label grows or the column
		/// narrows, which is what happened.
		///
		/// Empty when the label leaves no room. Losing the warning in a column too narrow to hold it is
		/// better than drawing it over the word it is warning about.
		/// </remarks>
		/// <param name="cell">The cell being drawn.</param>
		/// <param name="textWidth">Width of the label, as it will be drawn.</param>
		public static System.Drawing.Rectangle ShieldBounds(System.Drawing.Rectangle cell, int textWidth)
		{
			var textLeft = cell.Left + (cell.Width - textWidth) / 2;
			var left = textLeft - ShieldSize - ShieldGap;
			if (left < cell.Left + ShieldGap)
				return System.Drawing.Rectangle.Empty;
			return new System.Drawing.Rectangle(
				left,
				cell.Top + (cell.Height - ShieldSize) / 2,
				ShieldSize, ShieldSize);
		}

		/// <summary>Puts the Windows shield on a fix that will ask for Administrator.</summary>
		/// <remarks>
		/// Drawn rather than described, because this is the one mark Windows uses everywhere for the
		/// same thing, and a person reads it without being taught. Without it the prompt arrives after
		/// the press, which reads as something having gone wrong rather than as the expected question.
		/// </remarks>
		private void WarningsDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			if (grid.Columns[e.ColumnIndex] != SolutionColumn)
				return;
			var item = grid.Rows[e.RowIndex].DataBoundItem as IssueItem;
			if (item == null || !item.FixNeedsAdmin || item.Severity == IssueSeverity.None)
				return;
			var text = e.FormattedValue as string ?? string.Empty;
			var width = TextRenderer.MeasureText(text, e.CellStyle.Font).Width;
			var bounds = ShieldBounds(e.CellBounds, width);
			if (bounds.IsEmpty)
				return;
			e.Graphics.DrawIcon(System.Drawing.SystemIcons.Shield, bounds);
		}

		private void WarningsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (IssueItem)row.DataBoundItem;
			if (column == SeverityColumn)
			{
				switch (item.Severity)
				{
					case IssueSeverity.None:
						e.Value = null;
						break;
					case IssueSeverity.Low:
						e.Value = SeverityImageList.Images["Information"];
						break;
					case IssueSeverity.Important:
						e.Value = SeverityImageList.Images["Warning"];
						break;
					case IssueSeverity.Moderate:
						e.Value = SeverityImageList.Images["Warning"];
						break;
					case IssueSeverity.Critical:
						e.Value = SeverityImageList.Images["Error"];
						break;
					default:
						break;
				}
			}
			else if (column == SolutionColumn)
			{
				e.Value = item.Status == IssueStatus.Fixing ? "Please Wait..." : item.FixName;
			}
			else if (column == MoreColumn)
			{
				e.Value = item.MoreInfo == null ? "" : "More...";
			}
		}

		#region Queue Monitor

		private void QueueMonitorTimer_Tick(object sender, EventArgs e)
		{
			var nextRunTime = TasksTimer.NextRunTime;
			TimeSpan remains = new TimeSpan();
			if (nextRunTime.Ticks > 0)
			{
				remains = nextRunTime.Subtract(DateTime.Now);
			}
			var nextRun = string.Format("Next Run: {0:00}:{1:00}", remains.Minutes, remains.Seconds + (remains.Milliseconds / 1000m));
			ControlsHelper.SetText(NextRunLabel, nextRun);
			var lrt = TasksTimer.LastActionDoneTime;
			var lastRun = string.Format("Last Done: {0:00}:{1:00}", lrt.Minutes, lrt.Seconds + (lrt.Milliseconds / 1000m));
			var state = TasksTimer.IsRunning ? "↑" : " ";
			ControlsHelper.SetText(RunStateLabel, state);
		}

		#endregion

		private void ExceptionInfoButton_Click(object sender, EventArgs e)
		{
			var message = JocysCom.ClassLibrary.Runtime.LogHelper.ExceptionToText(LastException);
			MessageBox.Show(message, LastException.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void WarningsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateIgnoreButton();
		}

		void UpdateIgnoreButton()
		{
			var enabled = WarningsDataGridView.SelectedRows.Count > 0;
			ControlsHelper.SetEnabled(IgnoreButton, enabled);
		}

		bool IsIgnoreAll()
		{
			// If no enabled issues found.
			var ignoreAllChecked = IssueList.Count > 0 && !IssueList.Any(x => x.IsEnabled);
			return ignoreAllChecked;
		}

		void UpdateIgnoreAllButton()
		{
			// If no enabled issues found.
			ControlsHelper.SetChecked(IgnoreAllButton, IsIgnoreAll());
		}

	}
}
