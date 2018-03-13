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
		public int Offset { get; set; }
		public int Instance { get; set; }
		public ObjectAspect Aspect { get; set; }
		public DeviceObjectTypeFlags Flags { get; set; }
		public Guid Type { get; set; }

		public DeviceType OffsetType { get; set; }

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

		public string OffsetName
		{
			get
			{
				switch (OffsetType)
				{
					case DeviceType.Mouse: return ((MouseOffset)Offset).ToString();
					case DeviceType.Keyboard: return string.Format("Buttons{0}", Offset - 1);
					case DeviceType.Joystick: return ((JoystickOffset)Offset).ToString();
					case DeviceType.Gamepad:
						return Flags.HasFlag(DeviceObjectTypeFlags.NoData)
								? "" : ((GamepadOffset)Offset).ToString();

					default: return "";
				}

			}
		}

	}
}
