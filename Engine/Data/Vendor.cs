using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;

namespace x360ce.Engine.Data
{

	[MetadataType(typeof(VendorMetadata))]
	public partial class Vendor
	{
	}

	/// <summary>
	/// This is the Metadata for Vendor class.
	/// </summary>
	public interface VendorMetadata
	{
		[XmlAttribute()]
		object VendorId { get; set; }
	}

}