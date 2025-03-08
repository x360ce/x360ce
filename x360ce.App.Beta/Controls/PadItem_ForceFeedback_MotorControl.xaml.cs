using JocysCom.ClassLibrary.Controls;
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
		#region Fields

		// Made fields explicitly private and readonly.
		private readonly TrackBarUpDownTextBoxLink deadzoneLink;
		private readonly TrackBarUpDownTextBoxLink offsetLink;
		private readonly TrackBarUpDownTextBoxLink testLink;

		#endregion

		#region Constructor

		public PadItem_ForceFeedback_MotorControl()
		{
			// Initialize timer before initializing the component.
			InitHelper.InitTimer(this, InitializeComponent);

			// Initialize links binding the trackbar, updown and textbox controls.
			deadzoneLink = new TrackBarUpDownTextBoxLink(StrengthTrackBar, StrengthUpDown, StrengthTextBox, 0, 100);
			offsetLink = new TrackBarUpDownTextBoxLink(PeriodTrackBar, PeriodUpDown, PeriodTextBox, 0, 100);
			testLink = new TrackBarUpDownTextBoxLink(TestTrackBar, TestUpDown, TestTextBox, 0, 100);

			// Fill direction values.
			var effectDirections = (ForceEffectDirection[])Enum.GetValues(typeof(ForceEffectDirection));
			DirectionComboBox.ItemsSource = effectDirections;
		}

		#endregion

		#region Public Methods

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
					MainGroupBox.Header = "Left Motor - Big, Strong, Low-Frequency";
					break;
				case 1:
					MainGroupBox.Header = "Right Motor - Small, Gentle, High-Frequency";
					break;
				default:
					break;
			}

			var converter = new Converters.PadSettingToNumericConverter<decimal>();
			var enumConverter = new Converters.PadSettingToEnumConverter<ForceEffectDirection>();

			// Set binding.
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorDirection), DirectionComboBox, null, enumConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorStrength), StrengthUpDown, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftMotorPeriod), PeriodUpDown, null, converter);
		}

		public void ParentWindow_Unloaded()
		{
			// TODO: Lines below must be executed only when main window closes.
			DirectionComboBox.ItemsSource = null;
			// Dispose links if not null.
			deadzoneLink?.Dispose();
			offsetLink?.Dispose();
			testLink?.Dispose();

			// Fill direction values.
			SetBinding(null, 0);
		}

		#endregion

		#region Event Handlers

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
	}
}
