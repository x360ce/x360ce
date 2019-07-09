using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using JocysCom.RemoteController.Views;

namespace JocysCom.RemoteController
{
	public partial class App : Application
	{

		public App()
		{
			InitializeComponent();
			Monitor = new MoveMonitor();
			MainPage = new MainPage();
		}

		public static MoveMonitor Monitor;

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
