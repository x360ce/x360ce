using JocysCom.ClassLibrary.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ProgramItemControl.xaml
	/// </summary>
	public partial class ProgramItemControl : UserControl
	{
		public ProgramItemControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			DInputCheckBoxes = ControlsHelper.GetAll<CheckBox>(DInputMaskGroupBox);
			XInputCheckBoxes = ControlsHelper.GetAll<CheckBox>(XInputMaskGroupBox);
			HookCheckBoxes = ControlsHelper.GetAll<CheckBox>(HookMaskGroupBox);
			AutoMapCheckBoxes = ControlsHelper.GetAll<CheckBox>(AutoMapMaskGroupBox);
			// Fill architecture combo box.
			var paItems = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
			foreach (var item in paItems)
				ProcessorArchitectureComboBox.Items.Add(item);
			// Fill emulation type combo box.
			var etItems = (EmulationType[])Enum.GetValues(typeof(EmulationType));
			foreach (var item in etItems)
				EmulationTypeComboBox.Items.Add(item);
			lock (CurrentGameLock)
			{
				EnableEvents();
			}
		}

		object CurrentGameLock = new object();
		bool EnabledEvents = false;

		CheckBox[] XInputCheckBoxes;
		CheckBox[] DInputCheckBoxes;
		CheckBox[] HookCheckBoxes;
		CheckBox[] AutoMapCheckBoxes;

		x360ce.Engine.Data.IProgram _CurrentItem;
		x360ce.Engine.Data.Program _DefaultSettings;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.IProgram CurrentItem
		{
			get { return _CurrentItem; }
			set
			{
				lock (CurrentGameLock)
				{
					// Detach event from old game.
					if (_CurrentItem != null)
						_CurrentItem.PropertyChanged -= CurrentGame_PropertyChanged;
					// Assign new value
					_CurrentItem = value;
					// Update interface.
					DisableEvents();
					if (value == null)
						return;
					// Make sure item is not null.
					var item = _CurrentItem ?? new x360ce.Engine.Data.Program();
					// Set Item properties.
					SetMask(DInputCheckBoxes, (DInputMask)item.DInputMask);
					SetMask(XInputCheckBoxes, (XInputMask)item.XInputMask);
					SetMask(HookCheckBoxes, (HookMask)item.HookMask);
					SetMask(AutoMapCheckBoxes, (MapToMask)item.AutoMapMask);
					HookModeFakeVidNumericUpDown.Value = item.FakeVID;
					HookModeFakePidNumericUpDown.Value = item.FakePID;
					TimeoutNumericUpDown.Value = item.Timeout;
					ProcessorArchitectureComboBox.SelectedItem = (ProcessorArchitecture)item.ProcessorArchitecture;
					EmulationTypeComboBox.SelectedItem = (EmulationType)item.EmulationType;
					var game = _CurrentItem as UserGame;
					var isGame = game != null;
					if (isGame)
						_DefaultSettings = SettingsManager.Programs.Items.FirstOrDefault(x => x.FileName == game.FileName);
					// Allow reset to default for games.
					ActionGroupBox.Visibility = isGame
						? Visibility.Visible
						: System.Windows.Visibility.Collapsed;
					ResetToDefaultButton.IsEnabled = isGame && _DefaultSettings != null;
					UpdateFakeVidPidControls();
					UpdateDinputControls();
					// Enable events.
					EnableEvents();
					// attach event to new game.
					if (_CurrentItem != null)
						_CurrentItem.PropertyChanged += CurrentGame_PropertyChanged;
				}
			}
		}

		/// <summary>
		/// Event will be triggered if somebody will change property directly on an object.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var game = CurrentItem;
			if (game == null)
				return;
			lock (CurrentGameLock)
			{
				// Set values without triggering events.
				DisableEvents();
				switch (e.PropertyName)
				{
					case nameof(UserGame.EmulationType):
						EmulationTypeComboBox.SelectedItem = (EmulationType)game.EmulationType;
						break;
					case nameof(UserGame.ProcessorArchitecture):
						ProcessorArchitectureComboBox.SelectedItem = (ProcessorArchitecture)game.ProcessorArchitecture;
						break;
					default:
						break;
				}
				EnableEvents();
			}
		}

		int GetMask<T>(CheckBox[] boxes) where T : struct, IConvertible
		{
			uint mask = 0;
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get CheckBox linked to enumeration value.
				var boxName = string.Format("{0}CheckBox", value);
				var cb = boxes.FirstOrDefault(x => x.Name.Equals(boxName, StringComparison.OrdinalIgnoreCase));
				if (cb != null && cb.IsChecked == true)
					mask |= (uint)(object)value;
			}
			return (int)mask;
		}

		void SetMask<T>(CheckBox[] boxes, T mask) where T : struct, IConvertible
		{
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			var m = Convert.ToUInt32(mask);
			foreach (var value in xs)
			{
				// Get CheckBox linked to enumeration value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null)
				{
					var v = Convert.ToUInt32(value);
					cb.IsChecked = (m & v) != 0;
				}
			}
			var gb = ControlsHelper.GetParent<GroupBox>(boxes.FirstOrDefault());
			UpdateTitle(gb, (int)m);
		}

		/// <summary>
		/// Run inside "CurrentGameLock" lock only.
		/// </summary>
		void EnableEvents()
		{
			if (EnabledEvents)
				return;
			foreach (var cb in DInputCheckBoxes)
			{
				cb.Checked += CheckBox_Changed;
				cb.Unchecked += CheckBox_Changed;
			}
			foreach (var cb in XInputCheckBoxes)
			{
				cb.Checked += CheckBox_Changed;
				cb.Unchecked += CheckBox_Changed;
			}
			foreach (var cb in HookCheckBoxes)
			{
				cb.Checked += CheckBox_Changed;
				cb.Unchecked += CheckBox_Changed;
			}
			foreach (var cb in AutoMapCheckBoxes)
			{
				cb.Checked += CheckBox_Changed;
				cb.Unchecked += CheckBox_Changed;
			}
			HookModeFakeVidNumericUpDown.ValueChanged += HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged += HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged += TimeoutNumericUpDown_ValueChanged;
			ProcessorArchitectureComboBox.SelectionChanged += ProcessorArchitectureComboBox_SelectionChanged;
			EmulationTypeComboBox.SelectionChanged += EmulationTypeComboBox_SelectionChanged;
			EnabledEvents = true;
		}

		/// <summary>
		/// Run inside "CurrentGameLock" lock only.
		/// </summary>
		void DisableEvents()
		{
			if (!EnabledEvents)
				return;
			foreach (var cb in DInputCheckBoxes)
			{
				cb.Checked -= CheckBox_Changed;
				cb.Unchecked -= CheckBox_Changed;
			}
			foreach (var cb in XInputCheckBoxes)
			{
				cb.Checked -= CheckBox_Changed;
				cb.Unchecked -= CheckBox_Changed;
			}
			foreach (var cb in HookCheckBoxes)
			{
				cb.Checked -= CheckBox_Changed;
				cb.Unchecked -= CheckBox_Changed;
			}
			foreach (var cb in AutoMapCheckBoxes)
			{
				cb.Checked -= CheckBox_Changed;
				cb.Unchecked -= CheckBox_Changed;
			}
			HookModeFakeVidNumericUpDown.ValueChanged -= HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged -= HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged -= TimeoutNumericUpDown_ValueChanged;
			ProcessorArchitectureComboBox.SelectionChanged -= ProcessorArchitectureComboBox_SelectionChanged;
			EmulationTypeComboBox.SelectionChanged -= EmulationTypeComboBox_SelectionChanged;
			EnabledEvents = false;
		}

		/// <summary>
		/// CheckBox events could fire at the same time.
		/// Use lock to make sure that only one file is processed during synchronization.
		/// </summary>
		object CheckBoxLock = new object();

		void CheckBox_Changed(object sender, RoutedEventArgs e)
		{
			if (CurrentItem == null)
				return;
			lock (CheckBoxLock)
			{
				var cbx = (CheckBox)sender;
				CheckBox[] cbxList = null;
				if (XInputCheckBoxes.Contains(cbx))
					cbxList = XInputCheckBoxes;
				if (DInputCheckBoxes.Contains(cbx))
					cbxList = DInputCheckBoxes;
				if (cbxList != null)
				{
					var is64bit = cbx.Name.Contains("x64");
					var is32bit = cbx.Name.Contains("x86");
					// If 64-bit CheckBox an checked then...
					if (is64bit && cbx.IsChecked == true)
					{
						// Make sure that 32-bit is unchecked
						var cbx32 = cbxList.First(x => x.Name == cbx.Name.Replace("x64", "x86"));
						if (cbx32.IsChecked == true)
							cbx32.IsChecked = false;
					}
					// If 32-bit CheckBox an checked then...
					if (is32bit && cbx.IsChecked == true)
					{
						// Make sure that 64-bit is unchecked
						var cbx64 = cbxList.First(x => x.Name == cbx.Name.Replace("x86", "x64"));
						if (cbx64.IsChecked == true)
							cbx64.IsChecked = false;
					}
				}
				int mask = 0;
				if (DInputCheckBoxes.Contains(cbx))
				{
					// Set DInput mask.DInputCheckBoxes
					mask = GetMask<DInputMask>(DInputCheckBoxes);
					if (CurrentItem.DInputMask != mask)
						CurrentItem.DInputMask = mask;
				}
				else if (XInputCheckBoxes.Contains(cbx))
				{
					// Set XInput mask.
					mask = GetMask<XInputMask>(XInputCheckBoxes);
					if (CurrentItem.XInputMask != mask)
						CurrentItem.XInputMask = mask;
				}
				else if (HookCheckBoxes.Contains(cbx))
				{
					// Set hook mask.
					mask = GetMask<HookMask>(HookCheckBoxes);
					if (CurrentItem.HookMask != mask)
						CurrentItem.HookMask = mask;
				}
				else if (AutoMapCheckBoxes.Contains(cbx))
				{
					// Set auto map mask.
					mask = GetMask<MapToMask>(AutoMapCheckBoxes);
					if (CurrentItem.AutoMapMask != mask)
						CurrentItem.AutoMapMask = mask;
				}
				UpdateTitle(cbx.Parent as GroupBox, mask);
			}
		}

		void UpdateTitle(GroupBox gp, int mask)
		{
			var end = gp.Header.ToString().IndexOf(" - ") + 3;
			var prefix = gp.Header.ToString().Substring(0, end);
			ControlsHelper.SetText(gp, "{0}{1:X8}", prefix, mask);
		}

		private void ResetToDefaultButton_Click(object sender, EventArgs e)
		{
			var game = CurrentItem;
			if (game == null)
				return;
			var program = _DefaultSettings;
			if (program == null)
				return;
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Reset current settings to default?", "Reset",
				System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
			if (result != System.Windows.MessageBoxResult.OK)
				return;
			// Reset to default all properties which affects checksum.
			game.XInputMask = program.XInputMask;
			game.XInputPath = program.XInputPath ?? "";
			game.HookMask = program.HookMask;
			game.AutoMapMask = (int)MapToMask.None;
			game.EmulationType = (int)EmulationType.None;
			game.DInputMask = program.DInputMask;
			game.DInputFile = program.DInputFile ?? "";
			game.FakeVID = program.FakeVID;
			game.FakePID = program.FakePID;
			game.Timeout = program.Timeout;
		}

		private void DInputFileTextBox_TextChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			item.DInputFile = DInputFileTextBox.Text;
		}

		private void XInputPathTextBox_TextChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			item.XInputPath = XInputPathTextBox.Text;
		}

		private void HookModeFakeVidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)HookModeFakeVidNumericUpDown.Value;
			if (item.FakeVID != value)
			{
				item.FakeVID = value;
				ControlsHelper.SetText(HookModeFakeVidTextBox, "0x{0:X4}", (int)HookModeFakeVidNumericUpDown.Value);
			}
		}

		private void HookModeFakePidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)HookModeFakePidNumericUpDown.Value;
			if (item.FakePID != value)
			{
				item.FakePID = value;
				ControlsHelper.SetText(HookModeFakePidTextBox, "0x{0:X4}", (int)HookModeFakePidNumericUpDown.Value);
			}
		}

		private void HookPIDVIDCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateFakeVidPidControls();
		}

		int msVid = 0x45E;
		int msPid = 0x28E;
		string dinputFile = "asiloader.dll";

		void UpdateFakeVidPidControls()
		{
			var en = HookPIDVIDCheckBox.IsChecked == true;
			HookModeFakeVidNumericUpDown.IsEnabled = en;
			HookModeFakePidNumericUpDown.IsEnabled = en;
			if (en)
			{
				if (HookModeFakeVidNumericUpDown.Value == 0)
					HookModeFakeVidNumericUpDown.Value = msVid;
				if (HookModeFakePidNumericUpDown.Value == 0)
					HookModeFakePidNumericUpDown.Value = msPid;
			}
			else
			{
				if (HookModeFakeVidNumericUpDown.Value == msVid)
					HookModeFakeVidNumericUpDown.Value = 0;
				if (HookModeFakePidNumericUpDown.Value == msPid)
					HookModeFakePidNumericUpDown.Value = 0;
			}
		}

		private void DInput8_x86CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateDinputControls();
		}

		private void DInput8_x64CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateDinputControls();
		}

		void UpdateDinputControls()
		{
			var en =
				DInput8_x86CheckBox.IsChecked == true ||
				DInput8_x64CheckBox.IsChecked == true;
			DInputFileTextBox.IsEnabled = en;
			if (en)
			{
				if (DInputFileTextBox.Text == "")
					DInputFileTextBox.Text = dinputFile;
			}
			else
			{
				if (DInputFileTextBox.Text == dinputFile)
					DInputFileTextBox.Text = "";
			}
		}


		#region ■ Update original item when user interacts with the interface

		private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)TimeoutNumericUpDown.Value;
			if (item.Timeout != value)
				item.Timeout = value;
		}

		private void ProcessorArchitectureComboBox_SelectionChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)ProcessorArchitectureComboBox.SelectedItem;
			if (item.ProcessorArchitecture != value)
				item.ProcessorArchitecture = value;
		}

		private void EmulationTypeComboBox_SelectionChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)EmulationTypeComboBox.SelectedItem;
			if (item.EmulationType != value)
				item.EmulationType = value;
		}

		#endregion

		#region ■ Help Links

		string GetGoogleSearchUrl()
		{
			var c = CurrentItem;
			if (c == null)
				return "";
			var url = "https://www.google.co.uk/?#q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}

		string GetNGemuSearchUrl()
		{
			var c = CurrentItem;
			if (c == null)
				return "";
			var url = "http://ngemu.com/search/5815705?q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}

		string GetNGemuThreadUrl()
		{
			var c = CurrentItem;
			if (c == null)
				return "";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false);
			var url = "http://ngemu.com/threads/";
			url += System.Web.HttpUtility.UrlEncode(keyName) + "/";
			return url;
		}

		private void GoogleSearchButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenUrl(GetGoogleSearchUrl());
		}

		private void NGEmuSearchLinkButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenUrl(GetNGemuSearchUrl());
		}

		private void NGEmuThreadLinkButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenUrl(GetNGemuThreadUrl());
		}

		#endregion

        private void GoogleSearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NGEmuSearchLinkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HyperLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ControlsHelper.OpenPath(e.Uri.AbsoluteUri);
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
		}
	}
}
