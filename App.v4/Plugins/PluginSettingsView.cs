using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>A plugin device's settings as one object a property grid shows: a row for each setting, grouped by [Category].</summary>
	/// <remarks>
	/// The grid edits no plugin instance. A value typed in a row is checked as the set_ tools check it and kept for the
	/// device, and the engine gives it to the plugin before its next read, on the thread that reads it. A status row reads
	/// the instance the engine reads. A row whose value is kept is drawn bold, as Visual Studio draws a value changed from
	/// its default, and the row's Reset gives back the value a new device starts with.
	/// </remarks>
	public sealed class PluginSettingsView : ICustomTypeDescriptor
	{
		/// <param name="instanceGuid">The device, as the device list knows it.</param>
		/// <param name="deviceType">The device's kind.</param>
		/// <param name="device">The plugin instance the engine reads for the device, or null while it reads none.</param>
		/// <param name="store">Where the device's values are kept.</param>
		public PluginSettingsView(Guid instanceGuid, PluginDeviceType deviceType, IPluginDevice device, PluginSettings store)
		{
			InstanceGuid = instanceGuid;
			DeviceType = deviceType;
			Device = device;
			Store = store;
			_properties = new PropertyDescriptorCollection(deviceType.Settings.Select(x => (PropertyDescriptor)new SettingDescriptor(this, x)).ToArray(), true);
		}

		public Guid InstanceGuid { get; }
		public PluginDeviceType DeviceType { get; }
		public IPluginDevice Device { get; }
		public PluginSettings Store { get; }

		readonly PropertyDescriptorCollection _properties;

		/// <summary>What every status row says now, in the order of the rows, so a change can be noticed without reading the grid.</summary>
		public string[] Statuses()
		{
			return DeviceType.Settings.Where(x => x.Kind == PluginSettingKind.Status)
				.Select(x => PluginDeviceTools.ReadStatus(DeviceType, x, Device)).ToArray();
		}

		/// <summary>The value kept for every other row, or null where it has the first value, so a change made outside the grid can be noticed.</summary>
		public string[] Kept()
		{
			return DeviceType.Settings.Where(x => x.Kind != PluginSettingKind.Status)
				.Select(x => Store.Get(InstanceGuid, x.Name)).ToArray();
		}

		#region ICustomTypeDescriptor

		public AttributeCollection GetAttributes() { return AttributeCollection.Empty; }
		public string GetClassName() { return DeviceType.Name; }
		public string GetComponentName() { return null; }
		public TypeConverter GetConverter() { return new TypeConverter(); }
		public EventDescriptor GetDefaultEvent() { return null; }
		public PropertyDescriptor GetDefaultProperty() { return null; }
		public object GetEditor(Type editorBaseType) { return null; }
		public EventDescriptorCollection GetEvents() { return EventDescriptorCollection.Empty; }
		public EventDescriptorCollection GetEvents(Attribute[] attributes) { return EventDescriptorCollection.Empty; }
		public PropertyDescriptorCollection GetProperties() { return _properties; }
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes) { return _properties; }
		public object GetPropertyOwner(PropertyDescriptor pd) { return this; }

		#endregion

		/// <summary>One setting as a row: read from and kept in the store, or, for a status, read from the device.</summary>
		sealed class SettingDescriptor : PropertyDescriptor
		{
			public SettingDescriptor(PluginSettingsView view, PluginSetting setting)
				: base(setting.Name, new Attribute[]
				{
					new CategoryAttribute(setting.Category),
					new DisplayNameAttribute(setting.DisplayName),
					new DescriptionAttribute(setting.Description ?? ""),
				})
			{
				_view = view;
				_setting = setting;
				_converter = setting.Kind == PluginSettingKind.List ? new ChoiceConverter(setting)
					: setting.Kind == PluginSettingKind.Status ? new StringConverter()
					: setting.Converter;
			}

			readonly PluginSettingsView _view;
			readonly PluginSetting _setting;
			readonly TypeConverter _converter;

			public override Type ComponentType { get { return typeof(PluginSettingsView); } }
			public override bool IsReadOnly { get { return _setting.Kind == PluginSettingKind.Status; } }
			public override Type PropertyType { get { return IsReadOnly ? typeof(string) : _setting.Property.PropertyType; } }
			public override TypeConverter Converter { get { return _converter; } }

			string Kept { get { return _view.Store.Get(_view.InstanceGuid, _setting.Name); } }

			public override object GetValue(object component)
			{
				if (IsReadOnly)
					return PluginDeviceTools.ReadStatus(_view.DeviceType, _setting, _view.Device) ?? "";
				return _setting.FromText(Kept ?? _setting.DefaultText);
			}

			/// <summary>Keeps a value once it is one the setting takes; one it does not take throws, and the grid says why.</summary>
			public override void SetValue(object component, object value)
			{
				_view.Store.Set(_view.InstanceGuid, _setting.Name, _setting.Parse(_setting.ToText(value)));
				OnValueChanged(component, EventArgs.Empty);
			}

			public override bool CanResetValue(object component) { return !IsReadOnly && Kept != null; }

			public override void ResetValue(object component)
			{
				_view.Store.Remove(_view.InstanceGuid, _setting.Name);
				OnValueChanged(component, EventArgs.Empty);
			}

			public override bool ShouldSerializeValue(object component) { return CanResetValue(component); }
		}

		/// <summary>A list setting's values by the words its list shows for each, such as an enum value's [Description].</summary>
		sealed class ChoiceConverter : TypeConverter
		{
			public ChoiceConverter(PluginSetting setting)
			{
				_setting = setting;
			}

			readonly PluginSetting _setting;

			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return _setting.ListOnly; }

			/// <summary>Asked each time the list opens, so a converter can offer what is there now, such as the computer's addresses.</summary>
			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
			{
				return new StandardValuesCollection(_setting.GetChoices().Select(x => _setting.FromText(x.Value)).ToArray());
			}

			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				var text = value as string;
				return text == null ? base.ConvertFrom(context, culture, value) : _setting.FromText(_setting.Parse(text));
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if (destinationType != typeof(string))
					return base.ConvertTo(context, culture, value, destinationType);
				var kept = _setting.ToText(value);
				var choice = _setting.GetChoices().FirstOrDefault(x => x.Value == kept);
				return choice == null ? kept : choice.Text;
			}
		}
	}
}
