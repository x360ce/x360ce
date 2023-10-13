using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		public void SavePadSetting(UserSetting setting, PadSetting padSetting)
		{
			var ps = new PadSetting();
			ps.Load(padSetting);
			// If setting doesn't exists then...
			if (!PadSettings.Items.Any(x => x.PadSettingChecksum == ps.PadSettingChecksum))
			{
				// Add setting to configuration.
				lock (PadSettings.SyncRoot)
					PadSettings.Items.Add(ps);
			}
			// If pad setting checksum changed then...
			if (setting.PadSettingChecksum != ps.PadSettingChecksum)
			{
				// Assign updated checksum.
				setting.PadSettingChecksum = ps.PadSettingChecksum;
				var ud = GetDevice(setting.InstanceGuid);
				setting.Completion = UserSetting.GetCompletionPoints(ps, ud);
			}
			CleanupPadSettings();
		}

		/// <summary>
		/// Remove PAD settings, not attached to any device.
		/// </summary>
		public void CleanupPadSettings()
		{
			//// Get all records used by Settings.
			//var usedPadSettings = UserSettings.ItemsToArraySynchronized()
			//	.Select(x => x.PadSettingChecksum).Distinct().ToList();
			//// Get all records used by Summaries.
			//var usedPadSettings2 = Summaries.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			//// Get all records used by Presets.
			//var usedPadSettings3 = Presets.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			//// Combine all pad settings.
			//usedPadSettings.AddRange(usedPadSettings2);
			//usedPadSettings.AddRange(usedPadSettings3);
			//// Get all stored padSettings.
			//var allPadSettings = PadSettings.Items.Select(x => x.PadSettingChecksum).Distinct().ToArray();
			//// Wipe all not used pad settings.
			//var notUsed = allPadSettings.Except(usedPadSettings);
			//foreach (var nu in notUsed)
			//{
			//	var notUsedItems = PadSettings.Items.Where(x => x.PadSettingChecksum == nu).ToArray();
			//	PadSettings.Remove(notUsedItems);
			//}

			// Get all records used by Settings, Summaries, and Presets.
			var usedPadSettings = UserSettings.ItemsToArraySynchronized().Select(x => x.PadSettingChecksum).Distinct().ToList();
			usedPadSettings.AddRange(Summaries.Items.Select(x => x.PadSettingChecksum).Distinct());
			usedPadSettings.AddRange(Presets.Items.Select(x => x.PadSettingChecksum).Distinct());
			// Create a HashSet from the usedPadSettings list to optimize lookup performance.
			var usedPadSettingsHashSet = new HashSet<Guid>(usedPadSettings);
			// Get all stored padSettings.
			var allPadSettings = PadSettings.Items.ToList();
			// Find all not used pad settings.
			var notUsedItems = allPadSettings.Where(x => !usedPadSettingsHashSet.Contains(x.PadSettingChecksum)).ToList();
			// Wipe all not used pad settings. 
			foreach (var item in notUsedItems) { PadSettings.Remove(item); }
		}

		/// <summary>
		/// Insert missing pad settings and clean-up the list.
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
		public void UpsertSettings(params UserSetting[] list)
		{
			foreach (var item in list)
			{
				var old = UserSettings.ItemsToArraySynchronized()
					.FirstOrDefault(x => x.SettingId == item.SettingId);
				if (old == null)
				{
					UserSettings.Add(item);
				}
				// If item was updated then...
				else if (item.DateUpdated > old.DateUpdated)
				{
					JocysCom.ClassLibrary.Runtime.RuntimeHelper.CopyDataMembers(item, old);
				}
			}
		}

		static bool DoValidatePropertyNames = false;

		public static bool ValidatePropertyNames(SettingsMapItem[] maps, out PropertyInfo[] propertiesToSet)
		{
			var availableNames = maps.Select(x => x.PropertyName);
			var properties = typeof(PadSetting).GetProperties();
			propertiesToSet = properties.Where(x => x.PropertyType == typeof(string) && x.Name != "ButtonBig").ToArray();
			var requiredNames = propertiesToSet.Select(x => x.Name);
			var missing = requiredNames.Except(availableNames);
			if (missing.Count() > 0 && DoValidatePropertyNames)
			{
				var list = string.Join(", ", missing);
				MessageBox.Show("'PadSetting' class property names must match 'SettingName' class property names. Please make sure that these properties exists in 'SettingName' class:\r\n\r\n" + list);
				return false;
			}
			return true;
		}

		public void LoadPadSettingAndCleanup(UserSetting setting, PadSetting ps, bool add = false)
		{
			// Link setting with pad setting.
			setting.PadSettingChecksum = ps.PadSettingChecksum;
			// Insert pad setting first, because it will be linked with the setting.
			UpsertPadSettings(ps);
			// Insert setting if not in the list.
			if (add)
				UserSettings.Add(setting);
			// Clean-up pad settings.
			Current.CleanupPadSettings();
		}

		/// <summary>
		/// Load PAD settings to form.
		/// </summary>
		/// <param name="padSetting">Settings to read.</param>
		/// <param name="padIndex">Destination pad index.</param>
		public void LoadPadSettingsIntoSelectedDevice(MapTo padIndex, PadSetting ps)
		{
			// Get pad control with settings.
			var padControl = Global._MainWindow.MainPanel.MainBodyPanel.PadControls[(int)padIndex - 1];
			// Get selected setting.
			var setting = padControl.CurrentUserSetting;
			// Return if nothing selected.
			if (setting == null)
				return;
			// If setting not supplied then use empty (clear settings).
			if (ps == null)
				ps = new PadSetting();
			LoadPadSettingAndCleanup(setting, ps);
			SyncFormFromPadSetting(padIndex, ps);
			RaiseSettingsChanged(null);
			loadCount++;
			ConfigLoaded?.Invoke(this, new SettingEventArgs(typeof(PadSetting).Name, loadCount));
		}

		public void SyncFormFromPadSetting(MapTo padIndex, PadSetting ps)
		{
			// Get setting maps for specified PAD Control.
			var maps = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			PropertyInfo[] properties;
			if (!ValidatePropertyNames(maps, out properties))
				return;
			// Suspend form events (do not track setting changes on the form).
			SuspendEvents();
			foreach (var p in properties)
			{
				var map = maps.FirstOrDefault(x => x.PropertyName == p.Name);
				if (map == null)
					continue;
				var v = (string)p.GetValue(ps, null) ?? "";
				// If value is not set then...
				if (string.IsNullOrEmpty(v))
					// Restore default value.
					v = string.Format("{0}", map.DefaultValue ?? "");
				LoadSetting(map.Control, map.PropertyName, v);
			}
			// Resume form events (track setting changes on the form).
			ResumeEvents();
		}

		public bool ClearAll(MapTo padIndex)
		{
			var description = JocysCom.ClassLibrary.Runtime.Attributes.GetDescription(padIndex);
			var text = string.Format("Do you want to clear all {0} settings?", description);
			var form = new JocysCom.ClassLibrary.Controls.MessageBoxWindow();
			var result = form.ShowDialog(text, "Clear Controller Settings", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
			if (result != System.Windows.MessageBoxResult.Yes)
				return false;
			LoadPadSettingsIntoSelectedDevice(padIndex, null);
			return true;
		}

	}
}
