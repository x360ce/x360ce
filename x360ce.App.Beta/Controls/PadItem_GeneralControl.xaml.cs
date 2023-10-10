using JocysCom.ClassLibrary.Controls;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
			SettingsManager.UnLoadMonitor(TriggerLeftTextBox);
			SettingsManager.UnLoadMonitor(TriggerRightTextBox);

			SettingsManager.UnLoadMonitor(BumperLeftTextBox);
			SettingsManager.UnLoadMonitor(BumperRightTextBox);

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

			SettingsManager.UnLoadMonitor(StickLeftButtonTextBox);
			SettingsManager.UnLoadMonitor(StickLeftXTextBox);
			SettingsManager.UnLoadMonitor(StickLeftYTextBox);
			SettingsManager.UnLoadMonitor(StickLeftUpTextBox);
			SettingsManager.UnLoadMonitor(StickLeftLeftTextBox);
			SettingsManager.UnLoadMonitor(StickLeftRightTextBox);
			SettingsManager.UnLoadMonitor(StickLeftDownTextBox);

			SettingsManager.UnLoadMonitor(StickRightButtonTextBox);
			SettingsManager.UnLoadMonitor(StickRightXTextBox);
			SettingsManager.UnLoadMonitor(StickRightYTextBox);
			SettingsManager.UnLoadMonitor(StickRightUpTextBox);
			SettingsManager.UnLoadMonitor(StickRightLeftTextBox);
			SettingsManager.UnLoadMonitor(StickRightRightTextBox);
			SettingsManager.UnLoadMonitor(StickRightDownTextBox);
			if (ps == null)
				return;
			_padSetting = ps;
			var converter = new Converters.PaddSettingToText();
			// Bind controls.
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftTrigger), TriggerLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightTrigger), TriggerRightTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftShoulder), BumperLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightShoulder), BumperRightTextBox, null, converter);

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

			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbButton), StickLeftButtonTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbAxisX), StickLeftXTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbAxisY), StickLeftYTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbUp), StickLeftUpTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbLeft), StickLeftLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbRight), StickLeftRightTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.LeftThumbDown), StickLeftDownTextBox, null, converter);

			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbButton), StickRightButtonTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbAxisX), StickRightXTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbAxisY), StickRightYTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbUp), StickRightUpTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbLeft), StickRightLeftTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbRight), StickRightRightTextBox, null, converter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.RightThumbDown), StickRightDownTextBox, null, converter);
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
			TriggerLeftLabel.Content = item.LeftTrigger;
			TriggerRightLabel.Content = item.RightTrigger;
			// Bumpers.
			BumperLeftLabel.Content = item.LeftShoulder;
			BumperRightLabel.Content = item.RightShoulder;
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
			StickLeftButtonLabel.Content = item.LeftThumbButton;
			StickLeftXLabel.Content = item.LeftThumbAxisX;
			StickLeftYLabel.Content = item.LeftThumbAxisY;
			StickLeftDownLabel.Content = item.LeftThumbDown;
			StickLeftLeftLabel.Content = item.LeftThumbLeft;
			StickLeftRightLabel.Content = item.LeftThumbRight;
			StickLeftUpLabel.Content = item.LeftThumbUp;
			// Stick Right.
			StickRightButtonLabel.Content = item.RightThumbButton;
			StickRightXLabel.Content = item.RightThumbAxisX;
			StickRightYLabel.Content = item.RightThumbAxisY;
			StickRightDownLabel.Content = item.RightThumbDown;
			StickRightLeftLabel.Content = item.RightThumbLeft;
			StickRightRightLabel.Content = item.RightThumbRight;
			StickRightUpLabel.Content = item.RightThumbUp;
		}

		#region Drag and Drop Menu

		// Drag and Drop Menu PreviewMouseMove event.
		private void DragAndDropMenu_Source_PreviewMouseMove(object sender, MouseEventArgs e)
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
			//e.Handled = true;
		}

		// Colors.
		public SolidColorBrush colorActive = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF42C765");
		public SolidColorBrush colorNormalPath = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6699FF");
		public SolidColorBrush colorNormalTextBox = Brushes.White;
		public SolidColorBrush colorBlack = (SolidColorBrush)new BrushConverter().ConvertFrom("#11000000");
		public SolidColorBrush colorNormalLabel = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFDEDEDE");
		public SolidColorBrush colorOver = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFCC66");
		public SolidColorBrush colorRecord = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFF6B66");

		// Lists.
		public List<Label> ButtonList = new List<Label>();
		public List<Label> IButtonList = new List<Label>();

		public List<Label> AxisList = new List<Label>();
		public List<Label> HAxisList = new List<Label>();
		public List<Label> FAxisList = new List<Label>();
		public List<Label> IAxisList = new List<Label>();
		public List<Label> IHAxisList = new List<Label>();
		public List<Label> IFAxisList = new List<Label>();

		public List<Label> SliderList = new List<Label>();
		public List<Label> ISliderList = new List<Label>();

		public List<Label> POVList = new List<Label>();
		public List<Label> IPOVList = new List<Label>();

		public List<Label> POVButtonList = new List<Label>();

		private void CreateDragAndDropMenuLabels(int total, string headerName, string itemName)
		{
			// GroupBox Header.
			ContentControl headerContentControl = new ContentControl();		
			//Grid headerGrid = new Grid();
			TextBlock headerLabel = new TextBlock { Text = headerName, Margin = new Thickness(3, 0, 0, 0) };
			// StackPanel
			StackPanel headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			headerStackPanel.Children.Add(headerContentControl);
			headerStackPanel.Children.Add(headerLabel);
			// UniformGrid.
			UniformGrid buttonsUniformGrid = new UniformGrid { Columns = 8 };
			// GroupBox.
			GroupBox buttonsGroupBox = new GroupBox { Header = headerStackPanel, Content = buttonsUniformGrid };

			// Put GroupBoxes into NORMAL and INVERTED tabs.
			if (headerName.Contains("INVERTED")) { DragAndDropStackPanelInverted.Children.Add(buttonsGroupBox); }
			else { DragAndDropStackPanelNormal.Children.Add(buttonsGroupBox); }

			// Create labels.
			for (int i = 1; i <= total; i++)
			{
				Label buttonLabel = new Label
				{
					Name = itemName + i + "Label",
					Content = i.ToString(),
					ToolTip = itemName + " " + i,
					Tag = itemName + " " + i
				};
				// Assign mouse event.
				buttonLabel.PreviewMouseMove += DragAndDropMenu_Source_PreviewMouseMove;
				// Add label to UniformGrid.
				buttonsUniformGrid.Children.Add(buttonLabel);

				// Assign GroupBox header icons and put into lists.
				if (headerName == "BUTTON")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Button"];
					ButtonList.Add(buttonLabel);
				}
				else if (headerName == "BUTTON · INVERTED")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Button_Inverted"];
					IButtonList.Add(buttonLabel);
				}
				else if (headerName == "AXIS")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis"];
					AxisList.Add(buttonLabel);
				}
				else if (headerName == "AXIS · HALF TO FULL")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Half_to_Full"];
					HAxisList.Add(buttonLabel);
				}
				else if (headerName == "AXIS · FULL TO HALF")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Full_to_Half"];
					FAxisList.Add(buttonLabel);
				}
				else if (headerName == "AXIS · INVERTED")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Inverted"];
					IAxisList.Add(buttonLabel);
				}
				else if (headerName == "AXIS · INVERTED · HALF TO FULL")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Half_to_Full_Inverted"];
					IHAxisList.Add(buttonLabel);
				}
				else if (headerName == "AXIS · INVERTED · FULL TO HALF")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Full_to_Half_Inverted"];
					IFAxisList.Add(buttonLabel);
				}
				else if (headerName == "SLIDER")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis"];
					SliderList.Add(buttonLabel);
				}
				else if (headerName == "SLIDER · INVERTED")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_Axis_Inverted"];
					SliderList.Add(buttonLabel);
				}
				else if (headerName == "POV")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_POV"];
					POVList.Add(buttonLabel);
				}
				else if (headerName == "POV · BUTTON")
				{
					headerContentControl.Content = Application.Current.Resources["Icon_DragAndDrop_POV"];
					POVButtonList.Add(buttonLabel);
				}
			}
		}

		object updateLock = new object();
		object oldState = null;

		public void DragAndDropMenuUpdate(UserDevice ud)
		{
			CustomDiState customDiState = null;
			var state = ud?.DeviceState;
			if (state == null)
				return;
			if (state == oldState)
				return;
			lock (updateLock)
			{
				if (state is MouseState mState)
					customDiState = new CustomDiState(mState);
				if (state is KeyboardState kState)
					customDiState = new CustomDiState(kState);
				if (state is JoystickState jState)
					customDiState = new CustomDiState(jState);
			}
			if (customDiState == null)
				return;
			SetInputNormalActiveColor(ud, customDiState);
		}

		public void SetInputNormalActiveColor(UserDevice ud, CustomDiState customDiState)
		{
			// Button1Label, IButton1Label.
			if (ud.CapButtonCount > 0)
			{
				for (int index = 0; index < ud.CapButtonCount; index++)
				{
					ButtonList[index].Background = customDiState.Buttons[index] ? colorActive : colorNormalLabel;
					ButtonList[index].ToolTip = customDiState.Buttons[index].ToString();
					IButtonList[index].Background = customDiState.Buttons[index] ? colorNormalLabel : colorActive;
					IButtonList[index].ToolTip = (!customDiState.Buttons[index]).ToString();
				}
			}
			// Axis1Label, HAxis1Label, IAxis1Label, IHAxis1Label. 
			if (ud.CapAxeCount > 0)
			{
				for (int index = 0; index < ud.CapAxeCount; index++)
				{
					AxisList[index].Background =				
					HAxisList[index].Background =
					FAxisList[index].Background =
					IAxisList[index].Background =
					IHAxisList[index].Background =
					IFAxisList[index].Background =
						customDiState.Axis[index] > 32767 + 2000 || customDiState.Axis[index] < 32767 - 2000 ? colorActive : colorNormalLabel;

					AxisList[index].ToolTip = customDiState.Axis[index].ToString();
					IAxisList[index].ToolTip = Math.Abs(customDiState.Axis[index] - 65535).ToString();
					HAxisList[index].ToolTip = Math.Abs((customDiState.Axis[index] - 32767) * 2).ToString();
					FAxisList[index].ToolTip = Math.Round((double)customDiState.Axis[index] / 2).ToString();
					IHAxisList[index].ToolTip = Math.Abs(Math.Abs((customDiState.Axis[index] - 32767) * 2) -65535).ToString();
					IFAxisList[index].ToolTip = Math.Abs(Math.Round((double)customDiState.Axis[index] / 2 - 32767)).ToString();

					if (index == 2) LeftTextBox2.Content = Math.Abs((customDiState.Axis[2] - 32767) * 2).ToString();
				}
			}
			// Slider1Label, HSlider1Label, ISlider1Label, IHSlider1Label.
			if (SliderList.Count > 0)
			{
				for (int index = 0; index < SliderList.Count; index++)
				{
					SliderList[index].Background = customDiState.Sliders[index] > 2000 ? colorActive : colorNormalLabel;
					SliderList[index].ToolTip = customDiState.Sliders[index].ToString();
				}
			}
			// POV1Label.
			if (POVList.Count > 0)
			{
				for (int index = 0; index < POVList.Count; index++)
				{
					POVList[index].Background = customDiState.POVs[index] > -1 ? colorActive : colorNormalLabel;
					POVList[index].ToolTip = customDiState.POVs[index].ToString();

					POVButtonList[0].Background = customDiState.POVs[index] == 0 ? colorActive : colorNormalLabel;
					POVButtonList[0].ToolTip = customDiState.POVs[index] == 0 ? customDiState.POVs[index].ToString() : "-1";
					POVButtonList[1].Background = customDiState.POVs[index] == 9000 ? colorActive : colorNormalLabel;
					POVButtonList[1].ToolTip = customDiState.POVs[index] == 9000 ? customDiState.POVs[index].ToString() : "-1";
					POVButtonList[2].Background = customDiState.POVs[index] == 18000 ? colorActive : colorNormalLabel;
					POVButtonList[2].ToolTip = customDiState.POVs[index] == 18000 ? customDiState.POVs[index].ToString() : "-1";
					POVButtonList[3].Background = customDiState.POVs[index] == 27000 ? colorActive : colorNormalLabel;
					POVButtonList[3].ToolTip = customDiState.POVs[index] == 27000 ? customDiState.POVs[index].ToString() : "-1";
				}
			}
		}

		#endregion

		#region ■ Direct Input Menu

		List<MenuItem> DiMenuStrip = new List<MenuItem>();
		string cRecord = "[Record]";
		string cEmpty = "<empty>";
		string cPOVs = "POVs";

		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice ud)
		{
			// Delete all DragAndDrop menu children.
			DragAndDropStackPanelNormal.Children.Clear();
			DragAndDropStackPanelInverted.Children.Clear();
			// Clear all DragAndDrop menu lists.
			ButtonList.Clear();
			IButtonList.Clear();

			AxisList.Clear();
			HAxisList.Clear();
			FAxisList.Clear();
			IAxisList.Clear();
			IHAxisList.Clear();
			IFAxisList.Clear();

			SliderList.Clear();
			ISliderList.Clear();

			POVList.Clear();

			DiMenuStrip.Clear();
			MenuItem mi;
			mi = new MenuItem() { Header = cEmpty };
			mi.Foreground = SystemColors.ControlDarkBrush;
			mi.Click += DiMenuStrip_Click;
			DiMenuStrip.Add(mi);
			// Return if direct input device is not available.
			if (ud == null)
				return;
			// Add [Record] label.
			mi = new MenuItem() { Header = cRecord };
			//mi.Icon = new ContentControl();
			mi.Click += DiMenuStrip_Click;
			DiMenuStrip.Add(mi);

			

			// Do not add menu items for keyboard, because user interface will become too sluggish.
			// Recording feature is preferred way to map keyboard label.
			if (!ud.IsKeyboard)
			{
				// Add Drag and Drop menu buttons.
				CreateDragAndDropMenuLabels(ud.CapButtonCount, "BUTTON", "Button");
				CreateDragAndDropMenuLabels(ud.CapButtonCount, "BUTTON · INVERTED", "IButton");

				// Add Buttons.
				mi = new MenuItem() { Header = "Buttons" };
				DiMenuStrip.Add(mi);
				CreateItems(mi, "Inverted", "IButton {0}", "-{0}", ud.CapButtonCount);
				CreateItems(mi, "Button {0}", "{0}", ud.CapButtonCount);

				if (ud.DiAxeMask > 0)
				{
					// Add Drag and Drop menu axes.
					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS", "Axis");
					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS · HALF TO FULL", "HAxis");
					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS · FULL TO HALF", "FAxis");

					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS · INVERTED", "IAxis");
					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS · INVERTED · HALF TO FULL", "IHAxis");
					CreateDragAndDropMenuLabels(ud.CapAxeCount, "AXIS · INVERTED · FULL TO HALF", "IFAxis");

					// Add Axes.
					mi = new MenuItem() { Header = "Axes" };
					DiMenuStrip.Add(mi);
					CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Half", "HAxis {0}", "x{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Axis {0}", "a{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
				}
				var slidersCount = ud.DeviceObjects.Where(x => x.Type.Equals(SharpDX.DirectInput.ObjectGuid.Slider)).Count();
				if (slidersCount > 0)
				{
					// Add Drag and Drop menu sliders.
					CreateDragAndDropMenuLabels(slidersCount, "SLIDER", "Slider");
					CreateDragAndDropMenuLabels(slidersCount, "SLIDER · HALF", "HSlider");
					CreateDragAndDropMenuLabels(slidersCount, "SLIDER · INVERTED", "ISLider");
				}
				if (ud.DiSliderMask > 0)
				{

					// Add Sliders.            
					mi = new MenuItem() { Header = "Sliders" };
					DiMenuStrip.Add(mi);
					// 2 x Sliders, 2 x AccelerationSliders, 2 x state.ForceSliders, 2 x VelocitySliders
					CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Half", "HSlider {0}", "h{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Slider {0}", "s{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
				}
				// Add D-Pads.
				if (ud.CapPovCount > 0)
				{
					// Add Drag and Drop menu POVs.
					CreateDragAndDropMenuLabels(ud.CapPovCount, "POV", "POV1");
					CreateDragAndDropMenuLabels(ud.CapPovCount * 4, "POV · BUTTON", "POV1");

					// Add POVs.
					mi = new MenuItem() { Header = cPOVs };
					DiMenuStrip.Add(mi);
					// Add D-Pad Top, Right, Bottom, Left label.
					var dPadNames = Enum.GetNames(typeof(DPadEnum));
					for (int p = 0; p < ud.CapPovCount; p++)
					{
						var dPadItem = CreateItem("POV {0}", "{1}{0}", p + 1, SettingName.SType.POV);
						mi.Items.Add(dPadItem);
						for (int d = 0; d < dPadNames.Length; d++)
						{
							var dPadButtonIndex = p * 4 + d + 1;
							var dPadButtonItem = CreateItem("POV {0} {1}", "{2}{3}", p + 1, dPadNames[d], SettingName.SType.POVButton, dPadButtonIndex);
							dPadItem.Items.Add(dPadButtonItem);
						}
					}
				}
			}
		}

		void CreateItems(MenuItem parent, string subMenu, string text, string tag, int count, int? mask = null)
		{
			var smi = new MenuItem() { Header = subMenu };
			parent.Items.Add(smi);
			CreateItems(smi, text, tag, count, mask);
		}

		/// <summary>Create menu item.</summary>
		/// <param name="mask">Mask contains information if item is present.</param>
		void CreateItems(MenuItem parent, string text, string tag, int count, int? mask = null)
		{
			var items = new List<MenuItem>();
			for (int i = 0; i < count; i++)
			{
				// If mask specified and item is not present then...
				if (mask.HasValue && i < 32 && (((int)Math.Pow(2, i) & mask) == 0))
					continue;
				var item = CreateItem(text, tag, i + 1);
				items.Add(item);
			}
			foreach (var item in items)
				parent.Items.Add(item);
		}

		MenuItem CreateItem(string text, string tag, params object[] args)
		{
			var item = new MenuItem();
			item.Header = string.Format(text, args);
			item.Tag = string.Format(tag, args);
			item.Padding = new Thickness(0);
			item.Margin = new Thickness(0);
			item.Click += DiMenuStrip_Click;
			return item;
		}

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

		MenuItem LastItem;

		private TextBox CurrentTextBox;

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			var smi = (MenuItem)e.Source;
			if (mi != smi)
				return;

			LastItem?.Items.Clear();
			LastItem = mi;
			foreach (var item in DiMenuStrip)
				mi.Items.Add(item);

			var control = (Menu)mi.Parent;
			CurrentTextBox = (TextBox)control.Tag;

			ControlsHelper.BeginInvoke(() =>
			{
				mi.IsSubmenuOpen = true;
			});

		}

		void DiMenuStrip_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var fullValue = (string)item.Header;
			// If this DPad parent menu.
			if (fullValue == cRecord)
			{
				//var map = SettingsManager.Current.SettingsMap.First(x => x.Control == CurrentCbx);
				//StartRecording(map);
			}
			else
			{
				CurrentTextBox.Text = fullValue == cEmpty
					? ""
					: fullValue;
			}
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
			SetBinding(MapTo.None, null);
			DiMenuStrip.Clear();
		}

		private void RecordClear_MouseEnter(object sender, MouseEventArgs e)
		{
			RecordClearButtons.Visibility = Visibility.Visible;
		}

		private void RecordClear_MouseLeave(object sender, MouseEventArgs e)
		{
			RecordClearButtons.Visibility = Visibility.Collapsed;
		}
	}
}
