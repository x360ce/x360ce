using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.ComponentModel;

namespace x360ce.App
{
	[Serializable]
	public class SettingsFile
	{

		BindingList<com.x360ce.localhost.Program> _Programs;
		BindingList<com.x360ce.localhost.PadSetting> _Pads;

		public SettingsFile(){
			_Programs = new BindingList<com.x360ce.localhost.Program>();
			_Pads = new BindingList<com.x360ce.localhost.PadSetting>();
		}

		static SettingsFile _current;
		public static SettingsFile Current
		{
			get { return _current = _current ?? new SettingsFile(); }
		}

		public BindingList<com.x360ce.localhost.Program> Programs { get { return _Programs; } }
		public BindingList<com.x360ce.localhost.PadSetting> Pads { get { return _Pads; } }

		const string FileName = "x360ce.xml";

		public void Save()
		{
			Helper.SerializeToXmlFile(this, FileName, System.Text.Encoding.UTF8);
		}

		public void Load()
		{
			if (!System.IO.File.Exists(FileName)){
				//Save();
				return;
			}
			var data = (SettingsFile)Helper.DeserializeFromXmlFile(FileName, typeof(SettingsFile));
			if (data == null) return;
			Programs.Clear();
			if (data.Programs != null) for (int i = 0; i < data.Programs.Count; i++) Programs.Add(data.Programs[i]);
			Pads.Clear();
			if (data.Pads != null) for (int i = 0; i < data.Pads.Count; i++) Pads.Add(data.Pads[i]);
		}

	}
}
