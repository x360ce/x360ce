using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class AboutControl : UserControl
	{
		public AboutControl()
		{
			InitializeComponent();
		}

		void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			OpenUrl(((Control)sender).Text);
		}

		public void OpenUrl(string url)
		{
			try
			{
				System.Diagnostics.Process.Start(url);
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (System.Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		void AboutControl_Load(object sender, EventArgs e)
		{
			var stream = EngineHelper.GetResource("ChangeLog.txt");
			var sr = new StreamReader(stream);
			ChangeLogTextBox.Text = sr.ReadToEnd();
			AboutProductLabel.Text = string.Format(AboutProductLabel.Text, Application.ProductVersion);
		}
	
	}
}
