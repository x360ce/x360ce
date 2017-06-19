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
		/// Write all settings to XML files.
		/// </summary>
		public bool WriteAllSettingsToXML()
		{
			var maps = new[] { MapTo.Controller1, MapTo.Controller2, MapTo.Controller4, MapTo.Controller4 };
			foreach (var map in maps)
			{
				var ps = GetPadSetting(map);
				// If setting doesn't exists then...
				if (!PadSettings.Items.Any(x => x.PadSettingChecksum == ps.PadSettingChecksum))
				{
					// Add setting to configuration.
					PadSettings.Items.Add(ps);
				}
			}
			// Get all settings used by PADs.
			var usedChecksums = Settings.Items.Select(x => x.PadSettingChecksum).Distinct().ToArray();
			// Get all stored padSettings.
			var allChecksums = PadSettings.Items.Select(x => x.PadSettingChecksum).Distinct().ToArray();
			// Wipe all pad settings not attached to devices.
			var notUsed = allChecksums.Except(usedChecksums);
			foreach (var nu in notUsed)
			{
				var notUsedItems = PadSettings.Items.Where(x => x.PadSettingChecksum == nu).ToArray();
				PadSettings.Remove(notUsedItems);
			}
			return true;
		}

		bool ValidatePropertyNames(SettingsMapItem[] maps, out PropertyInfo[] propertiesToSet)
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
		public PadSetting GetPadSetting(MapTo padIndex)
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
			ps.PadSettingChecksum = ps.GetCheckSum();
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
			loadCount++;
			// Resume form events (track setting changes on the form).
			ResumeEvents();
			var ev = ConfigLoaded;
			if (ev != null)
			{
				ev(this, new SettingEventArgs(typeof(PadSetting).Name, loadCount));
			}
		}

	}
}
