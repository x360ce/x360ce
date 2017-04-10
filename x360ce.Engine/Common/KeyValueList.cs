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

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            object v = null;
            var found = false;
            foreach (var item in this)
            {
                if (Equals(item.Key, key))
                {
                    v = item.Value;
                    found = true;
                    break;
                }
            }
            // If value not found then...
            if (!found)
            {
                // Return default value.
                return defaultValue;
            }
            if (v == null) return (T)(object)null;
            // // If value is string but non string is wanded then...
            if ((v is string) && typeof(T) != typeof(string))
            {
                // Deserialize.
                v = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<T>((string)v);
            }
            return (T)v;
        }

    }
}
