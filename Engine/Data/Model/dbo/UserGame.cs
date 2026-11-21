using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce User Games</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_UserGames")]
	[DataContract(IsReference = true)]
	public partial class UserGame: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Game Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid GameId {
			get => _GameId;
			set => SetProperty(ref _GameId, value);
		}
		private Guid _GameId;

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

		/// <summary>File Version</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string FileVersion {
			get => _FileVersion;
			set => SetProperty(ref _FileVersion, value);
		}
		private string _FileVersion;

		/// <summary>Full Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string FullPath {
			get => _FullPath;
			set => SetProperty(ref _FullPath, value);
		}
		private string _FullPath;

		/// <summary>Enable Mask</summary>
		[DataMember]
		public int EnableMask {
			get => _EnableMask;
			set => SetProperty(ref _EnableMask, value);
		}
		private int _EnableMask;

		/// <summary>Company Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string CompanyName {
			get => _CompanyName;
			set => SetProperty(ref _CompanyName, value);
		}
		private string _CompanyName;

		/// <summary>Processor Architecture</summary>
		[DataMember]
		public int ProcessorArchitecture {
			get => _ProcessorArchitecture;
			set => SetProperty(ref _ProcessorArchitecture, value);
		}
		private int _ProcessorArchitecture;

		/// <summary>Emulation Type</summary>
		[DataMember]
		public int EmulationType {
			get => _EmulationType;
			set => SetProperty(ref _EmulationType, value);
		}
		private int _EmulationType;

		/// <summary>Auto Map Mask</summary>
		[DataMember]
		public int AutoMapMask {
			get => _AutoMapMask;
			set => SetProperty(ref _AutoMapMask, value);
		}
		private int _AutoMapMask;

		/// <summary>Hook Mask</summary>
		[DataMember]
		public int HookMask {
			get => _HookMask;
			set => SetProperty(ref _HookMask, value);
		}
		private int _HookMask;

		/// <summary>X Input Mask</summary>
		[DataMember]
		public int XInputMask {
			get => _XInputMask;
			set => SetProperty(ref _XInputMask, value);
		}
		private int _XInputMask;

		/// <summary>X Input Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string XInputPath {
			get => _XInputPath;
			set => SetProperty(ref _XInputPath, value);
		}
		private string _XInputPath;

		/// <summary>D Input Mask</summary>
		[DataMember]
		public int DInputMask {
			get => _DInputMask;
			set => SetProperty(ref _DInputMask, value);
		}
		private int _DInputMask;

		/// <summary>D Input File</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string DInputFile {
			get => _DInputFile;
			set => SetProperty(ref _DInputFile, value);
		}
		private string _DInputFile;

		/// <summary>FakeVID</summary>
		[DataMember]
		public int FakeVID {
			get => _FakeVID;
			set => SetProperty(ref _FakeVID, value);
		}
		private int _FakeVID;

		/// <summary>FakePID</summary>
		[DataMember]
		public int FakePID {
			get => _FakePID;
			set => SetProperty(ref _FakePID, value);
		}
		private int _FakePID;

		/// <summary>Timeout</summary>
		[DataMember]
		public int Timeout {
			get => _Timeout;
			set => SetProperty(ref _Timeout, value);
		}
		private int _Timeout;

		/// <summary>Weight</summary>
		[DataMember]
		public int Weight {
			get => _Weight;
			set => SetProperty(ref _Weight, value);
		}
		private int _Weight;

		/// <summary>Checksum</summary>
		[DataMember]
		public Guid Checksum {
			get => _Checksum;
			set => SetProperty(ref _Checksum, value);
		}
		private Guid _Checksum;

		/// <summary>Comment</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1024)]
		public string Comment {
			get => _Comment;
			set => SetProperty(ref _Comment, value);
		}
		private string _Comment;

		/// <summary>Is Enabled</summary>
		[DataMember]
		public bool IsEnabled {
			get => _IsEnabled;
			set => SetProperty(ref _IsEnabled, value);
		}
		private bool _IsEnabled;

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

	}
}
