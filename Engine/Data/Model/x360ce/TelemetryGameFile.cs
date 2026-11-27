using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Game File</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryGameFile", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryGameFile: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Game File Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid TelemetryGameFileId {
			get => _TelemetryGameFileId;
			set => SetProperty(ref _TelemetryGameFileId, value);
		}
		private Guid _TelemetryGameFileId;

		/// <summary>Telemetry Game Id</summary>
		[DataMember]
		public Guid TelemetryGameId {
			get => _TelemetryGameId;
			set => SetProperty(ref _TelemetryGameId, value);
		}
		private Guid _TelemetryGameId;

		/// <summary>Resource Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string ResourceName {
			get => _ResourceName;
			set => SetProperty(ref _ResourceName, value);
		}
		private string _ResourceName;

		/// <summary>Target Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string TargetPath {
			get => _TargetPath;
			set => SetProperty(ref _TargetPath, value);
		}
		private string _TargetPath;

	}
}
