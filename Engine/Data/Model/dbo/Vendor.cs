using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Vendors</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_Vendors")]
	[DataContract(IsReference = true)]
	public partial class Vendor: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Vendor Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public int VendorId {
			get => _VendorId;
			set => SetProperty(ref _VendorId, value);
		}
		private int _VendorId;

		/// <summary>Vendor Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string VendorName {
			get => _VendorName;
			set => SetProperty(ref _VendorName, value);
		}
		private string _VendorName;

		/// <summary>Short Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ShortName {
			get => _ShortName;
			set => SetProperty(ref _ShortName, value);
		}
		private string _ShortName;

		/// <summary>Web Site</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string WebSite {
			get => _WebSite;
			set => SetProperty(ref _WebSite, value);
		}
		private string _WebSite;

	}
}
