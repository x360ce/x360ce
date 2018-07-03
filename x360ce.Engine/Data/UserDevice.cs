using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Linq.Expressions;
using System.Xml.Serialization;
using System.Linq;

namespace x360ce.Engine.Data
{
    public partial class UserDevice : IDisplayName, IUserRecord
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
            if (InstanceGuid != ins.InstanceGuid)
            InstanceGuid = ins.InstanceGuid;
            if (InstanceName != ins.InstanceName)
            InstanceName = ins.InstanceName;
            if (ProductGuid != ins.ProductGuid)
            ProductGuid = ins.ProductGuid;
            if (ProductName != ins.ProductName)
            ProductName = ins.ProductName;
        }

		public void LoadCapabilities(Capabilities cap)
        {
            // Check if value is same to reduce grid refresh.
            if (CapAxeCount != cap.AxeCount)
                CapAxeCount = cap.AxeCount;
            if (CapButtonCount != cap.ButtonCount)
                CapButtonCount = cap.ButtonCount;
            if (CapDriverVersion != cap.DriverVersion)
                CapDriverVersion = cap.DriverVersion;
            if (CapFirmwareRevision != cap.FirmwareRevision)
                CapFirmwareRevision = cap.FirmwareRevision;
            if (CapFlags != (int)cap.Flags)
                CapFlags = (int)cap.Flags;
            if (CapForceFeedbackMinimumTimeResolution != cap.ForceFeedbackMinimumTimeResolution)
                CapForceFeedbackMinimumTimeResolution = cap.ForceFeedbackMinimumTimeResolution;
            if (CapForceFeedbackSamplePeriod != cap.ForceFeedbackSamplePeriod)
                CapForceFeedbackSamplePeriod = cap.ForceFeedbackSamplePeriod;
            if (CapHardwareRevision != cap.HardwareRevision)
                CapHardwareRevision = cap.HardwareRevision;
            if (CapPovCount != cap.PovCount)
                CapPovCount = cap.PovCount;
            if (CapIsHumanInterfaceDevice != cap.IsHumanInterfaceDevice)
                CapIsHumanInterfaceDevice = cap.IsHumanInterfaceDevice;
            if (CapSubtype != cap.Subtype)
                CapSubtype = cap.Subtype;
            if (CapType != (int)cap.Type)
                CapType = (int)cap.Type;
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
				DevHardwareIds = "";
				DevDevicePath = "";
				DevParentDeviceId = "";
				DevClassGuid = Guid.Empty;
				DevClassDescription = "";
			}
			else
			{
				// Check if value is same to reduce grid refresh.
				if (DevManufacturer != info.Manufacturer)
					DevManufacturer = info.Manufacturer;
				if (DevVendorId != (int)info.VendorId)
					DevVendorId = (int)info.VendorId;
				if (DevProductId != (int)info.ProductId)
					DevProductId = (int)info.ProductId;
				if (DevRevision != (int)info.Revision)
					DevRevision = (int)info.Revision;
				if (DevDescription != info.Description)
					DevDescription = info.Description;
				if (DevDeviceId != info.DeviceId)
					DevDeviceId = info.DeviceId;
				if (DevHardwareIds != info.HardwareIds)
					DevHardwareIds = info.HardwareIds;
				if (DevDevicePath != info.DevicePath)
					DevDevicePath = info.DevicePath;
				if (DevParentDeviceId != info.ParentDeviceId)
					DevParentDeviceId = info.ParentDeviceId;
				if (DevClassGuid != info.ClassGuid)
					DevClassGuid = info.ClassGuid;
				if (DevClassDescription != info.ClassDescription)
					DevClassDescription = info.ClassDescription;
			}
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
				HidHardwareIds = "";
				HidDevicePath = "";
                HidParentDeviceId = "";
                HidClassGuid = Guid.Empty;
                HidClassDescription = "";
				HidHardwareIds = info.HardwareIds;
            }
            else
            {
                // Check if value is same to reduce grid refresh.
                if (HidManufacturer != info.Manufacturer)
                    HidManufacturer = info.Manufacturer;
                if (HidVendorId != (int)info.VendorId)
                    HidVendorId = (int)info.VendorId;
                if (HidProductId != (int)info.ProductId)
                    HidProductId = (int)info.ProductId;
                if (HidRevision != (int)info.Revision)
                    HidRevision = (int)info.Revision;
                if (HidDescription != info.Description)
                    HidDescription = info.Description;
                if (HidDeviceId != info.DeviceId)
                    HidDeviceId = info.DeviceId;
				if (HidHardwareIds != info.HardwareIds)
					HidHardwareIds = info.HardwareIds;
				if (HidDevicePath != info.DevicePath)
                    HidDevicePath = info.DevicePath;
                if (HidParentDeviceId != info.ParentDeviceId)
                    HidParentDeviceId = info.ParentDeviceId;
                if (HidClassGuid != info.ClassGuid)
                    HidClassGuid = info.ClassGuid;
                if (HidClassDescription != info.ClassDescription)
                    HidClassDescription = info.ClassDescription;
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
		public string DevHardwareIds;

		[XmlIgnore]
		public string HidHardwareIds;

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
