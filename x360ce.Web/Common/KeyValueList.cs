using System;
using System.Collections.Generic;

namespace x360ce.Web
{
	[Serializable]
	public class KeyValueList : List<KeyValue>
	{
		public KeyValueList()
		{
		}

		public void Add(object key, object value)
		{
		    this.Add(new KeyValue(key, value));
		}

	}
}
