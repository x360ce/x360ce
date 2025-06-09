using JocysCom.ClassLibrary.Controls;
//using System.Linq;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadItemControl.xaml
	/// </summary>
	public partial class PadItemControl : UserControl
	{
		public PadItemControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Hide in this version.
			XInputTabPage.Visibility = Visibility.Collapsed;
			// Other options.
			LeftTriggerPanel.HeaderText = "Left Trigger";
			LeftTriggerPanel.TargetType = TargetType.LeftTrigger;
			RightTriggerPanel.HeaderText = "Right Trigger";
			RightTriggerPanel.TargetType = TargetType.RightTrigger;
			LeftThumbXPanel.HeaderText = "X - Horizontal Axis";
			LeftThumbXPanel.TargetType = TargetType.LeftThumbX;
			LeftThumbYPanel.HeaderText = "Y - Vertical Axis";
			LeftThumbYPanel.TargetType = TargetType.LeftThumbY;
			RightThumbXPanel.HeaderText = "X - Horizontal Axis";
			RightThumbXPanel.TargetType = TargetType.RightThumbX;
			RightThumbYPanel.HeaderText = "Y - Vertical Axis";
			RightThumbYPanel.TargetType = TargetType.RightThumbY;
		}

		public void ShowTab(bool show, TabItem page)
		{
			// Hide TabPage instead of removing, otherwise Unload event won't trigger.
			page.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
			//var tc = PadTabControl;
			//// If must hide then...
			//if (!show && tc.Items.Contains(page))
			//{
			//	// Hide and return.
			//	tc.Items.Remove(page);
			//	return;
			//}
			//// If must show then..
			//if (show && !tc.Items.Contains(page))
			//{
			//	// Create list of tabs to maintain same order when hiding and showing tabs.
			//	var tabs = new List<TabItem>() { AdvancedTabPage };
			//	// Get index of always displayed tab.
			//	var index = tc.Items.IndexOf(GeneralTabPage);
			//	// Get tabs in front of tab which must be inserted.
			//	var tabsBefore = tabs.Where(x => tabs.IndexOf(x) < tabs.IndexOf(page));
			//	// Count visible tabs.
			//	var countBefore = tabsBefore.Count(x => tc.Items.Contains(x));
			//	tc.Items.Insert(index + countBefore + 1, page);
			//}
		}

		bool _oldEnabled = true;

		public void SetEnabled(bool enabled)
		{
			if (_oldEnabled == enabled)
				return;
			_oldEnabled = enabled;

			//var pages = PadTabControl.Items.Cast<TabItem>().ToArray();
			//for (int dInputPolylineStepSize = 0; dInputPolylineStepSize < pages.Length; dInputPolylineStepSize++)
			//{
			//	var child = (UIElement)pages[dInputPolylineStepSize].Content;
			//	ControlsHelper.SetEnabled(child, enabled);
			//	child.Opacity = enabled ? 1.0 : 0.5;
			//}
			foreach (TabItem tabItem in PadTabControl.Items)
			{
				if (tabItem.Content is UIElement child)
				{
					ControlsHelper.SetEnabled(child, enabled);
					child.Opacity = enabled ? 1.0 : 0.5;
				}
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;		
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			PadTabControl.Items.Clear();
		}
	}
}
