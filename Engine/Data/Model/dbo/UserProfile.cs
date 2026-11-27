using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce User Profiles</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_UserProfiles")]
	[DataContract(IsReference = true)]
	public partial class UserProfile: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid Id {
			get => _Id;
			set => SetProperty(ref _Id, value);
		}
		private Guid _Id;

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

		/// <summary>Profile Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string ProfilePath {
			get => _ProfilePath;
			set => SetProperty(ref _ProfilePath, value);
		}
		private string _ProfilePath;

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
