using System;
using System.Collections;
using System.IO;

namespace JocysCom.ClassLibrary.Configuration
{
	public interface ISettingsData
	{
		bool ResetToDefault();
		void Save();
		void SaveAs(string fileName);
		void Load();
		void LoadFrom(string fileName);
		FileInfo XmlFile { get; }
		IList Items { get; }
		event EventHandler FilesChanged;
	}
}
