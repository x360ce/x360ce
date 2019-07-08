using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Windows.Input;

namespace JocysCom.RemoteController.Pages
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(false)]
	public partial class AboutPage : ContentPage
	{
		public AboutPage()
		{
			InitializeComponent();
			Title = "About";
			OpenWebCommand = new Command(() => Device.OpenUri(new Uri("https://www.x360ce.com")));
		}

		public ICommand OpenWebCommand { get; }
	}
}
