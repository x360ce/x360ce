using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		/// <summary>
		/// Apply all settings to XML.
		/// </summary>
		public bool ApplyAllSettingsToXML()
		{
			var padControls = MainForm.Current.PadControls;
			for (int i = 0; i < padControls.Length; i++)
			{
				// Get pad control with settings.
				var padControl = MainForm.Current.PadControls[i];
				var setting = padControl.GetSelectedSetting();
				// Skip if not selected.
				if (setting == null)
					continue;
				var padSetting = padControl.GetCurrentPadSetting();
				// If setting doesn't exists then...
				if (!PadSettings.Items.Any(x => x.PadSettingChecksum == padSetting.PadSettingChecksum))
				{
					// Add setting to configuration.
					PadSettings.Items.Add(padSetting);
				}
				// If pad setting checsum changed then...
				if (setting.PadSettingChecksum != padSetting.PadSettingChecksum)
				{
					// Assign updated checksum.
					setting.PadSettingChecksum = padSetting.PadSettingChecksum;
				}
			}
			CleanupPadSettings();
			return true;
		}

		/// <summary>
		/// Remove PAD settings, not attached to any device.
		/// </summary>
		public void CleanupPadSettings()
		{
			// Get all records used by Settings.
			var usedPadSettings = Settings.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Get all records used by Summaries.
			var usedPadSettings2 = Summaries.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Get all records used by Presets.
			var usedPadSettings3 = Presets.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Combine all pad settings.
			usedPadSettings.AddRange(usedPadSettings2);
			usedPadSettings.AddRange(usedPadSettings3);
			// Get all stored padSettings.
			var allPadSettings = PadSettings.Items.Select(x => x.PadSettingChecksum).Distinct().ToArray();
			// Wipe all not used pad settings.
			var notUsed = allPadSettings.Except(usedPadSettings);
			foreach (var nu in notUsed)
			{
				var notUsedItems = PadSettings.Items.Where(x => x.PadSettingChecksum == nu).ToArray();
				PadSettings.Remove(notUsedItems);
			}
		}

		/// <summary>
		/// Insert missing pad settings and cleanup the list.
		/// </summary>
		/// <param name="list"></param>
		public void UpsertPadSettings(params PadSetting[] list)
		{
			foreach (var item in list)
			{
				// If pad setting was not found then...
				if (!PadSettings.Items.Any(x => x.PadSettingChecksum == item.PadSettingChecksum))
					// Add pad setting.
					PadSettings.Add(item);
			}
		}

		/// <summary>
		/// Insert missing settings.
		/// </summary>
		/// <param name="list"></param>
		public void UpsertSettings(params Setting[] list)
		{
			foreach (var item in list)
			{
				var old = Settings.Items.FirstOrDefault(x => x.SettingId == item.SettingId);
				if (old == null)
				{
					Settings.Add(item);
				}
				// If item was updated then...
				else if (item.DateUpdated > old.DateUpdated)
				{
					JocysCom.ClassLibrary.Runtime.Helper.CopyDataMembers(item, old);
				}
			}
		}

		public static bool ValidatePropertyNames(SettingsMapItem[] maps, out PropertyInfo[] propertiesToSet)
		{
			var availableNames = maps.Select(x => x.PropertyName);
			var properties = typeof(PadSetting).GetProperties();
			propertiesToSet = properties.Where(x => x.PropertyType == typeof(string) && x.Name != "ButtonBig").ToArray();
			var requiredNames = propertiesToSet.Select(x => x.Name);
			var missing = requiredNames.Except(availableNames);
			if (missing.Count() > 0)
			{
				var list = string.Join(", ", missing);
				MessageBox.Show("'PadSetting' class property names must match 'SettingName' class property names. Plese make sure that these properties exists in 'SettingName' class:\r\n\r\n" + list);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		/// <param name="padIndex">Source pad index.</param>
		public PadSetting GetCurrentPadSetting(MapTo padIndex)
		{
			// Get settings related to PAD.
			var maps = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			PropertyInfo[] properties;
			if (!ValidatePropertyNames(maps, out properties))
				return null;
			var ps = new PadSetting();
			foreach (var p in properties)
			{
				var map = maps.First(x => x.PropertyName == p.Name);
				var key = map.IniPath.Split('\\')[1];
				// Get setting value from the form.
				var v = GetSettingValue(map.Control);
				// Set value onto padSetting.
				p.SetValue(ps, v ?? "", null);
			}
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>
		/// Load PAD settings to form.
		/// </summary>
		/// <param name="padSetting">Settings to read.</param>
		/// <param name="padIndex">Destination pad index.</param>
		public void LoadPadSettings(MapTo padIndex, PadSetting padSetting)
		{
			// Get settings related to PAD.
			var maps = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			PropertyInfo[] properties;
			if (!ValidatePropertyNames(maps, out properties))
				return;
			// Suspend form events (do not track setting changes on the form).
			SuspendEvents();
			// If pad settings must be reset then load default.
			var ps = padSetting == null ? new PadSetting() : padSetting;
			foreach (var p in properties)
			{
				var map = maps.First(x => x.PropertyName == p.Name);
				var key = map.IniPath.Split('\\')[1];
				var v = (string)p.GetValue(ps, null) ?? "";
				LoadSetting(map.Control, key, v);
			}
			// Resume form events (track setting changes on the form).
			ResumeEvents();
			// Must not use this (settings must take effect before, simplify later).
			Current.ApplyAllSettingsToXML();
			loadCount++;
			var ev = ConfigLoaded;
			if (ev != null)
			{
				ev(this, new SettingEventArgs(typeof(PadSetting).Name, loadCount));
			}
		}

	}
}
