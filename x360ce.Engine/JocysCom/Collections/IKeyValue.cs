namespace JocysCom.ClassLibrary.Collections
{
	public interface IKeyValue<TKey, TValue>
	{
		TKey Key { get; set; }
		TValue Value { get; set; }

	}
}
