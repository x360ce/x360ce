using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class LinearUserControl : UserControl
	{
		public LinearUserControl()
		{
			InitializeComponent();
		}

		private void TestButton_Click(object sender, EventArgs e)
		{
			var bmp = new Bitmap(128, 128);
			LinearPictureBox.BackgroundImage = bmp;
			var g = Graphics.FromImage(bmp);
			var nativeBrush = new SolidBrush(System.Drawing.Color.Gray);
			var transBrush = new SolidBrush(System.Drawing.Color.Red);
			var w = bmp.Width;
			for (int i = 0; i < w; i++)
			{
				g.FillEllipse(nativeBrush, i - 1, w - i - 1, 2, 2);
				g.FillEllipse(transBrush, i - 1, w - (GetValue(i * 256) / 256) - 1, 2, 2);
			}
		}

		int GetValue(int v)
		{
			short absval = (short)((Math.Abs(v) + (((32767.0 / 2.0) - (((Math.Abs((Math.Abs(v)) - (32767.0 / 2.0)))))) * (v * 0.01))));
			v = v > 0 ? absval : -absval;
			return v;
		}
	}
}
