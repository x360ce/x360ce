#nullable disable

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class MessageBoxForm : Form
	{
		public MessageBoxForm()
		{
			InitializeComponent();
		}

		public bool PlaySounds { get; set; }
		int h;
		int w;
		/// <summary>The room the heading takes above the text, or 0 without one.</summary>
		int headingOffset;
		/// <summary>The width of the heading, or 0 without one.</summary>
		int headingWidth;

		/// <summary>Displays a message box with the specified text, caption, buttons, icon, and default button.</summary>
		/// <param name="text">The text to display in the message box.</param>
		/// <param name="caption">The text to display in the title bar of the message box.</param>
		/// <param name="buttons">One of the <see cref="T:System.Windows.Forms.MessageBoxButtons" /> values that specifies which buttons to display in the message box.</param>
		/// <param name="icon">One of the <see cref="T:System.Windows.Forms.MessageBoxIcon" /> values that specifies which icon to display in the message box.</param>
		/// <param name="defaultButton">One of the <see cref="T:System.Windows.Forms.MessageBoxDefaultButton" /> values that specifies the default button for the message box.</param>
		/// <returns>One of the <see cref="T:System.Windows.Forms.DialogResult" /> values.</returns>
		public static DialogResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			return form.ShowForm(text, caption, buttons, icon, defaultButton);
		}

		/// <summary>Displays a message box with the specified text, caption, buttons, icon, and default button.</summary>
		/// <param name="text">The text to display in the message box.</param>
		/// <param name="caption">The text to display in the title bar of the message box.</param>
		/// <param name="buttons">One of the <see cref="T:System.Windows.Forms.MessageBoxButtons" /> values that specifies which buttons to display in the message box.</param>
		/// <param name="icon">One of the <see cref="T:System.Windows.Forms.MessageBoxIcon" /> values that specifies which icon to display in the message box.</param>
		/// <param name="defaultButton">One of the <see cref="T:System.Windows.Forms.MessageBoxDefaultButton" /> values that specifies the default button for the message box.</param>
		/// <param name="buttonText">Wording for the buttons, left to right, in place of the names of the results they return.</param>
		/// <param name="heading">The question, shown in bold above the text, or null for none.</param>
		/// <returns>One of the <see cref="T:System.Windows.Forms.DialogResult" /> values.</returns>
		/// <remarks>
		/// The wording matters when the question is not a yes or a no. "Retry", "Abort"
		/// and "Ignore" tell a reader what the button does to the dialog; what they need
		/// to know is what it does to their settings. The result each button returns is
		/// unchanged, so a caller reads the answer exactly as before.
		/// </remarks>
		public DialogResult ShowForm(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, string[] buttonText = null, string heading = null)
		{
			ShowHeading(heading);
			AddResizeEvents();
			TextLabel.Text = text;
			Text = caption;
			Button1.Visible = false;
			Button2.Visible = false;
			Button3.Visible = false;
			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
					EnableButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
					break;
				case MessageBoxButtons.OK:
					EnableButtons(DialogResult.OK);
					break;
				case MessageBoxButtons.OKCancel:
					EnableButtons(DialogResult.OK, DialogResult.Cancel);
					break;
				case MessageBoxButtons.RetryCancel:
					EnableButtons(DialogResult.Retry, DialogResult.Cancel);
					break;
				case MessageBoxButtons.YesNo:
					EnableButtons(DialogResult.Yes, DialogResult.No);
					break;
				case MessageBoxButtons.YesNoCancel:
					EnableButtons(DialogResult.Yes, DialogResult.No, DialogResult.Cancel);
					break;
			}
			if (buttonText != null)
			{
				var order = new[] { Button1, Button2, Button3 };
				for (var i = 0; i < order.Length && i < buttonText.Length; i++)
					if (order[i].DialogResult != DialogResult.None && !string.IsNullOrEmpty(buttonText[i]))
						order[i].Text = buttonText[i];
			}
			LayoutButtons();
			Message_Resize(this, EventArgs.Empty);
			var resources = new System.ComponentModel.ComponentResourceManager(GetType());
			var image = (Bitmap)resources.GetObject("MessageBoxIcon_Information_32x32");
			switch (icon)
			{
				case MessageBoxIcon.None:
					if (PlaySounds) System.Media.SystemSounds.Beep.Play();
					break;
				case MessageBoxIcon.Error: // Same as 'Hand' and 'Stop'.
					image = (Bitmap)resources.GetObject("MessageBoxIcon_Error_32x32");
					if (PlaySounds) System.Media.SystemSounds.Hand.Play();
					break;
				case MessageBoxIcon.Question:
					image = (Bitmap)resources.GetObject("MessageBoxIcon_Question_32x32");
					if (PlaySounds) System.Media.SystemSounds.Question.Play();
					break;
				case MessageBoxIcon.Warning: // Same as 'Exclamation'.
					image = (Bitmap)resources.GetObject("MessageBoxIcon_Warning_32x32");
					if (PlaySounds) System.Media.SystemSounds.Exclamation.Play();
					break;
				case MessageBoxIcon.Information: // Same as 'Asterisk'.
					if (PlaySounds) System.Media.SystemSounds.Asterisk.Play();
					break;
			}
			IconPictureBox.Image = image;
			switch (defaultButton)
			{
				case MessageBoxDefaultButton.Button1:
					ActiveControl = Button1;
					break;
				case MessageBoxDefaultButton.Button2:
					ActiveControl = Button2;
					break;
				case MessageBoxDefaultButton.Button3:
					ActiveControl = Button3;
					break;
			}
			ControlsHelper.CheckTopMost(this);
			return ShowDialog();
		}

		/// <summary>Shows the heading in bold above the text and moves the text below it.</summary>
		void ShowHeading(string heading)
		{
			headingOffset = 0;
			headingWidth = 0;
			// Asked of the text, not of Visible: a control inside a form that is not shown yet reads as not visible.
			var shown = !string.IsNullOrEmpty(heading);
			HeadingLabel.Visible = shown;
			if (!shown)
				return;
			HeadingLabel.Font = new Font(Font, FontStyle.Bold);
			HeadingLabel.Text = heading;
			var size = HeadingLabel.GetPreferredSize(new Size(HeadingLabel.MaximumSize.Width, 0));
			headingWidth = size.Width;
			headingOffset = size.Height + HeadingGap;
			TextLabel.Top = HeadingLabel.Top + headingOffset;
		}

		/// <summary>The space between the heading and the text.</summary>
		const int HeadingGap = 10;

		/// <summary>The space between the buttons, and between the buttons and the edge.</summary>
		const int ButtonGap = 6;
		const int ButtonMargin = 12;

		/// <summary>The width the buttons need, with the room on each side, or 0 before they are laid out.</summary>
		int buttonsWidth;

		/// <summary>Sizes each button to its wording and lines them up against the right edge.</summary>
		/// <remarks>Wording longer than a button's own width wrapped onto a second line, so a button is as wide as its wording needs.</remarks>
		void LayoutButtons()
		{
			var shown = new[] { Button1, Button2, Button3 }.Where(x => x.DialogResult != DialogResult.None).ToArray();
			var right = ClientSize.Width - ButtonMargin;
			var total = ButtonMargin;
			for (var i = shown.Length - 1; i >= 0; i--)
			{
				var button = shown[i];
				var wording = TextRenderer.MeasureText(button.Text.Replace("&", ""), button.Font).Width + 2 * ButtonMargin;
				button.Width = Math.Max(button.Width, wording);
				button.Left = right - button.Width;
				right = button.Left - ButtonGap;
				total += button.Width + ButtonGap;
			}
			buttonsWidth = total + ButtonMargin - ButtonGap;
		}

		public void AddResizeEvents()
		{
			h = Height - TextLabel.Height + headingOffset;
			w = Width - TextLabel.Width;
			TextLabel.AutoSize = true;
			TextLabel.Resize += Message_Resize;
		}

		public void EnableButton(ref Button button, DialogResult r1)
		{
			button.Text = "&" + r1.ToString();
			button.DialogResult = r1;
			button.Visible = true;
			button.Click += Button_Click;
		}

		public void EnableButtons(DialogResult r1)
		{
			EnableButton(ref Button1, r1);
			AcceptButton = Button1;
			CancelButton = Button1;
			Button1.Location = Button3.Location;
		}

		public void EnableButtons(DialogResult r1, DialogResult r2)
		{
			EnableButton(ref Button1, r1);
			EnableButton(ref Button2, r2);
			AcceptButton = Button1;
			CancelButton = Button2;
			Button1.Location = Button2.Location;
			Button2.Location = Button3.Location;
		}

		public void EnableButtons(DialogResult r1, DialogResult r2, DialogResult r3)
		{
			EnableButton(ref Button1, r1);
			EnableButton(ref Button2, r2);
			EnableButton(ref Button3, r3);
			AcceptButton = Button1;
			CancelButton = Button3;
		}

		void Button_Click(object sender, EventArgs e)
		{
			Button button = (Button)sender;
			DialogResult = button.DialogResult;
		}

		void Form_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control & e.KeyCode == Keys.C)
			{
				ControlsHelper.CopyToClipboardOrWarn(TextLabel.Text);
				e.Handled = true;
				return;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				DialogResult = CancelButton.DialogResult;
			}
			else if (e.KeyCode == Keys.Enter)
			{
				DialogResult = AcceptButton.DialogResult;
			}
		}

		void Message_Resize(object sender, EventArgs e)
		{
			Height = Math.Max(h + TextLabel.Height, MinimumSize.Height);
			Width = Math.Max(Math.Max(w + Math.Max(TextLabel.Width, headingWidth), MinimumSize.Width), buttonsWidth + Width - ClientSize.Width);
			if (TextLabel.Width + 1 >= TextLabel.MaximumSize.Width && TextLabel.Height + 1 >= TextLabel.MaximumSize.Height)
			{
				textBox1.Text = TextLabel.Text;
				textBox1.Size = TextLabel.Size;
				textBox1.Top = TextLabel.Top;
				textBox1.Left = TextLabel.Left;
			}
			else
			{
				textBox1.Text = "";
				textBox1.Visible = false;
			}
		}

		void MessageBoxForm_Load(object sender, EventArgs e)
		{
			ActiveControl.Select();
		}

		void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ControlsHelper.CopyToClipboardOrWarn(TextLabel.Text);
		}

		void MainLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			OpenUrl(MainLinkLabel.Text);
		}

		public static void OpenUrl(string url)
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
			catch (Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

	}
}
