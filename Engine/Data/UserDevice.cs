using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Linq.Expressions;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class UserDevice : IDisplayName, IUserRecord
	{

		public UserDevice()
		{
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
			IsEnabled = true;
			ConnectionClass = Guid.Empty;
		}

		[XmlIgnore]
		public string DisplayName
		{
			get
			{
				return string.Format("{0} - {1}", InstanceId, InstanceName);
			}
		}

		/// <summary>
		/// Whether <see cref="InputSourceType"/> is written to x360ce.UserDevices.xml and sent to the web service:
		/// only for a device the program does not read through DirectInput.
		/// </summary>
		/// <remarks>
		/// The program keeps a DirectInput device at 0, so a file written before the property existed reads back
		/// unchanged, and the device's checksum, which leaves out a value at 0, stays what it was. 1 is DirectInput
		/// too and is left out the same way. The database stores both as 1 (<see cref="DatabaseHelper.StoreInputSource"/>).
		/// </remarks>
		public bool ShouldSerializeInputSourceType()
		{
			return !IsDirectInput;
		}

		/// <summary>Whether the program reads the device through DirectInput: <see cref="InputSourceType"/> 0, as the program keeps it, or 1.</summary>
		[XmlIgnore]
		public bool IsDirectInput => InputSourceType == 0 || InputSourceType == (int)Engine.InputSourceType.DirectInput;

		/// <summary>The source the program reads the device through, DirectInput whether <see cref="InputSourceType"/> holds 0 or 1.</summary>
		[XmlIgnore]
		public Engine.InputSourceType InputSource => IsDirectInput ? Engine.InputSourceType.DirectInput : (Engine.InputSourceType)InputSourceType;

		/// <summary>Whether <paramref name="other"/> is this device's twin: the same controller, read through another source.</summary>
		/// <remarks>
		/// Twins have the same HID interface path, which DirectInput and Raw Input report in different letter case, so the
		/// paths are compared without regard to case. Both read each control into the same place.
		/// </remarks>
		public bool IsTwinOf(UserDevice other)
		{
			return other != null
				&& other.InputSource != InputSource
				&& !string.IsNullOrEmpty(HidDevicePath)
				&& string.Equals(HidDevicePath, other.HidDevicePath, StringComparison.OrdinalIgnoreCase);
		}

		public void LoadInstance(DeviceInstance ins)
		{
			// Names from the driver are cleaned on the way in, so the settings file stays writable.
			var instanceName = EngineHelper.ToXmlText(ins.InstanceName);
			var productName = EngineHelper.ToXmlText(ins.ProductName);
			if (InstanceGuid != ins.InstanceGuid)
				InstanceGuid = ins.InstanceGuid;
			if (InstanceName != instanceName)
				InstanceName = instanceName;
			if (ProductGuid != ins.ProductGuid)
				ProductGuid = ins.ProductGuid;
			if (ProductName != productName)
				ProductName = productName;
			ForceFeedbackDriver = Engine.ForceFeedbackDriver.FileOf(ins.ForceFeedbackDriverGuid);
		}

		/// <summary>
		/// Cleans the strings Windows reported for a device of characters XML cannot hold, in
		/// place, so both loaders below take the same text and the settings file stays writable.
		/// </summary>
		static DeviceInfo Clean(DeviceInfo info)
		{
			if (info == null)
				return null;
			info.Manufacturer = EngineHelper.ToXmlText(info.Manufacturer);
			info.Description = EngineHelper.ToXmlText(info.Description);
			info.DeviceId = EngineHelper.ToXmlText(info.DeviceId);
			info.HardwareIds = EngineHelper.ToXmlText(info.HardwareIds);
			info.DevicePath = EngineHelper.ToXmlText(info.DevicePath);
			info.ParentDeviceId = EngineHelper.ToXmlText(info.ParentDeviceId);
			info.ClassDescription = EngineHelper.ToXmlText(info.ClassDescription);
			return info;
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
			Clean(info);
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
			Clean(info);
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

		/// <summary>The file of the device's force feedback driver, or empty when it has none.</summary>
		[XmlIgnore]
		public string ForceFeedbackDriver = "";

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
		public SourceState SourceState;

		[XmlIgnore]
		public long SourceStateTime;

		/// <summary>The state shown before <see cref="SourceState"/>. The input thread fills this object again on the next poll and shows it, so a state stays unchanged for one whole poll after it is replaced. Whoever keeps a state longer takes a <see cref="SourceState.Clone"/>.</summary>
		[XmlIgnore]
		public SourceState OldSourceState;

		[XmlIgnore]
		public long OldSourceStateTime;

		[XmlIgnore]
		public SourceState OriginSourceState;

		[XmlIgnore]
		public long OriginSourceStateTime;

		/// <summary>The two DirectInput states the input thread reads this device into, in turn; <see cref="JoState"/> is the one read last.</summary>
		/// <remarks>Made on the first two reads and kept, so a poll makes no new state, and the one shown is never the one being read into.</remarks>
		[XmlIgnore]
		public JoystickState[] JoStates;

		/// <summary>Which of <see cref="JoStates"/> the next poll reads into.</summary>
		[XmlIgnore]
		public int JoStateTurn;

		/// <summary>The reading of a device with axes that report movement, before the state shown is worked out from it. The input thread's own, filled on every poll.</summary>
		[XmlIgnore]
		public SourceState SourceStateRead;

		/// <summary>The axes, one bit each as in <see cref="DiAxeMask"/>, that report how far they moved rather than where they are: a mouse's, a trackball's, a spinner's.</summary>
		/// <remarks>Set by the input thread when the device's objects are first read; none for a device read as a gamepad (<see cref="SourceState.TrustedRelativeMask"/>). The state shown works these axes out from where they were first read.</remarks>
		[XmlIgnore]
		public int DiRelativeAxisMask;

		/// <summary>The sliders, one bit each as in <see cref="DiSliderMask"/>, that report how far they moved rather than where they are: a spinner's dial.</summary>
		/// <remarks>Set with <see cref="DiRelativeAxisMask"/>, and worked out the same way.</remarks>
		[XmlIgnore]
		public int DiRelativeSliderMask;

		/// <summary>Whether the device was acquired since the origin of its moving axes and sliders was taken, so the next reading takes it again.</summary>
		/// <remarks>Set by the input thread each time it acquires the device. A device object made again, as after putting controllers in order, keeps running totals of its own.</remarks>
		[XmlIgnore]
		public bool DiRelativeRestart;

		[XmlIgnore]
		public ForceFeedbackState FFState;

		/// <summary>The Auto button's run on the Force Feedback page, while one is on. Set by the interface, driven by the engine.</summary>
		[XmlIgnore]
		public volatile SpringCalibration SpringCalibration;

		[XmlIgnore]
		public bool? IsExclusiveMode;

		/// <summary>Reads of this device that failed in a row. Set by the engine only.</summary>
		/// <remarks>After two, the engine rests the device until <see cref="DiReadRetryAt"/>.</remarks>
		[XmlIgnore]
		public int DiReadFailures;

		/// <summary>The <see cref="System.Environment.TickCount"/> from which a resting device is read again. Set by the engine only.</summary>
		[XmlIgnore]
		public int DiReadRetryAt;

		/// <summary>Whether a fault in this run of failed polls has been reported. Set by the engine only.</summary>
		[XmlIgnore]
		public bool DiReadFaultReported;

		/// <summary>Whether the Raw Input hub did not have this Raw Input device when the engine last read it. Set by the engine only.</summary>
		/// <remarks>
		/// Written when it changes: set by the read that does not find the device, which puts its state at rest once, and
		/// cleared by the next that does. While it is set the device reaches its controller as nothing, as a DirectInput
		/// device whose read failed does. The hub loses a device when it is unplugged, a moment before the device list
		/// marks it offline.
		/// </remarks>
		[XmlIgnore]
		public bool RawInputMissing;

		/// <summary>Attempts at this device's force feedback that failed in a row. Set by the engine only.</summary>
		/// <remarks>After two, the force rests until <see cref="ForceRetryAt"/> and is then sent again. The device is read all the while.</remarks>
		[XmlIgnore]
		public int ForceFailures;

		/// <summary>The <see cref="System.Environment.TickCount"/> from which force feedback that failed is tried again. Set by the engine only.</summary>
		[XmlIgnore]
		public int ForceRetryAt;

		/// <summary>The error code of the fault reported in this run of force feedback failures, or nought while none is. Set by the engine only.</summary>
		/// <remarks>The first failure of the run that is not a device condition. The Issues tab names it until an attempt succeeds.</remarks>
		[XmlIgnore]
		public int ForceFault;

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

		[XmlIgnore]
		public bool IsMouse => CapType == (int)SharpDX.DirectInput.DeviceType.Mouse;

		[XmlIgnore]
		public bool IsKeyboard => CapType == (int)SharpDX.DirectInput.DeviceType.Keyboard;

		[XmlIgnore]
		public bool AllowHide
		{
			get
			{
				return
					!IsKeyboard &&
					!IsMouse &&
					ConnectionClass != JocysCom.ClassLibrary.Win32.DEVCLASS.SYSTEM &&
					// Device Id must be set.
					!string.IsNullOrEmpty(HidDeviceId);
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
