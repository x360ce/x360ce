using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Game Mod</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryGameMod", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryGameMod: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Game Mod Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid TelemetryGameModId {
			get => _TelemetryGameModId;
			set => SetProperty(ref _TelemetryGameModId, value);
		}
		private Guid _TelemetryGameModId;

		/// <summary>Telemetry Game Id</summary>
		[DataMember]
		public Guid TelemetryGameId {
			get => _TelemetryGameId;
			set => SetProperty(ref _TelemetryGameId, value);
		}
		private Guid _TelemetryGameId;

		/// <summary>Mod Order</summary>
		[DataMember]
		public int ModOrder {
			get => _ModOrder;
			set => SetProperty(ref _ModOrder, value);
		}
		private int _ModOrder;

		/// <summary>Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string Name {
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		private string _Name;

		/// <summary>Detect File</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string DetectFile {
			get => _DetectFile;
			set => SetProperty(ref _DetectFile, value);
		}
		private string _DetectFile;

		/// <summary>Detect Product</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DetectProduct {
			get => _DetectProduct;
			set => SetProperty(ref _DetectProduct, value);
		}
		private string _DetectProduct;

		/// <summary>Url</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string Url {
			get => _Url;
			set => SetProperty(ref _Url, value);
		}
		private string _Url;

	}
}
