using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Remove white borders from toolstrip.
	/// How to use:
	/// toolStrip.Renderer = new ToolStripBorderlessRenderer();
	/// </summary>
	public class ToolStripBorderlessRenderer : ToolStripSystemRenderer
	{
		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			if (e.ToolStrip.GetType() == typeof(ToolStrip))
			{
				// skip render border
			}
			else
			{
				// do render border
				base.OnRenderToolStripBorder(e);
			}
		}

	}
}
