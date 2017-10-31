using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public class CustomToolStripSystemRenderer : ToolStripSystemRenderer
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
