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

        public object Get(object key)
        {
            foreach (var item in this)
            {
                if (Equals(item.Key, key))
                {
                    return item.Value;
                }
            }
            return null;
        }

    }
}
