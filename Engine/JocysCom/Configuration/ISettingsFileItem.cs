using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>File-based ISettingsItem: exposes Path, Name, BaseName, WriteTime, and IsReadOnlyFile.</summary>
	public interface ISettingsFileItem : ISettingsItem
	{
		/// <summary>Relative directory path of the settings file.</summary>
		[DefaultValue(null)]
		string Path { get; set; }

		/// <summary>File name of the settings file without extension.</summary>
		[DefaultValue(null)]
		string Name { get; set; }

		/// <summary>Internal base name (without extension) for identifying the settings file.</summary>
		[DefaultValue(null)]
		string BaseName { get; set; }

		/// <summary>Timestamp of the last write to the settings file, for change tracking.</summary>
		DateTime WriteTime { get; set; }

		/// <summary>Excludes this item from being saved to a separate file when true.</summary>
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