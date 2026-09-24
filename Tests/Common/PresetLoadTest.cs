// @under-test: App.v4/Common/SettingsManager.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A loaded preset shows every mapping it holds, as it holds it.
	/// </summary>
	/// <remarks>
	/// Loading a cloud preset for a Logitech RumblePad 2 left the B button and the D-Pad empty. The
	/// preset was written for the native library, where a bare number on a stick is an axis, and was
	/// read as a button instead; the load then treated it as a button chosen by hand and took it off
	/// every other box showing the same button.
	/// </remarks>
	[TestClass]
	public class PresetLoadTest
	{
		/// <summary>Loads the preset into a controller page and hands back what each named box shows.</summary>
		static void OnLoadedPage(PadSetting preset, Action<Func<string, string>> test)
		{
			OnLoadedPage(preset, pad => test(name => Box(pad, name).Text));
		}

		static Control Box(PadControl pad, string name)
		{
			return pad.Controls.Find(name, true).Single();
		}

		/// <summary>Loads the preset into a controller page and hands the page over.</summary>
		static void OnLoadedPage(PadSetting preset, Action<PadControl> test)
		{
			Ui.OnUiThread(() =>
			{
				// The main window shows the suspend count in its status bar; here nobody looks.
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				// A page adds its boxes to the settings map and nothing takes them out, so the boxes of
				// a page built earlier in this process would receive the load instead of this one.
				var settingsMap = SettingsManager.Current.SettingsMap;
				var earlier = settingsMap.Where(x => x.MapTo == MapTo.Controller2).ToArray();
				settingsMap.RemoveAll(x => x.MapTo == MapTo.Controller2);
				try
				{
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						SettingsManager.Current.SyncFormFromPadSetting(MapTo.Controller2, preset);
						test(pad);
					}
				}
				finally
				{
					settingsMap.RemoveAll(x => x.MapTo == MapTo.Controller2);
					settingsMap.AddRange(earlier);
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("mapping")]
		[Description("A preset written for the native library loads with its sticks, D-Pad and buttons in place")]
		public void A_preset_written_for_the_native_library_loads_whole()
		{
			// The preset from the report, as the cloud stores it.
			var preset = new PadSetting
			{
				ButtonA = "2",
				ButtonB = "3",
				ButtonX = "1",
				ButtonY = "4",
				DPad = "1",
				LeftThumbAxisX = "1",
				LeftThumbAxisY = "-2",
				RightThumbAxisX = "3",
				RightThumbAxisY = "-6",
				RightShoulder = "6",
			};
			OnLoadedPage(preset, box =>
			{
				Assert.AreEqual("Button 3", box("ButtonBComboBox"), "B button");
				Assert.AreEqual("Button 1", box("ButtonXComboBox"), "X button");
				Assert.AreEqual("POV 1", box("DPadComboBox"), "D-Pad");
				Assert.AreEqual("Axis 1", box("LeftThumbAxisXComboBox"), "Left stick across");
				Assert.AreEqual("IAxis 2", box("LeftThumbAxisYComboBox"), "Left stick up");
				Assert.AreEqual("Axis 3", box("RightThumbAxisXComboBox"), "Right stick across");
				Assert.AreEqual("IAxis 6", box("RightThumbAxisYComboBox"), "Right stick up");
			});
		}

		/// <summary>The preset from the report as the cloud gave it, copied from the Load Preset window.</summary>
		static readonly string[] CloudPreset =
		{
			"PadSettingChecksum: 874af3e4-8c5b-1088-e869-74d81ddc08be",
			"ButtonA: 2",
			"ButtonB: 3",
			"ButtonBack: 9",
			"ButtonStart: 10",
			"ButtonX: 1",
			"ButtonY: 4",
			"DPad: 1",
			"ForceEnable: 1",
			"ForceSwapMotor: 1",
			"GamePadType: 1",
			"LeftMotorPeriod: 60",
			"LeftShoulder: 5",
			"LeftThumbAxisX: 1",
			"LeftThumbAxisY: -2",
			"LeftThumbButton: 11",
			"LeftTrigger: 7",
			"LeftTriggerDeadZone: 5",
			"RightMotorPeriod: 120",
			"RightShoulder: 6",
			"RightThumbAxisX: 3",
			"RightThumbAxisY: -6",
			"RightThumbButton: 12",
			"RightTrigger: 8",
		};

		/// <summary>Loads the preset, reads it back from the page the way Copy Preset and saving do, and compares the two as text.</summary>
		static void AssertReadBackUnchanged(PadSetting preset)
		{
			var loaded = SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Yaml);
			OnLoadedPage(preset, pad =>
			{
				var readBack = SettingsManager.PadSettingToText(pad.CloneCurrentPadSetting(), SettingsManager.PresetFormat.Yaml);
				Assert.AreEqual(loaded, readBack, "The page gave back another preset than it was given.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("presets")]
		[Description("A preset pasted onto a page and copied again is the same text, checksum included")]
		public void A_preset_pasted_and_copied_again_is_the_same_text()
		{
			// Reported: pasted, then copied again, the preset came back with a1 for 1, p1 for 1, every
			// empty dead zone as 8192, the spring at 30, and so another checksum. The checksum is the
			// server's, which today's calculation over the same settings does not give, so it is kept
			// rather than worked out again.
			var pasted = string.Join(Environment.NewLine, CloudPreset);
			var preset = SettingsManager.PadSettingFromText(pasted);
			Assert.AreEqual(pasted, SettingsManager.PadSettingToText(preset, SettingsManager.PresetFormat.Yaml).TrimEnd());
			AssertReadBackUnchanged(preset);
		}

		[TestMethod, TestCategory("ui"), TestCategory("presets")]
		[Description("A preset written with type letters comes back with them, the same as one written without")]
		public void A_preset_written_with_type_letters_comes_back_with_them()
		{
			var preset = SettingsManager.PadSettingFromText(string.Join(Environment.NewLine, CloudPreset));
			preset.LeftThumbAxisX = "a1";
			preset.LeftThumbAxisY = "a-2";
			preset.DPad = "p1";
			preset.ButtonADeadZone = "8192";
			preset.PadSettingChecksum = preset.CleanAndGetCheckSum();
			AssertReadBackUnchanged(preset);
		}

		[TestMethod, TestCategory("ui"), TestCategory("presets")]
		[Description("A box changed after loading comes back changed")]
		public void A_box_changed_after_loading_comes_back_changed()
		{
			var preset = SettingsManager.PadSettingFromText(string.Join(Environment.NewLine, CloudPreset));
			OnLoadedPage(preset, pad =>
			{
				SettingsManager.Current.SetComboBoxValue((ComboBox)Box(pad, "RightThumbAxisXComboBox"), "Axis 4");
				var readBack = pad.CloneCurrentPadSetting();
				Assert.AreEqual("a4", readBack.RightThumbAxisX, "The new mapping was not given back.");
				Assert.AreEqual("-6", readBack.RightThumbAxisY, "A box left alone was rewritten.");
				Assert.AreNotEqual(preset.PadSettingChecksum, readBack.PadSettingChecksum, "A changed preset kept the old checksum.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("presets")]
		[Description("A setting at its default is left out of what the page gives back")]
		public void A_setting_at_its_default_is_left_out_when_the_page_is_read()
		{
			// Changed, so the page's own reading is what comes back rather than the preset as given.
			var preset = SettingsManager.PadSettingFromText(string.Join(Environment.NewLine, CloudPreset));
			preset.ButtonADeadZone = "8192";
			preset.ForceSpringStrength = "30";
			preset.PadSettingChecksum = preset.CleanAndGetCheckSum();
			OnLoadedPage(preset, pad =>
			{
				SettingsManager.Current.SetComboBoxValue((ComboBox)Box(pad, "RightThumbAxisXComboBox"), "Axis 4");
				var readBack = pad.CloneCurrentPadSetting();
				var atDefault = typeof(PadSetting).GetProperties()
					.Where(p => p.PropertyType == typeof(string) && typeof(SettingName).GetProperty(p.Name) != null)
					.Where(p =>
					{
						var attribute = (System.ComponentModel.DefaultValueAttribute)typeof(SettingName).GetProperty(p.Name)
							.GetCustomAttributes(typeof(System.ComponentModel.DefaultValueAttribute), false).FirstOrDefault();
						var value = (string)p.GetValue(readBack, null);
						return attribute != null && !string.IsNullOrEmpty(value) && value == (string)attribute.Value;
					})
					.Select(p => p.Name + ": " + p.GetValue(readBack, null)).ToArray();
				Assert.AreEqual(0, atDefault.Length, "Given back at their defaults: " + string.Join(", ", atDefault));
				Assert.AreEqual("5", readBack.LeftTriggerDeadZone, "A dead zone away from its default was left out.");
				Assert.AreEqual("a4", readBack.RightThumbAxisX, "The changed box was not given back.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("mapping")]
		[Description("A control a preset maps twice stays mapped twice")]
		public void A_control_a_preset_maps_twice_stays_mapped_twice()
		{
			// Taking a control off the other boxes is for choosing one by hand. A preset is loaded as
			// it was saved, or loading it and saving it again changes it.
			var preset = new PadSetting { ButtonB = "3", ButtonY = "3" };
			OnLoadedPage(preset, box =>
			{
				Assert.AreEqual("Button 3", box("ButtonBComboBox"), "B button");
				Assert.AreEqual("Button 3", box("ButtonYComboBox"), "Y button");
			});
		}
	}
}
