using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary.Data
{
	public class BindableItem : INotifyPropertyChanging, INotifyPropertyChanged
	{

		public event PropertyChangingEventHandler PropertyChanging;

		protected void OnPropertyChanging([CallerMemberName] string propertyName = null)
		 => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		 => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
