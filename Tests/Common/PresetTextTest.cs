// @under-test: App.v4/Common/SettingsManager.XML.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A preset copied as XML, JSON or YAML, and pasted back from any of them.
	/// </summary>
	/// <remarks>
	/// XML is the notation saved files and every earlier copy use. JSON and YAML are made from it
	/// and turned back into it, so all three carry the same fields, and a paste tells them apart by
	/// how the text begins.
	/// </remarks>
	[TestClass]
	public class PresetTextTest
	{
		/// <summary>A preset with the kinds of value that trip a notation up.</summary>
		static PadSetting Sample()
		{
			return new PadSetting
			{
				PadSettingChecksum = new Guid("57c514d1-f2a5-77bd-a448-00277be5672c"),
				ButtonA = "3",
				LeftThumbAxisY = "-2",
				DPad = "p1",
				LeftTrigger = "x-2",
				RightTrigger = "true",
				// Characters with a meaning in one notation or another: quotes, a backslash, a colon
				// followed by a space, a hash after a space, a slash and angle brackets.
				ButtonX = "it's \"quoted\" \\ a: b #not a comment / <1>",
			};
		}

		static void AssertSame(PadSetting expected, PadSetting actual, string format)
		{
			Assert.AreEqual(expected.PadSettingChecksum, actual.PadSettingChecksum, format + ": checksum");
			foreach (var p in typeof(PadSetting).GetProperties().Where(x => x.PropertyType == typeof(string) && x.CanWrite))
				Assert.AreEqual(p.GetValue(expected, null), p.GetValue(actual, null), format + ": " + p.Name);
		}

		[TestMethod, TestCategory("critical")]
		[Description("Every format carries the whole preset there and back")]
		public void Every_format_carries_the_whole_preset_there_and_back()
		{
			var preset = Sample();
			foreach (SettingsManager.PresetFormat format in Enum.GetValues(typeof(SettingsManager.PresetFormat)))
			{
				var text = SettingsManager.PadSettingToText(preset, format);
				Console.WriteLine("--- " + format + Environment.NewLine + text);
				AssertSame(preset, SettingsManager.PadSettingFromText(text), format.ToString());
			}
		}

		[TestMethod, TestCategory("critical")]
		[Description("XML is what it always was, so older copies and saved files still paste")]
		public void Xml_is_what_it_always_was()
		{
			var preset = Sample();
			Assert.AreEqual(
				JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(preset, null, true),
				SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Xml));
		}

		[TestMethod]
		[Description("JSON and YAML are written one setting to a line, readable as copied")]
		public void Json_and_yaml_are_one_setting_to_a_line()
		{
			var preset = Sample();
			var xmlFields = System.Xml.Linq.XDocument.Parse(SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Xml)).Root.Elements().Count();
			var json = SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Json);
			var jsonLines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			Assert.AreEqual("{", jsonLines[0]);
			Assert.AreEqual("}", jsonLines[jsonLines.Length - 1]);
			Assert.AreEqual(xmlFields, jsonLines.Length - 2, "JSON does not carry one line per XML field.");
			StringAssert.StartsWith(jsonLines[1], "  \"PadSettingChecksum\": \"57c514d1-");
			var yamlLines = SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Yaml)
				.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(xmlFields, yamlLines.Length, "YAML does not carry one line per XML field.");
			CollectionAssert.Contains(yamlLines, "LeftThumbAxisY: -2", "A value YAML reads as written is left plain.");
			CollectionAssert.Contains(yamlLines, "RightTrigger: 'true'", "A word YAML would read as a boolean is quoted.");
		}

		[TestMethod]
		[Description("An empty setting is left out of every format, as the XML has always left it out")]
		public void An_empty_setting_is_left_out_of_every_format()
		{
			var preset = Sample();
			preset.ButtonB = "";
			foreach (SettingsManager.PresetFormat format in Enum.GetValues(typeof(SettingsManager.PresetFormat)))
			{
				var text = SettingsManager.PadSettingToText(preset, format);
				Assert.IsFalse(text.Contains("ButtonB"), format + " carries an empty setting the XML leaves out.");
				Assert.IsNull(SettingsManager.PadSettingFromText(text).ButtonB, format + ": an empty setting came back set.");
			}
		}

		[TestMethod]
		[Description("A paste reads JSON and YAML written by hand, in any order")]
		public void A_paste_reads_json_and_yaml_written_by_hand()
		{
			var json = SettingsManager.PadSettingFromText("  {\n\"ButtonB\": 2, \"ButtonA\": \"3\", \"DPad\": null }");
			Assert.AreEqual("3", json.ButtonA);
			Assert.AreEqual("2", json.ButtonB, "A number is taken as its text.");
			Assert.IsNull(json.DPad, "A null leaves the setting unset.");
			var yaml = SettingsManager.PadSettingFromText(
				"﻿---\n# a comment\nButtonB: '2' # after a quoted value\nButtonA: 3 # after a plain one\nButtonX: \"a\\tb\"\nLeftTrigger: 'it''s'\nDPad:\n...\n");
			Assert.AreEqual("3", yaml.ButtonA);
			Assert.AreEqual("2", yaml.ButtonB);
			Assert.AreEqual("a\tb", yaml.ButtonX);
			Assert.AreEqual("it's", yaml.LeftTrigger);
			Assert.IsNull(yaml.DPad, "A name with no value leaves the setting unset.");
		}

		[TestMethod]
		[Description("Text that is no preset is refused and says why")]
		public void Text_that_is_no_preset_is_refused_and_says_why()
		{
			var cases = new[]
			{
				"",
				"just some words",
				"Buttons:\n  A: 3\n",
				"- ButtonA: 3\n",
				"ButtonA: |\n  3\n",
				"ButtonA: 'not closed\n",
				"{ \"Buttons\": { \"A\": 3 } }",
				"[1, 2]",
				"{ \"ButtonA\": ",
				"Not a name: 3\n",
			};
			foreach (var text in cases)
			{
				var ex = Assert.ThrowsExactly<InvalidDataException>(() => SettingsManager.PadSettingFromText(text), "Accepted: " + text);
				Console.WriteLine("{0,-30} -> {1}", text.Replace("\n", "\\n"), ex.Message);
			}
		}
	}
}
