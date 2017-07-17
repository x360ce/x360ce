using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace x360ce.Engine
{
	public class XInputMaskFileInfo
	{

		[XmlAttribute]
		public XInputMask Mask { get; set; }

		[XmlAttribute]
		public DateTime Modified { get; set; }

		[XmlAttribute]
		public long Size { get; set; }

		[XmlAttribute]
		public string FullName { get; set; }

	}
}
