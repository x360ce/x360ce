using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

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

		public KeyValue() { }

		public KeyValue(T key, T value)
		{
			_key = key;
			_value = value;
		}

		[XmlAttribute]
		public T Key
		{
			get { return _key; }
			set
			{
				if (!Equals(_key, value))
				{
					_key = value;
					OnPropertyChanged();
				}
			}
		}
		T _key;

		[XmlAttribute]
		public T Value
		{
			get { return _value; }
			set
			{
				if (!Equals(_value, value))
				{
					_value = value;
					OnPropertyChanged();
				}
			}
		}
		T _value;

		public override string ToString()
		{
			return string.Format("[{0},{1}]", Key, Value);
		}

		#region INotifyPropertyChanged

		// CWE-502: Deserialization of Untrusted Data
		// Fix: Apply [field: NonSerialized] attribute to an event inside class with [Serializable] attribute.
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}


	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue<TKey, TValue> : INotifyPropertyChanged
	{

		public KeyValue()
		{
		}

		public KeyValue(TKey key, TValue value)
		{
			_key = key;
			_value = value;
		}

		TKey _key;
		TValue _value;
		public TKey Key
		{
			get { return _key; }
			set
			{
				if (!Equals(_key, value))
				{
					_key = value;
					OnPropertyChanged();
				}
			}
		}
		public TValue Value
		{
			get { return _value; }
			set
			{
				if (!Equals(_value, value))
				{
					_value = value;
					OnPropertyChanged();
				}
			}
		}

		public override string ToString()
		{
			return string.Format("[{0},{1}]", Key, Value);
		}

		#region INotifyPropertyChanged

		// CWE-502: Deserialization of Untrusted Data
		// Fix: Apply [field: NonSerialized] attribute to an event inside class with [Serialized] attribute.
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}



}
