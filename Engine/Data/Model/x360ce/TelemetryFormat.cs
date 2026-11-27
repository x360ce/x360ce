using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>Telemetry Format</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("TelemetryFormat", Schema = "x360ce")]
	[DataContract(IsReference = true)]
	public partial class TelemetryFormat: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Telemetry Format Id (unique primary key).</summary>
		[Key]
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string TelemetryFormatId {
			get => _TelemetryFormatId;
			set => SetProperty(ref _TelemetryFormatId, value);
		}
		private string _TelemetryFormatId;

		/// <summary>Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string Name {
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		private string _Name;

		/// <summary>Publisher</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string Publisher {
			get => _Publisher;
			set => SetProperty(ref _Publisher, value);
		}
		private string _Publisher;

		/// <summary>Transport</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string Transport {
			get => _Transport;
			set => SetProperty(ref _Transport, value);
		}
		private string _Transport;

		/// <summary>Default Port</summary>
		[DataMember]
		public int DefaultPort {
			get => _DefaultPort;
			set => SetProperty(ref _DefaultPort, value);
		}
		private int _DefaultPort;

		/// <summary>Map Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string MapName {
			get => _MapName;
			set => SetProperty(ref _MapName, value);
		}
		private string _MapName;

		/// <summary>Packet Sizes</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string PacketSizes {
			get => _PacketSizes;
			set => SetProperty(ref _PacketSizes, value);
		}
		private string _PacketSizes;

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
