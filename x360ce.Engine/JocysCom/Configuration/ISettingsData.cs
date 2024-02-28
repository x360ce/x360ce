using System;
using System.ComponentModel;
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
		IBindingList Items { get; }
		event EventHandler FilesChanged;
		bool IsSavePending { get; set; }
		string DeleteItem(ISettingsItemFile itemFile);
		string RenameItem(ISettingsItemFile itemFile, string newName);
	}
}
