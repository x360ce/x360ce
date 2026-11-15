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

		/// <summary>Widens the window by what its tool bar lacks to show every button on itself, and places it again as asked.</summary>
		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);
			if (JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this) || !AppHelper.WidenToShowToolBars(this))
				return;
			if (StartPosition == FormStartPosition.CenterParent)
				CenterToParent();
			else if (StartPosition == FormStartPosition.CenterScreen)
				CenterToScreen();
		}
	}
}
