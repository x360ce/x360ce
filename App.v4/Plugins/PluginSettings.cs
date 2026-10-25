using JocysCom.ClassLibrary.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using x360ce.Engine;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>What kind of value a setting holds, which decides how its row on a plugin device's tab edits it.</summary>
	public enum PluginSettingKind
	{
		/// <summary>A bool: True or False.</summary>
		CheckBox,
		/// <summary>A number, held inside the property's [Range].</summary>
		Number,
		/// <summary>A string, or a type its converter writes as text.</summary>
		Text,
		/// <summary>An enum, or a property whose type converter offers values: a drop-down list.</summary>
		List,
		/// <summary>A property that cannot be set: its value, shown and refreshed.</summary>
		Status,
	}

	/// <summary>One entry of a setting's drop-down list.</summary>
	public sealed class PluginSettingChoice
	{
		public PluginSettingChoice(string text, string value)
		{
			Text = text;
			Value = value;
		}

		/// <summary>What the list shows.</summary>
		public string Text { get; }

		/// <summary>The value kept, as invariant text.</summary>
		public string Value { get; }

		public override string ToString() { return Text; }
	}

	/// <summary>One setting of a kind of plugin device: a public property of its class.</summary>
	/// <remarks>
	/// Every public property is one, except the members of <see cref="IPluginDevice"/>, indexers, a declared
	/// <see cref="PluginMcpServer"/>, and those marked [Browsable(false)]. A property with a public setter is edited, as a
	/// row of the tab's property grid: a bool and an enum from a drop-down list, a number and a string typed. A type
	/// converter that offers standard values gives any of them a drop-down list, which takes typed values too unless the
	/// converter says its values are the only ones. A property without a public setter is shown as status, read only.
	/// [DisplayName] names a setting, [Description] explains it, [Category] groups it, and [Range] limits a number. The
	/// value a new instance of the class starts with is the setting's default, which Reset gives back.
	/// </remarks>
	public sealed class PluginSetting
	{
		/// <summary>The group of a setting with no [Category].</summary>
		public const string DefaultCategory = "Settings";

		/// <summary>The group of a status with no [Category].</summary>
		public const string StatusCategory = "Status";

		PluginSetting(PropertyInfo property, TypeConverter converter, PluginSettingKind kind, object probe)
		{
			Property = property;
			Converter = converter;
			Kind = kind;
			var display = property.GetCustomAttribute<DisplayNameAttribute>();
			DisplayName = display == null || string.IsNullOrWhiteSpace(display.DisplayName) ? property.Name : display.DisplayName;
			var description = property.GetCustomAttribute<DescriptionAttribute>();
			Description = description == null ? null : description.Description;
			var category = property.GetCustomAttribute<CategoryAttribute>();
			Category = category != null && !string.IsNullOrWhiteSpace(category.Category)
				? category.Category
				: kind == PluginSettingKind.Status ? StatusCategory : DefaultCategory;
			if (kind == PluginSettingKind.Number)
				SetLimits(property);
			ListOnly = kind == PluginSettingKind.List && (property.PropertyType.IsEnum || converter.GetStandardValuesExclusive());
			if (probe != null)
				DefaultText = ToText(property.GetValue(probe));
		}

		public PropertyInfo Property { get; }
		public TypeConverter Converter { get; }
		public PluginSettingKind Kind { get; }

		/// <summary>The property's name, which the kept value is filed under.</summary>
		public string Name { get { return Property.Name; } }

		/// <summary>The setting's label: [DisplayName], or the property's name.</summary>
		public string DisplayName { get; }

		/// <summary>The setting's help, from [Description]; null when it has none.</summary>
		public string Description { get; }

		/// <summary>The group the setting is shown in.</summary>
		public string Category { get; }

		/// <summary>The smallest value a number takes.</summary>
		public decimal Minimum { get; private set; }

		/// <summary>The largest value a number takes.</summary>
		public decimal Maximum { get; private set; }

		/// <summary>The decimals a number is shown with: none for a whole number.</summary>
		public int DecimalPlaces { get; private set; }

		/// <summary>True when only the values of the list can be chosen.</summary>
		public bool ListOnly { get; }

		/// <summary>The value a new device starts with, as invariant text.</summary>
		public string DefaultText { get; }

		/// <summary>Every setting a kind of device has, in the order the class declares them.</summary>
		/// <param name="type">The plugin's device class.</param>
		/// <param name="probe">An instance of it, read for the value each setting starts with.</param>
		public static PluginSetting[] Find(Type type, object probe)
		{
			var own = new HashSet<string>(typeof(IPluginDevice).GetProperties().Select(x => x.Name));
			var descriptors = TypeDescriptor.GetProperties(type);
			var list = new List<PluginSetting>();
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.MetadataToken))
			{
				if (own.Contains(property.Name) || property.GetIndexParameters().Length > 0 || property.GetGetMethod() == null
					|| property.PropertyType == typeof(PluginMcpServer))
					continue;
				var browsable = property.GetCustomAttribute<BrowsableAttribute>();
				if (browsable != null && !browsable.Browsable)
					continue;
				var descriptor = descriptors[property.Name];
				if (descriptor == null)
					continue;
				var kind = KindOf(property, descriptor.Converter);
				if (kind.HasValue)
					list.Add(new PluginSetting(property, descriptor.Converter, kind.Value, probe));
			}
			return list.ToArray();
		}

		/// <summary>How a property is shown, or null when it cannot be edited as text.</summary>
		static PluginSettingKind? KindOf(PropertyInfo property, TypeConverter converter)
		{
			if (property.GetSetMethod() == null)
				return PluginSettingKind.Status;
			var type = property.PropertyType;
			// Checked before the converter, which offers True and False as a list.
			if (type == typeof(bool))
				return PluginSettingKind.CheckBox;
			if (type.IsEnum || converter.GetStandardValuesSupported())
				return PluginSettingKind.List;
			if (IsNumber(type))
				return PluginSettingKind.Number;
			if (type == typeof(string) || converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string)))
				return PluginSettingKind.Text;
			return null;
		}

		static bool IsNumber(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return !type.IsEnum;
				default:
					return false;
			}
		}

		void SetLimits(PropertyInfo property)
		{
			var type = property.PropertyType;
			var whole = Type.GetTypeCode(type) != TypeCode.Single && Type.GetTypeCode(type) != TypeCode.Double && Type.GetTypeCode(type) != TypeCode.Decimal;
			DecimalPlaces = whole ? 0 : 2;
			// A fraction without limits gets those of an int: a number box holds a decimal, which a double can outgrow.
			Minimum = whole ? Convert.ToDecimal(type.GetField("MinValue").GetValue(null), CultureInfo.InvariantCulture) : int.MinValue;
			Maximum = whole ? Convert.ToDecimal(type.GetField("MaxValue").GetValue(null), CultureInfo.InvariantCulture) : int.MaxValue;
			var range = property.GetCustomAttribute<RangeAttribute>();
			if (range == null)
				return;
			Minimum = Math.Max(Minimum, Convert.ToDecimal(range.Minimum, CultureInfo.InvariantCulture));
			Maximum = Math.Min(Maximum, Convert.ToDecimal(range.Maximum, CultureInfo.InvariantCulture));
		}

		/// <summary>The values the drop-down list offers.</summary>
		/// <remarks>Asked each time the list is made, so a converter can offer what is there now, such as the computer's addresses.</remarks>
		public PluginSettingChoice[] GetChoices()
		{
			var type = Property.PropertyType;
			if (type.IsEnum)
				return Enum.GetNames(type).Select(name =>
				{
					var description = type.GetField(name).GetCustomAttribute<DescriptionAttribute>();
					return new PluginSettingChoice(description == null ? name : description.Description, name);
				}).ToArray();
			var values = Converter.GetStandardValues();
			if (values == null)
				return new PluginSettingChoice[0];
			return values.Cast<object>().Select(x => new PluginSettingChoice(Converter.ConvertToString(x), ToText(x))).ToArray();
		}

		/// <summary>What the setting takes, as the end of a sentence: "true or false", "a number from 1 to 65535"; "read only" for a status.</summary>
		public string Takes
		{
			get
			{
				switch (Kind)
				{
					case PluginSettingKind.CheckBox:
						return "true or false";
					case PluginSettingKind.Number:
						return "a number from " + Minimum.ToString(CultureInfo.InvariantCulture) + " to " + Maximum.ToString(CultureInfo.InvariantCulture);
					case PluginSettingKind.List:
						var choices = string.Join(", ", GetChoices().Select(x => x.Text));
						return (ListOnly ? "one of: " : "a value such as: ") + choices;
					case PluginSettingKind.Text:
						return "text";
					default:
						return "read only";
				}
			}
		}

		/// <summary>
		/// The text a value is kept as, from text a person or a caller typed: an entry of the list by the words it shows,
		/// or the value itself. Throws an <see cref="ArgumentException"/> that says what the setting takes when the text
		/// is no value it can hold, outside its [Range], or not among the only values its list allows.
		/// </summary>
		public string Parse(string typed)
		{
			if (Kind == PluginSettingKind.Status)
				throw new ArgumentException(DisplayName + " is read only.");
			typed = typed ?? "";
			var choice = Kind == PluginSettingKind.List
				? GetChoices().FirstOrDefault(x => string.Equals(x.Text, typed, StringComparison.OrdinalIgnoreCase))
				: null;
			var text = choice == null ? typed.Trim() : choice.Value;
			object value;
			try
			{
				value = FromText(text);
			}
			// A converter reports text it cannot read in its own way; the framework's number converters throw a bare Exception.
			catch (Exception)
			{
				throw new ArgumentException(DisplayName + " takes " + Takes + ".");
			}
			if (Kind == PluginSettingKind.Number)
			{
				var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
				if (number < Minimum || number > Maximum)
					throw new ArgumentException(DisplayName + " takes " + Takes + ".");
			}
			text = ToText(value);
			if (ListOnly && !GetChoices().Any(x => x.Value == text))
				throw new ArgumentException(DisplayName + " takes " + Takes + ".");
			return text;
		}

		/// <summary>A value as the text it is kept as.</summary>
		public string ToText(object value)
		{
			return value == null ? "" : Converter.ConvertToInvariantString(value);
		}

		/// <summary>The value kept as text.</summary>
		public object FromText(string text)
		{
			return Converter.ConvertFromInvariantString(text);
		}

		/// <summary>What a status shows: the device's value now, as a person reads it.</summary>
		public string StatusText(object device)
		{
			var value = Property.GetValue(device);
			return value == null ? "" : Converter.ConvertToString(value);
		}

		/// <summary>Gives the device a value kept as text.</summary>
		public void Write(object device, string text)
		{
			Property.SetValue(device, FromText(text));
		}
	}

	/// <summary>One value kept for one setting of one plugin device.</summary>
	public class PluginSettingValue
	{
		/// <summary>The device's instance, as the device list knows it.</summary>
		public Guid InstanceGuid { get; set; }

		/// <summary>The setting: the property's name.</summary>
		public string Name { get; set; }

		/// <summary>The value, as invariant text.</summary>
		public string Value { get; set; }
	}

	/// <summary>The values kept for plugin devices' settings: what a device's settings tab shows, and the engine gives its plugin.</summary>
	/// <remarks>
	/// Only values a person changed are kept; a setting without one has the value a new device starts with. Kept in
	/// x360ce.PluginSettings.xml, read when first needed, so a program with no plugin device with settings never reads
	/// it. Read and changed on any thread: each change is made under one lock and raises <see cref="Version"/>, which the
	/// engine compares on each read to learn there is something to give the plugin.
	/// </remarks>
	public sealed class PluginSettings
	{
		/// <summary>The program's store, kept in the settings folder.</summary>
		public static PluginSettings Current { get; set; } = new PluginSettings("PluginSettings.xml");

		/// <summary>A store kept only in memory, until saved somewhere.</summary>
		public PluginSettings()
		{
			_data = new XSettingsData<PluginSettingValue>
			{
				Items = new SortableBindingList<PluginSettingValue>(),
				ClearWhenLoading = true,
				PreventWriteToNewerFiles = false,
			};
			_loaded = true;
		}

		/// <param name="fileSuffix">The settings file's name after "x360ce.", in the settings folder.</param>
		public PluginSettings(string fileSuffix)
		{
			_data = new XSettingsData<PluginSettingValue>(fileSuffix, "Plugin device settings: one changed value of one device's setting on each row.");
		}

		readonly XSettingsData<PluginSettingValue> _data;
		readonly object _lock = new object();
		bool _loaded;

		/// <summary>Raised by every change.</summary>
		public int Version { get { return _version; } }
		volatile int _version;

		/// <summary>Reads the file the first time the store is used. Called under the lock.</summary>
		void EnsureLoaded()
		{
			if (_loaded)
				return;
			_loaded = true;
			// A missing file holds nothing: it is written when there is something to keep.
			if (_data.XmlFile != null && File.Exists(_data.XmlFile.FullName))
				_data.Load();
		}

		PluginSettingValue Find(Guid instanceGuid, string name)
		{
			foreach (var row in _data.Items)
				if (row.InstanceGuid == instanceGuid && row.Name == name)
					return row;
			return null;
		}

		/// <summary>The value kept for a device's setting, or null when it has the value a new device starts with.</summary>
		public string Get(Guid instanceGuid, string name)
		{
			lock (_lock)
			{
				EnsureLoaded();
				var row = Find(instanceGuid, name);
				return row == null ? null : row.Value;
			}
		}

		/// <summary>Keeps a value of a device's setting.</summary>
		public void Set(Guid instanceGuid, string name, string value)
		{
			lock (_lock)
			{
				EnsureLoaded();
				var row = Find(instanceGuid, name);
				if (row == null)
					_data.Items.Add(new PluginSettingValue { InstanceGuid = instanceGuid, Name = name, Value = value });
				else if (row.Value == value)
					return;
				else
					row.Value = value;
				_version = _version + 1;
			}
		}

		/// <summary>Forgets the value kept for one setting of a device, so it has the value a new device starts with.</summary>
		public void Remove(Guid instanceGuid, string name)
		{
			lock (_lock)
			{
				EnsureLoaded();
				var row = Find(instanceGuid, name);
				if (row == null)
					return;
				_data.Items.Remove(row);
				_version = _version + 1;
			}
		}

		/// <summary>Forgets every value kept for a device, so each setting has the value a new device starts with.</summary>
		public void Reset(Guid instanceGuid)
		{
			lock (_lock)
			{
				EnsureLoaded();
				var rows = _data.Items.Where(x => x.InstanceGuid == instanceGuid).ToArray();
				if (rows.Length == 0)
					return;
				foreach (var row in rows)
					_data.Items.Remove(row);
				_version = _version + 1;
			}
		}

		/// <summary>Writes the file, without the values of a device no longer listed. Writes nothing when the store was never used.</summary>
		/// <param name="devices">Every device the device list holds.</param>
		public void Save(IEnumerable<Guid> devices)
		{
			lock (_lock)
			{
				if (!_loaded || _data.XmlFile == null)
					return;
				SaveAs(_data.XmlFile.FullName, devices);
			}
		}

		/// <summary>Writes the values to a file, without those of a device no longer listed.</summary>
		public void SaveAs(string path, IEnumerable<Guid> devices)
		{
			lock (_lock)
			{
				var keep = new HashSet<Guid>(devices);
				foreach (var row in _data.Items.Where(x => !keep.Contains(x.InstanceGuid)).ToArray())
					_data.Items.Remove(row);
				_data.SaveAs(path);
			}
		}

		/// <summary>Reads the values from a file in place of those held.</summary>
		public void LoadFrom(string path)
		{
			lock (_lock)
			{
				_loaded = true;
				_data.LoadFrom(path);
				_version = _version + 1;
			}
		}

		/// <summary>Follows the settings folder when it moves.</summary>
		public void Rebase()
		{
			_data.Rebase();
		}
	}
}
