using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce User Devices</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_UserDevices")]
	[DataContract(IsReference = true)]
	public partial class UserDevice: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
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

		/// <summary>Connection Class</summary>
		[DataMember]
		public Guid ConnectionClass {
			get => _ConnectionClass;
			set => SetProperty(ref _ConnectionClass, value);
		}
		private Guid _ConnectionClass;

		/// <summary>Cap Axe Count</summary>
		[DataMember]
		public int CapAxeCount {
			get => _CapAxeCount;
			set => SetProperty(ref _CapAxeCount, value);
		}
		private int _CapAxeCount;

		/// <summary>Cap Button Count</summary>
		[DataMember]
		public int CapButtonCount {
			get => _CapButtonCount;
			set => SetProperty(ref _CapButtonCount, value);
		}
		private int _CapButtonCount;

		/// <summary>Cap Driver Version</summary>
		[DataMember]
		public int CapDriverVersion {
			get => _CapDriverVersion;
			set => SetProperty(ref _CapDriverVersion, value);
		}
		private int _CapDriverVersion;

		/// <summary>Cap Firmware Revision</summary>
		[DataMember]
		public int CapFirmwareRevision {
			get => _CapFirmwareRevision;
			set => SetProperty(ref _CapFirmwareRevision, value);
		}
		private int _CapFirmwareRevision;

		/// <summary>Cap Flags</summary>
		[DataMember]
		public int CapFlags {
			get => _CapFlags;
			set => SetProperty(ref _CapFlags, value);
		}
		private int _CapFlags;

		/// <summary>Cap Force Feedback Minimum Time Resolution</summary>
		[DataMember]
		public int CapForceFeedbackMinimumTimeResolution {
			get => _CapForceFeedbackMinimumTimeResolution;
			set => SetProperty(ref _CapForceFeedbackMinimumTimeResolution, value);
		}
		private int _CapForceFeedbackMinimumTimeResolution;

		/// <summary>Cap Force Feedback Sample Period</summary>
		[DataMember]
		public int CapForceFeedbackSamplePeriod {
			get => _CapForceFeedbackSamplePeriod;
			set => SetProperty(ref _CapForceFeedbackSamplePeriod, value);
		}
		private int _CapForceFeedbackSamplePeriod;

		/// <summary>Cap Hardware Revision</summary>
		[DataMember]
		public int CapHardwareRevision {
			get => _CapHardwareRevision;
			set => SetProperty(ref _CapHardwareRevision, value);
		}
		private int _CapHardwareRevision;

		/// <summary>Cap Pov Count</summary>
		[DataMember]
		public int CapPovCount {
			get => _CapPovCount;
			set => SetProperty(ref _CapPovCount, value);
		}
		private int _CapPovCount;

		/// <summary>Cap Is Human Interface Device</summary>
		[DataMember]
		public bool CapIsHumanInterfaceDevice {
			get => _CapIsHumanInterfaceDevice;
			set => SetProperty(ref _CapIsHumanInterfaceDevice, value);
		}
		private bool _CapIsHumanInterfaceDevice;

		/// <summary>Cap Subtype</summary>
		[DataMember]
		public int CapSubtype {
			get => _CapSubtype;
			set => SetProperty(ref _CapSubtype, value);
		}
		private int _CapSubtype;

		/// <summary>Cap Type</summary>
		[DataMember]
		public int CapType {
			get => _CapType;
			set => SetProperty(ref _CapType, value);
		}
		private int _CapType;

		/// <summary>Di Axe Mask</summary>
		[DataMember]
		public int DiAxeMask {
			get => _DiAxeMask;
			set => SetProperty(ref _DiAxeMask, value);
		}
		private int _DiAxeMask;

		/// <summary>Di Slider Mask</summary>
		[DataMember]
		public int DiSliderMask {
			get => _DiSliderMask;
			set => SetProperty(ref _DiSliderMask, value);
		}
		private int _DiSliderMask;

		/// <summary>Di Actuator Mask</summary>
		[DataMember]
		public int DiActuatorMask {
			get => _DiActuatorMask;
			set => SetProperty(ref _DiActuatorMask, value);
		}
		private int _DiActuatorMask;

		/// <summary>Di Actuator Count</summary>
		[DataMember]
		public int DiActuatorCount {
			get => _DiActuatorCount;
			set => SetProperty(ref _DiActuatorCount, value);
		}
		private int _DiActuatorCount;

		/// <summary>Hid Manufacturer</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string HidManufacturer {
			get => _HidManufacturer;
			set => SetProperty(ref _HidManufacturer, value);
		}
		private string _HidManufacturer;

		/// <summary>Hid Vendor Id</summary>
		[DataMember]
		public int HidVendorId {
			get => _HidVendorId;
			set => SetProperty(ref _HidVendorId, value);
		}
		private int _HidVendorId;

		/// <summary>Hid Product Id</summary>
		[DataMember]
		public int HidProductId {
			get => _HidProductId;
			set => SetProperty(ref _HidProductId, value);
		}
		private int _HidProductId;

		/// <summary>Hid Revision</summary>
		[DataMember]
		public int HidRevision {
			get => _HidRevision;
			set => SetProperty(ref _HidRevision, value);
		}
		private int _HidRevision;

		/// <summary>Hid Description</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string HidDescription {
			get => _HidDescription;
			set => SetProperty(ref _HidDescription, value);
		}
		private string _HidDescription;

		/// <summary>Hid Device Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string HidDeviceId {
			get => _HidDeviceId;
			set => SetProperty(ref _HidDeviceId, value);
		}
		private string _HidDeviceId;

		/// <summary>Hid Device Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string HidDevicePath {
			get => _HidDevicePath;
			set => SetProperty(ref _HidDevicePath, value);
		}
		private string _HidDevicePath;

		/// <summary>Hid Parent Device Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string HidParentDeviceId {
			get => _HidParentDeviceId;
			set => SetProperty(ref _HidParentDeviceId, value);
		}
		private string _HidParentDeviceId;

		/// <summary>Hid Class Guid</summary>
		[DataMember]
		public Guid HidClassGuid {
			get => _HidClassGuid;
			set => SetProperty(ref _HidClassGuid, value);
		}
		private Guid _HidClassGuid;

		/// <summary>Hid Class Description</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string HidClassDescription {
			get => _HidClassDescription;
			set => SetProperty(ref _HidClassDescription, value);
		}
		private string _HidClassDescription;

		/// <summary>Dev Manufacturer</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string DevManufacturer {
			get => _DevManufacturer;
			set => SetProperty(ref _DevManufacturer, value);
		}
		private string _DevManufacturer;

		/// <summary>Dev Vendor Id</summary>
		[DataMember]
		public int DevVendorId {
			get => _DevVendorId;
			set => SetProperty(ref _DevVendorId, value);
		}
		private int _DevVendorId;

		/// <summary>Dev Product Id</summary>
		[DataMember]
		public int DevProductId {
			get => _DevProductId;
			set => SetProperty(ref _DevProductId, value);
		}
		private int _DevProductId;

		/// <summary>Dev Revision</summary>
		[DataMember]
		public int DevRevision {
			get => _DevRevision;
			set => SetProperty(ref _DevRevision, value);
		}
		private int _DevRevision;

		/// <summary>Dev Description</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string DevDescription {
			get => _DevDescription;
			set => SetProperty(ref _DevDescription, value);
		}
		private string _DevDescription;

		/// <summary>Dev Device Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string DevDeviceId {
			get => _DevDeviceId;
			set => SetProperty(ref _DevDeviceId, value);
		}
		private string _DevDeviceId;

		/// <summary>Dev Device Path</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string DevDevicePath {
			get => _DevDevicePath;
			set => SetProperty(ref _DevDevicePath, value);
		}
		private string _DevDevicePath;

		/// <summary>Dev Parent Device Id</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(512)]
		public string DevParentDeviceId {
			get => _DevParentDeviceId;
			set => SetProperty(ref _DevParentDeviceId, value);
		}
		private string _DevParentDeviceId;

		/// <summary>Dev Class Guid</summary>
		[DataMember]
		public Guid DevClassGuid {
			get => _DevClassGuid;
			set => SetProperty(ref _DevClassGuid, value);
		}
		private Guid _DevClassGuid;

		/// <summary>Dev Class Description</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(256)]
		public string DevClassDescription {
			get => _DevClassDescription;
			set => SetProperty(ref _DevClassDescription, value);
		}
		private string _DevClassDescription;

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

		/// <summary>Is Hidden</summary>
		[DataMember]
		public bool IsHidden {
			get => _IsHidden;
			set => SetProperty(ref _IsHidden, value);
		}
		private bool _IsHidden;

		/// <summary>Is Enabled</summary>
		[DataMember]
		public bool IsEnabled {
			get => _IsEnabled;
			set => SetProperty(ref _IsEnabled, value);
		}
		private bool _IsEnabled;

		/// <summary>Checksum</summary>
		[DataMember]
		public Guid Checksum {
			get => _Checksum;
			set => SetProperty(ref _Checksum, value);
		}
		private Guid _Checksum;

		/// <summary>Input Source Type</summary>
		[DataMember]
		public int InputSourceType {
			get => _InputSourceType;
			set => SetProperty(ref _InputSourceType, value);
		}
		private int _InputSourceType;

		/// <summary>Force Spring Friction</summary>
		[DataMember]
		public int ForceSpringFriction {
			get => _ForceSpringFriction;
			set => SetProperty(ref _ForceSpringFriction, value);
		}
		private int _ForceSpringFriction;

	}
}
