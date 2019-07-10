using Android.Media;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace JocysCom.RemoteController
{
	public partial class MoveMonitor
	{
		//private void BluetoothReceiver_Found(object sender, BluetoothDeviceReceiverEventArgs e)
		//{
		//	DeviceLabel.Text = string.Format(e.Device.Name);
		//}

		// Set speed delay for monitoring changes.
		public SensorSpeed speed = SensorSpeed.UI;
		public float sensitivity = 0.1f;

		#region Accelerometer

		//public bool AccelerometerEnabled
		//{
		//	get => Preferences.Get("AccelerometerEnabled", true);
		//	set { Preferences.Set("AccelerometerEnabled", value); OnPropertyChanged(); }
		//}

		public void AccelerometerSwitch_Toggled(object sender, ToggledEventArgs e)
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
					AccelerometerOld = null;
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

		Vector3? AccelerometerOld;

		void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var v = e.Reading.Acceleration;
			if (!AccelerometerOld.HasValue)
				AccelerometerOld = v;
			var d = v - AccelerometerOld.Value;
			if (Math.Abs(d.X) > sensitivity || Math.Abs(d.Y) > sensitivity || Math.Abs(d.Z) > sensitivity)
				PlayAudio(true);
			var ev = AccelerometerChanged;
			if (ev != null)
				ev(this, new MoveMonitorEventArgs<Vector3>(v, d));
		}

		public event EventHandler<MoveMonitorEventArgs<Vector3>> AccelerometerChanged;

		#endregion

		#region Gyroscope

		public void GyroscopeSwitch_Toggled(object sender, ToggledEventArgs e)
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
					GyroscopeMin = null;
					GyroscopeMax = null;
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
			var d = max - min;
			if (d.X > sensitivity || d.Y > sensitivity || d.Z > sensitivity)
				PlayAudio(true);
			var ev = GyroscopeChanged;
			if (ev != null)
				ev(this, new MoveMonitorEventArgs<Vector3>(v, d));
		}

		public event EventHandler<MoveMonitorEventArgs<Vector3>> GyroscopeChanged;

		#endregion

		#region Orientation

		public void OrientationSwitch_Toggled(object sender, ToggledEventArgs e)
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
					OrientationMin = null;
					OrientationMax = null;
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
			var d = max - min;
			if (d.X > sensitivity || d.Y > sensitivity || d.Z > sensitivity || d.W > sensitivity)
				PlayAudio(true);
			var ev = OrientationChanged;
			if (ev != null)
				ev(this, new MoveMonitorEventArgs<Vector4>(v, d));
		}

		public event EventHandler<MoveMonitorEventArgs<Vector4>> OrientationChanged;

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


		Ringtone _Alarm;
		System.Timers.Timer _Timer;

		public void PlayAudio(bool play)
		{
			if (_Alarm == null)
			{
				var uri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
				_Alarm = RingtoneManager.GetRingtone(global::Android.App.Application.Context, uri);
				_Timer = new System.Timers.Timer();
				_Timer.Interval = 2000;
				_Timer.AutoReset = false;
				_Timer.Elapsed += _Timer_Elapsed;
			}
			if (play && !_Alarm.IsPlaying)
			{
				AccelerometerOld = null;
				GyroscopeMin = null;
				GyroscopeMax = null;
				OrientationMin = null;
				OrientationMax = null;
				_Alarm.Play();
				_Timer.Start();
			}
			else if (!play && _Alarm.IsPlaying)
			{
				_Alarm.Stop();
			}
		}

		private void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_Alarm.Stop();
		}

		#endregion

	}
}
