namespace JocysCom.ClassLibrary.Collections
{
	/// <summary>
	/// Defines a generic contract for a key–value pair.
	/// </summary>
	/// <remarks>
	/// Used throughout the JocysCom.ClassLibrary.Collections namespace.
	/// Concretely implemented by the KeyValue<TKey, TValue> class (and its string specialization) in Core/Collections/KeyValue.cs.
	/// Surfaced to end users via the KeyValueUserControl in Core/Controls/KeyValueUserControl.xaml.cs, which binds, displays, and allows editing of collections of IKeyValue items.
	/// </remarks>
	public interface IKeyValue<TKey, TValue>
	{
		TKey Key { get; set; }
		TValue Value { get; set; }

	}
}
