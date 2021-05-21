using System;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ForceFeedbackMotorControl.xaml
	/// </summary>
	public partial class PadItem_ForceFeedback_MotorControl : UserControl
	{
		public PadItem_ForceFeedback_MotorControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			deadzoneLink = new TrackBarUpDownTextBoxLink(StrengthTrackBar, StrengthUpDown, StrengthTextBox, 0, 100);
			offsetLink = new TrackBarUpDownTextBoxLink(PeriodTrackBar, PeriodUpDown, PeriodTextBox, 0, 100);
			testLink = new TrackBarUpDownTextBoxLink(TestTrackBar, TestUpDown, TestTextBox, 0, 100);
			// fill direction values.
			var effectDirections = (ForceEffectDirection[])Enum.GetValues(typeof(ForceEffectDirection));
			DirectionComboBox.ItemsSource = effectDirections;
		}

		TrackBarUpDownTextBoxLink deadzoneLink;
		TrackBarUpDownTextBoxLink offsetLink;
		TrackBarUpDownTextBoxLink testLink;

		public void SetBinding(PadSetting ps, int motor)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(DirectionComboBox);
			SettingsManager.UnLoadMonitor(StrengthUpDown);
			SettingsManager.UnLoadMonitor(PeriodUpDown);
			if (ps == null)
				return;
			switch (motor)
			{
				case 0:
					MainGroupBox.Header = "Left Motor(Big, Strong, Low-Frequency)";
					break;
				case 1:
					MainGroupBox.Header = "Right Motor (Small, Gentle, High-Frequency)";
					break;
				default:
					break;
			}
			var converter = new Converters.PadSettingToIntegerConverter();
			var enumConverter = new Converters.PaddSettingToEnumConverter<ForceEffectDirection>();
			// Set binding.
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorDirection), DirectionComboBox, null, enumConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorStrength), StrengthUpDown, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorPeriod), PeriodUpDown, null, converter);
		}
	}
}
