using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Game</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryGame", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryGame: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Game Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid TelemetryGameId {
			get => _TelemetryGameId;
			set => SetProperty(ref _TelemetryGameId, value);
		}
		private Guid _TelemetryGameId;

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

		/// <summary>Game Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string GameName {
			get => _GameName;
			set => SetProperty(ref _GameName, value);
		}
		private string _GameName;

		/// <summary>Telemetry Format Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string TelemetryFormatId {
			get => _TelemetryFormatId;
			set => SetProperty(ref _TelemetryFormatId, value);
		}
		private string _TelemetryFormatId;

		/// <summary>Edition</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(64)]
		public string Edition {
			get => _Edition;
			set => SetProperty(ref _Edition, value);
		}
		private string _Edition;

		/// <summary>Launch Arguments</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string LaunchArguments {
			get => _LaunchArguments;
			set => SetProperty(ref _LaunchArguments, value);
		}
		private string _LaunchArguments;

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
