using System.ComponentModel;

namespace JocysCom.ClassLibrary.Configuration
{

	/// <summary>
	/// Defines an interface for a settings item, including properties for enabled state and emptiness check.
	/// </summary>
	public interface ISettingsItem : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether this settings item is enabled.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Gets a value indicating whether this settings item is empty or uninitialized.
		/// </summary>
		bool IsEmpty { get; }
	}
}
