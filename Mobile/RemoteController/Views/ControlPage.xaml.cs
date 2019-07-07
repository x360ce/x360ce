using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using JocysCom.RemoteController.Models;
using JocysCom.RemoteController.Views;
using JocysCom.RemoteController.ViewModels;
using Xamarin.Essentials;
using System.Numerics;

namespace JocysCom.RemoteController.Views
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
		string format = "{0:+#0.000000000;-#0.000000000; #0.000000000}";
		string formatDelta = "{0:0.000000000}";

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

		Vector3? AccelerometerMin;
		Vector3? AccelerometerMax;

		void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var v = e.Reading.Acceleration;
			var min = (AccelerometerMin = GetMin(v, AccelerometerMin)).Value;
			var max = (AccelerometerMax = GetMax(v, AccelerometerMax)).Value;
			var dx = string.Format(formatDelta, max.X - min.X);
			var dy = string.Format(formatDelta, max.Y - min.Y);
			var dz = string.Format(formatDelta, max.Z - min.Z);
			AccelerometerXLabel.Text = string.Format("X: " + format + ", " + dx, v.X);
			AccelerometerYLabel.Text = string.Format("Y: " + format + ", " + dy, v.Y);
			AccelerometerZLabel.Text = string.Format("Z: " + format + ", " + dz, v.Z);
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

		Vector3? GyroscopeMin;
		Vector3? GyroscopeMax;


		void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
		{
			var v = e.Reading.AngularVelocity;
			var min = (GyroscopeMin = GetMin(v, GyroscopeMin)).Value;
			var max = (GyroscopeMax = GetMax(v, GyroscopeMax)).Value;
			var dx = string.Format(formatDelta, max.X - min.X);
			var dy = string.Format(formatDelta, max.Y - min.Y);
			var dz = string.Format(formatDelta, max.Z - min.Z);
			GyroscopeXLabel.Text = string.Format("X: " + format + ", " + dx, v.X);
			GyroscopeYLabel.Text = string.Format("Y: " + format + ", " + dy, v.Y);
			GyroscopeZLabel.Text = string.Format("Z: " + format + ", " + dz, v.Z);
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

		Vector4? OrientationMin;
		Vector4? OrientationMax;

		void Orientation_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
		{
			var o = e.Reading.Orientation;
			var v = new Vector4(o.X, o.Y, o.Z, o.W);
			var min = (OrientationMin = GetMin(v, OrientationMin)).Value;
			var max = (OrientationMax = GetMax(v, OrientationMax)).Value;
			var dx = string.Format(formatDelta, max.X - min.X);
			var dy = string.Format(formatDelta, max.Y - min.Y);
			var dz = string.Format(formatDelta, max.Z - min.Z);
			var dw = string.Format(formatDelta, max.W - min.W);
			OrientationXLabel.Text = string.Format("X: " + format + ", " + dx, v.X);
			OrientationYLabel.Text = string.Format("Y: " + format + ", " + dy, v.Y);
			OrientationZLabel.Text = string.Format("Z: " + format + ", " + dz, v.Z);
			OrientationWLabel.Text = string.Format("W: " + format + ", " + dw, v.W);
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
			CompassNLabel.Text = string.Format("N: " + format, data.HeadingMagneticNorth);
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

		#region Helper methods

		Vector3 GetMin(Vector3 a, Vector3? old)
		{
			var b = old ?? a;
			return new Vector3(
				Math.Min(a.X, b.X),
				Math.Min(a.Y, b.Y),
				Math.Min(a.Z, b.Z)
			);
		}

		Vector3 GetMax(Vector3 a, Vector3? old)
		{
			var b = old ?? a;
			return new Vector3(
				Math.Max(a.X, b.X),
				Math.Max(a.Y, b.Y),
				Math.Max(a.Z, b.Z)
			);
		}

		Vector4 GetMin(Vector4 a, Vector4? old)
		{
			var b = old ?? a;
			return new Vector4(
				Math.Min(a.X, b.X),
				Math.Min(a.Y, b.Y),
				Math.Min(a.Z, b.Z),
				Math.Min(a.W, b.W)
			);
		}

		Vector4 GetMax(Vector4 a, Vector4? old)
		{
			var b = old ?? a;
			return new Vector4(
				Math.Max(a.X, b.X),
				Math.Max(a.Y, b.Y),
				Math.Min(a.Z, b.Z),
				Math.Min(a.W, b.W)
			);
		}


		#endregion

	}
}
