using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for LeftThumbControl.xaml
	/// </summary>
	public partial class LeftThumbControl : UserControl
	{
		public LeftThumbControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			LeftThumbXPanel.HeaderText = "X - Horizontal Axis";
			LeftThumbXPanel.TargetType = TargetType.LeftThumbX;
			LeftThumbYPanel.HeaderText = "Y - Vertical Axis";
			LeftThumbYPanel.TargetType = TargetType.LeftThumbY;
		}
	}
}
