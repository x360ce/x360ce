// The plugin devices and the control the plugin settings tests load, as a plugin author writes them.
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>How a device a plugin adds is listened to: what the tests set on it.</summary>
	public enum SettingsPluginMode
	{
		[Description("Fast, less accurate")]
		Fast,
		[Description("Accurate")]
		Accurate,
	}

	/// <summary>The addresses a device can listen on, as a list a person picks from or types into.</summary>
	public class SettingsPluginAddressConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new[] { "All Unassigned", "127.0.0.1" });
		}
	}

	/// <summary>A plugin device with one setting of each kind, as a controller fed over the network might have.</summary>
	[Description("A device with settings, for the plugin settings tests.")]
	public class SettingsPluginDevice : IPluginDevice
	{
		public static readonly Guid Product = new Guid("0b7d6f43-6a41-4b0c-9d55-2f6c1d9a8e10");
		public string Name { get { return "Settings Device"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 1; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 0; } }
		public int ButtonCount { get { return 1; } }

		/// <summary>How many times x360ce set a setting.</summary>
		[Browsable(false)]
		public int SetCount { get; private set; }

		[Category("Network"), DisplayName("Address"), Description("The address to listen on.")]
		[TypeConverter(typeof(SettingsPluginAddressConverter))]
		public string Address { get { return _address; } set { _address = value; SetCount++; } }
		string _address = "All Unassigned";

		[Category("Network"), DisplayName("Port"), Description("The UDP port to listen on.")]
		[Range(1, 65535)]
		public int Port { get { return _port; } set { _port = value; SetCount++; } }
		int _port = 26760;

		[Category("Network"), DisplayName("Listen"), Description("Listens for the controller.")]
		public bool Listen { get { return _listen; } set { _listen = value; SetCount++; } }
		bool _listen = true;

		[DisplayName("Smoothing"), Description("How much each value is smoothed.")]
		[Range(0.0, 1.0)]
		public double Smoothing { get { return _smoothing; } set { _smoothing = value; SetCount++; } }
		double _smoothing = 0.5;

		[DisplayName("Mode"), Description("Fast or accurate.")]
		public SettingsPluginMode Mode { get { return _mode; } set { _mode = value; SetCount++; } }
		SettingsPluginMode _mode;

		public string Label { get { return _label; } set { _label = value; SetCount++; } }
		string _label = "Pad";

		[DisplayName("Packets received"), Description("Packets received since the device was added.")]
		public int Packets { get; private set; }

		public void Receive() { Packets++; }

		public void Read(PluginState state)
		{
			state.Axes[0] = Listen ? PluginState.Middle : 0;
		}
	}

	/// <summary>A plugin's own control for its device, as a plugin declares one: a constructor taking the device.</summary>
	public class SettingsPluginControl : UserControl
	{
		public SettingsPluginControl(SettingsPluginDevice device)
		{
			Name = "SettingsPluginControl";
			Device = device;
		}

		public SettingsPluginDevice Device { get; }
	}
}
