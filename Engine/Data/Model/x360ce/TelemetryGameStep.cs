using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Game Step</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryGameStep", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryGameStep: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Game Step Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid TelemetryGameStepId {
			get => _TelemetryGameStepId;
			set => SetProperty(ref _TelemetryGameStepId, value);
		}
		private Guid _TelemetryGameStepId;

		/// <summary>Telemetry Game Id</summary>
		[DataMember]
		public Guid TelemetryGameId {
			get => _TelemetryGameId;
			set => SetProperty(ref _TelemetryGameId, value);
		}
		private Guid _TelemetryGameId;

		/// <summary>Step Order</summary>
		[DataMember]
		public int StepOrder {
			get => _StepOrder;
			set => SetProperty(ref _StepOrder, value);
		}
		private int _StepOrder;

		/// <summary>Is Warning</summary>
		[DataMember]
		public bool IsWarning {
			get => _IsWarning;
			set => SetProperty(ref _IsWarning, value);
		}
		private bool _IsWarning;

		/// <summary>Text</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1024)]
		public string Text {
			get => _Text;
			set => SetProperty(ref _Text, value);
		}
		private string _Text;

	}
}
