using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for RigthThumbControl.xaml
	/// </summary>
	public partial class RightThumbControl : UserControl
	{
		public RightThumbControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			RightThumbXPanel.HeaderText = "X - Horizontal Axis";
			RightThumbXPanel.TargetType = TargetType.RightThumbY;
			RightThumbYPanel.HeaderText = "Y - Vertical Axis";
			RightThumbYPanel.TargetType = TargetType.LeftThumbY;
		}
	}
}
