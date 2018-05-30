using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using System.Linq;

namespace x360ce.Engine
{
	public class DeviceObjectItem
	{

		public DeviceObjectItem()
		{
		}

		public DeviceObjectItem(int offset, Guid type, ObjectAspect aspect, DeviceObjectTypeFlags flags, int instance, string name)
		{
			Offset = offset;
			Type = type;
			Aspect = aspect;
			Flags = flags;
			Instance = instance;
			Name = name;
		}

		public string Name { get; set; }

        /// <summary>
        /// Important note: 
        /// This Offset is in the native data format of the device. The native data format corresponds to the raw device data.
        /// This member does not correspond to the device constant, such as DIJOFS_BUTTON0 (JoystickOffset.Buttons0), for this object.
        /// </summary>
        public int Offset { get; set; }
        public int ObjectId { get; set; }
        public int Instance { get; set; }

		// Zero based index.
		public int DiIndex { get; set; }

		public ObjectAspect Aspect { get; set; }
		public DeviceObjectTypeFlags Flags { get; set; }
		public Guid Type { get; set; }

        public string AspectName
		{
			get
			{
				var s = string.Format("{0}", Aspect);
				return s == "0" ? "" : s;
			}
		}

		static Dictionary<Guid, string> TypeNames;
		static object TypeNamesLock = new object();

		public string TypeName
		{
			get
			{

				lock (TypeNamesLock)
				{
					if (TypeNames == null)
					{
						TypeNames = new Dictionary<Guid, string>();
						var og = typeof(SharpDX.DirectInput.ObjectGuid);
						var fields = og.GetFields().Where(x => x.FieldType == typeof(Guid));
						foreach (var field in fields)
						{
							TypeNames.Add((Guid)field.GetValue(og), field.Name);
						}
					}
					// Return type name.
					return TypeNames.ContainsKey(Type) ? TypeNames[Type] : Type.ToString();
				}
			}
		}

	}
}
