using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Products</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_Products")]
	[DataContract(IsReference = true)]
	public partial class Product: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Product Guid (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid ProductGuid {
			get => _ProductGuid;
			set => SetProperty(ref _ProductGuid, value);
		}
		private Guid _ProductGuid;

		/// <summary>Product Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string ProductName {
			get => _ProductName;
			set => SetProperty(ref _ProductName, value);
		}
		private string _ProductName;

		/// <summary>Instance Count</summary>
		[DataMember]
		public int InstanceCount {
			get => _InstanceCount;
			set => SetProperty(ref _InstanceCount, value);
		}
		private int _InstanceCount;

		/// <summary>Vendor Id</summary>
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		[DataMember]
		public int VendorId {
			get => _VendorId;
			set => SetProperty(ref _VendorId, value);
		}
		private int _VendorId;

		/// <summary>Product Id</summary>
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		[DataMember]
		public int ProductId {
			get => _ProductId;
			set => SetProperty(ref _ProductId, value);
		}
		private int _ProductId;

	}
}
