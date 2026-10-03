using System;

namespace x360ce.Engine
{
	[Serializable]
	public partial class KeyValue
	{

		public KeyValue()
		{
		}

		public KeyValue(object key, object value)
		{
			Key = key;
			Value = value;
		}
		
		public object Key { get; set; }
		public object Value { get; set; }
		
	}
}
