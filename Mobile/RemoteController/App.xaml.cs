using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using JocysCom.x360ce.Mobile.RemoteController.Services;
using JocysCom.x360ce.Mobile.RemoteController.Views;

namespace JocysCom.x360ce.Mobile.RemoteController
{
	public partial class App : Application
	{

		public App()
		{
			InitializeComponent();

			DependencyService.Register<MockDataStore>();
			MainPage = new MainPage();
		}

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
