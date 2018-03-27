using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Threading;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
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
            NoIssuesPanel.Visible = false;
            LinePanel.Visible = false;
            ExceptionInfoButton.Visible = false;
            // List which contains all issues.
            IssueList = new BindingListInvoked<IssueItem>();
            IssueList.SynchronizingObject = this;
            IssueList.ListChanged += IssueList_ListChanged;
            // List which is bound to the grid and displays issues, which needs user attention.
            Warnings = new BindingListInvoked<IssueItem>();
            Warnings.SynchronizingObject = this;
            Warnings.ListChanged += Warnings_ListChanged;
            // Configure data grid.
            ControlsHelper.ApplyBorderStyle(IssuesDataGridView);
            IssuesDataGridView.AutoGenerateColumns = false;
            IssuesDataGridView.DataSource = Warnings;
            // Timer which checks for the issues.
            var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
            var title = ai.GetTitle(true, true, true, true, false) + " - Issues";
            Text = title;
            TasksTimer = new JocysCom.ClassLibrary.Threading.QueueTimer<object>(0, 5000, this);
            TasksTimer.DoWork += queueTimer_DoWork;
            TasksTimer.Queue.ListChanged += Data_ListChanged;
            // Start monitoring tasks queue.
            QueueMonitorTimer.Start();
        }

        private void Warnings_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
            {
                UpdateNoIssuesPanel();
            }
        }

        void UpdateNoIssuesPanel()
        {
            // Panel is visible only if all tests are complete and no issues were found.
            var noIssues = CheckAllIsComplete && Warnings.Count == 0;
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
            if (!IsSuspended())
                CheckAll();
        }

        private void IssueList_ListChanged(object sender, ListChangedEventArgs e)
        {
            // Get issues in progress.
            var list = IssueList.Where(x => x.Status != IssueStatus.Idle).ToArray();
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.AppendFormat("{0}/{1} {2}: {3}", IssueList.IndexOf(item), IssueList.Count, item.GetType().Name, item.Status);
            }
            StatusLabel.Text = sb.ToString();
        }

        internal bool IsDesignMode { get { return ControlsHelper.IsDesignMode(this); } }

        bool CheckAllIsComplete = false;

        void CheckAll()
        {
            bool clearRest = false;
            foreach (var issue in IssueList)
            {
                if (IsDisposing)
                    return;
                if (clearRest)
                    issue.Severity = IssueSeverity.None;
                else
                    issue.Check();
                if (IsDisposing)
                    return;
                // If issue is critical then...
                if (issue.Severity == IssueSeverity.Critical)
                {
                    // Skip checking other issues.
                    clearRest = true;
                }
                // Try to get issue from warnings list.
                var item = Warnings.FirstOrDefault(x => x.Name == issue.Name);
                // If issue found and not a problem then...
                if (item != null && issue.Severity == IssueSeverity.None)
                    // Remove from the list.
                    Warnings.Remove(item);
                // If issue not found and there is a problem then...
                else if (item == null && issue.Severity != IssueSeverity.None)
                    // Add to the list.
                    Warnings.Add(issue);
            }
            HasIssues = IgnoreAll
                ? false
                : Warnings.Any(x => x.Severity > IssueSeverity.Moderate);
            CheckAllIsComplete = true;
            BeginInvoke((MethodInvoker)delegate ()
            {
                UpdateNoIssuesPanel();
            });
            var ev = CheckCompleted;
            if (ev != null)
                CheckCompleted(this, new EventArgs());
        }


        public Func<bool> IsSuspended;

        // List of warnings to show.
        public BindingListInvoked<IssueItem> Warnings;
        object checkTimerLock = new object();
        bool IgnoreAll;

        BindingListInvoked<IssueItem> IssueList;
        object IssueListLock = new object();

        public void AddIssues(params IssueItem[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                IssueList.Add(items[i]);
                items[i].Checking += Item_Checking;
                items[i].Checked += Item_Checked;
                items[i].Fixing += Item_Fixing;
                items[i].Fixed += Item_Fixed;
            }
        }

        public bool HasIssues;

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
                var item = (IssueItem)row.DataBoundItem;
                item.Fix();
            }
        }

        private void IgnoreAllButton_Click(object sender, EventArgs e)
        {
            IgnoreAll = !IgnoreAll;
            IgnoreAllButton.Checked = IgnoreAll;
        }

        private void IssuesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == -1) return;
            var grid = (DataGridView)sender;
            var row = grid.Rows[e.RowIndex];
            var column = grid.Columns[SeverityColumn.Name];
            var item = (IssueItem)row.DataBoundItem;
            if (e.ColumnIndex == grid.Columns[SeverityColumn.Name].Index)
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
            else if (e.ColumnIndex == grid.Columns[SolutionColumn.Name].Index)
            {
                e.Value = item.Status == IssueStatus.Fixing ? "Please Wait..." : item.FixName;
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
    }
}
