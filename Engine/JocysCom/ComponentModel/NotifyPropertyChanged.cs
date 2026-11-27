#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.ComponentModel
{
	/// <summary>
	/// INotifyPropertyChanged base with optional marshalling of property-change notifications to the
	/// <see cref="SynchronizationContext"/> of the thread that created the object, such as the UI thread.
	/// </summary>
	/// <remarks>
	/// Supports MVVM data binding scenarios, with optional UI-thread invocation of property change events.
	/// </remarks>
	public class NotifyPropertyChanged : INotifyPropertyChanged
	{

		#region ■ INotifyPropertyChanged

		/// <summary>
		/// The synchronization context of the thread that created the object; null when that thread has none.
		/// </summary>
		private readonly SynchronizationContext _ctx = SynchronizationContext.Current;

		/// <summary>
		/// Notifies clients that a property value has changed.
		/// </summary>
		// SUPPRESS: CWE-502: Deserialization of Untrusted Data
		// Fix: Apply [field: NonSerialized] attribute to an event inside class with [Serialized] attribute.
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises the PropertyChanged event. When UseApplicationDispatcher is true and the object was created on a thread with a
		/// synchronization context, a call from any other context is posted to the captured context.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (UseApplicationDispatcher && _ctx != null)
			{
				// If already on different thread then...
				if (SynchronizationContext.Current != _ctx)
				{
					_ctx.Post(state => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((string)state)), propertyName);
					return;
				}
			}
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// When true, posts property-change notifications raised on another thread to the synchronization context of the thread
		/// that created the object, such as the UI thread. Has no effect when that thread had no synchronization context.
		/// Defaults to false. Not serialized.
		/// </summary>
		[field: NonSerialized, XmlIgnore]
		public bool UseApplicationDispatcher = false;

		/// <summary>
		/// Sets the backing field if the new value differs (per <see cref="EqualityComparer{T}.Default"/>), then invokes
		/// OnPropertyChanged. Skips notifications when value is unchanged.
		/// </summary>
		/// <typeparam name="T">Type of the backing field.</typeparam>
		/// <param name="property">Reference to the backing field.</param>
		/// <param name="value">New value to assign.</param>
		/// <param name="propertyName">Name of the property; supplied by CallerMemberName automatically.</param>
		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(property, value))
				return;
			property = value;
			// Invoke overridden OnPropertyChanged method in the most derived class of the object.
			OnPropertyChanged(propertyName);
		}


		#endregion
	}
}
