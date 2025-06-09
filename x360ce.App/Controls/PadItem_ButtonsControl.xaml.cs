using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AxisToButtonListControl.xaml
	/// </summary>
	public partial class PadItem_ButtonsControl : UserControl
	{
		public PadItem_ButtonsControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}
	}
}
