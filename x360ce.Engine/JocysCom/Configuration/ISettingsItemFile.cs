using System;

namespace JocysCom.ClassLibrary.Configuration
{
	public interface ISettingsItemFile
	{
		/// <summary>
		/// Map to property which contains base file name.
		/// </summary>
		string BaseName { get; set; }

		/// <summary>
		/// Map to property which contains last time item was modified.
		/// Update on INotifyPropertyChanged.
		/// </summary>
		DateTime WriteTime { get; set; }

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

