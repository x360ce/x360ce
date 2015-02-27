using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace x360ce.App.Controls
{
	public partial class GameSettingDetailsUserControl : BaseUserControl
	{
		public GameSettingDetailsUserControl()
		{
			InitializeComponent();
			var paItems = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
			foreach (var item in paItems) ProcessorArchitectureComboBox.Items.Add(item);
			XInputCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("XInput")).ToArray();
			HookCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("Hook")).ToArray();
			lock (CurrentGameLock)
			{
				EnableEvents();
			}
		}

		object CurrentGameLock = new object();
		bool EnabledEvents = false;

		CheckBox[] XInputCheckBoxes;
		CheckBox[] HookCheckBoxes;

		x360ce.Engine.Data.Game _CurrentGame;
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.Game CurrentGame
		{
			get { return _CurrentGame; }
			set
			{
				_CurrentGame = value;
				var en = (value != null);
				var item = value ?? new x360ce.Engine.Data.Game();
				var inputMask = (XInputMask)item.XInputMask;
				var hookMask = (HookMask)item.HookMask;
				SetMask(en, hookMask, inputMask, item.FullPath, item.ProcessorArchitecture);
			}
		}

		public void SetMask(bool en, HookMask hookMask, XInputMask inputMask, string path, int proc)
		{
			lock (CurrentGameLock)
			{
				if (EnabledEvents) DisableEvents();
				// Check/Uncheck XInput checkboxes.
				var xs = (XInputMask[])Enum.GetValues(typeof(XInputMask));
				XInputMaskTextBox.Text = ((int)inputMask).ToString("X8");
				foreach (var value in xs)
				{
					// Get checkbox linked to enum value.
					var cb = XInputCheckBoxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
					if (cb != null) cb.Checked = inputMask.HasFlag(value);
				}
				// Check/Uncheck Hook checkboxes.
				var hs = (HookMask[])Enum.GetValues(typeof(HookMask));
				HookMaskTextBox.Text = ((int)inputMask).ToString("X8");
				foreach (var value in hs)
				{
					// Get checkbox linked to enum value.
					var cb = HookCheckBoxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
					if (cb != null) cb.Checked = hookMask.HasFlag(value);
				}
				// Processor architecture.
				ProcessorArchitectureComboBox.SelectedItem = Enum.IsDefined(typeof(ProcessorArchitecture), proc)
					? (ProcessorArchitecture)proc
					: ProcessorArchitecture.None;
				// Enable events.
				EnableEvents();
			}
		}

		public T GetMask<T>(CheckBox[] boxes)
		{
			uint mask = 0;
			// Check/Uncheck checkboxes.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get checkbox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null && cb.Checked) mask |= (uint)(object)value;
			}
			return (T)(object)mask;
		}

		void EnableEvents()
		{
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			EnabledEvents = true;
		}

		void DisableEvents()
		{
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			EnabledEvents = false;
		}

		void CheckBox_Changed(object sender, EventArgs e)
		{
			if (CurrentGame == null) return;
			var xm = (int)GetMask<XInputMask>(XInputCheckBoxes);
			CurrentGame.XInputMask = xm;
			XInputMaskTextBox.Text = xm.ToString("X8");
			var hm = (int)GetMask<HookMask>(HookCheckBoxes);
			CurrentGame.HookMask = hm;
			HookMaskTextBox.Text = hm.ToString("X8");
			SettingsFile.Current.Save();
		}

		void SetCheckXinput(object sender, XInputMask mask)
		{
			//if (CurrentGame == null) return;
			//var name = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(mask);
			//var path = System.IO.Path.GetDirectoryName(CurrentGame.FullPath);
			//var fullPath = System.IO.Path.Combine(path, name);
			//var box = (CheckBox)sender;
			//var exists = Helper.CreateDllFile(box.Checked, fullPath);
			//if (exists != box.Checked) box.Checked = exists;
		}

	}
}
