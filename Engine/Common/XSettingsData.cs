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
			_FileSuffix = fileSuffix;
			// A load replaces the list with what the file holds, as validated.
			ClearWhenLoading = true;
			// Two copies of the program may save the same file. Each save is kept, rather than one
			// copy silently stopping its saves once the other has written.
			PreventWriteToNewerFiles = false;
			Rebase();
		}

		/// <summary>The part of the file name that says which settings these are.</summary>
		[NonSerialized]
		string _FileSuffix;

		/// <summary>
		/// Works the file name out again from wherever settings are kept now.
		/// </summary>
		/// <remarks>
		/// The folder is chosen once at startup, but it can change while the program is
		/// running: a save that fails because the file belongs to another Windows
		/// account is answered by moving to a folder this user owns. Without this the
		/// move would appear to work while every later save went on writing to the file
		/// that could not be written.
		/// </remarks>
		public void Rebase()
		{
			if (string.IsNullOrEmpty(_FileSuffix))
				return;
			var path = string.Format("{0}\\Settings\\x360ce.{1}", EngineHelper.AppDataPath, _FileSuffix);
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
