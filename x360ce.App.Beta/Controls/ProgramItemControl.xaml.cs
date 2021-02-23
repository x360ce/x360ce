using System.ComponentModel;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ProgramItemControl.xaml
	/// </summary>
	public partial class ProgramItemControl : UserControl
	{
		public ProgramItemControl()
		{
			InitializeComponent();
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.IProgram CurrentItem
		{
			get { return _CurrentItem; }
			set
			{
			}
		}
		private x360ce.Engine.Data.IProgram _CurrentItem;

	}
}
