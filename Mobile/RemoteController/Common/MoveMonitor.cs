using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Linq;
using System.IO;

namespace JocysCom.RemoteController
{
	public partial class MoveMonitor
	{
		public MoveMonitor()
		{
			InitAudio();
		}

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
				PlayAlarm(true);
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
				PlayAlarm(true);
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
				PlayAlarm(true);
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

		System.Timers.Timer _AlarmTimer;

		public void PlayAlarm(bool play)
		{
			if (play && !IsAlarmEnabled)
			{
				IsAlarmEnabled = play;
				AccelerometerOld = null;
				GyroscopeMin = null;
				GyroscopeMax = null;
				OrientationMin = null;
				OrientationMax = null;
				_AlarmPlayer.Play();
				_AlarmTimer.Start();
			}
			else if (!play && IsAlarmEnabled)
			{
				_AlarmPlayer.Stop();
			}
			if (play)
			{
				// Restart stop timer.
				_AlarmTimer.Stop();
				_AlarmTimer.Start();
			}
			IsAlarmEnabled = play;
		}

		public void PlayBeep(bool play)
		{
			if (play && !IsBeepEnabled)
			{
				_BeepPlayer.Play();
			}
			else if (!play && IsBeepEnabled)
			{
				_BeepPlayer.Stop();
			}
			IsBeepEnabled = play;
		}

		private void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			PlayAlarm(false);
		}

		#endregion

		#region Audio

		public ISimpleAudioPlayer _BeepPlayer;
		public ISimpleAudioPlayer _AlarmPlayer;

		bool IsAlarmEnabled;
		bool IsBeepEnabled;

		void InitAudio()
		{
			var alarmStream = GetStreamFromFile("Alarm.wav");
			_AlarmPlayer = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
			_AlarmPlayer.PlaybackEnded += _AlarmPlayer_PlaybackEnded;
			_AlarmPlayer.Load(alarmStream);
			var beepStream = GetStreamFromFile("Beep.wav");
			_BeepPlayer = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
			_BeepPlayer.PlaybackEnded += _BeepPlayer_PlaybackEnded;
			_BeepPlayer.Load(beepStream);
			// Stop timer.
			_AlarmTimer = new System.Timers.Timer();
			_AlarmTimer.Interval = 10000;
			_AlarmTimer.AutoReset = false;
			_AlarmTimer.Elapsed += _Timer_Elapsed;
		}

		Stream GetStreamFromFile(string filename)
		{
			var assembly = this.GetType().Assembly;
			var path = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(filename));
			var stream = assembly.GetManifestResourceStream(path);
			return stream;
		}

		private void _BeepPlayer_PlaybackEnded(object sender, EventArgs e)
		{
			if (IsBeepEnabled)
				_BeepPlayer.Play();
		}

		private void _AlarmPlayer_PlaybackEnded(object sender, EventArgs e)
		{
			if (IsAlarmEnabled)
				_AlarmPlayer.Play();
		}

		#endregion

	}
}
