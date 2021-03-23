using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for TriggersControl.xaml
	/// </summary>
	public partial class TriggersControl : UserControl
	{
		public TriggersControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			LeftTriggerPanel.HeaderText = "Left Trigger";
			LeftTriggerPanel.TargetType = TargetType.LeftTrigger;
			RightTriggerPanel.HeaderText = "Right Trigger";
			RightTriggerPanel.TargetType = TargetType.RightTrigger;
		}
	}
}
