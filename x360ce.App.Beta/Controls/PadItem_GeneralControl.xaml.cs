using JocysCom.ClassLibrary.Controls;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

//using System.Xml.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
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

		public void SetBinding(MapTo mappedTo, PadSetting ps)
		{
			_MappedTo = mappedTo;
			if (_padSetting != null)
				_padSetting.PropertyChanged -= _padSetting_PropertyChanged;
			// Unbind controls.
			SettingsManager.UnLoadMonitor(TriggerLTextBox);
			SettingsManager.UnLoadMonitor(TriggerRTextBox);

			SettingsManager.UnLoadMonitor(BumperLTextBox);
			SettingsManager.UnLoadMonitor(BumperRTextBox);

			SettingsManager.UnLoadMonitor(MenuBackTextBox);
			SettingsManager.UnLoadMonitor(MenuStartTextBox);
			SettingsManager.UnLoadMonitor(MenuGuideTextBox);

			SettingsManager.UnLoadMonitor(ActionYTextBox);
			SettingsManager.UnLoadMonitor(ActionXTextBox);
			SettingsManager.UnLoadMonitor(ActionBTextBox);
			SettingsManager.UnLoadMonitor(ActionATextBox);

			SettingsManager.UnLoadMonitor(DPadTextBox);
			SettingsManager.UnLoadMonitor(DPadUpTextBox);
			SettingsManager.UnLoadMonitor(DPadLeftTextBox);
			SettingsManager.UnLoadMonitor(DPadRightTextBox);
			SettingsManager.UnLoadMonitor(DPadDownTextBox);

			SettingsManager.UnLoadMonitor(StickLButtonTextBox);
			SettingsManager.UnLoadMonitor(StickLAxisXTextBox);
			SettingsManager.UnLoadMonitor(StickLAxisYTextBox);
			SettingsManager.UnLoadMonitor(StickLUpTextBox);
			SettingsManager.UnLoadMonitor(StickLLeftTextBox);
			SettingsManager.UnLoadMonitor(StickLRightTextBox);
			SettingsManager.UnLoadMonitor(StickLDownTextBox);

			SettingsManager.UnLoadMonitor(StickRButtonTextBox);
			SettingsManager.UnLoadMonitor(StickRAxisXTextBox);
			SettingsManager.UnLoadMonitor(StickRAxisYTextBox);
			SettingsManager.UnLoadMonitor(StickRUpTextBox);
			SettingsManager.UnLoadMonitor(StickRLeftTextBox);
			SettingsManager.UnLoadMonitor(StickRRightTextBox);
			SettingsManager.UnLoadMonitor(StickRDownTextBox);
			if (ps == null)
				return;
			_padSetting = ps;
			var converter = new Converters.PaddSettingToText();
			// Bind controls.
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftTrigger), TriggerLTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightTrigger), TriggerRTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftShoulder), BumperLTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightShoulder), BumperRTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonBack), MenuBackTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonStart), MenuStartTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonGuide), MenuGuideTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonY), ActionYTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonX), ActionXTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonB), ActionBTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.ButtonA), ActionATextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.DPad), DPadTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.DPadUp), DPadUpTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.DPadLeft), DPadLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.DPadRight), DPadRightTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.DPadDown), DPadDownTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbButton), StickLButtonTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbAxisX), StickLAxisXTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbAxisY), StickLAxisYTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbUp), StickLUpTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbLeft), StickLLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbRight), StickLRightTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbDown), StickLDownTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbButton), StickRButtonTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbAxisX), StickRAxisXTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbAxisY), StickRAxisYTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbUp), StickRUpTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbLeft), StickRLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbRight), StickRRightTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbDown), StickRDownTextBox, null, converter);

			_padSetting.PropertyChanged += _padSetting_PropertyChanged;
		}

		private void _padSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) { }
		private void SetPresetButton_Click(object sender, RoutedEventArgs e) { }
		private void RemapAllButton_Click(object sender, RoutedEventArgs e) { }

		public void MapNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox)sender;
			var item = (Layout)box.SelectedItem;
			if (item == null)
				return;
			// Triggers.
			TriggerLLabel.Content = item.LeftTrigger;
			TriggerRLabel.Content = item.RightTrigger;
			// Bumpers.
			BumperLLabel.Content = item.LeftShoulder;
			BumperRLabel.Content = item.RightShoulder;
			// Menu.
			MenuBackLabel.Content = item.ButtonBack;
			MenuGuideLabel.Content = item.ButtonGuide;
			MenuStartLabel.Content = item.ButtonStart;
			// Action.
			ActionALabel.Content = item.ButtonA;
			ActionBLabel.Content = item.ButtonB;
			ActionXLabel.Content = item.ButtonX;
			ActionYLabel.Content = item.ButtonY;
			// D-Pad.
			DPadLabel.Content = item.DPad;
			DPadDownLabel.Content = item.DPadDown;
			DPadLeftLabel.Content = item.DPadLeft;
			DPadRightLabel.Content = item.DPadRight;
			DPadUpLabel.Content = item.DPadUp;
			// Stick Left.
			StickLButtonLabel.Content = item.LeftThumbButton;
			StickLAxisXLabel.Content = item.LeftThumbAxisX;
			StickLAxisYLabel.Content = item.LeftThumbAxisY;
			StickLDownLabel.Content = item.LeftThumbDown;
			StickLLeftLabel.Content = item.LeftThumbLeft;
			StickLRightLabel.Content = item.LeftThumbRight;
			StickLUpLabel.Content = item.LeftThumbUp;
			// Stick Right.
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
			Label label = sender as Label;
			if (label != null && e.LeftButton == MouseButtonState.Pressed)
			{
				DragDrop.DoDragDrop(label, label.Tag.ToString(), DragDropEffects.Copy);
			}
		}

		// Drag and Drop Menu Drop event.
		private void DragAndDropMenu_Target_Drop(object sender, DragEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			if (e.Data.GetDataPresent(DataFormats.Text))
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
		//SolidColorBrush colorNormalLabel = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFDEDEDE");
		//SolidColorBrush colorOver = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFCC66");
		SolidColorBrush colorRecord = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFF6B66");

		Dictionary<int, (Label, Label)> ButtonDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> IButtonDictionary = new Dictionary<int, (Label, Label)>();

		Dictionary<int, (Label, Label)> PovDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> PovBDictionary = new Dictionary<int, (Label, Label)>();

		Dictionary<int, (Label, Label)> AxisDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> IAxisDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> HAxisDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> IHAxisDictionary = new Dictionary<int, (Label, Label)>();

		Dictionary<int, (Label, Label)> SliderDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> ISliderDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> HSliderDictionary = new Dictionary<int, (Label, Label)>();
		Dictionary<int, (Label, Label)> IHSliderDictionary = new Dictionary<int, (Label, Label)>();

		object updateLock = new object();
		object oldState = null;

		CustomDiState GetCustomDiState(UserDevice ud)
		{
			CustomDiState customDiState = null;
			var state = ud?.DeviceState;
			if (state == null || state == oldState)
				return null;
			lock (updateLock)
			{
				if (state is MouseState mState)
					customDiState = new CustomDiState(mState);
				if (state is KeyboardState kState)
					customDiState = new CustomDiState(kState);
				if (state is JoystickState jState)
					customDiState = new CustomDiState(jState);
			}
			return customDiState == null ? null : customDiState;
		}

		UniformGrid PovUnifromGrid;

		// Create DragAndDrop menu labels.
		private void DragAndDropMenuLabels_Create(Dictionary<int, (Label, Label)> dictionary, List<int> list, string itemName, string headerName, string iconName)
		{
			try
			{
				// GroupBox Header (icon and text).
				StackPanel headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
				headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName] });
				headerStackPanel.Children.Add(new TextBlock { Text = headerName, Margin = new Thickness(3, 0, 0, 0) });
				// GroupBox Content (UniformGrid for Labels).
				UniformGrid buttonsUniformGrid = new UniformGrid { Columns = list.Last().ToString().Length > 2 ? 6 : 8 };
				// GroupBox.
				GroupBox buttonsGroupBox = new GroupBox { Header = headerStackPanel, Content = buttonsUniformGrid };
				// Put GroupBoxes into NORMAL and INVERTED tabs.
				if (iconName.Contains("Inverted")) { DragAndDropStackPanelInverted.Children.Add(buttonsGroupBox); }
				else { DragAndDropStackPanelNormal.Children.Add(buttonsGroupBox); }

				// Put POVB buttons inside POV GroupBox.
				if (itemName == "POV") { PovUnifromGrid = buttonsUniformGrid; }
				if (itemName == "POVB") { buttonsGroupBox.Visibility = Visibility.Collapsed; }

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
						buttonLabel.Tag = "POV " + povNumber + " " + povNumberB;
					}
					else
					{
						buttonLabel.Content = (i + 1).ToString();
						buttonLabel.Tag = itemName + " " + buttonLabel.Content;
					}

					buttonLabel.PreviewMouseMove += DragAndDropMenuLabel_Source_PreviewMouseMove;

					Label valueLabel = new Label();
					valueLabel.IsHitTestVisible = false;
					valueLabel.FontSize = 8;
					valueLabel.Padding = new Thickness(0);
					valueLabel.Background = colorLight;

					StackPanel stackPanel = new StackPanel();
					stackPanel.Children.Add(buttonLabel);
					stackPanel.Children.Add(valueLabel);

					// Put POVB buttons inside POV GroupBox.
					if (itemName == "POVB") { PovUnifromGrid.Children.Add(stackPanel); }
					else { buttonsUniformGrid.Children.Add(stackPanel); }

					dictionary.Add(i, (buttonLabel, valueLabel));
				}
			}
			catch (Exception ex)
			{
				_ = ex.Message;
			}
		}

		List<int> buttons = new List<int>();
		List<int> povs = new List<int>();
		List<int> axes = new List<int>();
		List<int> sliders = new List<int>();

		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice ud)
		{

			if (GetCustomDiState(ud) == null) return;

			// Clear StackPanel children in XAML page.
			DragAndDropStackPanelNormal.Children.Clear();
			DragAndDropStackPanelInverted.Children.Clear();
			// Clear dictionaries.
			ButtonDictionary.Clear();
			IButtonDictionary.Clear();
			PovDictionary.Clear();
			PovBDictionary.Clear();
			AxisDictionary.Clear();
			HAxisDictionary.Clear();
			IAxisDictionary.Clear();
			IHAxisDictionary.Clear();
			SliderDictionary.Clear();
			HSliderDictionary.Clear();
			ISliderDictionary.Clear();
			IHSliderDictionary.Clear();

			// Lists with InstanceNumber's.
			buttons.Clear();
			povs.Clear();
			axes.Clear();
			sliders.Clear();

			GetDeviceObjectInstancesByObjectTypeGuid(ud);

			buttons.Sort();
			povs.Sort();
			axes.Sort();
			sliders.Sort();

			// Buttons and Keys.
			if (buttons.Count() > 0)
			{
				DragAndDropMenuLabels_Create(ButtonDictionary, buttons, "Button", "BUTTON", "Icon_DragAndDrop_Button");
				DragAndDropMenuLabels_Create(IButtonDictionary, buttons, "IButton", "BUTTON", "Icon_DragAndDrop_Button_Inverted");
			}
			// POVs.
			if (povs.Count() > 0)
			{
				DragAndDropMenuLabels_Create(PovDictionary, povs, "POV", "POV", "Icon_DragAndDrop_POV");
				var povButtons = new List<int>();
				for (int i = 0; i < povs.Count() * 4; i++) { povButtons.Add(i); }
				DragAndDropMenuLabels_Create(PovBDictionary, povButtons, "POVB", "POV · BUTTON", "Icon_DragAndDrop_POV");
			}
			// Axes.
			if (axes.Count() > 0)
			{
				DragAndDropMenuLabels_Create(AxisDictionary, axes, "Axis", "AXIS", "Icon_DragAndDrop_Axis");
				DragAndDropMenuLabels_Create(IAxisDictionary, axes, "IAxis", "AXIS", "Icon_DragAndDrop_Axis_Inverted");
				DragAndDropMenuLabels_Create(HAxisDictionary, axes, "HAxis", "AXIS · HALF", "Icon_DragAndDrop_Axis_Half");
				DragAndDropMenuLabels_Create(IHAxisDictionary, axes, "IHAxis", "AXIS · HALF · INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
			}
			// Sliders.
			if (sliders.Count() > 0)
			{
				DragAndDropMenuLabels_Create(SliderDictionary, sliders, "Slider", "SLIDER", "Icon_DragAndDrop_Axis");
				DragAndDropMenuLabels_Create(ISliderDictionary, sliders, "ISlider", "SLIDER", "Icon_DragAndDrop_Axis_Inverted");
				DragAndDropMenuLabels_Create(HSliderDictionary, sliders, "HSlider", "SLIDER · HALF", "Icon_DragAndDrop_Axis_Half");
				DragAndDropMenuLabels_Create(IHSliderDictionary, sliders, "IHSlider", "SLIDER · HALF · INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
			}
		}

		private void GetDeviceObjectInstancesByObjectTypeGuid(UserDevice ud, int usage = 0)
		{
			var device = ud.Device as Joystick;

			if (device == null)
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
			// var povsCount = deviceObjects.Where(x => x.ObjectType == ObjectGuid.PovController).Count();
			for (int i = 0; i < ud.CapPovCount; i++) { povs.Add(i); }

			// Buttons, Keys. 
			// var buttonCount = deviceObjects.Where(x => x.ObjectType == ObjectGuid.Button || x.ObjectType == ObjectGuid.Key).Count();
			for (int i = 0; i < ud.CapButtonCount; i++) { buttons.Add(i); }

			// Axes.
			foreach (DeviceObjectInstance item in deviceObjects.Where(x => x.ObjectType != ObjectGuid.Unknown).OrderBy(x => x.UsagePage).ThenBy(x => x.Usage).ThenBy(x => x.ObjectId.InstanceNumber))
			{
				// if (item.ObjectType == ObjectGuid.PovController) { povs.Add(item.ObjectId.InstanceNumber); }
				// else if (item.ObjectType == ObjectGuid.Key) { buttons.Add(item.ObjectId.InstanceNumber); }
				// else if (item.ObjectType == ObjectGuid.Button) { buttons.Add(item.Offset); }
				// Axes.
				if (new[] { ObjectGuid.XAxis, ObjectGuid.YAxis, ObjectGuid.ZAxis, ObjectGuid.RxAxis, ObjectGuid.RyAxis, ObjectGuid.RzAxis }.Contains(item.ObjectType))
				{
					if (ud.IsMouse) { axes.Add(item.ObjectId.InstanceNumber); }
					else { if (CustomDiHelper.AxisUsageDictionary.TryGetValue(item.Usage, out var value)) axes.Add(value.Item2); }
				}

				stringBuilder.Append($"INFO: " +
						$"UsagePage {item.UsagePage}. " +
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
				if (field.FieldType == typeof(Guid) && (Guid)field.GetValue(null) == guid) return field.Name;
			}
			return "Unknown";
		}

		private void SetDInputLabelContent(UserDevice ud, TargetType targetType, Label label)
		{
			Map map = _padSetting.Maps.FirstOrDefault(x => x.Target == targetType);
			if (map == null || map.Index <= 0) return;

			var customDiState = GetCustomDiState(ud);
			var i = map.Index - 1;

			if (map.Index <= ud.DiState.Axis.Length/* || map.Index <= ud.DiState.Sliders.Length*/)
			{
				if (map.IsAxis || map.IsHalf || map.IsInverted) label.Content = customDiState.Axis[i];
				else if (map.IsButton) label.Content = customDiState.Buttons[i] ? 1 : 0;
				else if (map.IsSlider) label.Content = customDiState.Sliders[i];
			}
		}

		// Update DragAndDrop menu labels.
		public void DragAndDropMenuLabels_Update(UserDevice ud)
		{
			var customDiState = GetCustomDiState(ud);
			if (customDiState == null) return;
			// Deadzone for Drag and Drop Background color change.
			var DragAndDropAxisDeadzone = 8000;
			var DragAndDropSliderDeadzone = 16000;

			// Trigger.
			SetDInputLabelContent(ud, TargetType.LeftTrigger, TriggerLDInputLabel);
			SetDInputLabelContent(ud, TargetType.RightTrigger, TriggerRDInputLabel);
			TriggerLDeadzoneLabel.Content = _padSetting.LeftTriggerDeadZone;
			TriggerRDeadzoneLabel.Content = _padSetting.RightTriggerDeadZone;
			// Stick Left.
			SetDInputLabelContent(ud, TargetType.LeftThumbX, StickLAxisXDInputLabel);
			SetDInputLabelContent(ud, TargetType.LeftThumbY, StickLAxisYDInputLabel);
			StickLDeadzoneXLabel.Content = _padSetting.LeftThumbDeadZoneX;
			StickLDeadzoneYLabel.Content = _padSetting.LeftThumbDeadZoneY;
			// Stick Right.
			SetDInputLabelContent(ud, TargetType.RightThumbX, StickRAxisXDInputLabel);
			SetDInputLabelContent(ud, TargetType.RightThumbY, StickRAxisYDInputLabel);
			StickRDeadzoneXLabel.Content = _padSetting.RightThumbDeadZoneX;
			StickRDeadzoneYLabel.Content = _padSetting.RightThumbDeadZoneY;

			// Buttons.
			foreach (var i in ButtonDictionary)
			{
				var bDS = ud.DiState.Buttons[i.Key]; // customDiState.Buttons[i.Key];
				IButtonDictionary[i.Key].Item1.Background = bDS ? Brushes.Transparent : colorActive;
				ButtonDictionary[i.Key].Item1.Background = bDS ? colorActive : Brushes.Transparent;
				ButtonDictionary[i.Key].Item2.Content = bDS.ToString();
				IButtonDictionary[i.Key].Item2.Content = bDS.ToString() == "True" ? "False" : "True";

				// Record button.
				if (recordTextBox != null && bDS)
				{
					recordTextBox.Text = ButtonDictionary[i.Key].Item1.Tag.ToString();
					recordTextBox.BorderBrush = colorBackgroundDark;
					recordTextBox = null;
				}
			}
			// POVs.
			var povButtonValues = new[] { 0, 9000, 18000, 27000, 0, 9000, 18000, 27000, 0, 9000, 18000, 27000, 0, 9000, 18000, 27000 };
			foreach (var i in PovDictionary)
			{
				var pDS = ud.DiState.POVs[i.Key]; // customDiState.POVs[i.Key];
				PovDictionary[i.Key].Item1.Background = pDS > -1 ? colorActive : Brushes.Transparent;
				PovDictionary[i.Key].Item2.Content = pDS;
				// Up, Right, Down, Left.
				for (int b = 0; b < PovDictionary.Count() * 4 && b < povButtonValues.Length; b++)
				{
					PovBDictionary[b].Item1.Background = pDS == povButtonValues[b] ? colorActive : Brushes.Transparent;
					PovBDictionary[b].Item2.Content = pDS == povButtonValues[b] ? pDS : -1;
				}
			}
			// Stick axes.
			foreach (var i in AxisDictionary)
			{
				var aDS = ud.DiState.Axis[i.Key]; //customDiState.Axis[i.Key];
				AxisDictionary[i.Key].Item2.Content = aDS;
				HAxisDictionary[i.Key].Item2.Content = Math.Max(0, Math.Min((aDS - 32767) * 2, 65535));
				IAxisDictionary[i.Key].Item2.Content = Math.Abs(65535 - aDS);
				IHAxisDictionary[i.Key].Item2.Content = Math.Max(0, Math.Min((Math.Abs(65535 - aDS) - 32767) * 2, 65535));
				// Background.
				var active = aDS < 32767 - DragAndDropAxisDeadzone || aDS > 32767 + DragAndDropAxisDeadzone;
				AxisDictionary[i.Key].Item1.Background = HAxisDictionary[i.Key].Item1.Background = active ? colorActive : Brushes.Transparent;
				IAxisDictionary[i.Key].Item1.Background = IHAxisDictionary[i.Key].Item1.Background = active ? Brushes.Transparent : colorActive;
			}

			// Slider axes.
			foreach (var i in SliderDictionary)
			{
				var sDS = ud.DiState.Sliders[i.Key];
				SliderDictionary[i.Key].Item2.Content = sDS;
				HSliderDictionary[i.Key].Item2.Content = Math.Max(0, Math.Min((sDS - 32767) * 2, 65535));
				ISliderDictionary[i.Key].Item2.Content = Math.Abs(65535 - sDS);
				IHSliderDictionary[i.Key].Item2.Content = Math.Max(0, Math.Min((Math.Abs(65535 - sDS) - 32767) * 2, 65535));
				// Background.
				SliderDictionary[i.Key].Item1.Background = HSliderDictionary[i.Key].Item1.Background = sDS > DragAndDropSliderDeadzone ? colorActive : Brushes.Transparent;
				ISliderDictionary[i.Key].Item1.Background = IHSliderDictionary[i.Key].Item1.Background = sDS > DragAndDropSliderDeadzone ? Brushes.Transparent : colorActive;
			}
		}
		#endregion

		#region ■ Direct Input Menu

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
			var g1 = RecordClearGrid.Parent as Grid;
			g1.Children.Remove(RecordClearGrid);

			var t2 = sender as TextBox;
			var s2 = t2.Parent as StackPanel;
			var g2 = s2.Parent as Grid;

			if (g2.HorizontalAlignment == HorizontalAlignment.Left)
			{
				Grid.SetColumn(RCStackPanel, 1);
				RecordClearColumn0.Width = new GridLength(77);
				RecordClearColumn1.Width = new GridLength(1, GridUnitType.Star);
				RCStackPanel.FlowDirection = FlowDirection.LeftToRight;
			}
			else
			{
				Grid.SetColumn(RCStackPanel, 0);
				RecordClearColumn0.Width = new GridLength(1, GridUnitType.Star);
				RecordClearColumn1.Width = new GridLength(77);
				RCStackPanel.FlowDirection = FlowDirection.RightToLeft;
			}

			RCStackPanel.HorizontalAlignment = g2.HorizontalAlignment;
			RecordButton.Tag = t2;
			ClearButton.Tag = t2;
			g2.Children.Add(RecordClearGrid);
			RecordClearGrid.Visibility = Visibility.Visible;
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			((sender as Button).Tag as TextBox).Text = "";
		}

		TextBox recordTextBox;

		//PadItem_General_XboxImageControl padItem_General_XboxImageControl = new PadItem_General_XboxImageControl();
		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			if (recordTextBox == null)
			{
				recordTextBox = (sender as Button).Tag as TextBox;
				recordTextBox.BorderBrush = colorRecord;
				recordTextBox.Text = "";
				//padItem_General_XboxImageControl.setNormalOverActiveRecordColor(sender as Button, padItem_General_XboxImageControl.colorRecord);
			}
			else
			{
				recordTextBox.BorderBrush = colorBackgroundDark;
				recordTextBox = null;
			}
		}

		private void RecordClear_MouseEnter(object sender, MouseEventArgs e)
		{
			RecordClearGrid.Visibility = Visibility.Visible;
		}

		private void RecordClear_MouseLeave(object sender, MouseEventArgs e)
		{
			RecordClearGrid.Visibility = Visibility.Collapsed;
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
			SetBinding(MapTo.None, null);
			// DiMenuStrip.Clear();
		}
	}
}
