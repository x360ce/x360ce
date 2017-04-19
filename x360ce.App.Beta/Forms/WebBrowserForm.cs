using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Forms
{
	public partial class WebBrowserForm : Form
	{

		public string NavigateUrl;

		public WebBrowserForm()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			// Important: DocumentCompleted event won't fire on some computers if you uncomment Navigated event.
			// Browser.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.Browser_Navigated);
			HandleCreated += Form_HandleCreated;
			// Also there is a known bug that prevents the DocumentCompleted event from firing if the WebBrowser
			// is not visible and is not added to a visible form as well.
		}

		public bool IsDesignMode
		{
			get { return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
		}

		void Form_HandleCreated(object sender, EventArgs e)
		{
			MainWebBrowser.ScrollBarsEnabled = false;
			MainWebBrowser.Navigate(NavigateUrl);
		}

		private void CloseButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
