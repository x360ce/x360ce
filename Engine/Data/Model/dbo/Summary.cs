using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Summaries</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_Summaries")]
	[DataContract(IsReference = true)]
	public partial class Summary: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Summary Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid SummaryId {
			get => _SummaryId;
			set => SetProperty(ref _SummaryId, value);
		}
		private Guid _SummaryId;

		/// <summary>Users</summary>
		[DataMember]
		public int Users {
			get => _Users;
			set => SetProperty(ref _Users, value);
		}
		private int _Users;

		/// <summary>Pad Setting Checksum</summary>
		[DataMember]
		public Guid PadSettingChecksum {
			get => _PadSettingChecksum;
			set => SetProperty(ref _PadSettingChecksum, value);
		}
		private Guid _PadSettingChecksum;

		/// <summary>Product Guid</summary>
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

		/// <summary>File Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string FileName {
			get => _FileName;
			set => SetProperty(ref _FileName, value);
		}
		private string _FileName;

		/// <summary>File Product Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string FileProductName {
			get => _FileProductName;
			set => SetProperty(ref _FileProductName, value);
		}
		private string _FileProductName;

		/// <summary>Date Created</summary>
		[DataMember]
		public DateTime DateCreated {
			get => _DateCreated;
			set => SetProperty(ref _DateCreated, value);
		}
		private DateTime _DateCreated;

		/// <summary>Date Updated</summary>
		[DataMember]
		public DateTime DateUpdated {
			get => _DateUpdated;
			set => SetProperty(ref _DateUpdated, value);
		}
		private DateTime _DateUpdated;

		/// <summary>Weight</summary>
		[DataMember]
		public int Weight {
			get => _Weight;
			set => SetProperty(ref _Weight, value);
		}
		private int _Weight;

	}
}
