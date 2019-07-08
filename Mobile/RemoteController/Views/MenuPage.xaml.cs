using JocysCom.RemoteController.Models;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace JocysCom.RemoteController.Views
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(false)]
	public partial class MenuPage : ContentPage
	{
		MainPage RootPage { get => Application.Current.MainPage as MainPage; }
		List<HomeMenuItem> menuItems;
		public MenuPage()
		{
			InitializeComponent();
			menuItems = new List<HomeMenuItem>
			{
				new HomeMenuItem {MenuType = MenuItemType.Controller, Title="Controller", PageType = typeof(Pages.ControllerPage) },
				new HomeMenuItem {MenuType = MenuItemType.Security, Title="Security" , PageType = typeof(Pages.SecurityPage)},
				new HomeMenuItem {MenuType = MenuItemType.Settings, Title="Settings", PageType = typeof(Pages.SettingsPage) },
				new HomeMenuItem {MenuType = MenuItemType.About, Title="About" , PageType = typeof(Pages.AboutPage)},
			};

			ListViewMenu.ItemsSource = menuItems;
			ListViewMenu.SelectedItem = menuItems[0];
			ListViewMenu.ItemSelected += async (sender, e) =>
			{
				if (e.SelectedItem == null)
					return;
				var item = (HomeMenuItem)e.SelectedItem;
				await RootPage.NavigateFromMenu(item);
			};
		}
	}
}
