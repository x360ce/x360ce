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

        BindingList<x360ce.Engine.Data.Program> _Programs;
        BindingList<x360ce.Engine.Data.PadSetting> _Pads;

		public SettingsFile(){
            _Programs = new BindingList<x360ce.Engine.Data.Program>();
            _Pads = new BindingList<x360ce.Engine.Data.PadSetting>();
		}

		static SettingsFile _current;
		public static SettingsFile Current
		{
			get { return _current = _current ?? new SettingsFile(); }
		}

		public BindingList<x360ce.Engine.Data.Program> Programs { get { return _Programs; } }
        public BindingList<x360ce.Engine.Data.PadSetting> Pads { get { return _Pads; } }

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
