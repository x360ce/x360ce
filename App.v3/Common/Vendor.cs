using System;
using System.Xml.Serialization;

namespace x360ce.App.Common
{
	[Serializable]
	public class Vendor
	{
		[XmlAttribute()]
		public int VendorId { get; set; }
		[XmlAttribute()]
		public string VendorName { get; set; }
		[XmlAttribute()]
		public string ShortName { get; set; }
		[XmlAttribute()]
		public string WebSite { get; set; }
	}
}
