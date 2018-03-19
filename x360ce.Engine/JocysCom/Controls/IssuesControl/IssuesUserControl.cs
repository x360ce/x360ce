using JocysCom.ClassLibrary.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.IssueControl
{
    public partial class IssuesUserControl : UserControl
    {
        public IssuesUserControl()
        {
            InitializeComponent();
            if (IsDesignMode) return;
            // List which contains all issues.
            IssueList = new List<IssueItem>();
            // List which is bound to the grid and displays issues, which needs user attention.
            Warnings = new BindingListInvoked<IssueItem>();
            Warnings.SynchronizingObject = this;
            // Configure data grid.
            ControlsHelper.ApplyBorderStyle(IssuesDataGridView);
            IssuesDataGridView.AutoGenerateColumns = false;
            IssuesDataGridView.DataSource = Warnings;
            // Timer which checks for the issues.
            CheckTimer = new System.Timers.Timer();
            CheckTimer.Interval = 1000;
            CheckTimer.AutoReset = false;
            CheckTimer.SynchronizingObject = this;
            CheckTimer.Elapsed += CheckTimer_Elapsed;
            var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
            var title = ai.GetTitle(true, true, true, true, false) + " - Issues";
            Text = title;
        }

        internal bool IsDesignMode { get { return ControlsHelper.IsDesignMode(this); } }

        private void CheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (checkTimerLock)
            {
                // If timer is disposed then return;
                if (CheckTimer == null)
                    return;
                if (!IsSuspended())
                    CheckAll();
            }
            // If check time is not every 5 seconds then...
            if (CheckTimer.Interval != 5000)
                // Reset check to every 5 seconds.
                CheckTimer.Interval = 5000;
            CheckTimer.Start();
        }

        public Func<bool> IsSuspended;

        // List of warnings to show.
        public BindingListInvoked<IssueItem> Warnings;
        object checkTimerLock = new object();
        public System.Timers.Timer CheckTimer;
        bool IgnoreAll;

        List<IssueItem> IssueList;
        object IssueListLock = new object();

        public void AddIssues(params IssueItem[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                IssueList.Add(items[i]);
                items[i].Fixing += Item_Fixing;
                items[i].Fixed += Item_Fixed;
            }
        }

        void CheckAll()
        {
            bool clearRest = false;
            foreach (var issue in IssueList)
            {
                if (clearRest)
                    issue.Severity = IssueSeverity.None;
                else issue.Check();
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
                else if (item != null && issue.Severity != IssueSeverity.None)
                    // Add to the list.
                    Warnings.Add(issue);
            }
            HasIssues = IgnoreAll
                ? false
                : Warnings.Any(x => x.Severity > IssueSeverity.Moderate);
            var ev = CheckCompleted;
            if (ev != null)
                CheckCompleted(this, new EventArgs());
        }

        public bool HasIssues;

        public event EventHandler CheckCompleted;

        private void Item_Fixed(object sender, EventArgs e)
        {
            // Reset check timer.
            lock (checkTimerLock)
            {
                // If timer is disposed then return;
                if (CheckTimer == null)
                    return;
                CheckTimer.Stop();
                CheckTimer.Interval = 500;
                CheckTimer.Start();
                StatusLabel.Text = "";
            }
        }
        private void Item_Fixing(object sender, EventArgs e)
        {
            // Reset check timer.
            lock (checkTimerLock)
            {
                // If timer is disposed then return;
                if (CheckTimer == null)
                    return;
                CheckTimer.Stop();
                StatusLabel.Text = "Please wait...";
            }
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
                // Clear list.
                var items = IssueList.ToArray();
                IssueList.Clear();
                // Remove events.
                foreach (var item in items)
                {
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
            var item = (IssueItem)row.DataBoundItem;
            if (e.ColumnIndex == grid.Columns[SeverityColumn.Name].Index)
            {
                switch (item.Severity)
                {
                    case IssueSeverity.None:
                        e.Value = null;
                        break;
                    case IssueSeverity.Low:
                        e.Value = Helper.FindResource<Bitmap>("MessageBoxIcon_Information_32x32");
                        break;
                    case IssueSeverity.Important:
                        e.Value = Helper.FindResource<Bitmap>("MessageBoxIcon_Warning_32x32");
                        break;
                    case IssueSeverity.Moderate:
                        e.Value = Helper.FindResource<Bitmap>("MessageBoxIcon_Warning_32x32");
                        break;
                    case IssueSeverity.Critical:
                        e.Value = Helper.FindResource<Bitmap>("MessageBoxIcon_Error_32x32");
                        break;
                    default:
                        break;
                }

            }
        }


    }
}
