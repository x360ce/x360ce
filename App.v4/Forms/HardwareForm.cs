using System.Windows.Forms;

namespace x360ce.App.Forms
{
	public partial class HardwareForm : Form
	{
		public HardwareForm()
		{
			InitializeComponent();
			// Images are drawn at the size they were made, so they are enlarged to the screen's scale.
			JocysCom.ClassLibrary.Controls.ControlsHelper.ScaleImages(this);
		}
	}
}
