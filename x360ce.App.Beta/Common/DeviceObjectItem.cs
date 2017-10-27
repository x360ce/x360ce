using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace x360ce.App
{
	public class DeviceObjectItem
	{
		public string Name { get; set; }
		public int Offset { get; set; }
		public string OffsetName { get; set; }
		public int Instance { get; set; }
		public short Usage { get; set; }
		public ObjectAspect Aspect { get; set; }
		public DeviceObjectTypeFlags Flags { get; set; }
		public Guid GuidValue { get; set; }
		public string GuidName { get; set; }
	}
}
