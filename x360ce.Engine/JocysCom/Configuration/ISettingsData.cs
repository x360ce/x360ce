using System;
using System.ComponentModel;
using System.IO;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>Contract for managing settings data persisted as XML, including reset, load, save, and individual file operations.</summary>
	public interface ISettingsData
	{
		/// <summary>Reset settings data to default values.</summary>
		/// <returns>True if operation was successful; otherwise, False.</returns>
		bool ResetToDefault();

		/// <summary>Save current settings data via XML serialization to the default file location.</summary>
		void Save(object[] items = null);

		/// <summary>Save current settings data via XML serialization to the specified file.</summary>
		/// <param name="fileName">Destination file name or path.</param>
		void SaveAs(string fileName, object[] items = null);

		/// <summary>Load settings data from the default XML file.</summary>
		void Load();

		/// <summary>Load settings data from the specified XML file.</summary>
		/// <param name="fileName">Source file name or path.</param>
		void LoadFrom(string fileName);

		/// <summary>FileInfo for the XML file that stores settings data.</summary>
		FileInfo XmlFile { get; }

		/// <summary>Collection of settings items for data binding.</summary>
		IBindingList Items { get; }

		/// <summary>Raised when a file associated with the settings data changes.</summary>
		event EventHandler FilesChanged;

		/// <summary>Indicates whether there are pending save operations.</summary>
		bool IsSavePending { get; set; }

		/// <summary>Delete the specified settings item file and update settings data accordingly.</summary>
		/// <param name="itemFile">Settings item file to delete.</param>
		/// <returns>A message indicating the result of the delete operation.</returns>
		string DeleteItem(ISettingsFileItem itemFile);

		/// <summary>Rename the specified settings item file and update settings data and file system accordingly.</summary>
		/// <param name="itemFile">Settings item file to rename.</param>
		/// <param name="newName">New name for the settings item file.</param>
		/// <returns>A message indicating the outcome of the rename operation.</returns>
		string RenameItem(ISettingsFileItem itemFile, string newName);
	}
}