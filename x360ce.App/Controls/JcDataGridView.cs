using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public class JcDataGridView : DataGridView
	{

		public JcDataGridView()
		{
			this.Paint += dataGridView1_Paint;
			this.CellPainting += dataGridView1_CellPainting;
		}

		void dataGridView1_Paint(object sender, PaintEventArgs e)
		{
			string[] monthes = { "January", "February", "March" };
			for (int j = 0; j < 6;)
			{
				var r1 = this.GetCellDisplayRectangle(j, -1, true); //get the column header cell
				r1.X += 1;
				r1.Y += 1;
				r1.Width = r1.Width * 2 - 2;
				r1.Height = r1.Height / 2 - 2;
				e.Graphics.FillRectangle(new SolidBrush(this.ColumnHeadersDefaultCellStyle.BackColor), r1);
				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				e.Graphics.DrawString(monthes[j / 2],
					this.ColumnHeadersDefaultCellStyle.Font,
					new SolidBrush(this.ColumnHeadersDefaultCellStyle.ForeColor),
					r1,
					format);
				j += 2;
			}
		}

		void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex == -1 && e.ColumnIndex > -1)
			{
				e.PaintBackground(e.CellBounds, false);
				var r2 = e.CellBounds;
				r2.Y += e.CellBounds.Height / 2;
				r2.Height = e.CellBounds.Height / 2;
				e.PaintContent(r2);
				e.Handled = true;
			}
		}


	}
}
