using System;
using System.IO;
using System.Xml.Serialization;
using JocysCom.ClassLibrary.ComponentModel;

namespace x360ce.Engine
{
	[Serializable, XmlRoot("Data")]
	public class XSettingsData<T> : JocysCom.ClassLibrary.Configuration.SettingsData<T>
	{

		public XSettingsData()
		{
		}

		public XSettingsData(string fileSuffix, string comment = null)
		{
			Items = new SortableBindingList<T>();
			_Comment = comment;
			var path = string.Format("{0}\\Settings\\x360ce.{2}", EngineHelper.AppDataPath, _CurrentVersion, fileSuffix);
			_XmlFile = new FileInfo(path);
		}

		/// <summary>
		/// File Version.
		/// </summary>
		[NonSerialized]
		int _CurrentVersion = 4;

		public bool IsValidVersion()
		{
			return Version == _CurrentVersion;
		}

	}
}
