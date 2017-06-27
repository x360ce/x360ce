using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Collections
{

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue : KeyValue<string>
	{
		public KeyValue() { }

		public KeyValue(string key, string value) : base(key, value) { }
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue<T> : INotifyPropertyChanged
	{

		public KeyValue()
		{
		}

		public KeyValue(T key, T value)
		{
			_key = key;
			_value = value;
		}

		T _key;
		T _value;
		public T Key
		{
			get { return _key; }
			set
			{
				if (!IEquatable<T>.Equals(_key, value))
				{
					_key = value;
					NotifyPropertyChanged("Key");
				}
			}
		}
		public T Value
		{
			get { return _value; }
			set
			{
				if (!IEquatable<T>.Equals(_value, value))
				{
					_value = value;
					NotifyPropertyChanged("Value");
				}
			}
		}

		public override string ToString()
		{
			return string.Format("[{0},{1}]", Key, Value);
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion


	}


	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue<K, V> : INotifyPropertyChanged
	{

		public KeyValue()
		{
		}

		public KeyValue(K key, V value)
		{
			_key = key;
			_value = value;
		}

		K _key;
		V _value;
		public K Key
		{
			get { return _key; }
			set
			{
				if (!IEquatable<K>.Equals(_key, value))
				{
					_key = value;
					NotifyPropertyChanged("Key");
				}
			}
		}
		public V Value
		{
			get { return _value; }
			set
			{
				if (!IEquatable<V>.Equals(_value, value))
				{
					_value = value;
					NotifyPropertyChanged("Value");
				}
			}
		}

		public override string ToString()
		{
			return string.Format("[{0},{1}]", Key, Value);
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName = "")
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion


	}



}
