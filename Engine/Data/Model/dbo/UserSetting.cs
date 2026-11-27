using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Settings</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_Settings")]
	[DataContract(IsReference = true)]
	public partial class UserSetting: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Setting Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid SettingId {
			get => _SettingId;
			set => SetProperty(ref _SettingId, value);
		}
		private Guid _SettingId;

		/// <summary>Instance Guid</summary>
		[DataMember]
		public Guid InstanceGuid {
			get => _InstanceGuid;
			set => SetProperty(ref _InstanceGuid, value);
		}
		private Guid _InstanceGuid;

		/// <summary>Instance Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string InstanceName {
			get => _InstanceName;
			set => SetProperty(ref _InstanceName, value);
		}
		private string _InstanceName;

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

		/// <summary>Device Type</summary>
		[DataMember]
		public int DeviceType {
			get => _DeviceType;
			set => SetProperty(ref _DeviceType, value);
		}
		private int _DeviceType;

		/// <summary>Map To</summary>
		[DataMember]
		public int MapTo {
			get => _MapTo;
			set => SetProperty(ref _MapTo, value);
		}
		private int _MapTo;

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

		/// <summary>Comment</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1024)]
		public string Comment {
			get => _Comment;
			set => SetProperty(ref _Comment, value);
		}
		private string _Comment;

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

		/// <summary>Date Selected</summary>
		[DataMember]
		public DateTime DateSelected {
			get => _DateSelected;
			set => SetProperty(ref _DateSelected, value);
		}
		private DateTime _DateSelected;

		/// <summary>Is Enabled</summary>
		[DataMember]
		public bool IsEnabled {
			get => _IsEnabled;
			set => SetProperty(ref _IsEnabled, value);
		}
		private bool _IsEnabled;

		/// <summary>Pad Setting Checksum</summary>
		[DataMember]
		public Guid PadSettingChecksum {
			get => _PadSettingChecksum;
			set => SetProperty(ref _PadSettingChecksum, value);
		}
		private Guid _PadSettingChecksum;

		/// <summary>Completion</summary>
		[DataMember]
		public int Completion {
			get => _Completion;
			set => SetProperty(ref _Completion, value);
		}
		private int _Completion;

		/// <summary>Checksum</summary>
		[DataMember]
		public Guid Checksum {
			get => _Checksum;
			set => SetProperty(ref _Checksum, value);
		}
		private Guid _Checksum;

		/// <summary>Computer Id</summary>
		[DataMember]
		public Guid ComputerId {
			get => _ComputerId;
			set => SetProperty(ref _ComputerId, value);
		}
		private Guid _ComputerId;

		/// <summary>Profile Id</summary>
		[DataMember]
		public Guid ProfileId {
			get => _ProfileId;
			set => SetProperty(ref _ProfileId, value);
		}
		private Guid _ProfileId;

	}
}
