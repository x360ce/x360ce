using JocysCom.ClassLibrary.Controls;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	// Half.
	public class ContainsKeywordConverterType : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text = value as string;
			if (!string.IsNullOrEmpty(text))
			{
				if (text.StartsWith("IButton"))
					return "IButton";
				else if (text.StartsWith("Button"))
					return "Button";
				else if (text.StartsWith("IAxis"))
					return "IAxis";
				else if (text.StartsWith("Axis"))
					return "Axis";
				else if (text.StartsWith("IHAxis"))
					return "IHAxis";
				else if (text.StartsWith("HAxis"))
					return "HAxis";
				else if (text.StartsWith("ISlider"))
					return "ISlider";
				else if (text.StartsWith("Slider"))
					return "Slider";
				else if (text.StartsWith("IHSlider"))
					return "IHSlider";
				else if (text.StartsWith("HSlider"))
					return "HSlider";
				else
					return "Empty";
			}
			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public static class InversionHelper
	{
		public static readonly DependencyProperty InversionTargetProperty =
			DependencyProperty.RegisterAttached(
				"InversionTarget",
				typeof(TextBox),
				typeof(InversionHelper),
				new PropertyMetadata(null));

		public static void SetInversionTarget(UIElement element, TextBox value)
		{
			element.SetValue(InversionTargetProperty, value);
		}

		public static TextBox GetInversionTarget(UIElement element)
		{
			return (TextBox)element.GetValue(InversionTargetProperty);
		}
	}


	/// <summary>
	/// Interaction logic for PadControl_GeneralControl.xaml
	/// </summary>
	public partial class PadItem_GeneralControl : UserControl
	{
		public PadItem_GeneralControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		PadSetting _padSetting;
		MapTo _MappedTo;

		public void SetBinding(MapTo mappedTo, PadSetting ps, List<ImageInfo> imageInfo)
		{
			_MappedTo = mappedTo;
			//if (_padSetting != null) _padSetting.PropertyChanged -= _padSetting_PropertyChanged;

			// Unbind controls.
			foreach (var item in imageInfo) { SettingsManager.UnLoadMonitor(item.ControlBindedName as Control); }

			// Bind controls.
			if (ps == null) return;
			_padSetting = ps;
			var converter = new Converters.PaddSettingToText();


			foreach (var item in imageInfo) { SettingsManager.LoadAndMonitor(ps, item.Code.ToString(), item.ControlBindedName as Control, null, converter); }

			//_padSetting.PropertyChanged += _padSetting_PropertyChanged;
		}

		//private void _padSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		// This event handler was originally empty.
		//}

		private void SetPresetButton_Click(object sender, RoutedEventArgs e) { }
		private void RemapAllButton_Click(object sender, RoutedEventArgs e) { }

		public void MapNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(sender is ComboBox box) || !(box.SelectedItem is Layout item)) return;

			// Update Trigger labels.
			TriggerLLabel.Content = item.LeftTrigger;
			TriggerRLabel.Content = item.RightTrigger;
			// Update Bumper labels.
			BumperLLabel.Content = item.LeftShoulder;
			BumperRLabel.Content = item.RightShoulder;
			// Update Menu labels.
			MenuBackLabel.Content = item.ButtonBack;
			MenuGuideLabel.Content = item.ButtonGuide;
			MenuStartLabel.Content = item.ButtonStart;
			// Update Action labels.
			ActionALabel.Content = item.ButtonA;
			ActionBLabel.Content = item.ButtonB;
			ActionXLabel.Content = item.ButtonX;
			ActionYLabel.Content = item.ButtonY;
			// Update D-Pad labels.
			DPadLabel.Content = item.DPad;
			DPadDownLabel.Content = item.DPadDown;
			DPadLeftLabel.Content = item.DPadLeft;
			DPadRightLabel.Content = item.DPadRight;
			DPadUpLabel.Content = item.DPadUp;
			// Update Stick Left labels.
			StickLButtonLabel.Content = item.LeftThumbButton;
			StickLAxisXLabel.Content = item.LeftThumbAxisX;
			StickLAxisYLabel.Content = item.LeftThumbAxisY;
			StickLDownLabel.Content = item.LeftThumbDown;
			StickLLeftLabel.Content = item.LeftThumbLeft;
			StickLRightLabel.Content = item.LeftThumbRight;
			StickLUpLabel.Content = item.LeftThumbUp;
			// Update Stick Right labels.
			StickRButtonLabel.Content = item.RightThumbButton;
			StickRAxisXLabel.Content = item.RightThumbAxisX;
			StickRAxisYLabel.Content = item.RightThumbAxisY;
			StickRDownLabel.Content = item.RightThumbDown;
			StickRLeftLabel.Content = item.RightThumbLeft;
			StickRRightLabel.Content = item.RightThumbRight;
			StickRUpLabel.Content = item.RightThumbUp;
		}

		#region Drag and Drop Menu

		// Drag and Drop Menu PreviewMouseMove event.
		private void DragAndDropMenuLabel_Source_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (sender is Label label && e.LeftButton == MouseButtonState.Pressed)
			{
				DragDrop.DoDragDrop(label, label.Tag?.ToString() ?? string.Empty, DragDropEffects.Copy);
			}
		}

		// Drag and Drop Menu Drop event.
		private void DragAndDropMenu_Target_Drop(object sender, DragEventArgs e)
		{
			if (sender is TextBox textbox && e.Data.GetDataPresent(DataFormats.Text))
			{
				textbox.Clear();
				textbox.Text = (string)e.Data.GetData(DataFormats.Text);
			}
			e.Handled = true;
		}

		// Colors.
		SolidColorBrush colorActive = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF42C765");
		SolidColorBrush colorLight = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF0F0F0");
		SolidColorBrush colorBackgroundDark = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFDEDEDE");
		//SolidColorBrush colorNormalPath = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6699FF");
		//SolidColorBrush colorNormalTextBox = Brushes.White;
		//SolidColorBrush colorBlack = (SolidColorBrush)new BrushConverter().ConvertFrom("#11000000");
		SolidColorBrush colorNormal = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6699FF");
		//SolidColorBrush colorOver = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFCC66");
		SolidColorBrush colorRecord = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFF6B66");

		Dictionary<int, (Label, Label, Label)> ButtonDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> PovDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> PovBDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> AxisDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> HAxisDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> SliderDictionary = new Dictionary<int, (Label, Label, Label)>();
		Dictionary<int, (Label, Label, Label)> HSliderDictionary = new Dictionary<int, (Label, Label, Label)>();

		object updateLock = new object();
		object oldState = null;

		public CustomDiState GetCustomDiState(UserDevice ud)
		{
			CustomDiState customDiState = null;
			var state = ud?.DeviceState;
			if (state == null || state == oldState)
				return null;
			lock (updateLock)
			{
				if (state is MouseState mState)
					customDiState = new CustomDiState(mState);
				else if (state is KeyboardState kState)
					customDiState = new CustomDiState(kState);
				else if (state is JoystickState jState)
					customDiState = new CustomDiState(jState);
			}
			return customDiState;
		}

		UniformGrid PovUnifromGrid;

		// Create DragAndDrop menu labels.
		private void DragAndDropMenuLabels_Create(Dictionary<int, (Label, Label, Label)> dictionary, List<int> list, string itemName, string headerName, string iconName)
		{
			try
			{
				// GroupBox Header (icon and text).
				StackPanel headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
				// Group icons.
				headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName] });
				if (!headerName.Contains("POV"))
				{
					headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName + "_Inverted"], Margin = new Thickness(3, 0, 0, 0) });
				}
				// Group title.
				headerStackPanel.Children.Add(new TextBlock { Text = headerName, Margin = new Thickness(3, 0, 0, 0) });
				if (headerName.Contains("HALF"))
				{
					headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName + "_2"], Margin = new Thickness(3, 0, 3, 0) });
					headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName + "_2"] });
				}
				// GroupBox Content (UniformGrid for Labels).
				UniformGrid buttonsUniformGrid = new UniformGrid { Columns = list.Last().ToString().Length > 2 ? 6 : 8 };
				// GroupBox.
				GroupBox buttonsGroupBox = new GroupBox { Header = headerStackPanel, Content = buttonsUniformGrid };

				// Put GroupBoxes into NORMAL and INVERTED tabs.
				if (iconName.Contains("Inverted"))
				{
					// DragAndDropStackPanelInverted.Children.Add(buttonsGroupBox);
				}
				else
				{
					DragAndDropStackPanel.Children.Add(buttonsGroupBox);
				}

				// Put POVB buttons inside POV GroupBox.
				if (itemName == "POV")
				{
					PovUnifromGrid = buttonsUniformGrid;
				}
				if (itemName == "POVB")
				{
					buttonsGroupBox.Visibility = Visibility.Collapsed;
				}

				// Create drag and drop buttons.
				dictionary.Clear();
				foreach (var i in list)
				{
					Label buttonLabel = new Label();
					if (itemName == "POVB")
					{
						var povNumber = (i / 4) + 1;
						// Name.
						var povNumberN = new[] { "U", "R", "D", "L" }[i % 4];
						buttonLabel.Content = povNumberN;
						// Drag and drop text.
						var povNumberB = new[] { "Up", "Right", "Down", "Left" }[i % 4];
						buttonLabel.Tag = $"POV {povNumber} {povNumberB}";
					}
					else
					{
						buttonLabel.Content = (i + 1).ToString();
						buttonLabel.Tag = $"{itemName} {buttonLabel.Content}";
					}

					buttonLabel.PreviewMouseMove += DragAndDropMenuLabel_Source_PreviewMouseMove;

					Label valueLabel = new Label
					{
						IsHitTestVisible = false,
						FontSize = 8,
						Padding = new Thickness(0),
						Background = colorLight
					};

					Label valueLabelInverted = new Label
					{
						IsHitTestVisible = false,
						FontSize = 8,
						Foreground = colorNormal,
						Padding = new Thickness(0),
						Background = colorLight
					};

					StackPanel stackPanel = new StackPanel();
					stackPanel.Children.Add(buttonLabel);
					stackPanel.Children.Add(valueLabel);
					stackPanel.Children.Add(valueLabelInverted);

					// Put POVB buttons inside POV GroupBox.
					if (itemName == "POVB")
					{
						PovUnifromGrid.Children.Add(stackPanel);
					}
					else
					{
						buttonsUniformGrid.Children.Add(stackPanel);
					}

					dictionary.Add(i, (buttonLabel, valueLabel, valueLabelInverted));
				}
			}
			catch (Exception ex)
			{
				// Simply ignore the exception by storing the message.
				var _ = ex.Message;
			}
		}

		List<int> buttons = new List<int>();
		List<int> povs = new List<int>();
		List<int> axes = new List<int>();
		List<int> sliders = new List<int>();

		// Runs every time a new DirectInput device becomes available / unaivalble.
		public void ResetDiMenuStrip(UserDevice ud)
		{
			// Clear drag and drop StackPanel children elements in XAML page.
			DragAndDropStackPanel.Children.Clear();
			// Clear dictionaries used to create drag and drop StackPanel content.
			ButtonDictionary.Clear();
			PovDictionary.Clear();
			PovBDictionary.Clear();
			AxisDictionary.Clear();
			HAxisDictionary.Clear();
			SliderDictionary.Clear();
			HSliderDictionary.Clear();

			// Clear lists with DirectInput device InstanceNumber's.
			buttons.Clear();
			povs.Clear();
			axes.Clear();
			sliders.Clear();

			if (ud == null || GetCustomDiState(ud) == null) return;

			GetDeviceObjectInstancesByObjectTypeGuid(ud);

			buttons.Sort();
			povs.Sort();
			axes.Sort();
			sliders.Sort();

			// Buttons and Keys.
			if (buttons.Any())
			{
				DragAndDropMenuLabels_Create(ButtonDictionary, buttons, "Button", "BUTTON", "Icon_DragAndDrop_Button");
			}
			// Axes.
			if (axes.Any())
			{
				DragAndDropMenuLabels_Create(AxisDictionary, axes, "Axis", "AXIS", "Icon_DragAndDrop_Axis");
				DragAndDropMenuLabels_Create(HAxisDictionary, axes, "HAxis", "AXIS · HALF", "Icon_DragAndDrop_Axis_Half");
			}
			// Sliders.
			if (sliders.Any())
			{
				DragAndDropMenuLabels_Create(SliderDictionary, sliders, "Slider", "SLIDER", "Icon_DragAndDrop_Axis");
				DragAndDropMenuLabels_Create(HSliderDictionary, sliders, "HSlider", "SLIDER · HALF", "Icon_DragAndDrop_Axis_Half");
			}
			// POVs.
			if (povs.Any())
			{
				DragAndDropMenuLabels_Create(PovDictionary, povs, "POV", "POV", "Icon_DragAndDrop_POV");
				var povButtons = new List<int>();
				// Add 4 buttons (Up, Right, Bottom, Left) for each POV. 
				for (int i = 0; i < povs.Count * 4; i++) { povButtons.Add(i); }
				DragAndDropMenuLabels_Create(PovBDictionary, povButtons, "POVB", "POV · BUTTON", "Icon_DragAndDrop_POV");
			}
		}

		private void GetDeviceObjectInstancesByObjectTypeGuid(UserDevice ud, int usage = 0)
		{
			if (!(ud.Device is Joystick device))
				return;

			var deviceObjects = device?.GetObjects();
			StringBuilder stringBuilder = new StringBuilder();

			// Sliders.
			var state = device.GetCurrentState();
			if (state.Sliders[0] != 0) sliders.Add(0);
			if (state.Sliders[1] != 0) sliders.Add(1);
			if (state.AccelerationSliders[0] != 0) sliders.Add(2);
			if (state.AccelerationSliders[1] != 0) sliders.Add(3);
			if (state.ForceSliders[0] != 0) sliders.Add(4);
			if (state.ForceSliders[1] != 0) sliders.Add(5);
			if (state.VelocitySliders[0] != 0) sliders.Add(6);
			if (state.VelocitySliders[1] != 0) sliders.Add(7);

			// POVs.
			for (int i = 0; i < ud.CapPovCount; i++)
			{
				povs.Add(i);
			}

			// Buttons, Keys.
			for (int i = 0; i < ud.CapButtonCount; i++)
			{
				buttons.Add(i);
			}

			// Axes.
			foreach (DeviceObjectInstance item in deviceObjects
				.Where(x => x.ObjectType != ObjectGuid.Unknown)
				.OrderBy(x => x.UsagePage)
				.ThenBy(x => x.Usage)
				.ThenBy(x => x.ObjectId.InstanceNumber))
			{
				// Axes.
				if (new[] { ObjectGuid.XAxis, ObjectGuid.YAxis, ObjectGuid.ZAxis, ObjectGuid.RxAxis, ObjectGuid.RyAxis, ObjectGuid.RzAxis }
					.Contains(item.ObjectType))
				{
					if (ud.IsMouse)
						axes.Add(item.ObjectId.InstanceNumber);
					else if (CustomDiHelper.AxisUsageDictionary.TryGetValue(item.Usage, out var value))
						axes.Add(value.Item2);
				}
				stringBuilder.Append($"INFO: UsagePage {item.UsagePage}. " +
					$"Usage {item.Usage.ToString().PadLeft(2, ' ')}. " +
					$"InstanceNumber {item.ObjectId.InstanceNumber.ToString().PadLeft(2, ' ')}. " +
					$"Offset {item.Offset.ToString().PadLeft(2, ' ')}. " +
					$"ObjectType {item.ObjectType} ({GetObjectTypeName(item.ObjectType)}). " +
					$"Name {item.Name}. " +
					$"Flags {item.ObjectId.Flags}.\n");
			}

			Debug.WriteLine($"\nINFO: InstanceName {ud.InstanceName}.\n{stringBuilder}");
			//if (ud.InstanceGuid == new Guid("2c6612a0-d772-11e7-8003-444553540000") ||
			//	ud.InstanceGuid == new Guid("dff18ef0-139b-11ea-8001-444553540000") ||
			//	ud.InstanceGuid == new Guid("1a36a500-7535-11ef-8001-444553540000") ||
			//	ud.InstanceGuid == new Guid("db208ce0-e8ce-11e7-8002-444553540000"))
			//{
			//	var ins = ud.Device;
			//}
		}

		public static string GetObjectTypeName(Guid guid)
		{
			foreach (FieldInfo field in typeof(ObjectGuid).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if (field.FieldType == typeof(Guid) && (Guid)field.GetValue(null) == guid)
					return field.Name;
			}
			return "Unknown";
		}

		//private void SetLabelDIContent(int axisLength, CustomDiState customDiState, TargetType targetType, Label label)
		//{
		//	Map map = _padSetting.Maps.FirstOrDefault(x => x.Target == targetType);

		//	if (map?.Index <= 0 || map.Index > axisLength)
		//		return;

		//	var i = map.Index - 1;
		//	if (map.IsAxis || map.IsHalf || map.IsInverted)
		//	{
		//		label.Content = customDiState.Axis[i];
		//	}
		//	else if (map.IsButton)
		//	{
		//		label.Content = customDiState.Buttons[i] ? 1 : 0;
		//	}
		//	else if (map.IsSlider)
		//	{
		//		label.Content = customDiState.Sliders[i];
		//	}
		//}

		// Update DragAndDrop menu labels.
		public void DragAndDropMenuLabels_Update(UserDevice ud)
		{
			// var customDiState = GetCustomDiState(ud);
			// if (customDiState == null) return;

			// int axisLength = ud.DiState.Axis.Length;

			//// Trigger.
			// SetLabelDIContent(axisLength, customDiState, TargetType.LeftTrigger, TriggerLLabelDI);
			// SetLabelDIContent(axisLength, customDiState, TargetType.RightTrigger, TriggerRLabelDI);
			// TriggerLLabelDZ.Content = _padSetting.LeftTriggerDeadZone;
			// TriggerRLabelDZ.Content = _padSetting.RightTriggerDeadZone;

			//// Buttons.
			// SetLabelDIContent(axisLength, customDiState, TargetType.Button, BumperLLabelDI);

			//// Stick Left.
			// SetLabelDIContent(axisLength, customDiState, TargetType.LeftThumbX, StickLAxisXLabelDI);
			// SetLabelDIContent(axisLength, customDiState, TargetType.LeftThumbY, StickLAxisYLabelDI);
			// StickLAxisXLabelDZ.Content = _padSetting.LeftThumbDeadZoneX;
			// StickLAxisYLabelDZ.Content = _padSetting.LeftThumbDeadZoneY;

			//// Stick Right.
			// SetLabelDIContent(axisLength, customDiState, TargetType.RightThumbX, StickRAxisXLabelDI);
			// SetLabelDIContent(axisLength, customDiState, TargetType.RightThumbY, StickRAxisYLabelDI);
			// StickRAxisXLabelDZ.Content = _padSetting.RightThumbDeadZoneX;
			// StickRAxisYLabelDZ.Content = _padSetting.RightThumbDeadZoneY;

			// Buttons.
			foreach (var kvp in ButtonDictionary)
			{
				bool bDS = ud.DiState.Buttons[kvp.Key];

				ButtonDictionary[kvp.Key].Item1.Background = bDS ? colorActive : Brushes.Transparent;
				ButtonDictionary[kvp.Key].Item2.Content = bDS.ToString();
				ButtonDictionary[kvp.Key].Item3.Content = (bDS ? "True" : "False") == "True" ? "False" : "True";

				// Record active button.
				if (recordTextBox != null && bDS)
				{
					RecordAxisOrButton(ButtonDictionary[kvp.Key].Item1.Tag.ToString());
				}
			}

			// POVs.
			int[] povButtonValues = new[] { 0, 9000, 18000, 27000, 0, 9000, 18000, 27000, 0, 9000, 18000, 27000, 0, 9000, 18000, 27000 };
			foreach (var kvp in PovDictionary)
			{
				int pDS = ud.DiState.POVs[kvp.Key];
				PovDictionary[kvp.Key].Item1.Background = pDS > -1 ? colorActive : Brushes.Transparent;
				PovDictionary[kvp.Key].Item2.Content = pDS;
				// Up, Right, Down, Left.
				for (int b = 0; b < PovDictionary.Count * 4 && b < povButtonValues.Length; b++)
				{
					PovBDictionary[b].Item1.Background = pDS == povButtonValues[b] ? colorActive : Brushes.Transparent;
					PovBDictionary[b].Item2.Content = pDS == povButtonValues[b] ? pDS : -1;
				}

				// Record active POV.
				if (recordTextBox != null && pDS > -1)
				{
					var povName = PovDictionary[kvp.Key].Item1.Tag.ToString(); // "POV 1".
					var povDirection = povName;
					if (recordTextBox != DPadTextBox)
					{
						switch (pDS)
						{
							case 0: povName = povName + " Up"; break;
							case 9000: povName = povName + " Right"; break;
							case 18000: povName = povName + " Down"; break;
							case 27000: povName = povName + " Left"; break;
						}
					}
					RecordAxisOrButton(povName);
				}
			}

			// Stick axes.
			const int DragAndDropAxisDeadzone = 8000;
			foreach (var kvp in AxisDictionary)
			{
				int aDS = ud.DiState.Axis[kvp.Key];
				bool active = aDS < 32767 - DragAndDropAxisDeadzone || aDS > 32767 + DragAndDropAxisDeadzone;
				AxisDictionary[kvp.Key].Item1.Background = active ? colorActive : Brushes.Transparent;
				HAxisDictionary[kvp.Key].Item1.Background = active ? colorActive : Brushes.Transparent;

				AxisDictionary[kvp.Key].Item2.Content = aDS;
				HAxisDictionary[kvp.Key].Item2.Content = Math.Max(0, Math.Min((aDS - 32767) * 2, 65535));
				AxisDictionary[kvp.Key].Item3.Content = Math.Abs(65535 - aDS);
				HAxisDictionary[kvp.Key].Item3.Content = Math.Max(0, Math.Min((Math.Abs(65535 - aDS) - 32767) * 2, 65535));

				// Record active axis.
				if (recordTextBox != null && active)
				{
					RecordAxisOrButton(AxisDictionary[kvp.Key].Item1.Tag.ToString());
				}
			}

			// Slider axes.
			const int DragAndDropSliderDeadzone = 16000;
			foreach (var kvp in SliderDictionary)
			{
				int sDS = ud.DiState.Sliders[kvp.Key];
				bool active = sDS > DragAndDropSliderDeadzone;
				SliderDictionary[kvp.Key].Item1.Background = active ? colorActive : Brushes.Transparent;
				HSliderDictionary[kvp.Key].Item1.Background = active ? colorActive : Brushes.Transparent;

				SliderDictionary[kvp.Key].Item2.Content = sDS;
				HSliderDictionary[kvp.Key].Item2.Content = Math.Max(0, Math.Min((sDS - 32767) * 2, 65535));

				SliderDictionary[kvp.Key].Item3.Content = Math.Abs(65535 - sDS);
				HSliderDictionary[kvp.Key].Item3.Content = Math.Max(0, Math.Min((Math.Abs(65535 - sDS) - 32767) * 2, 65535));

				// Record active axis.
				if (recordTextBox != null && active)
				{
					RecordAxisOrButton(SliderDictionary[kvp.Key].Item1.Tag.ToString());
				}
			}
		}
		#endregion

		#region ■ Direct Input Menu

		// Drag and Drop related commented code is preserved.
		//List<MenuItem> DiMenuStrip = new List<MenuItem>();
		//string cRecord = "[Record]";
		//string cEmpty = "<empty>";
		//string cPOVs = "povs";

		// Function is recreated as soon as new DirectInput Device is available.
		//public void ResetDiMenuStrip2(UserDevice ud)
		//{
		//	DiMenuStrip.Clear();
		//	MenuItem mi;
		//	mi = new MenuItem() { Header = cEmpty };
		//	mi.Foreground = SystemColors.ControlDarkBrush;
		//	mi.Click += DiMenuStrip_Click;
		//	DiMenuStrip.Add(mi);
		//	// Return if direct input device is not available.
		//	if (ud == null)
		//		return;
		//	// Add [Record] label.
		//	mi = new MenuItem() { Header = cRecord };
		//	//mi.Icon = new ContentControl();
		//	mi.Click += DiMenuStrip_Click;
		//	DiMenuStrip.Add(mi);

		//	// Do not add menu items for keyboard, because user interface will become too sluggish.
		//	// Recording feature is preferred way to map keyboard label.
		//	if (!ud.IsKeyboard)
		//	{
		//		// Add Buttons.
		//		mi = new MenuItem() { Header = "Buttons" };
		//		DiMenuStrip.Add(mi);
		//		CreateItems(mi, "Inverted", "IButton {0}", "-{0}", ud.CapButtonCount);
		//		CreateItems(mi, "Button {0}", "{0}", ud.CapButtonCount);
		// Add Axes.
		//		if (ud.DiAxeMask > 0)
		//		{
		//			mi = new MenuItem() { Header = "Axes" };
		//			DiMenuStrip.Add(mi);
		//			CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
		//			CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
		//			CreateItems(mi, "Half", "HAxis {0}", "x{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
		//			CreateItems(mi, "Axis {0}", "a{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
		//		}
		//		Add Sliders. 
		//		if (ud.DiSliderMask > 0)
		//		{
		//			mi = new MenuItem() { Header = "Sliders" };
		//			DiMenuStrip.Add(mi);
		//			// 2 x Sliders, 2 x AccelerationSliders, 2 x bDS.ForceSliders, 2 x VelocitySliders
		//			CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
		//			CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
		//			CreateItems(mi, "Half", "HSlider {0}", "h{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
		//			CreateItems(mi, "Slider {0}", "s{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
		//		}
		//		// Add D-Pads.
		//		if (ud.CapPovCount > 0)
		//		{
		//			// Add povs.
		//			mi = new MenuItem() { Header = cPOVs };
		//			DiMenuStrip.Add(mi);
		//			// Add D-Pad Top, Right, Bottom, Left label.
		//			var dPadNames = Enum.GetNames(typeof(DPadEnum));
		//			for (int dInputPolylineStepSize = 0; dInputPolylineStepSize < ud.CapPovCount; dInputPolylineStepSize++)
		//			{
		//				var dPadItem = CreateItem("POV {0}", "{1}{0}", dInputPolylineStepSize + 1, SettingName.SType.POV);
		//				mi.Items.Add(dPadItem);
		//				for (int d = 0; d < dPadNames.Length; d++)
		//				{
		//					var dPadButtonIndex = dInputPolylineStepSize * 4 + d + 1;
		//					var dPadButtonItem = CreateItem("POV {0} {1}", "{2}{3}", dInputPolylineStepSize + 1, dPadNames[d], SettingName.SType.POVButton, dPadButtonIndex);
		//					dPadItem.Items.Add(dPadButtonItem);
		//				}
		//			}
		//		}
		//	}
		//}

		//void CreateItems(MenuItem parent, string subMenu, string text, string tag, int count, int? mask = null)
		//{
		//	var smi = new MenuItem() { Header = subMenu };
		//	parent.Items.Add(smi);
		//	CreateItems(smi, text, tag, count, mask);
		//}

		/// <summary>Create menu item.</summary>
		/// <param name="mask">Mask contains information if item is present.</param>
		//void CreateItems(MenuItem parent, string text, string tag, int count, int? mask = null)
		//{
		//	var items = new List<MenuItem>();
		//	for (int i = 0; i < count; i++)
		//	{
		//		// If mask specified and item is not present then...
		//		if (mask.HasValue && i < 32 && (((int)Math.Pow(2, i) & mask) == 0))
		//			continue;
		//		var item = CreateItem(text, tag, i + 1);
		//		items.Add(item);
		//	}
		//	foreach (var item in items)
		//		parent.Items.Add(item);
		//}

		//MenuItem CreateItem(string text, string tag, params object[] args)
		//{
		//	var item = new MenuItem();
		//	item.Header = string.Format(text, args);
		//	item.Tag = string.Format(tag, args);
		//	item.Padding = new Thickness(0);
		//	item.Margin = new Thickness(0);
		//	item.Click += DiMenuStrip_Click;
		//	return item;
		//}

		//void DiMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		//{
		//	EnableDPadMenu(false);
		//}

		//public void EnableDPadMenu(bool enable)
		//{
		//	foreach (ToolStripMenuItem item in DiMenuStrip.Items)
		//	{
		//		if (!item.Text.StartsWith(cRecord)
		//			&& !item.Text.StartsWith(cEmpty)
		//			&& !item.Text.StartsWith(cPOVs))
		//		{
		//			item.Visible = !enable;
		//		}
		//		if (item.Text.StartsWith(cPOVs))
		//		{
		//			if (item.HasDropDownItems)
		//			{
		//				foreach (ToolStripMenuItem l1 in item.DropDownItems)
		//				{
		//					foreach (ToolStripMenuItem l2 in l1.DropDownItems)
		//						l2.Visible = !enable;
		//				}
		//			}
		//		}
		//	}
		//}

		#endregion

		//MenuItem LastItem;

		//private TextBox CurrentTextBox;

		//private void MenuItem_Click(object sender, RoutedEventArgs e)
		//{
		//	var mi = (MenuItem)sender;
		//	var smi = (MenuItem)e.Source;
		//	if (mi != smi)
		//		return;

		//	LastItem?.Items.Clear();
		//	LastItem = mi;
		//	foreach (var item in DiMenuStrip)
		//		mi.Items.Add(item);

		//	var control = (Menu)mi.Parent;
		//	CurrentTextBox = (TextBox)control.Tag;

		//	ControlsHelper.BeginInvoke(() =>
		//	{
		//		mi.IsSubmenuOpen = true;
		//	});

		//}

		//void DiMenuStrip_Click(object sender, RoutedEventArgs e)
		//{
		//	var item = (MenuItem)sender;
		//	var fullValue = (string)item.Header;
		//	// If this DPad parent menu.
		//	if (fullValue == cRecord)
		//	{
		//		//var map = SettingsManager.Current.SettingsMap.First(x => x.Control == CurrentCbx);
		//		//StartRecording(map);
		//	}
		//	else
		//	{
		//		CurrentTextBox.Text = fullValue == cEmpty
		//			? ""
		//			: fullValue;
		//	}
		//}

		private void RecordClear_MouseEnterTextBox(object sender, MouseEventArgs e)
		{
			// If it was already hosted somewhere else, remove it first,
			if (RCStackPanel.Parent is StackPanel s1) { s1.Children.Remove(RCStackPanel); }

			// Act on TextBoxes inside a StackPanel.
			if (sender is TextBox t2 && t2.Parent is StackPanel s2)
			{
				if (s2.HorizontalAlignment == HorizontalAlignment.Left)
				{
					RCStackPanel.FlowDirection = FlowDirection.LeftToRight;
					ClearButton.FlowDirection = FlowDirection.LeftToRight;
					// Calculate the insertion index = just before the last element.
					int insertIndex = Math.Max(0, s2.Children.Count - 1);
					s2.Children.Insert(insertIndex, RCStackPanel);
				}
				else
				{
					RCStackPanel.FlowDirection = FlowDirection.RightToLeft;
					ClearButton.FlowDirection = FlowDirection.LeftToRight;
					// Calculate the insertion index = just before the last element.
					s2.Children.Insert(1, RCStackPanel);
				}

				RecordButton.Tag = ClearButton.Tag = t2;
				ClearButton.Visibility = (t2.Text.Length > 0) ? Visibility.Visible : Visibility.Collapsed;
				RCStackPanel.Visibility = Visibility.Visible;
			}
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.Tag is TextBox tb)
			{
				tb.Text = "";
			}
		}

		TextBox recordTextBox;

		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			if (recordTextBox == null)
			{
				// Get TextBox from sender Tag value and set it as recordTextBox. If recordTextBox is not null (recording state) it will be filled with first detected button or axis.
				recordTextBox = (sender as Button)?.Tag as TextBox;
				if (recordTextBox != null)
				{
					recordTextBox.BorderBrush = colorRecord;
					recordTextBox.Text = "";
				}
			}
			else
			{
				recordTextBox.BorderBrush = colorBackgroundDark;
				recordTextBox = null;
			}
		}

		private void RecordAxisOrButton(string axisOrButtonName)
		{
			recordTextBox.Text = axisOrButtonName;
			recordTextBox.BorderBrush = colorBackgroundDark;
			recordTextBox = null;
		}

		private void RecordClear_MouseEnter(object sender, MouseEventArgs e)
		{
			RCStackPanel.Visibility = Visibility.Visible;
		}

		private void RecordClear_MouseLeave(object sender, MouseEventArgs e)
		{
			RCStackPanel.Visibility = Visibility.Collapsed;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			SetBinding(MapTo.None, null, null);
			// DiMenuStrip.Clear();
		}

		private void InvertButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is TextBox textBox)
			{
				// Force a re-evaluation of the button’s data trigger by reassigning its Tag.
				// This tricks the DataTrigger that binds to Tag.Text into refreshing immediately.
				button.Tag = null;
				button.Tag = textBox;

				if (!string.IsNullOrEmpty(textBox.Text))
				{
					textBox.Text = textBox.Text.StartsWith("I")
						? textBox.Text.Substring(1)
						: "I" + textBox.Text;
				}
			}
		}
	}
}

