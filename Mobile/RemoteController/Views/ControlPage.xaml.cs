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
		}

		// Set speed delay for monitoring changes.
		SensorSpeed speed = SensorSpeed.UI;

		#region Accelerometer

		public bool AccelerometerEnabled
		{
			get => Preferences.Get("AccelerometerEnabled", true);
			set { Preferences.Set("AccelerometerEnabled", value); OnPropertyChanged(); }
		}

		private void AccelerometerSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			try
			{
				if (e.Value && !Accelerometer.IsMonitoring)
				{
					Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
					Accelerometer.Start(speed);
				}
				else if (!e.Value && Accelerometer.IsMonitoring)
				{
					Accelerometer.Stop();
					Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
				}
			}
			catch (FeatureNotSupportedException)
			{
				// Feature not supported on device
			}
			catch (Exception)
			{
				// Other error has occurred.
			}
		}

		void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var data = e.Reading;
			AccelerometerXLabel.Text = string.Format("X: {0}", data.Acceleration.X);
			AccelerometerYLabel.Text = string.Format("Y: {0}", data.Acceleration.Y);
			AccelerometerZLabel.Text = string.Format("Z: {0}", data.Acceleration.Z);
		}

		#endregion

		#region Gyroscope

		private void GyroscopeSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			try
			{
				if (e.Value && !Gyroscope.IsMonitoring)
				{
					Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
					Gyroscope.Start(speed);
				}
				else if (!e.Value && Gyroscope.IsMonitoring)
				{
					Gyroscope.Stop();
					Gyroscope.ReadingChanged -= Gyroscope_ReadingChanged;
				}
			}
			catch (FeatureNotSupportedException)
			{
				// Feature not supported on device
			}
			catch (Exception)
			{
				// Other error has occurred.
			}
		}

		void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
		{
			var data = e.Reading;
			GyroscopeXLabel.Text = string.Format("X: {0}", data.AngularVelocity.X);
			GyroscopeYLabel.Text = string.Format("Y: {0}", data.AngularVelocity.Y);
			GyroscopeZLabel.Text = string.Format("Z: {0}", data.AngularVelocity.Z);
		}

		#endregion

		#region Orientation

		private void OrientationSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			try
			{
				if (e.Value && !OrientationSensor.IsMonitoring)
				{
					OrientationSensor.ReadingChanged += Orientation_ReadingChanged;
					OrientationSensor.Start(speed);
				}
				else if (!e.Value && OrientationSensor.IsMonitoring)
				{
					OrientationSensor.Stop();
					OrientationSensor.ReadingChanged -= Orientation_ReadingChanged;
				}
			}
			catch (FeatureNotSupportedException)
			{
				// Feature not supported on device
			}
			catch (Exception)
			{
				// Other error has occurred.
			}
		}

		void Orientation_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
		{
			var data = e.Reading;
			OrientationXLabel.Text = string.Format("X: {0}", data.Orientation.X);
			OrientationYLabel.Text = string.Format("Y: {0}", data.Orientation.Y);
			OrientationZLabel.Text = string.Format("Z: {0}", data.Orientation.Z);
			OrientationWLabel.Text = string.Format("W: {0}", data.Orientation.W);
		}

		#endregion

		#region Compass

		private void CompassSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			try
			{
				if (e.Value && !Compass.IsMonitoring)
				{
					Compass.ReadingChanged += Compass_ReadingChanged;
					Compass.Start(speed);
				}
				else if (!e.Value && Compass.IsMonitoring)
				{
					Compass.Stop();
					Compass.ReadingChanged -= Compass_ReadingChanged;
				}
			}
			catch (FeatureNotSupportedException)
			{
				// Feature not supported on device
			}
			catch (Exception)
			{
				// Other error has occurred.
			}
		}

		void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
		{
			var data = e.Reading;
			CompassNLabel.Text = string.Format("North: {0}", data.HeadingMagneticNorth);
		}

		#endregion

		#region Vibrate

		private void VibrateSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			try
			{
				VibrateValueLabel.Text = string.Format("Vibrating: {0}", e.Value);
				if (e.Value)
				{
					Vibration.Vibrate(5000);
				}
				else
				{
					Vibration.Cancel();
				}
			}
			catch (FeatureNotSupportedException)
			{
				// Feature not supported on device
			}
			catch (Exception)
			{
				// Other error has occurred.
			}
		}

		#endregion


	}
}
