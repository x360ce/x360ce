using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.Multimedia;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Xml.Serialization;
using x360ce.Engine;

namespace x360ce.App
{
	/// <summary>
	/// Turn this into table later.
	/// </summary>
	public class DiDevice : INotifyPropertyChanged
	{
		#region Instance

		public void LoadInstance(DeviceInstance ins)
		{
			ForceFeedbackDriverGuid = ins.ForceFeedbackDriverGuid;
			InstanceGuid = ins.InstanceGuid;
			InstanceName = ins.InstanceName;
			ProductGuid = ins.ProductGuid;
			ProductName = ins.ProductName;
			Usage = ins.Usage;
			UsagePage = ins.UsagePage;
		}

		public Guid ForceFeedbackDriverGuid { get; set; }
		public Guid InstanceGuid { get; set; }
		public string InstanceName { get; set; }
		public Guid ProductGuid { get; set; }
		public string ProductName { get; set; }
		public UsageId Usage { get; set; }
		public UsagePage UsagePage { get; set; }

		#endregion

		#region Capabilities

		// DInput Device Capabilities.

		public void LoadCapabilities(Capabilities cap)
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

		public int CapAxeCount { get; set; }
		public int CapButtonCount { get; set; }
		public int CapDriverVersion { get; set; }
		public int CapFirmwareRevision { get; set; }
		public int CapFlags { get; set; }
		public int CapForceFeedbackMinimumTimeResolution { get; set; }
		public int CapForceFeedbackSamplePeriod { get; set; }
		public int CapHardwareRevision { get; set; }
		public int CapPovCount { get; set; }
		public bool CapIsHumanInterfaceDevice { get; set; }
		public int CapSubtype { get; set; }
		public int CapType { get; set; }

		#endregion

		#region Hid Device Info

		public void LoadHidDeviceInfo(DeviceInfo info)
		{
			HidManufacturer = info.Manufacturer;
			HidVendorId = info.VendorId;
			HidProductId = info.ProductId;
			HidRevision = info.Revision;
			HidDescription = info.Description;
			HidDeviceId = info.DeviceId;
			HidDevicePath = info.DevicePath;
			HidParentDeviceId = info.ParentDeviceId;
			HidClassGuid = info.ClassGuid;
			HidClassDescription = info.ClassDescription;
		}

		public string HidManufacturer { get; set; }
		public uint HidVendorId { get; set; }
		public uint HidProductId { get; set; }
		public uint HidRevision { get; set; }
		public string HidDescription { get; set; }
		public string HidDeviceId { get; set; }
		public string HidDevicePath { get; set; }
		public string HidParentDeviceId { get; set; }
		public Guid HidClassGuid { get; set; }
		public string HidClassDescription { get; set; }

		#endregion

		#region Dev Device Info

		public void LoadDevDeviceInfo(DeviceInfo info)
		{
			DevManufacturer = info.Manufacturer;
			DevVendorId = info.VendorId;
			DevProductId = info.ProductId;
			DevRevision = info.Revision;
			DevDescription = info.Description;
			DevDeviceId = info.DeviceId;
			DevDevicePath = info.DevicePath;
			DevParentDeviceId = info.ParentDeviceId;
			DevClassGuid = info.ClassGuid;
			DevClassDescription = info.ClassDescription;
		}

		public string DevManufacturer { get; set; }
		public uint DevVendorId { get; set; }
		public uint DevProductId { get; set; }
		public uint DevRevision { get; set; }
		public string DevDescription { get; set; }
		public string DevDeviceId { get; set; }
		public string DevDevicePath { get; set; }
		public string DevParentDeviceId { get; set; }
		public Guid DevClassGuid { get; set; }
		public string DevClassDescription { get; set; }

		#endregion

		#region Ignored Properties

		/// <summary>DInput Device State.</summary>
		[XmlIgnore]
		public Joystick Device;

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

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Use: ReportPropertyChanged(x => x.PropertyName);
		/// </summary>
		void ReportPropertyChanged(Expression<Func<DiDevice, object>> selector)
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			var body = (MemberExpression)((UnaryExpression)selector.Body).Operand;
			var name = body.Member.Name;
			ev(this, new PropertyChangedEventArgs(name));
		}

		#endregion



	}
}
