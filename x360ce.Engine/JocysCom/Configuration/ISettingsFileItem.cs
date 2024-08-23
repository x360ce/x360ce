using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Defines an interface for a settings item that represents or interacts with a file. Includes properties for managing file names and tracking write timestamps.
	/// </summary>
	public interface ISettingsFileItem : ISettingsItem
	{
		/// <summary>
		/// Parent Directory of the file associated with this settings item.
		/// This represents the actual relative directory path of the file on the file system.
		/// </summary>
		[DefaultValue(null)]
		string Path { get; set; }

		/// <summary>
		/// File name associated with this settings item.
		/// This represents the actual name of the file on the file system without extension.
		/// </summary>
		[DefaultValue(null)]
		string Name { get; set; }

		/// <summary>
		/// Base name of the file without extension, used for internal identification of the file within the settings management context.
		/// </summary>
		[DefaultValue(null)]
		string BaseName { get; set; }

		/// <summary>
		/// Timestamp indicating the last time the file was written to.
		/// This is used for tracking changes to the file over time.
		/// </summary>
		DateTime WriteTime { get; set; }

		/// <summary>
		/// Indicate that item should not be saved into a separate file.
		/// </summary>
		[XmlIgnore, DefaultValue(false)]
		bool IsReadOnlyFile { get; set; }

		/* Example:
 
		#region ■ ISettingsItemFile

		[XmlIgnore]
		string ISettingsItemFile.BaseName { get => Name; set => Name = value; }

		[XmlIgnore]
		DateTime ISettingsItemFile.WriteTime { get; set; }

		#endregion

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			((ISettingsItemFile)this).WriteTime = DateTime.Now;
		}

		*/

	}
}

