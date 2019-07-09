using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace JocysCom.RemoteController.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SecurityPage : ContentPage
	{
		public SecurityPage()
		{
			InitializeComponent();
			DisableButton.IsEnabled = false;
		}

		private void EnableButton_Clicked(object sender, EventArgs e)
		{
			App.Monitor.AccelerometerSwitch_Toggled(null, new ToggledEventArgs(true));
			App.Monitor.OrientationSwitch_Toggled(null, new ToggledEventArgs(true));
			App.Monitor.GyroscopeSwitch_Toggled(null, new ToggledEventArgs(true));
			EnableButton.IsEnabled = false;
			EnableButton.BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8);
			DisableButton.IsEnabled = true;
			DisableButton.BackgroundColor = Color.FromRgb(0.4, 1.0, 0.4);
		}

		private void DisableButton_Clicked(object sender, EventArgs e)
		{
			App.Monitor.AccelerometerSwitch_Toggled(null, new ToggledEventArgs(false));
			App.Monitor.OrientationSwitch_Toggled(null, new ToggledEventArgs(false));
			App.Monitor.GyroscopeSwitch_Toggled(null, new ToggledEventArgs(false));
			DisableButton.BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8);
			DisableButton.IsEnabled = false;
			EnableButton.IsEnabled = true;
			EnableButton.BackgroundColor = Color.FromRgb(1.0, 0.4, 0.4);
		}
	}
}
