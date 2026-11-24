using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Format Spec</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryFormatSpec", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryFormatSpec: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Format Spec Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid TelemetryFormatSpecId {
			get => _TelemetryFormatSpecId;
			set => SetProperty(ref _TelemetryFormatSpecId, value);
		}
		private Guid _TelemetryFormatSpecId;

		/// <summary>Telemetry Format Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string TelemetryFormatId {
			get => _TelemetryFormatId;
			set => SetProperty(ref _TelemetryFormatId, value);
		}
		private string _TelemetryFormatId;

		/// <summary>Spec Order</summary>
		[DataMember]
		public int SpecOrder {
			get => _SpecOrder;
			set => SetProperty(ref _SpecOrder, value);
		}
		private int _SpecOrder;

		/// <summary>Url</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string Url {
			get => _Url;
			set => SetProperty(ref _Url, value);
		}
		private string _Url;

		/// <summary>Version</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string Version {
			get => _Version;
			set => SetProperty(ref _Version, value);
		}
		private string _Version;

		/// <summary>Spec Date</summary>
		[DataMember]
		public DateTime? SpecDate {
			get => _SpecDate;
			set => SetProperty(ref _SpecDate, value);
		}
		private DateTime? _SpecDate;

		/// <summary>Note</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1024)]
		public string Note {
			get => _Note;
			set => SetProperty(ref _Note, value);
		}
		private string _Note;

	}
}
