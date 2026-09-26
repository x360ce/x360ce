using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		/// <summary>The file types offered when a preset is saved or opened.</summary>
		public const string PresetFileFilter = "Controller preset (*.xml)|*.xml|All files (*.*)|*.*";

		/// <summary>Writes one controller's settings to a file, in the form the clipboard carries them.</summary>
		public static void SavePadSetting(string path, PadSetting padSetting)
		{
			var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(padSetting, null, true);
			System.IO.File.WriteAllText(path, xml);
		}

		/// <summary>Writes a preset where asked, or says why the place would not take it.</summary>
		/// <remarks>
		/// A folder that refuses the file - a game's folder under Program Files, a read-only file - is
		/// the person's to change, not a fault of the program. Saving next to a game closed the program
		/// with the refusal unhandled.
		/// </remarks>
		/// <returns>True when written; false with <paramref name="refusal"/> set when not.</returns>
		public static bool TrySavePadSetting(string path, PadSetting padSetting, out string refusal)
		{
			try
			{
				SavePadSetting(path, padSetting);
				refusal = null;
				return true;
			}
			catch (System.Exception ex) when (ex is System.UnauthorizedAccessException || ex is System.IO.IOException)
			{
				refusal = ex.Message;
				return false;
			}
		}

		/// <summary>Reads one controller's settings from a file written by <see cref="SavePadSetting"/>.</summary>
		public static PadSetting LoadPadSetting(string path)
		{
			var xml = System.IO.File.ReadAllText(path);
			return JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<PadSetting>(xml);
		}

		#region Preset as text

		/// <summary>The notations a preset can be copied in, in the order the Copy Preset menu lists them.</summary>
		public enum PresetFormat
		{
			Yaml,
			Xml,
			Json,
		}

		/// <summary>What Copy Preset copies in when no notation is chosen: YAML, the easiest of the three to read.</summary>
		public const PresetFormat DefaultPresetFormat = PresetFormat.Yaml;

		/// <summary>What a paste that holds no preset says.</summary>
		public const string NotAPresetMessage = "The text is not a preset in YAML, XML or JSON.";

		/// <summary>A controller's settings as text, in the notation asked for.</summary>
		/// <remarks>
		/// XML is the notation saved files and every earlier copy use, and the other two are made from
		/// it: JSON and YAML carry exactly the fields the XML carries, in the same order, so the three
		/// can never disagree about what a preset holds.
		/// </remarks>
		public static string PadSettingToText(PadSetting padSetting, PresetFormat format)
		{
			var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(padSetting, null, true);
			if (format == PresetFormat.Xml)
				return xml;
			var fields = FieldsOfXml(xml);
			return format == PresetFormat.Json ? JsonOfFields(fields) : YamlOfFields(fields);
		}

		/// <summary>Reads a controller's settings from text in any of the notations, told apart by how the text begins.</summary>
		/// <remarks>
		/// XML starts with an angle bracket and JSON with a brace; anything else is read as YAML, which
		/// has no opening mark of its own. JSON and YAML are turned back into the same XML the other
		/// way round, so every notation is read by the one reader that has always read presets.
		/// </remarks>
		/// <exception cref="System.IO.InvalidDataException">The text is not a preset.</exception>
		public static PadSetting PadSettingFromText(string text)
		{
			var t = (text ?? "").Trim().TrimStart('﻿');
			if (t.Length == 0)
				throw new System.IO.InvalidDataException(NotAPresetMessage);
			if (t[0] == '<')
				return JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<PadSetting>(t);
			var fields = t[0] == '{' ? FieldsOfJson(t) : FieldsOfYaml(t);
			if (fields.Count == 0)
				throw new System.IO.InvalidDataException(NotAPresetMessage);
			return JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<PadSetting>(XmlOfFields(fields));
		}

		/// <summary>Puts a preset on the clipboard as text, or says why it could not.</summary>
		public static void CopyPresetToClipboard(PadSetting padSetting, PresetFormat format = DefaultPresetFormat)
		{
			JocysCom.ClassLibrary.Controls.ControlsHelper.CopyToClipboardOrWarn(PadSettingToText(padSetting, format));
		}

		/// <summary>Opens the format menu under a Copy Preset button, and copies the preset in the format chosen.</summary>
		/// <remarks>
		/// The Copy Preset button copies in the default format with one click; the arrow button beside
		/// it opens this menu, so the other formats are one step away for the mouse, the keyboard and a
		/// screen reader alike.
		/// </remarks>
		public static void ShowCopyPresetMenu(Control button, PadSetting padSetting)
		{
			var menu = new ContextMenuStrip();
			foreach (PresetFormat format in System.Enum.GetValues(typeof(PresetFormat)))
				menu.Items.Add(new ToolStripMenuItem("Copy as " + format.ToString().ToUpperInvariant())
				{
					Name = "CopyAs" + format + "MenuItem",
					Tag = format,
				});
			PresetFormat? chosen = null;
			menu.ItemClicked += (s, e) => chosen = (PresetFormat)e.ClickedItem.Tag;
			// Copied once the menu has gone, from outside its own closing, so a warning that the
			// clipboard is busy does not open over a menu still on screen.
			menu.Closed += (s, e) => button.BeginInvoke((System.Action)(() =>
			{
				menu.Dispose();
				if (chosen.HasValue)
					CopyPresetToClipboard(padSetting, chosen.Value);
			}));
			menu.Show(button, new System.Drawing.Point(0, button.Height));
		}

		/// <summary>The fields of a preset's XML, in order: one name and text for each element under the root.</summary>
		static List<KeyValuePair<string, string>> FieldsOfXml(string xml)
		{
			var doc = new System.Xml.XmlDocument();
			doc.LoadXml(xml);
			var fields = new List<KeyValuePair<string, string>>();
			foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
				if (node.NodeType == System.Xml.XmlNodeType.Element)
					fields.Add(new KeyValuePair<string, string>(node.LocalName, node.InnerText));
			return fields;
		}

		/// <summary>The XML the preset reader expects, built from fields read out of JSON or YAML.</summary>
		static string XmlOfFields(List<KeyValuePair<string, string>> fields)
		{
			var doc = new System.Xml.XmlDocument();
			var root = doc.CreateElement(typeof(PadSetting).Name);
			doc.AppendChild(root);
			foreach (var field in fields)
			{
				string name;
				try
				{
					name = System.Xml.XmlConvert.VerifyName(field.Key);
				}
				catch (System.Xml.XmlException ex)
				{
					throw new System.IO.InvalidDataException("'" + field.Key + "' is not the name of a preset setting.", ex);
				}
				var element = doc.CreateElement(name);
				element.InnerText = field.Value;
				root.AppendChild(element);
			}
			return doc.OuterXml;
		}

		/// <summary>The fields as a JSON object, one per line, every value a string as it is in the XML.</summary>
		static string JsonOfFields(List<KeyValuePair<string, string>> fields)
		{
			var sb = new System.Text.StringBuilder("{");
			for (var i = 0; i < fields.Count; i++)
			{
				sb.Append(i == 0 ? "" : ",").AppendLine();
				sb.Append("  ").Append(JsonString(fields[i].Key)).Append(": ").Append(JsonString(fields[i].Value));
			}
			if (fields.Count > 0)
				sb.AppendLine();
			return sb.Append('}').ToString();
		}

		/// <summary>A JSON string, escaping only what JSON requires, so a formula reads as written.</summary>
		static string JsonString(string s)
		{
			var sb = new System.Text.StringBuilder("\"");
			foreach (var c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < ' ')
							sb.Append("\\u").Append(((int)c).ToString("x4"));
						else
							sb.Append(c);
						break;
				}
			}
			return sb.Append('"').ToString();
		}

		/// <summary>The fields of a JSON object, read by the JSON reader .NET carries.</summary>
		/// <remarks>
		/// That reader presents JSON as XML: each member an element named after it (or an element named
		/// "item" with the name in an attribute, where the name cannot be an XML name), and a "type"
		/// attribute saying what kind of value it holds. Numbers and booleans are taken as their text.
		/// </remarks>
		static List<KeyValuePair<string, string>> FieldsOfJson(string json)
		{
			var doc = new System.Xml.XmlDocument();
			var bytes = System.Text.Encoding.UTF8.GetBytes(json);
			using (var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader(bytes, System.Xml.XmlDictionaryReaderQuotas.Max))
			{
				try
				{
					doc.Load(reader);
				}
				catch (System.Xml.XmlException ex)
				{
					throw new System.IO.InvalidDataException(NotAPresetMessage + " " + ex.Message, ex);
				}
			}
			var root = doc.DocumentElement;
			if (root.GetAttribute("type") != "object")
				throw new System.IO.InvalidDataException(NotAPresetMessage);
			var fields = new List<KeyValuePair<string, string>>();
			foreach (var element in root.ChildNodes.OfType<System.Xml.XmlElement>())
			{
				var name = element.HasAttribute("item") ? element.GetAttribute("item") : element.LocalName;
				var type = element.GetAttribute("type");
				if (type == "object" || type == "array")
					throw new System.IO.InvalidDataException("'" + name + "' holds a group or a list; a preset holds single values only.");
				if (type == "null")
					continue;
				fields.Add(new KeyValuePair<string, string>(name, element.InnerText));
			}
			return fields;
		}

		/// <summary>The fields as YAML: one "name: value" line each.</summary>
		static string YamlOfFields(List<KeyValuePair<string, string>> fields)
		{
			var sb = new System.Text.StringBuilder();
			foreach (var field in fields)
				sb.Append(YamlScalar(field.Key)).Append(": ").Append(YamlScalar(field.Value)).AppendLine();
			return sb.ToString();
		}

		/// <summary>A YAML value, written plain where YAML reads it back as the same text and in single quotes where it would not.</summary>
		static string YamlScalar(string s)
		{
			return IsPlainYaml(s) ? s : "'" + s.Replace("'", "''") + "'";
		}

		static bool IsPlainYaml(string s)
		{
			if (string.IsNullOrEmpty(s) || s.Trim() != s)
				return false;
			// A mark that opens something else in YAML. A minus, question mark or colon opens a
			// plain value when something other than a space follows, as in an inverted axis, "-2".
			if ("-?:".IndexOf(s[0]) >= 0 && (s.Length == 1 || s[1] == ' '))
				return false;
			if (",[]{}#&*!|>'\"%@`".IndexOf(s[0]) >= 0)
				return false;
			if (s.Contains(": ") || s.Contains(" #") || s.EndsWith(":"))
				return false;
			foreach (var c in s)
				if (c < ' ')
					return false;
			// Words YAML reads as something other than text. Quoted, so a YAML tool keeps them as written.
			switch (s.ToLowerInvariant())
			{
				case "~":
				case "null":
				case "true":
				case "false":
				case "yes":
				case "no":
				case "on":
				case "off":
					return false;
			}
			return true;
		}

		/// <summary>The fields of YAML written the way <see cref="YamlOfFields"/> writes it: one level of "name: value" lines.</summary>
		/// <remarks>
		/// A preset is a flat list of named text values, so this is the part of YAML a preset can use:
		/// plain, single-quoted and double-quoted values, comments, and document markers. Nesting, lists
		/// and multi-line text are refused by line number rather than guessed at.
		/// </remarks>
		static List<KeyValuePair<string, string>> FieldsOfYaml(string yaml)
		{
			var fields = new List<KeyValuePair<string, string>>();
			var lines = yaml.Replace("\r\n", "\n").Split('\n');
			for (var i = 0; i < lines.Length; i++)
			{
				var number = i + 1;
				var line = lines[i].TrimEnd();
				var trimmed = line.TrimStart();
				if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed == "---" || trimmed == "...")
					continue;
				if (trimmed.Length != line.Length || trimmed.StartsWith("- ") || trimmed == "-")
					throw new System.IO.InvalidDataException(string.Format("{0} Line {1} is indented or a list item; a preset is one level of 'name: value' lines.", NotAPresetMessage, number));
				var pos = 0;
				var name = ReadYamlScalar(line, ref pos, true, number);
				while (pos < line.Length && line[pos] == ' ')
					pos++;
				if (pos >= line.Length || line[pos] != ':')
					throw new System.IO.InvalidDataException(string.Format("{0} Line {1} is not 'name: value'.", NotAPresetMessage, number));
				pos++;
				while (pos < line.Length && line[pos] == ' ')
					pos++;
				// Nothing after the colon, a tilde or "null" is YAML's empty value; the field is left unset.
				if (pos >= line.Length || line[pos] == '#')
					continue;
				if ("|>[{&*!".IndexOf(line[pos]) >= 0)
					throw new System.IO.InvalidDataException(string.Format("{0} Line {1} holds multi-line text, a group or a list; a preset holds single values only.", NotAPresetMessage, number));
				var plain = line[pos] != '\'' && line[pos] != '"';
				var value = ReadYamlScalar(line, ref pos, false, number);
				if (plain && (value == "~" || value == "null"))
					continue;
				fields.Add(new KeyValuePair<string, string>(name, value));
			}
			return fields;
		}

		/// <summary>One YAML value from <paramref name="pos"/>: quoted, or plain up to the colon of a name or a comment.</summary>
		static string ReadYamlScalar(string line, ref int pos, bool isName, int number)
		{
			if (line[pos] == '\'' || line[pos] == '"')
			{
				var quote = line[pos++];
				var sb = new System.Text.StringBuilder();
				while (true)
				{
					if (pos >= line.Length)
						throw new System.IO.InvalidDataException(string.Format("{0} Line {1} has a quote that is not closed.", NotAPresetMessage, number));
					var c = line[pos++];
					if (c == quote)
					{
						// Two single quotes stand for one inside single quotes.
						if (quote == '\'' && pos < line.Length && line[pos] == '\'')
						{
							sb.Append('\'');
							pos++;
							continue;
						}
						return sb.ToString();
					}
					if (quote == '"' && c == '\\' && pos < line.Length)
					{
						var e = line[pos++];
						switch (e)
						{
							case 'n': sb.Append('\n'); break;
							case 'r': sb.Append('\r'); break;
							case 't': sb.Append('\t'); break;
							case '0': sb.Append('\0'); break;
							case 'u':
								if (pos + 4 > line.Length)
									throw new System.IO.InvalidDataException(string.Format("{0} Line {1} has an unfinished \\u escape.", NotAPresetMessage, number));
								sb.Append((char)System.Convert.ToInt32(line.Substring(pos, 4), 16));
								pos += 4;
								break;
							default: sb.Append(e); break;
						}
						continue;
					}
					sb.Append(c);
				}
			}
			var start = pos;
			if (isName)
			{
				// A name ends at the colon that is followed by a space or the end of the line.
				while (pos < line.Length && !(line[pos] == ':' && (pos + 1 == line.Length || line[pos + 1] == ' ')))
					pos++;
				return line.Substring(start, pos - start).Trim();
			}
			var comment = line.IndexOf(" #", pos, System.StringComparison.Ordinal);
			var end = comment < 0 ? line.Length : comment;
			pos = line.Length;
			return line.Substring(start, end - start).Trim();
		}

		#endregion

		/// <summary>
		/// Apply all settings to XML.
		/// </summary>
		public bool ApplyAllSettingsToXML()
		{
			var padControls = MainForm.Current.PadControls;
			for (int i = 0; i < padControls.Length; i++)
			{
				// Get pad control with settings.
				var padControl = MainForm.Current.PadControls[i];
				var setting = padControl.GetSelectedSetting();
				// Skip if not selected.
				if (setting == null)
					continue;
				var padSetting = padControl.CloneCurrentPadSetting();
				// If setting doesn't exists then...
				if (!PadSettings.Items.Any(x => x.PadSettingChecksum == padSetting.PadSettingChecksum))
				{
					// Add setting to configuration.
					lock (PadSettings.SyncRoot)
						PadSettings.Items.Add(padSetting);
				}
				// If pad setting checksum changed then...
				if (setting.PadSettingChecksum != padSetting.PadSettingChecksum)
				{
					// Assign updated checksum.
					setting.PadSettingChecksum = padSetting.PadSettingChecksum;
					var ud = SettingsManager.GetDevice(setting.InstanceGuid);
					setting.Completion = UserSetting.GetCompletionPoints(padSetting, ud);
				}
			}
			CleanupPadSettings();
			return true;
		}

		/// <summary>
		/// Remove PAD settings, not attached to any device.
		/// </summary>
		public void CleanupPadSettings()
		{
			// Get all records used by Settings.
			var usedPadSettings = UserSettings.ItemsToArraySyncronized()
				.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Get all records used by Summaries.
			var usedPadSettings2 = Summaries.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Get all records used by Presets.
			var usedPadSettings3 = Presets.Items.Select(x => x.PadSettingChecksum).Distinct().ToList();
			// Combine all pad settings.
			usedPadSettings.AddRange(usedPadSettings2);
			usedPadSettings.AddRange(usedPadSettings3);
			// Get all stored padSettings.
			var allPadSettings = PadSettings.Items.Select(x => x.PadSettingChecksum).Distinct().ToArray();
			// Wipe all not used pad settings.
			var notUsed = allPadSettings.Except(usedPadSettings);
			foreach (var nu in notUsed)
			{
				var notUsedItems = PadSettings.Items.Where(x => x.PadSettingChecksum == nu).ToArray();
				PadSettings.Remove(notUsedItems);
			}
		}

		/// <summary>
		/// Insert missing pad settings and clean-up the list.
		/// </summary>
		/// <param name="list"></param>
		public void UpsertPadSettings(params PadSetting[] list)
		{
			foreach (var item in list)
			{
				// If pad setting was not found then...
				if (!PadSettings.Items.Any(x => x.PadSettingChecksum == item.PadSettingChecksum))
					// Add pad setting.
					PadSettings.Add(item);
			}
		}

		/// <summary>
		/// Insert missing settings.
		/// </summary>
		/// <param name="list"></param>
		public void UpsertSettings(params UserSetting[] list)
		{
			foreach (var item in list)
			{
				var old = UserSettings.ItemsToArraySyncronized()
					.FirstOrDefault(x => x.SettingId == item.SettingId);
				if (old == null)
				{
					UserSettings.Add(item);
				}
				// If item was updated then...
				else if (item.DateUpdated > old.DateUpdated)
				{
					JocysCom.ClassLibrary.Runtime.RuntimeHelper.CopyDataMembers(item, old);
				}
			}
		}

		/// <summary>What has already been complained about, so it is complained about once.</summary>
		static readonly HashSet<string> _unmappedReported = new HashSet<string>();

		public static bool ValidatePropertyNames(SettingsMapItem[] maps, out PropertyInfo[] propertiesToSet)
		{
			var availableNames = maps.Select(x => x.PropertyName);
			var properties = typeof(PadSetting).GetProperties();
			propertiesToSet = properties.Where(x => x.PropertyType == typeof(string) && x.Name != "ButtonBig").ToArray();
			var requiredNames = propertiesToSet.Select(x => x.Name);
			var missing = requiredNames.Except(availableNames);
			if (missing.Count() > 0)
			{
				var list = string.Join(", ", missing);
				// Said once. There is one of these checks per controller and the answer is the same for
				// all four, so it was said four times - each box modal, each drawn over the last, and the
				// buttons of the ones behind out of reach. The window could not be used and the program
				// had to be stopped from Task Manager, which is a worse fault than the one being
				// reported. Every call still returns false, so nothing carries on regardless.
				if (!_unmappedReported.Add(list))
					return false;
				MessageBox.Show("'PadSetting' class property names must match 'SettingName' class property names. Please make sure that these properties exists in 'SettingName' class:\r\n\r\n" + list);
				return false;
			}
			return true;
		}

		public void LoadPadSettingAndCleanup(UserSetting setting, PadSetting ps, bool add = false)
		{
			// Link setting with pad setting.
			setting.PadSettingChecksum = ps.PadSettingChecksum;
			// Insert pad setting first, because it will be linked with the setting.
			UpsertPadSettings(ps);
			// Insert setting if not in the list.
			if (add)
				UserSettings.Add(setting);
			// Clean-up pad settings.
			Current.CleanupPadSettings();
		}

		/// <summary>
		/// Load PAD settings to form.
		/// </summary>
		/// <param name="padIndex">Destination pad index.</param>
		/// <param name="setting">The device selected on that pad, or null when none is.</param>
		/// <param name="ps">Settings to read.</param>
		public void LoadPadSettingsIntoSelectedDevice(MapTo padIndex, UserSetting setting, PadSetting ps)
		{
			// Return if nothing selected.
			if (setting == null)
				return;
			// If setting not supplied then use empty (clear settings).
			if (ps == null)
				ps = new PadSetting();
			LoadPadSettingAndCleanup(setting, ps);
			SyncFormFromPadSetting(padIndex, ps);
			RaiseSettingsChanged(null);
			loadCount++;
			var ev = ConfigLoaded;
			if (ev != null)
			{
				ev(this, new SettingEventArgs(typeof(PadSetting).Name, loadCount));
			}
		}

		public void SyncFormFromPadSetting(MapTo padIndex, PadSetting ps)
		{
			// Get setting maps for specified PAD Control.
			var maps = SettingsMap.Where(x => x.MapTo == padIndex).ToArray();
			PropertyInfo[] properties;
			if (!ValidatePropertyNames(maps, out properties))
				return;
			// Suspend form events (do not track setting changes on the form).
			SuspendEvents();
			foreach (var p in properties)
			{
				var map = maps.First(x => x.PropertyName == p.Name);
				var key = map.IniPath.Split('\\')[1];
				var v = (string)p.GetValue(ps, null) ?? "";
				// If value is not set then...
				if (string.IsNullOrEmpty(v))
					// Restore default value.
					v = string.Format("{0}", map.DefaultValue ?? "");
				LoadSetting(map.Control, key, v);
			}
			// Resume form events (track setting changes on the form).
			ResumeEvents();
			var source = new PageSource { Checksum = ps.PadSettingChecksum };
			foreach (var p in properties)
			{
				source.Stored[p.Name] = (string)p.GetValue(ps, null) ?? "";
				source.Shown[p.Name] = GetPresetValue(maps.First(x => x.PropertyName == p.Name));
			}
			_pageSources[padIndex] = source;
		}

		/// <summary>A setting as a page gives it to a preset: left out when it is at its default.</summary>
		/// <remarks>
		/// A page shows a setting the preset leaves out at its default, so a preset read back from the
		/// page leaves the default out again. Written in, it filled every preset with the numbers
		/// nobody had set.
		/// </remarks>
		public string GetPresetValue(SettingsMapItem map)
		{
			var v = GetSettingValue(map.Control);
			return v == string.Format("{0}", map.DefaultValue) ? "" : v;
		}

		/// <summary>The preset a controller page was last filled from.</summary>
		class PageSource
		{
			public System.Guid Checksum;
			/// <summary>Each setting as the preset stored it.</summary>
			public readonly Dictionary<string, string> Stored = new Dictionary<string, string>();
			/// <summary>Each setting as the page gave it back straight after it was filled.</summary>
			public readonly Dictionary<string, string> Shown = new Dictionary<string, string>();
		}

		readonly Dictionary<MapTo, PageSource> _pageSources = new Dictionary<MapTo, PageSource>();

		/// <summary>Gives back what nobody changed on a controller page as the preset it came from stored it.</summary>
		/// <remarks>
		/// A page shows one stored value in one way but gives it back in one form of its own: a stick axis
		/// stored as "1" shows as Axis 1 and comes back as "a1", and a slider rounds. Read back as it was,
		/// a preset pasted and copied again came back as another preset.
		///
		/// With nothing changed, the page gives back the preset it was given, checksum included: the
		/// checksum is the preset's name on the server and in every list, and the one a preset arrives with
		/// need not be the one its settings give now. With something changed, a setting left alone keeps
		/// the text it was stored with, unless the page shows it at its default, which a preset leaves out.
		/// </remarks>
		/// <param name="padIndex">The page read.</param>
		/// <param name="read">The settings as the page gives them back; changed to what is kept.</param>
		public PadSetting KeepWhatWasNotChanged(MapTo padIndex, PadSetting read)
		{
			PageSource source;
			if (!_pageSources.TryGetValue(padIndex, out source))
			{
				read.PadSettingChecksum = read.CleanAndGetCheckSum();
				return read;
			}
			var untouched = source.Shown
				.Where(x => ((string)typeof(PadSetting).GetProperty(x.Key).GetValue(read, null) ?? "") == x.Value)
				.Select(x => x.Key).ToArray();
			var unchanged = untouched.Length == source.Shown.Count;
			foreach (var name in untouched)
				if (unchanged || source.Shown[name] != "")
					typeof(PadSetting).GetProperty(name).SetValue(read, source.Stored[name], null);
			read.PadSettingChecksum = unchanged && source.Checksum != System.Guid.Empty
				? source.Checksum
				: read.CleanAndGetCheckSum();
			return read;
		}

	}
}
