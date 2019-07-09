using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Numerics;
using static Android.Provider.MediaStore;
using Android.Media;

[assembly: Dependency(typeof(Audio))]
namespace JocysCom.RemoteController.Pages
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(false)]
	public partial class SettingsPage : ContentPage
	{

		public SettingsPage()
		{
			InitializeComponent();
			App.Monitor.AccelerometerChanged += Monitor_AccelerometerChanged;
			App.Monitor.OrientationChanged += Monitor_OrientationChanged;
			App.Monitor.GyroscopeChanged += Monitor_GyroscopeChanged;
			AccelerometerSwitch.Toggled += App.Monitor.AccelerometerSwitch_Toggled;
			OrientationSwitch.Toggled += App.Monitor.OrientationSwitch_Toggled;
			GyroscopeSwitch.Toggled += App.Monitor.GyroscopeSwitch_Toggled;
		}

		string format = "{0}: {1:+#0.000000000;-#0.000000000; #0.000000000}, {2:0.000000000}";
		int updateTime = 250;

		DateTime AccelerometerTime;
		DateTime OrientationTime;
		DateTime GyroscopeTime;

		private void Monitor_AccelerometerChanged(object sender, MoveMonitorEventArgs<Vector3> e)
		{
			// Update every half seconds
			var now = DateTime.Now;
			if (now.Subtract(AccelerometerTime).TotalMilliseconds > updateTime)
			{
				AccelerometerTime = now;
				AccelerometerXLabel.Text = string.Format(format, "X", e.Value.X, e.Delta.X);
				AccelerometerYLabel.Text = string.Format(format, "Y", e.Value.Y, e.Delta.Y);
				AccelerometerZLabel.Text = string.Format(format, "Z", e.Value.Z, e.Delta.Z);
			}
		}

		private void Monitor_OrientationChanged(object sender, MoveMonitorEventArgs<Vector4> e)
		{
			// Update every half seconds
			var now = DateTime.Now;
			if (now.Subtract(OrientationTime).TotalMilliseconds > updateTime)
			{
				OrientationTime = now;
				OrientationXLabel.Text = string.Format(format, "X", e.Value.X, e.Delta.X);
				OrientationYLabel.Text = string.Format(format, "Y", e.Value.Y, e.Delta.Y);
				OrientationZLabel.Text = string.Format(format, "Z", e.Value.Z, e.Delta.Z);
				OrientationWLabel.Text = string.Format(format, "W", e.Value.W, e.Delta.W);
			}
		}

		private void Monitor_GyroscopeChanged(object sender, MoveMonitorEventArgs<Vector3> e)
		{
			// Update every half seconds
			var now = DateTime.Now;
			if (now.Subtract(GyroscopeTime).TotalMilliseconds > updateTime)
			{
				GyroscopeTime = now;
				GyroscopeXLabel.Text = string.Format(format, "X", e.Value.X, e.Delta.X);
				GyroscopeYLabel.Text = string.Format(format, "Y", e.Value.Y, e.Delta.Y);
				GyroscopeZLabel.Text = string.Format(format, "Z", e.Value.Z, e.Delta.Z);
			}
		}

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
