using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce User Computers</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_UserComputers")]
	[DataContract(IsReference = true)]
	public partial class UserComputer: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid Id {
			get => _Id;
			set => SetProperty(ref _Id, value);
		}
		private Guid _Id;

		/// <summary>Application Id</summary>
		[DataMember]
		public Guid ApplicationId {
			get => _ApplicationId;
			set => SetProperty(ref _ApplicationId, value);
		}
		private Guid _ApplicationId;

		/// <summary>User Id</summary>
		[DataMember]
		public Guid UserId {
			get => _UserId;
			set => SetProperty(ref _UserId, value);
		}
		private Guid _UserId;

		/// <summary>Computer Id</summary>
		[DataMember]
		public Guid ComputerId {
			get => _ComputerId;
			set => SetProperty(ref _ComputerId, value);
		}
		private Guid _ComputerId;

		/// <summary>Computer Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string ComputerName {
			get => _ComputerName;
			set => SetProperty(ref _ComputerName, value);
		}
		private string _ComputerName;

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

		/// <summary>Checksum</summary>
		[DataMember]
		public Guid Checksum {
			get => _Checksum;
			set => SetProperty(ref _Checksum, value);
		}
		private Guid _Checksum;

	}
}
