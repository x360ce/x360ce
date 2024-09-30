using JocysCom.ClassLibrary.Controls;
using System;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for WebBrowserWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class WebBrowserWindow : Window
	{
		public WebBrowserWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		public string NavigateUrl;


		/// <summary>
		/// This overrides the windows messaging processing. Be careful with this method,
		/// because this method is responsible for all the windows messages that are coming to the form.
		/// </summary>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			// Handle created.
			//MainWebBrowser.ScrollBarsEnabled = false;
			MainWebBrowser.Navigate(NavigateUrl);
			MainWebBrowser.LoadCompleted += MainWebBrowser_LoadCompleted;
		}


		void Form_HandleCreated(object sender, EventArgs e)
		{
		}

		private void MainWebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			var w = MainWebBrowser.Width;
			var h = MainWebBrowser.Height;
			//var size = MainWebBrowser.Document.Window.Size;
			//var wd = size.Width - graphWidth;
			//var hd = size.Height - h;
			//var newSize = new Size(Size.Width + wd, Size.Height + hd);
			//Size =  newSize;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
