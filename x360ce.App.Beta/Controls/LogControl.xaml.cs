using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for LogControl.xaml
	/// </summary>
	public partial class LogControl : UserControl
	{
		public LogControl()
		{
			Items = new SortableBindingList<LogItem>();
			InitHelper.InitTimer(this, InitializeComponent);
			//InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			MainDataGrid.AutoGenerateColumns = false;
			UpdateAppearance();
		}

		#region ■ TabPage: Log

		public SortableBindingList<LogItem> Items { get; set;  }

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			Items.Clear();
		}

		int LogSize { get { int i; return int.TryParse(LogSizeComboBox.Text, out i) ? i : 200; } }

		private void LogSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			while (Items.Count >= LogSize)
				Items.RemoveAt(0);
		}

		private LogItem lastItem;

		public LogItem Add(string format, params object[] args)
		{
			var text = (args == null)
			 ? format
			 : string.Format(format, args);
			var e = new LogItem();
			e.Message = text;
			Add(e);
			return e;
		}

		public void Add(LogItem li)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				// Calculate time from last inserted item.
				if (lastItem != null)
					li.Delay = li.Date.Subtract(lastItem.Date);
				lastItem = li;
				if (LogGridScrollUp)
					Items.Insert(0, li);
				else
					Items.Add(li);

			});
		}

		[DefaultValue(false), Category("Misc Appearance")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public bool LogGridScrollUp { get => _LogGridScrollUp; set { _LogGridScrollUp = value; } }
		bool _LogGridScrollUp;

		[DefaultValue(true), Category("Misc Appearance")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public bool ShowLogSize { get => _ShowLogSize; set { _ShowLogSize = value; UpdateAppearance(); } }
		bool _ShowLogSize = true;

		void UpdateAppearance()
		{
			LogSizeLabel.Visibility = ShowLogSize ? Visibility.Visible : Visibility.Collapsed;
			LogSizeComboBox.Visibility = ShowLogSize ? Visibility.Visible : Visibility.Collapsed;
		}

		#endregion

		private void MainDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			var grid = (DataGrid)sender;
			// If the entire contents fit on the screen then ignore this event.
			if (e.ExtentHeight < e.ViewportHeight)
				return;
			// If no items are available to display then ignore this event
			if (grid.Items.Count <= 0)
				return;
			// If the ExtentHeight and ViewportHeight haven't changed then ignore this event
			if (e.ExtentHeightChange == 0.0 && e.ViewportHeightChange == 0.0)
				return;
			if (LogGridScrollUp)
			{
				// Scroll the new item into view.
				var firstItem = grid.Items[0];
				grid.ScrollIntoView(firstItem);
			}
			else
			{
				// We pick a threshold of 5 items since issues were seen when resizing the window with smaller threshold values.
				var oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
				var oldVerticalOffset = e.VerticalOffset - e.VerticalChange;
				var oldViewportHeight = e.ViewportHeight - e.ViewportHeightChange;
				// If scroll was close to the bottom when a new item appeared then...
				if (oldVerticalOffset + oldViewportHeight + 5 >= oldExtentHeight)
				{
					// Scroll the new item into view.
					var lastItem = grid.Items[grid.Items.Count - 1];
					grid.ScrollIntoView(lastItem);
				}
			}

		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			MainDataGrid.ItemsSource = Items;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			MainDataGrid.ItemsSource = null;
			Items.Clear();
			Items = null;
			lastItem = null;
			DataContext = null;
		}

	}
}
