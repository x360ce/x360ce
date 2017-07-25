using JocysCom.ClassLibrary.Configuration;
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
		public SearchResult ReadIniFile(UserGame game)
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
		public string GetIniContent(UserGame game)
		{
			var sb = new StringBuilder();
			var optionsContent = GetIniContent(OptionsSection);
			sb.Append(optionsContent);
			var mappingsContent = GetIniContent(MappingsSection);
			sb.AppendLine();
			sb.Append(mappingsContent);
			sb.AppendLine();
			sb.Append(';').Append('-', 64).AppendLine();
			sb.AppendLine("; Instance settings used for manually mapped controllers.");
			sb.Append(';').Append('-', 64).AppendLine();
			// Get all instances mapped to PADs for specific game.
			var instances = Settings.Items.Where(x => x.MapTo > 0 && x.IsEnabled && x.FileName == game.FileName && x.FileProductName == game.FileProductName).ToArray();
			for (int i = 0; i < instances.Count(); i++)
			{
				sb.AppendLine();
				var instance = instances[i];
				// Add PAD setting instance.
				sb.AppendFormat("[{0}]", GetInstanceSection(instance.InstanceGuid));
				sb.AppendLine();
				// Get pad settings attached to specific instance.
				var ps = PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == instance.PadSettingChecksum);
				// If pad settings not found then...
				if (ps == null)
				{
					// Reset setttings.
					ps = new PadSetting();
					instance.PadSettingChecksum = ps.PadSettingChecksum;
				}
				// Convert PadSettings to INI string.
				sb.Append(ConvertToIni(ps));

			}
			sb.AppendLine();
			sb.Append(';').Append('-', 64).AppendLine();
			sb.AppendLine("; Product settings used for AUTO mapped controllers.");
			sb.Append(';').Append('-', 64).AppendLine();
			var products = Presets.Items.ToArray();
			for (int i = 0; i < products.Count(); i++)
			{
				sb.AppendLine();
				var product = products[i];
				// Add PAD setting instance.
				sb.AppendFormat("[{0}]", GetProductSection(product.ProductGuid));
				sb.AppendLine();
				// Get padd settings attached to specific product.
				var ps = PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == product.PadSettingChecksum);
				// If pad settings not found then...
				if (ps == null)
				{
					// Reset setttings.
					ps = new PadSetting();
					product.PadSettingChecksum = ps.PadSettingChecksum;
				}
				// Convert PadSettings to INI string.
				sb.Append(ConvertToIni(ps));
			}
			// Get mapped instances.
			return sb.ToString();
		}

		public string ConvertToIni(PadSetting ps)
		{
			var sb = new StringBuilder();
			// Get settings related to PAD. Use Controller1 as reference.
			var maps = SettingsMap.Where(x => x.MapTo == MapTo.Controller1).ToArray();
			PropertyInfo[] properties;
			if (!ValidatePropertyNames(maps, out properties))
				return null;
			foreach (var p in properties)
			{
				var map = maps.First(x => x.PropertyName == p.Name);
				var key = map.IniPath.Split('\\')[1];
				// Get setting value from the pad setting.
				var value = (string)p.GetValue(ps, null) ?? "";
				if (!string.IsNullOrEmpty(value))
				{
					sb.AppendFormat("{0}={1}", key, value);
					sb.AppendLine();
				}
			}
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
				sb.AppendFormat("{0}={1}\r\n", item.IniKey, value);
			}
			return sb.ToString();
		}

		public void SaveINI(UserGame game)
		{
			var fullPath = Path.GetDirectoryName(game.FullPath);
			var fullName = Path.Combine(fullPath, IniFileName);
			var content = GetIniContent(game);
			var bytes = SettingsHelper.GetFileConentBytes(content, Encoding.Unicode);
			SettingsHelper.WriteIfDifferent(fullName, bytes);
		}

	}
}
