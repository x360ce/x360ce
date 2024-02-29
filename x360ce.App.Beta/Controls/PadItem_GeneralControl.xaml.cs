using JocysCom.ClassLibrary.Controls;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

			TriggerLDeadzoneLabel.Content = ps.LeftTriggerDeadZone;
			TriggerRDeadzoneLabel.Content = ps.RightTriggerDeadZone;

			StickLDeadzoneXLabel.Content = ps.LeftThumbDeadZoneX;
			StickLDeadzoneYLabel.Content = ps.LeftThumbDeadZoneY;

			StickRDeadzoneXLabel.Content = ps.RightThumbDeadZoneX;
			StickRDeadzoneYLabel.Content = ps.RightThumbDeadZoneY;

			_padSetting.PropertyChanged += _padSetting_PropertyChanged;
		}

		private void _padSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) { }
		private void SetPresetButton_Click(object sender, System.Windows.RoutedEventArgs e) { }
		private void RemapAllButton_Click(object sender, System.Windows.RoutedEventArgs e) { }

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
		//SolidColorBrush colorNormalPath = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6699FF");
		//SolidColorBrush colorNormalTextBox = Brushes.White;
		//SolidColorBrush colorBlack = (SolidColorBrush)new BrushConverter().ConvertFrom("#11000000");
		//SolidColorBrush colorNormalLabel = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFDEDEDE");
		//SolidColorBrush colorOver = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFCC66");
		//SolidColorBrush colorRecord = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFF6B66");

		// Lists.
		List<bool> ButtonStateList = new List<bool>();
		List<bool> POVStateList = new List<bool>();
		List<bool> POVBStateList = new List<bool>();
		List<bool> StickAxisStateList = new List<bool>();
		List<bool> SliderAxisStateList = new List<bool>();

		List<Label> ButtonList = new List<Label>();
		List<Label> IButtonList = new List<Label>();

		List<Label> AxisList = new List<Label>();
		List<Label> IAxisList = new List<Label>();
		List<Label> HAxisList = new List<Label>();
		List<Label> IHAxisList = new List<Label>();

		List<Label> SliderList = new List<Label>();
		List<Label> ISliderList = new List<Label>();
		List<Label> HSliderList = new List<Label>();
		List<Label> IHSliderList = new List<Label>();

		List<Label> POVList = new List<Label>();
		List<Label> IPOVList = new List<Label>();
		List<Label> POVButtonList = new List<Label>();
		List<Label> IPOVButtonList = new List<Label>();

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
		private void DragAndDropMenuLabels_Create(List<bool> total, List<Label> list, string itemName, string headerName, string iconName)
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

			// Create labels.
			for (int i = 0; i < total.Count(); i++)
			{
				string number = (i + 1).ToString();
				Label buttonLabel = new Label();
				buttonLabel.Name = itemName + number + "Label";
				buttonLabel.Content = number;
				buttonLabel.ToolTip = itemName + " " + number;
				buttonLabel.Tag = itemName + " " + number;
				buttonLabel.Visibility = total[i] ? Visibility.Visible : Visibility.Collapsed;
				buttonLabel.PreviewMouseMove += DragAndDropMenuLabel_Source_PreviewMouseMove;
				// Add label to group UniformGrid.
				buttonsUniformGrid.Children.Add(buttonLabel);
				// Add label to group list.
				list.Add(buttonLabel);
			}
		}

		int buttons = 0;
		int povs = 0;
		int stickAxes = 0;
		int sliderAxes = 0;

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
				// Clear Label lists.
				ButtonStateList.Clear();
				POVStateList.Clear();
				POVBStateList.Clear();
				StickAxisStateList.Clear();
				SliderAxisStateList.Clear();
				// Buttons.
				ButtonList.Clear();
				IButtonList.Clear();
				// POVs.
				POVList.Clear();
				POVButtonList.Clear();
				IPOVList.Clear();
				IPOVButtonList.Clear();
				// Axes.
				AxisList.Clear();
				IAxisList.Clear();
				HAxisList.Clear();
				IHAxisList.Clear();
				// Sliders.
				SliderList.Clear();
				HSliderList.Clear();
				ISliderList.Clear();
				IHSliderList.Clear();

				// Create button, POV, stick axis, slider axis bool lists (true = exists).
				for (int i = 0; i < ud.CapButtonCount; i++) { ButtonStateList.Add(true); }
				for (int i = 0; i < ud.CapPovCount; i++) { POVStateList.Add(true); }
				for (int i = 0; i < ud.CapPovCount * 4; i++) { POVBStateList.Add(true); }
				// Create bool list, based on axis default position: (~0, ~65535) or (~32767).
				for (int i = 0; i < customDiState.Axis.Count(); i++) { StickAxisStateList.Add(customDiState.Axis[i] > (65535 / 4) && customDiState.Axis[i] < 65535 - (65535 / 4)); }
				for (int i = 0; i < customDiState.Sliders.Count(); i++) { SliderAxisStateList.Add(customDiState.Sliders[i] < (65535 / 4) && customDiState.Sliders[i] > 65535 - (65535 / 4)); }

				buttons = ButtonStateList.Count(b => b);
				povs = POVStateList.Count(b => b);
				stickAxes = StickAxisStateList.Count(b => b);
				// Number of sliders: all controller axes - axes with default-initial position ~32767.
				sliderAxes = ud.CapAxeCount - StickAxisStateList.Count(b => b);
				// var slidersCount = ud.DeviceObjects?.Count(x => x.Type.Equals(ObjectGuid.Slider)) ?? 0;

				if (buttons > 0)
				{
					DragAndDropMenuLabels_Create(ButtonStateList, ButtonList, "Button", "BUTTON", "Icon_DragAndDrop_Button");
					DragAndDropMenuLabels_Create(ButtonStateList, IButtonList, "IButton", "BUTTON", "Icon_DragAndDrop_Button_Inverted");
				}
				if (povs > 0)
				{
					DragAndDropMenuLabels_Create(POVStateList, POVList, "POV", "POV", "Icon_DragAndDrop_POV");
					DragAndDropMenuLabels_Create(POVBStateList, POVButtonList, "POVB", "POV · BUTTON", "Icon_DragAndDrop_POV");
				}
				if (stickAxes > 0)
				{
					DragAndDropMenuLabels_Create(StickAxisStateList, AxisList, "Axis", "AXIS", "Icon_DragAndDrop_Axis");
					DragAndDropMenuLabels_Create(StickAxisStateList, IAxisList, "IAxis", "AXIS", "Icon_DragAndDrop_Axis_Inverted");
					DragAndDropMenuLabels_Create(StickAxisStateList, HAxisList, "HAxis", "AXIS · HALF", "Icon_DragAndDrop_Axis_Half");
					DragAndDropMenuLabels_Create(StickAxisStateList, IHAxisList, "IHAxis", "AXIS · HALF INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
				}
				if (sliderAxes > 0)
				{
					DragAndDropMenuLabels_Create(SliderAxisStateList, SliderList, "Slider", "SLIDER", "Icon_DragAndDrop_Axis");
					DragAndDropMenuLabels_Create(SliderAxisStateList, ISliderList, "ISlider", "SLIDER", "Icon_DragAndDrop_Axis_Inverted");
					DragAndDropMenuLabels_Create(SliderAxisStateList, HSliderList, "HSlider", "SLIDER · HALF", "Icon_DragAndDrop_Axis_Half");
					DragAndDropMenuLabels_Create(SliderAxisStateList, IHSliderList, "IHSlider", "SLIDER · HALF INVERTED", "Icon_DragAndDrop_Axis_Half_Inverted");
				}
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

			// Buttons.
			if (buttons > 0)
			{
				for (int i = 0; i < buttons; i++)
				{
					var bDS = customDiState.Buttons[i];
					ButtonList[i].ToolTip =
					IButtonList[i].ToolTip = bDS.ToString();
					ButtonList[i].Background = bDS ? colorActive : Brushes.Transparent;
					IButtonList[i].Background = bDS ? Brushes.Transparent : colorActive;
				}
			}
			// POVs.
			if (povs > 0)
			{
				var povButtonValues = new[] { 0, 9000, 18000, 27000, 0, 9000, 18000, 27000 };
				for (int i = 0; i < povs; i++)
				{
					var pDS = customDiState.POVs[i];
					POVList[i].Background = pDS > -1 ? colorActive : Brushes.Transparent;
					POVList[i].ToolTip = pDS;
					// Up, Right, Down, Left.
					for (int b = 0; b < povs * 4 && b < povButtonValues.Length; b++)
					{
						POVButtonList[b].Background = pDS == povButtonValues[b] ? colorActive : Brushes.Transparent;
						POVButtonList[b].ToolTip = pDS == povButtonValues[b] ? pDS : -1;
					}
				}
			}
			// Stick axes.
			if (stickAxes > 0)
			{
				for (int i = 0; i < customDiState.Axis.Count(); i++)
				{
					var aDS = customDiState.Axis[i];
					// Background.
					AxisList[i].Background =
					HAxisList[i].Background = aDS < 32767 - DragAndDropAxisDeadzone || aDS > 32767 + DragAndDropAxisDeadzone ? colorActive : Brushes.Transparent;
					IAxisList[i].Background =
					IHAxisList[i].Background = aDS < 32767 - DragAndDropAxisDeadzone || aDS > 32767 + DragAndDropAxisDeadzone ? Brushes.Transparent : colorActive;
					// Tooltip.
					AxisList[i].ToolTip =
					IAxisList[i].ToolTip =
					HAxisList[i].ToolTip =
					IHAxisList[i].ToolTip = aDS;
					// Triggers.
					if (i == 2) { TriggerLDInputLabel.Content = TriggerRDInputLabel.Content = aDS; }
					// Stick Left.
					else if (i == 0) { StickLAxisXDinputLabel.Content = aDS; }
					else if (i == 1) { StickLAxisYDInputLabel.Content = aDS; }
					// Stick Right.
					else if (i == 3) { StickRAxisXDInputLabel.Content = aDS; }
					else if (i == 4) { StickRAxisYDInputLabel.Content = aDS; }
				}
			}
			// Slider axes.
			if (sliderAxes > 0)
			{
				for (int i = 0; i < customDiState.Sliders.Count(); i++)
				{
					var sDS = customDiState.Sliders[i];
					if (sDS > 0)
					{
						// Visibility.
						SliderList[i].Visibility = 
						HSliderList[i].Visibility = 
						ISliderList[i].Visibility = 
						IHSliderList[i].Visibility = Visibility.Visible;
						// Background.
						SliderList[i].Background =
						HSliderList[i].Background = sDS > DragAndDropSliderDeadzone ? colorActive : Brushes.Transparent;
						ISliderList[i].Background =
						IHSliderList[i].Background = sDS > DragAndDropSliderDeadzone ? Brushes.Transparent : colorActive;
						// Tooltip.
						SliderList[i].ToolTip =
						ISliderList[i].ToolTip =
						HSliderList[i].ToolTip =
						IHSliderList[i].ToolTip = sDS;
					}
				}
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

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			SetBinding(MapTo.None, null);
			// DiMenuStrip.Clear();
		}

		private void RecordClear_MouseEnterTextBox(object sender, MouseEventArgs e)
		{
			var g1 = RecordClearGrid.Parent as Grid;
			g1.Children.Remove(RecordClearGrid);

			var t2 = sender as TextBox;
			var s2 = t2.Parent as StackPanel;
			var g2 = s2.Parent as Grid;

			if (g2.HorizontalAlignment == HorizontalAlignment.Left)
			{
				Grid.SetColumn(RCBorder, 1);
				Grid.SetColumn(RCStackPanel, 1);
				RecordClearColumn0.Width = new GridLength(77);
				RecordClearColumn1.Width = new GridLength(1, GridUnitType.Star);
				RCStackPanel.FlowDirection = FlowDirection.LeftToRight;
			}
			else
			{
				Grid.SetColumn(RCBorder, 0);
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

		PadItem_General_XboxImageControl padItem_General_XboxImageControl = new PadItem_General_XboxImageControl();
		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			//var textBox = (sender as Button).Tag as TextBox;
			padItem_General_XboxImageControl.setNormalOverActiveRecordColor(sender as Button, padItem_General_XboxImageControl.colorRecord);
		}

		private void RecordClear_MouseEnter(object sender, MouseEventArgs e)
		{
			RecordClearGrid.Visibility = Visibility.Visible;
		}

		private void RecordClear_MouseLeave(object sender, MouseEventArgs e)
		{
			RecordClearGrid.Visibility = Visibility.Collapsed;
		}
	}
}
