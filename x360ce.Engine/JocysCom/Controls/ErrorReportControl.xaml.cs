using JocysCom.ClassLibrary.Runtime;
using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for ErrorReportControl.xaml
	/// </summary>
	public partial class ErrorReportControl : UserControl
	{
		public ErrorReportControl()
		{
			InitializeComponent();
			ErrorsFolderTextBox.Text = LogHelper.Current.LogsFolder;
			var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
			var errors = dir.GetFiles("*.htm").OrderByDescending(x => x.CreationTime).ToArray();
			ErrorComboBox.ItemsSource = errors;
			ErrorComboBox.DisplayMemberPath = nameof(FileInfo.Name);
			if (errors.Length > 0)
				ErrorComboBox.SelectedIndex = 0;
			MainBrowser.LoadCompleted += MainBrowser_LoadCompleted;

		}

		private void MainBrowser_LoadCompleted(object sender, NavigationEventArgs e)
		{
			var doc = (IHTMLDocument3)MainBrowser.Document;
			if (doc == null)
				return;
			var body = doc.getElementsByTagName("body").OfType<IHTMLElement>().First();
			if (body == null)
				return;
			body.setAttribute("contentEditable", "true");
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
			if (item == null)
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
	}
}
