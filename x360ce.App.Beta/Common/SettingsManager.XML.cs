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
		/// Write all settings to XML files.
		/// </summary>
		public bool WriteAllSettingsToXML()
		{
			var padSetting = GetPadSetting(MapTo.Controller1);

			return true;
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		/// <param name="padIndex">Source pad index.</param>
		public PadSetting GetPadSetting(MapTo padIndex)
		{
			var padSetting = new PadSetting();
			// Get settings related to PAD.
			var items = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			var props = padSetting.GetType().GetProperties();
			var availableNames = items.Select(x => x.PropertyName);
			var requiredNames = props.Where(x => x.PropertyType == typeof(string)).Select(x => x.Name);
			var missing = requiredNames.Except(availableNames).Except(new string[] { "ButtonBig" });
			if (missing.Count() > 0)
			{
				var list = string.Join(", ", missing);
				MessageBox.Show("'PadSetting' class property names must match 'SettingName' class property names. Plese make sure that these properties exists in 'SettingName' class:\r\n\r\n" + list);
				return null;
			}
			foreach (var item in items)
			{
				string key = item.IniPath.Split('\\')[1];
				var prop = props.FirstOrDefault(x => x.Name == item.PropertyName);
				// If property was not found then...
				if (prop == null)
				{
					//var message = string.Format("ReadPadSettings: Property '{0}' was not found", item.PropertyName);
					//MessageBoxForm.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					continue;
				}
				// Get setting value.
				var v = GetSettingValue(item.Control);
				prop.SetValue(padSetting, v ?? "", null);
			}
			padSetting.PadSettingChecksum = padSetting.GetCheckSum();
			return padSetting;
		}

		/// <summary>
		/// Load PAD settings to form.
		/// </summary>
		/// <param name="padSetting">Settings to read.</param>
		/// <param name="padIndex">Destination pad index.</param>
		public void LoadPadSettings(MapTo padIndex, PadSetting padSetting)
		{
			if (padSetting == null) padSetting = new PadSetting();
			// Get settings related to PAD.
			var items = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			var props = padSetting.GetType().GetProperties();
			foreach (var item in items)
			{
				string key = item.IniPath.Split('\\')[1];
				var prop = props.FirstOrDefault(x => x.Name == item.PropertyName);
				// If property was not found then...
				if (prop == null)
				{
					//var message = string.Format("ReadPadSettings: Property '{0}' was not found", item.PropertyName);
					//MessageBoxForm.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					continue;
				}
				var v = (string)prop.GetValue(padSetting, null) ?? "";
				LoadSetting(item.Control, key, v);
			}
			loadCount++;
			var ev = ConfigLoaded;
			if (ev != null)
			{
				ev(this, new SettingEventArgs(padSetting.GetType().Name, loadCount));
			}
		}



	}
}
