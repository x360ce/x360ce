using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ForceFeedbackControl.xaml
	/// </summary>
	public partial class PadItem_ForceFeedbackControl : UserControl
	{
		public PadItem_ForceFeedbackControl()
		{
			InitializeComponent();
			var effectsTypes = Enum.GetValues(typeof(ForceEffectType)).Cast<ForceEffectType>().Distinct().ToArray();
			ForceTypeComboBox.ItemsSource = effectsTypes;
			overallStrengthLink = new TrackBarUpDownTextBoxLink(StrengthTrackBar, StrengthUpDown, StrengthTextBox, 0, 100);
			InitUpdateTimer();
		}

		System.Timers.Timer updateTimer;

		void InitUpdateTimer()
		{
			updateTimer = new System.Timers.Timer();
			updateTimer.AutoReset = false;
			updateTimer.Interval = 500;
			updateTimer.Elapsed += UpdateTimer_Elapsed;

		}

		void UpdateTimerReset()
		{
			updateTimer.Stop();
			updateTimer.Start();
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ControlsHelper.BeginInvoke(SendVibration);
		}

		TrackBarUpDownTextBoxLink overallStrengthLink;

		PadSetting _padSetting;
		MapTo _MappedTo;

		public void SetBinding(MapTo mappedTo, PadSetting o)
		{
			_MappedTo = mappedTo;
			if (_padSetting != null)
				_padSetting.PropertyChanged -= _padSetting_PropertyChanged;
			LeftForceFeedbackMotorPanel.TestUpDown.ValueChanged -= TestUpDown_ValueChanged;
			RightForceFeedbackMotorPanel.TestUpDown.ValueChanged -= TestUpDown_ValueChanged;
			// Unbind first.
			SettingsManager.UnLoadMonitor(EnabledCheckBox);
			SettingsManager.UnLoadMonitor(SwapMotorsCheckBox);
			SettingsManager.UnLoadMonitor(ForceTypeComboBox);
			SettingsManager.UnLoadMonitor(StrengthUpDown);
			if (o == null)
				return;
			_padSetting = o;
			var converter = new Converters.PaddSettingToIntegerConverter();
			var enumConverter = new Converters.PaddSettingToEnumConverter<ForceEffectType>();
			// Set binding.
			SettingsManager.LoadAndMonitor(o, nameof(o.ForceEnable), EnabledCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.ForceSwapMotor), SwapMotorsCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.ForceType), ForceTypeComboBox, null, enumConverter);
			SettingsManager.LoadAndMonitor(o, nameof(o.ForceOverall), StrengthUpDown, null, converter);
			_padSetting.PropertyChanged += _padSetting_PropertyChanged;
			LeftForceFeedbackMotorPanel.TestUpDown.ValueChanged += TestUpDown_ValueChanged;
			RightForceFeedbackMotorPanel.TestUpDown.ValueChanged += TestUpDown_ValueChanged;
		}

		private void ForceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var type = (ForceEffectType)ForceTypeComboBox.SelectedItem;
			var list = new List<string>();
			if (type == ForceEffectType.Constant || type == ForceEffectType._Type2)
				list.Add("Constant force type. Good for vibrating motors on game pads.");
			if (type.HasFlag(ForceEffectType.PeriodicSine))
				list.Add("Periodic 'Sine Wave' force type. Good for car/plane engine vibration. Good for torque motors on wheels.");
			if (type.HasFlag(ForceEffectType.PeriodicSawtooth))
				list.Add("Periodic 'Sawtooth Down Wave' force type. Good for gun recoil. Good for torque motors on wheels.");
			if (type.HasFlag(ForceEffectType._Type2))
				list.Add("Alternative implementation - two motors / actuators per effect.");
			ForceTypeDescriptionTextBlock.Text = string.Format("{0} ({1}) - {2}", type, (int)type, string.Join(" ", list));
		}

		public static List<string> ForceProperties = new List<string>()
		{
			nameof(PadSetting.ForceEnable),
			nameof(PadSetting.ForceOverall),
			nameof(PadSetting.ForceSwapMotor),
			nameof(PadSetting.ForceType),
			nameof(PadSetting.LeftMotorDirection),
			nameof(PadSetting.LeftMotorPeriod),
			nameof(PadSetting.LeftMotorStrength),
			nameof(PadSetting.RightMotorDirection),
			nameof(PadSetting.RightMotorPeriod),
			nameof(PadSetting.RightMotorStrength),
		};

		private void _padSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			// If property changed, which can affect force then...
			if (ForceProperties.Contains(e.PropertyName))
				UpdateTimerReset();
		}

		private void TestUpDown_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
		{
			UpdateTimerReset();
		}

		void SendVibration()
		{
			var index = (int)_MappedTo - 1;
			var game = SettingsManager.CurrentGame;
			var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			int leftTestValue = LeftForceFeedbackMotorPanel.TestUpDown.Value ?? 0;
			int rightTestValue = RightForceFeedbackMotorPanel.TestUpDown.Value ?? 0;
			if (isVirtual)
			{
				var largeMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, leftTestValue);
				var smallMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, rightTestValue);
				Global.DHelper.SetVibration(_MappedTo, largeMotor, smallMotor, 0);
			}
			else
			{
				lock (Controller.XInputLock)
				{
					// Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
					var leftMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, leftTestValue);
					var rightMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, rightTestValue);
					var gamePad = Global.DHelper.LiveXiControllers[index];
					var isConnected = Global.DHelper.LiveXiConnected[index];
					if (Controller.IsLoaded && isConnected)
					{
						var vibration = new Vibration();
						vibration.LeftMotorSpeed = leftMotor;
						vibration.RightMotorSpeed = rightMotor;
						gamePad.SetVibration(vibration);
					}
				}
			}
		}
	}
}
