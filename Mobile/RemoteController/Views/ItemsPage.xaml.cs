using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using JocysCom.x360ce.Mobile.RemoteController.Models;
using JocysCom.x360ce.Mobile.RemoteController.Views;
using JocysCom.x360ce.Mobile.RemoteController.ViewModels;
using Xamarin.Essentials;

namespace JocysCom.x360ce.Mobile.RemoteController.Views
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(false)]
	public partial class ItemsPage : ContentPage
	{
		ItemsViewModel viewModel;

		public ItemsPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new ItemsViewModel();
			// Register for reading changes, be sure to unsubscribe when finished
			Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
			ToggleButton.Clicked += ToggleButton_Clicked;
		}

		private void ToggleButton_Clicked(object sender, EventArgs e)
		{
			ToggleAccelerometer();
		}

		//async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
		//{
		//	var item = args.SelectedItem as Item;
		//	if (item == null)
		//		return;

		//	await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)));

		//	// Manually deselect item.
		//	ItemsListView.SelectedItem = null;
		//}

		//async void AddItem_Clicked(object sender, EventArgs e)
		//{
		//	await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
		//}

		//protected override void OnAppearing()
		//{
		//	base.OnAppearing();

		//	if (viewModel.Items.Count == 0)
		//		viewModel.LoadItemsCommand.Execute(null);
		//}


		// Set speed delay for monitoring changes.
		SensorSpeed speed = SensorSpeed.UI;

		public void ToggleAccelerometer()
		{
			try
			{
				if (Accelerometer.IsMonitoring)
					Accelerometer.Stop();
				else
					Accelerometer.Start(speed);
			}
			catch (FeatureNotSupportedException fnsEx)
			{
				// Feature not supported on device
			}
			catch (Exception ex)
			{
				// Other error has occurred.
			}
		}

		void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var data = e.Reading;
			Console.WriteLine($"Reading: X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}");
			XLabel.Text = string.Format("X: {0}", data.Acceleration.X);
			// Process Acceleration X, Y, and Z
		}

	}
}
