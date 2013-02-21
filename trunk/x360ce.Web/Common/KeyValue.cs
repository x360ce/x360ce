using System;

namespace x360ce.Web
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
