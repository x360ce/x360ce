using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Lets the user review a written error report and send it to support.
	/// </summary>
	/// <remarks>
	/// The report itself is the HTML file LogHelper wrote. It is shown in a browser made
	/// editable, so the user can add a description or strip anything they would rather not
	/// send, and what they see is exactly what is mailed.
	/// </remarks>
	public partial class ErrorReportUserControl : UserControl
	{

		public ErrorReportUserControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			ErrorsFolderTextBox.Text = LogHelper.Current.LogsFolder;
			MainBrowser.DocumentCompleted += MainBrowser_DocumentCompleted;
			RefreshErrorsComboBox();
			StatusLabel.Text = "";
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Everything else on this form is read-only, a list, or a button. Start on the one
			// field the user may want to fill in rather than on whichever control tabs first.
			FromEmailTextBox.Select();
		}

		/// <summary>Address reports are sent to. Set by the hosting application.</summary>
		public string SupportEmail
		{
			get { return ToEmailTextBox.Text; }
			set { ToEmailTextBox.Text = value; }
		}

		void RefreshErrorsComboBox()
		{
			var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
			// Folder is created when the first error is logged.
			if (!dir.Exists)
				return;
			var asm = new AssemblyInfo();
			var errors = dir.GetFiles("*.htm").OrderByDescending(x => x.CreationTime).ToArray();
			// Company, product and version, so a report can be sorted without opening it. The version
			// matters most: the same fault arrives for months from people still on an older build.
			SubjectTextBox.Text = string.Format("Issue with {0} {1} {2}", asm.Company, asm.Product, asm.Version);
			ErrorComboBox.DisplayMember = nameof(FileInfo.Name);
			ErrorComboBox.DataSource = errors;
			if (errors.Length > 0)
				ErrorComboBox.SelectedIndex = 0;
			else
				MainBrowser.Navigate("about:blank");
		}

		private void MainBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			var doc = MainBrowser.Document;
			if (doc == null)
				return;
			var body = doc.Body;
			if (body == null)
				return;
			body.InnerHtml = "<p>Hi,</p><p></p><p>I would like to report a problem. Error details attached below:</p>" + (body.InnerHtml ?? "");
			body.SetAttribute("contentEditable", "true");
		}

		private void OpenErrorsFolderButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenPath(ErrorsFolderTextBox.Text);
		}

		private void ErrorComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = ErrorComboBox.SelectedItem as FileInfo;
			if (item == null)
			{
				MainBrowser.Navigate("about:blank");
			}
			else
			{
				var uri = new Uri(item.FullName);
				MainBrowser.Navigate(uri.AbsoluteUri);
			}
		}

		private void CloseButton_Click(object sender, EventArgs e)
		{
			CloseHost(DialogResult.Cancel);
		}

		private void ClearErrorsButton_Click(object sender, EventArgs e)
		{
			ClearErrors?.Invoke(this, new EventArgs());
			CloseHost(DialogResult.Cancel);
		}

		void CloseHost(DialogResult result)
		{
			var form = FindForm();
			if (form == null)
				return;
			form.DialogResult = result;
			// A form shown with Show rather than ShowDialog ignores DialogResult.
			if (!form.Modal)
				form.Close();
		}

		private void OpenMailButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenUrl("mailto://" + ToEmailTextBox.Text);
		}

		public string GetBody()
		{
			return MainBrowser.Document?.Body?.InnerHtml;
		}

		public string GetMetaContent(string name)
		{
			var doc = MainBrowser.Document;
			if (doc == null)
				return null;
			var metaElements = doc.GetElementsByTagName("meta");
			foreach (HtmlElement el in metaElements)
			{
				if (string.Equals(el.GetAttribute("name"), name, StringComparison.OrdinalIgnoreCase))
					return el.GetAttribute("content");
			}
			return null;
		}

		private void SendErrorButton_Click(object sender, EventArgs e)
		{
			var m = new MailMessage();
			AddHeader(m, LogHelper.XLogHelperErrorSource);
			AddHeader(m, LogHelper.XLogHelperErrorType);
			AddHeader(m, LogHelper.XLogHelperErrorCode);
			m.Subject = SubjectTextBox.Text;
			if (!string.IsNullOrEmpty(FromEmailTextBox.Text))
				m.From = new MailAddress(FromEmailTextBox.Text);
			m.To.Add(new MailAddress(ToEmailTextBox.Text));
			m.IsBodyHtml = true;
			m.Body = GetBody();
			SendMessages?.Invoke(this, new EventArgs<List<MailMessage>>(new List<MailMessage> { m }));
		}

		void AddHeader(MailMessage message, string name)
		{
			var value = GetMetaContent(name);
			if (!string.IsNullOrEmpty(value))
				message.Headers.Add(name, value);
		}

		public event EventHandler<EventArgs<List<MailMessage>>> SendMessages;
		public event EventHandler ClearErrors;

	}
}
