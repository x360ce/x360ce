using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsControl.xaml
	/// </summary>
	public partial class OptionsControl : UserControl
	{
		public OptionsControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}
	}
}
