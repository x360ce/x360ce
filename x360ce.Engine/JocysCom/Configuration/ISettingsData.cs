using System;
using System.ComponentModel;
using System.IO;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Defines an interface for managing settings data, with methods for persistence, defaults reset, and file operations.
	/// </summary>
	public interface ISettingsData
	{
		/// <summary>
		/// Resets the settings data to default values.
		/// </summary>
		/// <returns>True if operation was successful; otherwise, False.</returns>
		bool ResetToDefault();

		/// <summary>
		/// Saves the current settings data to the default file location.
		/// </summary>
		void Save(object[] items = null);

		/// <summary>
		/// Saves the current settings data to a specified file.
		/// </summary>
		/// <param name="fileName">The file name/path where the settings will be saved.</param>
		void SaveAs(string fileName, object[] items = null);

		/// <summary>
		/// Loads the settings data from the default file location.
		/// </summary>
		void Load();

		/// <summary>
		/// Loads the settings data from a specified file.
		/// </summary>
		/// <param name="fileName">The file name/path from which to load settings.</param>
		void LoadFrom(string fileName);

		/// <summary>
		/// Gets the FileInfo object for the XML file that stores the settings data.
		/// </summary>
		FileInfo XmlFile { get; }

		/// <summary>
		/// Gets the collection of settings items.
		/// </summary>
		IBindingList Items { get; }

		/// <summary>
		/// Occurs when a file associated with the settings data changes.
		/// </summary>
		event EventHandler FilesChanged;

		/// <summary>
		/// Gets or sets a value indicating whether there are pending save operations for the settings data.
		/// </summary>
		bool IsSavePending { get; set; }

		/// <summary>
		/// Deletes a specified settings item file and updates the settings data accordingly.
		/// </summary>
		/// <param name="itemFile">The settings item file to delete.</param>
		/// <returns>A message indicating the result of the delete operation.</returns>
		string DeleteItem(ISettingsFileItem itemFile);

		/// <summary>
		/// Renames a specified settings item file and updates the settings data and file system.
		/// </summary>
		/// <param name="itemFile">The settings item file to rename.</param>
		/// <param name="newName">The new name for the settings item file.</param>
		/// <returns>A message indicating the outcome of the rename operation.</returns>
		string RenameItem(ISettingsFileItem itemFile, string newName);
	}
}
