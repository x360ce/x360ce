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
			//ApplyStyleToCheckBoxes();
		}

		object CurrentGameLock = new object();

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
				DisableEvents();
				// Update XInput mask.
				Xinput11CheckBox.Checked = inputMask.HasFlag(XInputMask.Xinput11);
				Xinput12CheckBox.Checked = inputMask.HasFlag(XInputMask.Xinput12);
				Xinput13CheckBox.Checked = inputMask.HasFlag(XInputMask.Xinput13);
				Xinput14CheckBox.Checked = inputMask.HasFlag(XInputMask.Xinput14);
				Xinput91CheckBox.Checked = inputMask.HasFlag(XInputMask.Xinput91);
				XInputMaskTextBox.Text = ((int)inputMask).ToString("X8");
				// Update hook mask.
				HookCOMCheckBox.Checked = hookMask.HasFlag(HookMask.COM);
				HookDICheckBox.Checked = hookMask.HasFlag(HookMask.DI);
				HookDISABLECheckBox.Checked = hookMask.HasFlag(HookMask.DISABLE);
				HookLLCheckBox.Checked = hookMask.HasFlag(HookMask.LL);
				HookNameCheckBox.Checked = hookMask.HasFlag(HookMask.NAME);
				HookPIDVIDCheckBox.Checked = hookMask.HasFlag(HookMask.PIDVID);
				HookSACheckBox.Checked = hookMask.HasFlag(HookMask.SA);
				HookSTOPCheckBox.Checked = hookMask.HasFlag(HookMask.STOP);
				HookWTCheckBox.Checked = hookMask.HasFlag(HookMask.WT);
				HookMaskTextBox.Text = ((int)hookMask).ToString("X8");
				// Processor architecture.
				ProcessorArchitectureComboBox.SelectedItem = Enum.IsDefined(typeof(ProcessorArchitecture), proc)
					? (ProcessorArchitecture)proc
					: ProcessorArchitecture.None;

				// Enable events.
				EnableEvents();
			}
		}

		void EnableEvents()
		{
			Xinput11CheckBox.CheckedChanged += CheckBox_Changed;
			Xinput12CheckBox.CheckedChanged += CheckBox_Changed;
			Xinput13CheckBox.CheckedChanged += CheckBox_Changed;
			Xinput14CheckBox.CheckedChanged += CheckBox_Changed;
			Xinput91CheckBox.CheckedChanged += CheckBox_Changed;
			HookCOMCheckBox.CheckedChanged += CheckBox_Changed;
			HookDICheckBox.CheckedChanged += CheckBox_Changed;
			HookDISABLECheckBox.CheckedChanged += CheckBox_Changed;
			HookLLCheckBox.CheckedChanged += CheckBox_Changed;
			HookNameCheckBox.CheckedChanged += CheckBox_Changed;
			HookPIDVIDCheckBox.CheckedChanged += CheckBox_Changed;
			HookSACheckBox.CheckedChanged += CheckBox_Changed;
			HookSTOPCheckBox.CheckedChanged += CheckBox_Changed;
			HookWTCheckBox.CheckedChanged += CheckBox_Changed;
		}

		void DisableEvents()
		{
			Xinput11CheckBox.CheckedChanged -= CheckBox_Changed;
			Xinput12CheckBox.CheckedChanged -= CheckBox_Changed;
			Xinput13CheckBox.CheckedChanged -= CheckBox_Changed;
			Xinput14CheckBox.CheckedChanged -= CheckBox_Changed;
			Xinput91CheckBox.CheckedChanged -= CheckBox_Changed;
			HookCOMCheckBox.CheckedChanged -= CheckBox_Changed;
			HookDICheckBox.CheckedChanged -= CheckBox_Changed;
			HookDISABLECheckBox.CheckedChanged -= CheckBox_Changed;
			HookLLCheckBox.CheckedChanged -= CheckBox_Changed;
			HookNameCheckBox.CheckedChanged -= CheckBox_Changed;
			HookPIDVIDCheckBox.CheckedChanged -= CheckBox_Changed;
			HookSACheckBox.CheckedChanged -= CheckBox_Changed;
			HookSTOPCheckBox.CheckedChanged -= CheckBox_Changed;
			HookWTCheckBox.CheckedChanged -= CheckBox_Changed;
		}

		void CheckBox_Changed(object sender, EventArgs e)
		{
			if (CurrentGame == null) return;
			var xm = XInputMask.None;
			if (Xinput11CheckBox.Checked) xm |= XInputMask.Xinput11;
			if (Xinput12CheckBox.Checked) xm |= XInputMask.Xinput12;
			if (Xinput13CheckBox.Checked) xm |= XInputMask.Xinput13;
			if (Xinput14CheckBox.Checked) xm |= XInputMask.Xinput14;
			if (Xinput91CheckBox.Checked) xm |= XInputMask.Xinput91;
			if (CurrentGame.XInputMask != (int)xm)
			{
				CurrentGame.XInputMask = (int)xm;
				XInputMaskTextBox.Text = CurrentGame.XInputMask.ToString("X8");
			}
			var hm = HookMask.NONE;
			if (HookCOMCheckBox.Checked) hm |= HookMask.COM;
			if (HookDICheckBox.Checked) hm |= HookMask.DI;
			if (HookDISABLECheckBox.Checked) hm |= HookMask.DISABLE;
			if (HookLLCheckBox.Checked) hm |= HookMask.LL;
			if (HookNameCheckBox.Checked) hm |= HookMask.NAME;
			if (HookPIDVIDCheckBox.Checked) hm |= HookMask.PIDVID;
			if (HookSACheckBox.Checked) hm |= HookMask.SA;
			if (HookSTOPCheckBox.Checked) hm |= HookMask.STOP;
			if (HookWTCheckBox.Checked) hm |= HookMask.WT;
			if (CurrentGame.HookMask != (int)xm)
			{
				CurrentGame.HookMask = (int)hm;
				HookMaskTextBox.Text = CurrentGame.HookMask.ToString("X8");
			}
			SettingsFile.Current.Save();
		}

		void SetCheckXinput(object sender, string name)
		{
			if (CurrentGame == null) return;
			var path = System.IO.Path.GetDirectoryName(CurrentGame.FullPath);
			var fullPath = System.IO.Path.Combine(path, name);
			var box = (CheckBox)sender;
			var exists = Helper.CreateDllFile(box.Checked, fullPath);
			if (exists != box.Checked) box.Checked = exists;
		}

		private void Xinput91CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetCheckXinput(sender, Helper.dllFile0);
		}

		private void Xinput11CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetCheckXinput(sender, Helper.dllFile1);
		}

		private void Xinput12CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetCheckXinput(sender, Helper.dllFile2);
		}

		private void Xinput13CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetCheckXinput(sender, Helper.dllFile3);
		}

		private void Xinput14CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetCheckXinput(sender, Helper.dllFile4);
		}
	}
}
