using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Collections
{

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue : KeyValue<string, string>, IKeyValue<string, string>
	{
		public KeyValue() { }

		public KeyValue(string key, string value) : base(key, value) { }
	}

	/// <summary>
	/// Generic serializable key/value pair container implementing IKeyValue&lt;TKey, TValue&gt; and INotifyPropertyChanged.
	/// Raises PropertyChanged events on value updates.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValue<TKey, TValue> : IKeyValue<TKey, TValue>, INotifyPropertyChanged
	{

		public KeyValue()
		{
		}

		public KeyValue(TKey key, TValue value)
		{
			_Key = key;
			_Value = value;
		}

		public TKey Key
		{
			get { return _Key; }
			set
			{
				if (Equals(_Key, value))
					return;
				SetProperty(ref _Key, value);
			}
		}
		TKey _Key;

		public TValue Value
		{
			get { return _Value; }
			set
			{
				if (Equals(_Value, value))
					return;
				SetProperty(ref _Value, value);
			}
		}
		TValue _Value;

		public override string ToString()
		{
			return string.Format("[{0},{1}]", Key, Value);
		}

		#region ■ INotifyPropertyChanged

		// SUPPRESS: CWE-502: Deserialization of Untrusted Data
		// Fix: Apply [field: NonSerialized] attribute to an event inside class with [Serialized] attribute.
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			property = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}

}
