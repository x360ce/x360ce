using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using JocysCom.RemoteController.Models;

namespace JocysCom.RemoteController.Views
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(false)]
	public partial class MainPage : MasterDetailPage
	{
		Dictionary<MenuItemType, NavigationPage> MenuPages = new Dictionary<MenuItemType, NavigationPage>();

		public MainPage()
		{
			InitializeComponent();
			MasterBehavior = MasterBehavior.Popover;
			// Set default.
			//MenuPages.Add((MenuItemType)0, (NavigationPage)Detail);
		}

		public async Task NavigateFromMenu(HomeMenuItem item)
		{
			// Create page if missing.
			if (!MenuPages.ContainsKey(item.MenuType))
				MenuPages.Add(item.MenuType, new NavigationPage((Page)Activator.CreateInstance(item.PageType)));
			// Show Page.
			var newPage = MenuPages[item.MenuType];
			if (newPage != null && Detail != newPage)
			{
				Detail = newPage;
				if (Device.RuntimePlatform == Device.Android)
					await Task.Delay(100);
				IsPresented = false;
			}
		}

	}
}
