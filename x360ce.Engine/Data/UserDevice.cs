using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq.Expressions;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class UserDevice : IDisplayName, IChecksum, IDateTime
	{

		public UserDevice()
		{
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
			IsEnabled = true;
		}

		[XmlIgnore]
		public string DisplayName
		{
			get
			{
				return string.Format("{0} - {1}", InstanceId, InstanceName);
			}
		}

		public void LoadInstance(DeviceInstance ins)
		{
			InstanceGuid = ins.InstanceGuid;
			InstanceName = ins.InstanceName;
			ProductGuid = ins.ProductGuid;
			ProductName = ins.ProductName;
		}

		public void LoadCapabilities(SharpDX.DirectInput.Capabilities cap)
		{
			CapAxeCount = cap.AxeCount;
			CapButtonCount = cap.ButtonCount;
			CapDriverVersion = cap.DriverVersion;
			CapFirmwareRevision = cap.FirmwareRevision;
			CapFlags = (int)cap.Flags;
			CapForceFeedbackMinimumTimeResolution = cap.ForceFeedbackMinimumTimeResolution;
			CapForceFeedbackSamplePeriod = cap.ForceFeedbackSamplePeriod;
			CapHardwareRevision = cap.HardwareRevision;
			CapPovCount = cap.PovCount;
			CapIsHumanInterfaceDevice = cap.IsHumanInterfaceDevice;
			CapSubtype = cap.Subtype;
			CapType = (int)cap.Type;
		}

		public void LoadHidDeviceInfo(DeviceInfo info)
		{
			if (info == null)
			{
				HidManufacturer = "";
				HidVendorId = 0;
				HidProductId = 0;
				HidRevision = 0;
				HidDescription = "";
				HidDeviceId = "";
				HidDevicePath = "";
				HidParentDeviceId = "";
				HidClassGuid = Guid.Empty;
				HidClassDescription = "";
			}
			else
			{
				HidManufacturer = info.Manufacturer;
				HidVendorId = (int)info.VendorId;
				HidProductId = (int)info.ProductId;
				HidRevision = (int)info.Revision;
				HidDescription = info.Description;
				HidDeviceId = info.DeviceId;
				HidDevicePath = info.DevicePath;
				HidParentDeviceId = info.ParentDeviceId;
				HidClassGuid = info.ClassGuid;
				HidClassDescription = info.ClassDescription;
			}
		}

		public void LoadDevDeviceInfo(DeviceInfo info)
		{
			if (info == null)
			{
				DevManufacturer = "";
				DevVendorId = 0;
				DevProductId = 0;
				DevRevision = 0;
				DevDescription = "";
				DevDeviceId = "";
				DevDevicePath = "";
				DevParentDeviceId = "";
				DevClassGuid = Guid.Empty;
				DevClassDescription = "";
			}
			else
			{
				DevManufacturer = info.Manufacturer;
				DevVendorId = (int)info.VendorId;
				DevProductId = (int)info.ProductId;
				DevRevision = (int)info.Revision;
				DevDescription = info.Description;
				DevDeviceId = info.DeviceId;
				DevDevicePath = info.DevicePath;
				DevParentDeviceId = info.ParentDeviceId;
				DevClassGuid = info.ClassGuid;
				DevClassDescription = info.ClassDescription;
			}
		}

		#region Ignored properties used by application to store various device states.

		[XmlIgnore]
		public bool DeviceChanged;

		/// <summary>DInput Device State.</summary>
		[XmlIgnore]
		public Joystick Device;

		[XmlIgnore]
		public DeviceObjectItem[] DeviceObjects;

		[XmlIgnore]
		public DeviceEffectItem[] DeviceEffects;

		/// <summary>DInput JoystickState State.</summary>
		[XmlIgnore]
		public JoystickState JoState;

		/// <summary>X360CE custom DirectInput state used for configuration.</summary>
		[XmlIgnore]
		public CustomDiState DiState;

        [XmlIgnore]
        public long DiStateTime;

        [XmlIgnore]
        public CustomDiState OldDiState;

        [XmlIgnore]
        public long OldDiStateTime;

        [XmlIgnore]
		public ForceFeedbackState FFState;

		[XmlIgnore]
		public bool? IsExclusiveMode;

		[XmlIgnore]
		public bool IsOnline
		{
			get { return _IsOnline; }
			set { _IsOnline = value; ReportPropertyChanged(x => x.IsOnline); }
		}
		bool _IsOnline;

		[XmlIgnore]
		public string InstanceId
		{
			get
			{
				return EngineHelper.GetID(InstanceGuid);
			}
		}

		#endregion

		#region INotifyPropertyChanged

		/// <summary>
		/// Use: ReportPropertyChanged(x => x.PropertyName);
		/// </summary>
		void ReportPropertyChanged(Expression<Func<UserDevice, object>> selector)
		{
			var body = (MemberExpression)((UnaryExpression)selector.Body).Operand;
			var name = body.Member.Name;
			ReportPropertyChanged(name);
		}

		#endregion
	}
}
