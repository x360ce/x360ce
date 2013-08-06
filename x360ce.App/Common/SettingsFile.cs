using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;

namespace x360ce.App
{
	[Serializable]
	public class SettingsFile
	{

		static SettingsFile _current;
		public static SettingsFile Current
		{
			get { return _current = _current ?? new SettingsFile(); }
		}

		List<com.x360ce.localhost.Program> Programs { get; set; }
		List<com.x360ce.localhost.PadSetting> Pads { get; set; }

		public void Save()
		{
			Helper.SerializeToXmlFile(Current, "x360ce.xml", System.Text.Encoding.UTF8);
		}

		public void Load()
		{
			_current = (SettingsFile)Helper.DeserializeFromXmlFile("x360ce.xml", typeof(SettingsFile));
		}

	}
}
