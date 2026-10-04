using System.ComponentModel;

namespace JocysCom.ClassLibrary.Configuration
{
	
	/// <summary>Defines a settings item that notifies on property changes and indicates whether it is enabled or empty.</summary>
	public interface ISettingsItem : INotifyPropertyChanged
	{
		/// <summary>Indicates whether the item is enabled. Implementers must raise PropertyChanged when this value changes.</summary>
		bool IsEnabled { get; set; }

		/// <summary>Indicates whether the item is uninitialized or contains no data.</summary>
		bool IsEmpty { get; }
	}
}