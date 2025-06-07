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
		#region Fields

		private System.Timers.Timer updateTimer;
		private readonly TrackBarUpDownTextBoxLink overallStrengthLink;
		private PadSetting _padSetting;
		private MapTo _MappedTo;

		// Contains property names which, if changed, trigger a force update.
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

		#endregion

		#region Constructor

		public PadItem_ForceFeedbackControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			var effectsTypes = Enum.GetValues(typeof(ForceEffectType))
				.Cast<ForceEffectType>()
				.Distinct()
				.ToArray();
			ForceTypeComboBox.ItemsSource = effectsTypes;
			overallStrengthLink = new TrackBarUpDownTextBoxLink(StrengthTrackBar, StrengthUpDown, StrengthTextBox, 0, 100);
			InitUpdateTimer();
		}

		#endregion

		#region Timer Methods

		private void InitUpdateTimer()
		{
			updateTimer = new System.Timers.Timer();
			updateTimer.AutoReset = false;
			updateTimer.Interval = 500;
			updateTimer.Elapsed += UpdateTimer_Elapsed;
		}

		private void UpdateTimerReset()
		{
			updateTimer.Stop();
			updateTimer.Start();
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ControlsHelper.BeginInvoke(SendVibration);
		}

		#endregion

		#region Binding Methods

		public void SetBinding(MapTo mappedTo, PadSetting ps)
		{
			_MappedTo = mappedTo;
			if (_padSetting != null) _padSetting.PropertyChanged -= _padSetting_PropertyChanged;
			LeftForceFeedbackMotorPanel.TestUpDown.ValueChanged -= TestUpDown_ValueChanged;
			RightForceFeedbackMotorPanel.TestUpDown.ValueChanged -= TestUpDown_ValueChanged;
			// Unbind first.
			SettingsManager.UnLoadMonitor(ForceEnabledCheckBox);
			SettingsManager.UnLoadMonitor(SwapMotorsCheckBox);
			SettingsManager.UnLoadMonitor(ForceTypeComboBox);
			SettingsManager.UnLoadMonitor(StrengthUpDown);
			if (ps == null)
				return;
			_padSetting = ps;
			var intConverter = new Converters.PadSettingToNumericConverter<decimal>();
			var boolConverter = new Converters.PadSettingToBoolConverter();
			var enumConverter = new Converters.PadSettingToEnumConverter<ForceEffectType>();
			// Set binding.
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ForceEnable), ForceEnabledCheckBox, null, boolConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ForceSwapMotor), SwapMotorsCheckBox, null, boolConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ForceType), ForceTypeComboBox, null, enumConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ForceOverall), StrengthUpDown, null, intConverter);
			_padSetting.PropertyChanged += _padSetting_PropertyChanged;
			LeftForceFeedbackMotorPanel.TestUpDown.ValueChanged += TestUpDown_ValueChanged;
			RightForceFeedbackMotorPanel.TestUpDown.ValueChanged += TestUpDown_ValueChanged;
		}

		#endregion

		#region Event Handlers

		private void _padSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			// If property changed, which can affect force then...
			if (ForceProperties.Contains(e.PropertyName))
				UpdateTimerReset();
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

		private void TestUpDown_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<decimal?> e)
		{
			UpdateTimerReset();
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		#endregion

		#region Private Methods

		private void SendVibration()
		{
			var index = (int)_MappedTo - 1;
			var game = SettingsManager.CurrentGame;
			var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			int leftTestValue = (int)LeftForceFeedbackMotorPanel.TestUpDown.Value;
			int rightTestValue = (int)RightForceFeedbackMotorPanel.TestUpDown.Value;
			if (isVirtual)
			{
				var largeMotor = (byte)ConvertHelper.ConvertRange(leftTestValue, 0, 100, byte.MinValue, byte.MaxValue);
				var smallMotor = (byte)ConvertHelper.ConvertRange(rightTestValue, 0, 100, byte.MinValue, byte.MaxValue);
				Global.DHelper.SetVibration(_MappedTo, largeMotor, smallMotor, 0);
			}
			else
			{
				lock (Controller.XInputLock)
				{
					// Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
					var leftMotor = (short)ConvertHelper.ConvertRange(leftTestValue, 0, 100, short.MinValue, short.MaxValue);
					var rightMotor = (short)ConvertHelper.ConvertRange(rightTestValue, 0, 100, short.MinValue, short.MaxValue);
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

		#endregion

		#region Public Methods

		public void ParentWindow_Unloaded()
		{
			// TODO: Lines below must be executed onbmly when main window close.
			overallStrengthLink.Dispose();
			SetBinding(_MappedTo, null);
		}

		#endregion
	}
}
