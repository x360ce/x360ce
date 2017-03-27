using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		/// Write PAD settings to INI file.
		/// </summary>
		/// <param name="padSectionName">INI section.</param>
		/// <param name="s">Settings.</param>
		/// <param name="p">PAD Settings.</param>
		public void WritePadSettingsToIni(string padSectionName, Setting s, PadSetting p)
		{
			ReadWritePadSettings(padSectionName, ref s, ref p, true);
		}

		/// <summary>
		/// Read PAD settings from INI file.
		/// </summary>
		/// <param name="padSectionName">INI section.</param>
		/// <param name="s">Settings.</param>
		/// <param name="p">PAD Settings.</param>
		public void ReadPadSettingsFromIni(string padSectionName, out Setting s, out PadSetting p)
		{
			s = new Setting();
			p = new PadSetting();
			ReadWritePadSettings(padSectionName, ref s, ref p, false);
		}

		/// <summary>
		/// Read PAD settings from INI file.
		/// </summary>
		/// <param name="padSectionName">INI section.</param>
		/// <param name="s">Settings.</param>
		/// <param name="p">PAD Settings.</param>
		/// <remarks>use 'ref' to indicate that objects will be updated inside.</remarks>
		void ReadWritePadSettings(string padSectionName, ref Setting s, ref PadSetting p, bool write)
		{
			s = new Setting();
			p = new PadSetting();
			// Get PAD1 settings data as a reference.
			var items = SettingsMap.Where(x => x.MapTo == MapTo.Controller1).ToArray();
			var sProps = s.GetType().GetProperties();
			var pProps = p.GetType().GetProperties();
			var ini2 = new Ini(IniFileName);
			foreach (var item in items)
			{
				string key = item.IniPath.Split('\\')[1];
				// Try to find property inside Settings object.
				var sProp = sProps.FirstOrDefault(x => x.Name == item.PropertyName);
				if (sProp != null)
				{
					if (write)
					{
						var sv = (string)sProp.GetValue(s, null) ?? "";
						// Write value to INI file.
						ini2.SetValue(padSectionName, key, sv);
					}
					else
					{
						// Read value from INI file.
						var sv = ini2.GetValue(padSectionName, key) ?? "";
						sProp.SetValue(s, sv, null);
					}
					continue;
				}
				// Try to find property inside PAD Settings object.
				var pProp = pProps.FirstOrDefault(x => x.Name == item.PropertyName);
				if (pProp != null)
				{

					if (write)
					{
						var pv = (string)pProp.GetValue(p, null) ?? "";
						// Write value to INI file.
						ini2.SetValue(padSectionName, key, pv);
					}
					else
					{
						// Read value from INI file.
						var pv = ini2.GetValue(padSectionName, key) ?? "";
						sProp.SetValue(s, pv, null);
					}
					continue;
				}
				// Property was not found.
				var message = string.Format("ReadWritePadSettings: Property '{0}' was not found", item.PropertyName);
				// Inform developer with the message.
				MessageBoxForm.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Read INI file by game.
		/// </summary>
		/// <param name="game"></param>
		public SearchResult ReadIniFile(Game game)
		{
			var result = new SearchResult();
			var settings = new List<Setting>();
			var padSettings = new List<PadSetting>();
			// Get game directory.
			var dir = new FileInfo(game.FullPath).Directory;
			// Get INI file.
			var iniFile = dir.GetFiles(IniFileName).FirstOrDefault();
			if (iniFile != null)
			{
				var ini = new Ini(iniFile.FullName);
				var optionKeys = new[]
				{
					SettingName.PAD1,
					SettingName.PAD2,
					SettingName.PAD3,
					SettingName.PAD4,
				};
				for (int i = 0; i < optionKeys.Length; i++)
				{
					var optionKey = optionKeys[i];
					var mapTo = (MapTo)(i + 1);
					var value = ini.GetValue(OptionsSection, optionKey);
					var padSectionNames = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var padSectionName in padSectionNames)
					{
						Setting setting;
						PadSetting padSetting;
						ReadPadSettingsFromIni(padSectionName, out setting, out padSetting);
						setting.MapTo = (int)mapTo;
						// If settings was not added already then...
						if (!settings.Any(x => x.InstanceGuid.Equals(setting.InstanceGuid)))
						{
							settings.Add(setting);
							// If PAD setting was not added already then...
							if (padSettings.Any(x => x.PadSettingChecksum.Equals(x.PadSettingChecksum)))
							{
								padSettings.Add(padSetting);
							}
						}
					}
				}
			}
			return result;
		}

		/// Write game settings to INI file.
		/// </summary>
		/// <param name="game"></param>
		public string GetIniContent(Game game)
		{
			var sb = new StringBuilder();
			var optionsContent = GetIniContent(OptionsSection);
			var mappingsContent = GetIniContent(MappingsSection);
			sb.Append(optionsContent);
			sb.AppendLine();
			sb.Append(mappingsContent);
			sb.AppendLine();
			return sb.ToString();
		}

		/// <summary>
		/// Write game settings to INI file.
		/// </summary>
		/// <param name="game"></param>
		public string GetIniContent(string sectionName)
		{
			var items = SettingsMap.Where(x => x.IniSection == sectionName).ToArray();
			var sb = new StringBuilder();
			sb.AppendFormat("[{0}]", sectionName).AppendLine();
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				var value = GetSettingValue(item.Control);
				sb.AppendFormat("{0}={1}", item.IniKey, value);
			}
			return sb.ToString();
		}

		///// <summary>
		///// Write game settings to INI file.
		///// </summary>
		///// <param name="game"></param>
		//public string GetIniContent(Game game)
		//{
		//	// Get game directory.
		//	var dir = new FileInfo(game.FullPath).Directory;
		//	// Get INI file.
		//	var iniFile = dir.GetFiles(IniFileName).FirstOrDefault();
		//	var ini = new Ini(iniFile.FullName);
		//	var optionKeys = new[] { SettingName.PAD1, SettingName.PAD2, SettingName.PAD3, SettingName.PAD4 };
		//	for (int i = 0; i < optionKeys.Length; i++)
		//	{
		//		var optionKey = optionKeys[i];
		//		var mapTo = (MapTo)(i + 1);
		//		// Write PADx.
		//		var mapItem = SettingsMap.FirstOrDefault(x => x.IniSection == OptionsSection && x.IniKey == optionKey);
		//		WriteSettingsToIni(mapItem);
		//		var settings = SettingManager.Settings.Items.Where(x => x.MapTo == (int)mapTo).ToArray();
		//		for (int s = 0; s < settings.Length; s++)
		//		{
		//			var setting = settings[i];
		//			var padSetting = SettingManager.GetPadSetting(setting.PadSettingChecksum);
		//			var padSectionName = string.Format("IG_{0:N}", setting.InstanceGuid);
		//			WritePadSettingsToIni(padSectionName, setting, padSetting);
		//		}
		//	}
		//	return null;
		//}


		/// <summary>
		/// Write game settings to INI file.
		/// </summary>
		/// <param name="game"></param>
		public string GetIniContent2(Game game)
		{
			// Get game directory.
			var dir = new FileInfo(game.FullPath).Directory;
			// Get INI file.
			var iniFile = dir.GetFiles(IniFileName).FirstOrDefault();
			var ini = new Ini(iniFile.FullName);
			var optionKeys = new[] { SettingName.PAD1, SettingName.PAD2, SettingName.PAD3, SettingName.PAD4 };
			for (int i = 0; i < optionKeys.Length; i++)
			{
				var optionKey = optionKeys[i];
				var mapTo = (MapTo)(i + 1);
				// Write PADx.
				var mapItem = SettingsMap.FirstOrDefault(x => x.IniSection == OptionsSection && x.IniKey == optionKey);
				WriteSettingsToIni(mapItem);
				var settings = SettingsManager.Settings.Items.Where(x => x.MapTo == (int)mapTo).ToArray();
				for (int s = 0; s < settings.Length; s++)
				{
					var setting = settings[i];
					var padSetting = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
					var padSectionName = string.Format("IG_{0:N}", setting.InstanceGuid);
					WritePadSettingsToIni(padSectionName, setting, padSetting);
				}
			}
			return null;
		}

		/// <summary>
		/// Write value to INI file by control.
		/// </summary>
		public bool WriteAllSettingsToInit()
		{
			return WriteSettingsToIni(SettingsMap.ToArray());
		}

		/// <summary>
		/// Write ALL values to INI.
		/// </summary>
		public bool WriteSettingToIni(Control control)
		{
			var item = SettingsMap.FirstOrDefault(x => x.Control == control);
			return WriteSettingsToIni(item);
		}

		/// <summary>
		/// Write value to INI file by control.
		/// </summary>
		bool WriteSettingsToIni(params SettingsMapItem[] items)
		{
			var saved = false;
			var ini = new Ini(IniFileName);
			foreach (var item in items)
			{
				if (item != null)
				{
					var section = item.IniSection;
					string key = item.IniKey;
					var iniValue = ini.GetValue(section, key);
					var newValue = GetSettingValue(item.Control);
					if (iniValue != newValue)
					{
						var result = ini.SetValue(section, key, newValue);
						if (result != 0) saved = true;
					}
				}
			}
			return saved;
		}

	}
}
