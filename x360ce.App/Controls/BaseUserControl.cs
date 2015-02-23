using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{
	public class BaseUserControl: UserControl
	{

		public void ApplyStyleToCheckBoxes()
		{
			List<CheckBox> checkBoxes = new List<CheckBox>();
			GetCheckBoxes(this, ref checkBoxes);
			foreach (CheckBox c in checkBoxes)
			{
				c.ImageAlign = ContentAlignment.MiddleLeft;
				c.Paint += CheckBox_Paint;
			}
		}

		void CheckBox_Paint(object sender, PaintEventArgs e)
		{
			var box = (CheckBox)sender;
			if (box.Appearance != Appearance.Normal) return;
			var image = box.CheckState == CheckState.Checked
				? box.Enabled ? Properties.Resources.checkbox_16x16 : Properties.Resources.checkbox_disabled_16x16
				: box.Enabled ? Properties.Resources.checkbox_unchecked_16x16 : Properties.Resources.checkbox_unchecked_disabled_16x16;
			//var hw = image.Width / 2;
			var top = ((box.Height - image.Height) / 2); // - 5
			// box.Padding = new Padding(box.Padding.Left, box.Padding.Top, box.Padding.Right, box.Padding.Bottom);
			var pen = new SolidBrush(box.BackColor);
			e.Graphics.FillRectangle(pen, 0, 0, image.Width, box.Height); // - 8
			e.Graphics.DrawImage(image, 0, top);
		}

		public void GetCheckBoxes(Control c, ref List<CheckBox> l)
		{
			CheckBox[] boxes = c.Controls.OfType<CheckBox>().ToArray();
			Control[] bases = c.Controls.Cast<Control>().Where(x => x.GetType().BaseType.Equals(typeof(BaseUserControl))).ToArray();
			l.AddRange(boxes);
			Control[] c2 = c.Controls.Cast<Control>().Except(boxes).Except(bases).ToArray();
			for (int i = 0; i <= c2.Length - 1; i++)
			{
				GetCheckBoxes(c2[i], ref l);
			}
		}
	}
}
