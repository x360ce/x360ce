using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary.Data
{
	/// <summary>
	/// Base class providing property change notification compatible with both INotifyPropertyChanging and INotifyPropertyChanged interfaces.
	/// Enables two-phase change notifications for WPF/MVVM binding scenarios, allowing views and other observers to track updates to model or viewmodel properties.
	/// This pattern is widely adopted throughout the codebase for data binding and UI synchronization.
	/// </summary>
	public class BindableItem : INotifyPropertyChanging, INotifyPropertyChanged
	{
		/// <summary>
		/// Occurs before a property value changes. Used by data binding and observers to react to impending updates.
		/// </summary>
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Raises the PropertyChanging event for the specified property.
		/// </summary>
		/// <param name="propertyName">Name of the property. CallerMemberName is supplied automatically.</param>
		protected void OnPropertyChanging([CallerMemberName] string propertyName = null)
			=> PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

		/// <summary>
		/// Occurs after a property value has changed, signaling observers that the property was updated.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises the PropertyChanged event for the specified property.
		/// </summary>
		/// <param name="propertyName">Name of the property. CallerMemberName is supplied automatically.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		/// <summary>
		/// Sets the value of a backing field and raises property change notifications (both PropertyChanging and PropertyChanged).
		/// Skips notification if the value is unchanged. This utility simplifies implementation of observable properties in derived classes.
		/// </summary>
		/// <typeparam name="T">Type of the backing field and value.</typeparam>
		/// <param name="field">Reference to the backing field.</param>
		/// <param name="value">New value to assign.</param>
		/// <param name="propertyName">Name of the property. CallerMemberName is supplied automatically.</param>
		protected void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return;
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
