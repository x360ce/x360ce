using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Threading;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	/// <summary>
	/// Interaction logic for IssuesControl.xaml
	/// </summary>
	public partial class IssuesControl : UserControl
	{
		public IssuesControl()
		{
			//InitHelper.InitTimer(this, InitializeComponent);
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			NoIssuesPanel.Visibility = Visibility.Collapsed;
			LinePanel.Visibility = Visibility.Collapsed;
			ExceptionInfoButton.Visibility = Visibility.Collapsed;
			RunStateLabel.Content = "";
		}

		private System.Windows.Forms.Timer QueueMonitorTimer;

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
				//var count = TasksTimer.Queue.Count;
				//ControlsHelper.SetText(f.CloudMessagesLabel, "M: {0}", count);
			}
		}

		/// <summary>
		/// This function will run on different thread than UI. Make sure to use Invoke for interface update.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		void QueueTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			// Return if suspended function is not set.
			if (IsSuspended?.Invoke() ?? true)
				return;
			CheckAll();
			// Update list will affect Warnings list, which is bound to MainDataGrid on UI,
			// therefore use Invoke to make sure that it runs on the same thread as UI.
			ControlsHelper.Invoke(() =>
			{
				UpdateDisplayedList();
			});
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
			var v = sb.ToString();
			if (!Equals(StatusLabel.Content, v))
				StatusLabel.Content = v;
			UpdateIgnoreAllButton();
			UpdateNoIssuesPanel();
		}

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
			CheckCompleted?.Invoke(this, new EventArgs());
		}


		public Func<bool> IsSuspended;

		// List of warnings to show.
		public BindingListInvoked<IssueItem> Warnings;
		BindingListInvoked<IssueItem> IssueList = new BindingListInvoked<IssueItem>();

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
				ExceptionInfoButton.Visibility = Visibility.Visible;
			}
		}

		private void Item_Fixed(object sender, EventArgs e)
		{
			var ii = (IssueItem)sender;
			if (ii.LastException != null)
			{
				LastException = ii.LastException;
				ExceptionInfoButton.Visibility = Visibility.Visible;
			}
			TasksTimer.DoActionNow();
			TasksTimer.SleepTimerStart();
		}
		private void Item_Fixing(object sender, EventArgs e)
		{
			TasksTimer.SleepTimerStop();
		}

		public bool IsDisposing;

		//private void WarningsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		//{
		//	if (e.RowIndex < 0 || e.ColumnIndex < 0)
		//		return;
		//	var grid = (DataGridView)sender;
		//	var row = grid.Rows[e.RowIndex];
		//	var column = grid.Columns[e.ColumnIndex];
		//	var item = (IssueItem)row.DataBoundItem;
		//	if (column == SeverityColumn)
		//	{
		//		switch (item.Severity)
		//		{
		//			case IssueSeverity.None:
		//				e.Value = null;
		//				break;
		//			case IssueSeverity.Low:
		//				e.Value = SeverityImageList.Images["Information"];
		//				break;
		//			case IssueSeverity.Important:
		//				e.Value = SeverityImageList.Images["Warning"];
		//				break;
		//			case IssueSeverity.Moderate:
		//				e.Value = SeverityImageList.Images["Warning"];
		//				break;
		//			case IssueSeverity.Critical:
		//				e.Value = SeverityImageList.Images["Error"];
		//				break;
		//			default:
		//				break;
		//		}
		//	}
		//	else if (column == SolutionColumn)
		//	{
		//		e.Value = item.Status == IssueStatus.Fixing ? "Please Wait..." : item.FixName;
		//	}
		//	else if (column == MoreColumn)
		//	{
		//		e.Value = item.MoreInfo is null ? "" : "More...";
		//	}
		//}

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
			//var lrt = TasksTimer.LastActionDoneTime;
			//var lastRun = string.Format("Last Done: {0:00}:{1:00}", lrt.Minutes, lrt.Seconds + (lrt.Milliseconds / 1000m));
			var state = TasksTimer.IsRunning ? "↑" : " ";
			ControlsHelper.SetText(RunStateLabel, state);
		}

		#endregion

		void UpdateIgnoreButton()
		{
			var enabled = MainDataGrid.SelectedItems.Count > 0;
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
			var check = IsIgnoreAll();
			ControlsHelper.SetChecked(IgnoreAllButton, check);
		}

		private void IgnoreAllButton_Click(object sender, RoutedEventArgs e)
		{
			var ignoreAll = IsIgnoreAll();
			var items = IssueList.ToArray();
			foreach (var item in items)
				item.IsEnabled = ignoreAll;
			TasksTimer.DoActionNow();
		}

		private void IgnoreButton_Click(object sender, RoutedEventArgs e)
		{
			var items = MainDataGrid.SelectedItems.Cast<IssueItem>().ToArray();
			foreach (var item in items)
				item.IsEnabled = false;
			TasksTimer.DoActionNow();
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateIgnoreButton();
		}

		private void HyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			ControlsHelper.OpenUrl(e.Uri.AbsoluteUri);
		}

		private void SolutionButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (IssueItem)button.Tag;
			Task.Factory.StartNew(new Action(() =>
			{
				item.Fix();
			}));
		}

		private void ExceptionInfoButton_Click(object sender, RoutedEventArgs e)
		{
			var message = JocysCom.ClassLibrary.Runtime.LogHelper.ExceptionToText(LastException);
			MessageBox.Show(message, LastException.Message, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.AllowLoad(this))
				Load();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.AllowUnload(this))
				Unload();
		}

		public void Load()
		{
			// List which contains all issues.
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			//IssueList = new BindingListInvoked<IssueItem>();
			UpdateIgnoreAllButton();
			// List which is bound to the grid and displays issues, which needs user attention.
			Warnings = new BindingListInvoked<IssueItem>();
			Warnings.SynchronizingObject = scheduler;
			ControlsHelper.SetItemsSource(MainDataGrid, Warnings);
			UpdateIgnoreButton();
			// Timer which checks for the issues.
			var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			var title = ai.GetTitle(true, true, true, true, false) + " - Issues";
			TasksTimer = new QueueTimer<object>(0, 0);
			TasksTimer.DoWork += QueueTimer_DoWork;
			TasksTimer.Queue.ListChanged += Data_ListChanged;
			// Start monitoring tasks queue.
			QueueMonitorTimer = new System.Windows.Forms.Timer();
			QueueMonitorTimer.Tick += QueueMonitorTimer_Tick;
			QueueMonitorTimer.Start();
		}

		public void Unload()
		{
			IsDisposing = true;
			// Clear list.
			var items = IssueList?.ToArray();
			IssueList?.Clear();
			if (items != null)
			{
				// Remove events.
				foreach (var item in items)
				{
					item.Checking -= Item_Checking;
					item.Checked -= Item_Checked;
					item.Fixing -= Item_Fixing;
					item.Fixed -= Item_Fixed;
				}
			}
			ControlsHelper.SetItemsSource(MainDataGrid, null);
			if (Warnings != null)
			{
				Warnings.SynchronizingObject = null;
				Warnings.Clear();
				Warnings = null;
			}
			if (TasksTimer != null)
			{
				TasksTimer.DoWork -= QueueTimer_DoWork;
				TasksTimer.Queue.ListChanged -= Data_ListChanged;
				TasksTimer.Dispose();
			}
			if (QueueMonitorTimer != null)
			{
				QueueMonitorTimer.Tick -= QueueMonitorTimer_Tick;
				QueueMonitorTimer.Dispose();
			}
		}
	}
}
