using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Use Direct Input device to emulate keyboard keys.
	/// </summary>
	public partial class KeyboardControl : UserControl
	{
		public KeyboardControl()
		{
			InitializeComponent();
		}

		void MapKeyboardControl_Load(object sender, EventArgs e)
		{


			//KeyboardContextMenuStrip.Items.Add(
		}

		void MapDataGridView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			//e.KeyCode 
		}

		void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{

		}

	
		void KeyboardTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			//Control.ModifierKeys == Keys.Shift

			var key = string.Empty;
			key += e.Shift ? "SHIFT+" : "";
			key += e.Control ? "CTRL+" : "";
			key += e.Alt ? "ALT+" : "";
			key += e.KeyData.ToString()
				.Replace("Key", "")
				.Replace("Shift", "")
				.Replace("Control", "")
				.Replace("Alt", "")
				.Replace("Menu", "")
				.Replace(",", "")
				.Replace(" ", "")
				.Trim('+');
			KeyboardTextBox.Text = key;
			textBox1.Text = e.KeyData.ToString();
			e.Handled = true;
			e.SuppressKeyPress = true;

		}

		void AppendButton_Click(object sender, EventArgs e)
		{
			var m = string.Format("{0},{1},{2};", KeyboardTextBox.Text, DelayNumericUpDown.Value, LoopCheckBox.Checked ? "1" : "0");
			ScriptTextBox.AppendText(m);
		}

		void KeyboardTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		void KeyboardTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		void KeyboardTextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
		}

		void TriggerButton_Click(object sender, EventArgs e)
		{
			timer1.Enabled = !timer1.Enabled;
			TriggerButton.Text = timer1.Enabled ? "Enabled" : "Disabled";

		}


		int h = 0;

		void timer1_Tick(object sender, EventArgs e)
		{
			h++;
			SendKeys.Send("{8}");
			if (h % 5 == 0)
			{
				System.Threading.Thread.Sleep(500);
				SendKeys.Send("{9}");
			}
			if (h % 8 == 0)
			{
				System.Threading.Thread.Sleep(500);
				SendKeys.Send("{0}");
			}
		}

	}
}
