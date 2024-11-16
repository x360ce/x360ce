using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for ErrorReportControl.xaml
	/// </summary>
	public partial class ErrorReportControl : UserControl
	{

		public ErrorReportControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			ErrorsFolderTextBox.Text = LogHelper.Current.LogsFolder;
			RefreshErrorsComboBox();
			StatusLabel.Content = "";
		}

		void RefreshErrorsComboBox()
		{
			var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
			if (!dir.Exists)
				return;
			var asm = new AssemblyInfo();
			var errors = dir.GetFiles("*.htm").OrderByDescending(x => x.CreationTime).ToArray();
			SubjectTextBox.Text = string.Format("Problem with {0}", asm.Product);
			ErrorComboBox.ItemsSource = errors;
			ErrorComboBox.DisplayMemberPath = nameof(FileInfo.Name);
			if (errors.Length > 0)
				ErrorComboBox.SelectedIndex = 0;
			else
				MainBrowser.Navigate("about:blank");
		}

		private void MainBrowser_LoadCompleted(object sender, NavigationEventArgs e)
		{
#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework

			var doc = (IHTMLDocument3)MainBrowser.Document;
			if (doc is null)
				return;
			var body = doc.getElementsByTagName("body").OfType<IHTMLElement>().First();
			if (body is null)
				return;
			//var doc2 = (IHTMLDocument2)MainBrowser.Document;
			//doc2.charset = "utf-8";
			//MainBrowser.Refresh();
			body.insertAdjacentHTML("afterbegin", "<p>Hi,</p><p></p><p>I would like to report a problem. Error details attached below:</p>");
			body.setAttribute("contentEditable", "true");
#endif
		}

		private void OpenErrorsFolder_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.OpenPath(ErrorsFolderTextBox.Text);
		}

		public void SetErrors(string errorsFolder = null)
		{
			errorsFolder = LogHelper.Current.LogsFolder;
		}

		private void ErrorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = ErrorComboBox.SelectedItem as FileInfo;
			if (item is null)
			{
				MainBrowser.Navigate("about:blank");
			}
			else
			{
				var uri = new System.Uri(item.FullName);
				MainBrowser.Navigate(uri.AbsoluteUri);
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			var win = (Window)Parent;
			win.DialogResult = false;
		}

		private void ClearErrorsButton_Click(object sender, RoutedEventArgs e)
		{
			ClearErrors?.Invoke(this, new EventArgs());
			var win = (Window)Parent;
			win.DialogResult = false;
		}

		private void OpenMailButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.OpenUrl("mailto://" + ToEmailTextBox.Text);
		}

		public string GetBody()
		{
#if NETCOREAPP // .NET Core
			throw new NotImplementedException();
#elif NETSTANDARD // .NET Standard
			throw new NotImplementedException();
#else // .NET Framework
			var doc = (IHTMLDocument3)MainBrowser.Document;
			if (doc is null)
				return null;
			var body = doc.getElementsByTagName("body").OfType<IHTMLElement>().First();
			if (body is null)
				return null;
			return body.innerHTML;
#endif
		}

		public string GetMetaContent(string name)
		{
#if NETCOREAPP // .NET Core
			throw new NotImplementedException();
#elif NETSTANDARD // .NET Standard
			throw new NotImplementedException();
#else // .NET Framework
			var doc = (IHTMLDocument3)MainBrowser.Document;
			if (doc is null)
				return null;
			var meta = doc.getElementsByName(name).OfType<IHTMLMetaElement>().FirstOrDefault();
			if (meta is null)
				return null;
			return meta.content;
#endif
		}

		private void SendErrorButton_Click(object sender, RoutedEventArgs e)
		{
			var m = new MailMessage();
			var source = GetMetaContent(LogHelper.XLogHelperErrorSource);
			if (!string.IsNullOrEmpty(source))
				m.Headers.Add(LogHelper.XLogHelperErrorSource, source);
			var errType = GetMetaContent(LogHelper.XLogHelperErrorType);
			if (!string.IsNullOrEmpty(errType))
				m.Headers.Add(LogHelper.XLogHelperErrorType, errType);
			var errCode = GetMetaContent(LogHelper.XLogHelperErrorCode);
			if (!string.IsNullOrEmpty(errCode))
				m.Headers.Add(LogHelper.XLogHelperErrorCode, errCode);
			m.Subject = SubjectTextBox.Text;
			if (!string.IsNullOrEmpty(FromEmailTextBox.Text))
				m.From = new MailAddress(FromEmailTextBox.Text);
			m.To.Add(new MailAddress(ToEmailTextBox.Text));
			m.IsBodyHtml = true;
			m.Body = GetBody();
			var messages = new List<MailMessage>();
			messages.Add(m);
			SendMessages?.Invoke(this, new EventArgs<List<MailMessage>>(messages));
		}

		public event EventHandler<EventArgs<List<MailMessage>>> SendMessages;
		public event EventHandler ClearErrors;

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			MainBrowser.LoadCompleted += MainBrowser_LoadCompleted;
			// Monitor parent Window closing for correct disposal of resources.
			var window = ControlsHelper.GetParent<Window>(this);
			window.Closing += (sender2, e2) => isWindowClosing = !e2.Cancel;
		}

		bool isWindowClosing = false;

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			MainBrowser.LoadCompleted -= MainBrowser_LoadCompleted;
			// Dispose resources on parent Window closing.
			if (isWindowClosing)
			{
				MainBrowser.Dispose();
			}
		}

	}
}

