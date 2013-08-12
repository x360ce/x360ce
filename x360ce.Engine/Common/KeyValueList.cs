using System;
using System.Collections.Generic;

namespace x360ce.Engine
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
