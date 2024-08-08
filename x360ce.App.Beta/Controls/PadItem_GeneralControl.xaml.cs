using JocysCom.ClassLibrary.Controls;
using SharpDX.DirectInput;
//using SharpDX.XInput;
using System;
// using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;

//using System.Data.Objects;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

// using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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

		// Create DragAndDrop menu labels.
		private void DragAndDropMenuLabels_Create(Dictionary<int, (Label, Label)> dictionary, List<int> list, string itemName, string headerName, string iconName)
		{
			// GroupBox Header (icon and text).
			StackPanel headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			headerStackPanel.Children.Add(new ContentControl { Content = Application.Current.Resources[iconName] });
			headerStackPanel.Children.Add(new TextBlock { Text = headerName, Margin = new Thickness(3, 0, 0, 0) });
			// GroupBox Content (UniformGrid for Labels).
			UniformGrid buttonsUniformGrid = new UniformGrid { Columns = 8 };
			// GroupBox.
			GroupBox buttonsGroupBox = new GroupBox { Header = headerStackPanel, Content = buttonsUniformGrid };

			// Put GroupBoxes into NORMAL and INVERTED tabs.
			if (iconName.Contains("Inverted")) { DragAndDropStackPanelInverted.Children.Add(buttonsGroupBox); }
			else { DragAndDropStackPanelNormal.Children.Add(buttonsGroupBox); }

			// Create drag and drop buttons.
			dictionary.Clear();
			foreach (var i in list)
			{
				Label buttonLabel = new Label();
				buttonLabel.Content = (i + 1).ToString();
				buttonLabel.Tag = itemName + buttonLabel.Content;
				buttonLabel.PreviewMouseMove += DragAndDropMenuLabel_Source_PreviewMouseMove;

				Label valueLabel = new Label();
				valueLabel.IsHitTestVisible = false;
				valueLabel.FontSize = 8;
				valueLabel.Padding = new Thickness(0);
				valueLabel.Background = colorLight;

				StackPanel stackPanel = new StackPanel();
				stackPanel.Children.Add(buttonLabel);
				stackPanel.Children.Add(valueLabel);
				buttonsUniformGrid.Children.Add(stackPanel);

				dictionary.Add(i, (buttonLabel, valueLabel));
			}
		}

		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice ud)
		{
			var customDiState = GetCustomDiState(ud);
			if (customDiState == null) return;

			if (!ud.IsKeyboard)
			{
				// Clear StackPanel children in XAML page.
				DragAndDropStackPanelNormal.Children.Clear();
				DragAndDropStackPanelInverted.Children.Clear();
				// Dictionary
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

				// Buttons.
				if (ud.CapButtonCount > 0)
				{
					var objects = ud.DeviceObjects.Where(x => x.Type.Equals(ObjectGuid.Button)).ToList().OrderBy(x => x.Instance);
					var list = new List<int>();
					foreach (var deviceObjectItem in objects) { list.Add(deviceObjectItem.Instance); }
					DragAndDropMenuLabels_Create(ButtonDictionary, list, "Button", "BUTTON", "Icon_DragAndDrop_Button");
					DragAndDropMenuLabels_Create(IButtonDictionary, list, "IButton", "BUTTON", "Icon_DragAndDrop_Button_Inverted");
				}
				// POVs.
				if (ud.CapPovCount > 0)
				{
					var povs = ud.DiState.POVs;
					var objects = ud.DeviceObjects.Where(x => x.Type.Equals(ObjectGuid.PovController)).ToList().OrderBy(x => x.Instance);
					var list = new List<int>();
					foreach (var deviceObjectItem in objects) { list.Add(deviceObjectItem.Instance); }
					DragAndDropMenuLabels_Create(PovDictionary, list, "POV", "POV", "Icon_DragAndDrop_POV");
					// POV Up, Right, Down, Left.
					var listB = new List<int>();
					for (int i = 0; i < PovDictionary.Count() * 4; i++) { listB.Add(i); }
					DragAndDropMenuLabels_Create(PovBDictionary, listB, "POVB", "POV · BUTTON", "Icon_DragAndDrop_POV");
				}
				// Axes.
				if (ud.DiState.Axis.Where(x => x > 0).Count() > 0)
				{
					var list = new List<int>();
					for (int i = 0; i < ud.DiState.Axis.Count(); i++) { if (ud.DiState.Axis[i] > 0) { list.Add(i); } }
					DragAndDropMenuLabels_Create(AxisDictionary, list, "Axis", "AXIS", "Icon_DragAndDrop_Axis");
					DragAndDropMenuLabels_Create(IAxisDictionary, list, "IAxis", "AXIS", "Icon_DragAndDrop_Axis_Inverted");
					DragAndDropMenuLabels_Create(HAxisDictionary, list, "HAxis", "AXIS · HALF", "Icon_DragAndDrop_Axis_Half");
					DragAndDropMenuLabels_Create(IHAxisDictionary, list, "IHAxis", "AXIS · HALF · INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
				}
				// Sliders.
				if (ud.DiState.Sliders.Where(x => x > 0).Count() > 0)
				{
					var list = new List<int>();
					for (int i = 0; i < ud.DiState.Sliders.Count(); i++) { if (ud.DiState.Sliders[i] > 0) { list.Add(i); } }
					DragAndDropMenuLabels_Create(SliderDictionary, list, "Slider", "SLIDER", "Icon_DragAndDrop_Axis");
					DragAndDropMenuLabels_Create(ISliderDictionary, list, "ISlider", "SLIDER", "Icon_DragAndDrop_Axis_Inverted");
					DragAndDropMenuLabels_Create(HSliderDictionary, list, "HSlider", "SLIDER · HALF", "Icon_DragAndDrop_Axis_Half");
					DragAndDropMenuLabels_Create(IHSliderDictionary, list, "IHSlider", "SLIDER · HALF · INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
				}
			}
		}

		private void SetDInputLabelContent(UserDevice ud, TargetType targetType, Label label)
		{
			Map map = _padSetting.Maps.FirstOrDefault(x => x.Target == targetType);
			if (map == null || map.Index <= 0) return;

			var customDiState = GetCustomDiState(ud);
			var i = map.Index - 1;

			if (map.Index <= ud.DiState.Axis.Length)
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
				IHAxisDictionary[i.Key].Item2.Content = Math.Max(0, Math.Min((Math.Abs(65535 - aDS) - 32767) * 2, 65535)); ;
				// Background.
				var active = aDS < 32767 - DragAndDropAxisDeadzone || aDS > 32767 + DragAndDropAxisDeadzone;
				AxisDictionary[i.Key].Item1.Background = HAxisDictionary[i.Key].Item1.Background = active ? colorActive : Brushes.Transparent;
				IAxisDictionary[i.Key].Item1.Background = IHAxisDictionary[i.Key].Item1.Background = active ? Brushes.Transparent : colorActive;
			}
			// Slider axes.
			foreach (var i in SliderDictionary)
			{
				var sDS = ud.DiState.Sliders[i.Key]; // customDiState.Sliders[i.Key];
				SliderDictionary[i.Key].Item2.Content = sDS;
				ISliderDictionary[i.Key].Item2.Content = sDS;
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
		
		PadItem_General_XboxImageControl padItem_General_XboxImageControl = new PadItem_General_XboxImageControl();
		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			if (recordTextBox == null)
			{
				recordTextBox = (sender as Button).Tag as TextBox;
				recordTextBox.BorderBrush = colorRecord;
				recordTextBox.Text = "";
				// padItem_General_XboxImageControl.setNormalOverActiveRecordColor(sender as Button, padItem_General_XboxImageControl.colorRecord);
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
