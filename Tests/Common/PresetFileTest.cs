// @under-test: App.v4/Common/SettingsManager.XML.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.App;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A preset file is the one form of a controller's settings that leaves the machine, so what goes
	/// in must come back out unchanged, and a file that is not one must be refused rather than applied.
	/// </summary>
	[TestClass]
	public class PresetFileTest
	{
		[TestMethod, TestCategory("presets"), TestCategory("smoke")]
		[Description("A preset written to a file reads back with the same mappings")]
		public void Saved_preset_reads_back_the_same()
		{
			var written = new PadSetting();
			written.ButtonA = "1";
			written.ButtonB = "2";
			written.ButtonBack = "7";
			written.DPad = "p1";
			written.DPadUp = "d1";
			written.AxisToDPadDeadZone = "512";
			written.ForceEnable = "1";
			var path = TempPath();
			try
			{
				SettingsManager.SavePadSetting(path, written);
				var read = SettingsManager.LoadPadSetting(path);
				Assert.AreEqual("1", read.ButtonA);
				Assert.AreEqual("2", read.ButtonB);
				Assert.AreEqual("7", read.ButtonBack);
				Assert.AreEqual("p1", read.DPad);
				Assert.AreEqual("d1", read.DPadUp);
				Assert.AreEqual("512", read.AxisToDPadDeadZone);
				Assert.AreEqual("1", read.ForceEnable);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[TestMethod, TestCategory("presets"), TestCategory("smoke")]
		[Description("A file that is not a preset is refused rather than applied as an empty one")]
		public void A_file_that_is_not_a_preset_is_refused()
		{
			var path = TempPath();
			try
			{
				File.WriteAllText(path, "This is a note, not a preset.");
				try
				{
					SettingsManager.LoadPadSetting(path);
				}
				catch (Exception)
				{
					return;
				}
				Assert.Fail("A text file was read as a preset, so opening the wrong file would wipe the controller.");
			}
			finally
			{
				File.Delete(path);
			}
		}

		[TestMethod, TestCategory("presets"), TestCategory("smoke")]
		[Description("A folder that refuses the preset file is reported, not thrown")]
		public void A_refused_save_is_reported_rather_than_thrown()
		{
			// A read-only file in the way: the folder answers "denied", as a game's folder under
			// Program Files does. Reported against 4.22.21.0 as the program closing on Save Preset.
			var path = TempPath();
			File.WriteAllText(path, "");
			File.SetAttributes(path, FileAttributes.ReadOnly);
			try
			{
				string refusal;
				var saved = SettingsManager.TrySavePadSetting(path, new PadSetting(), out refusal);
				Assert.IsFalse(saved, "A read-only file was reported as written.");
				Assert.IsFalse(string.IsNullOrEmpty(refusal), "Nothing says why the save was refused.");
				StringAssert.Contains(refusal, path, "The refusal does not name the file.");
			}
			finally
			{
				File.SetAttributes(path, FileAttributes.Normal);
				File.Delete(path);
			}
		}

		[TestMethod, TestCategory("presets"), TestCategory("smoke")]
		[Description("A save that works answers so, with nothing to say")]
		public void A_save_that_works_says_nothing()
		{
			var path = TempPath();
			try
			{
				string refusal;
				Assert.IsTrue(SettingsManager.TrySavePadSetting(path, new PadSetting(), out refusal));
				Assert.IsNull(refusal);
				Assert.IsTrue(File.Exists(path));
			}
			finally
			{
				File.Delete(path);
			}
		}

		static string TempPath()
		{
			return Path.Combine(Path.GetTempPath(), "x360ce-preset-" + Guid.NewGuid().ToString("N") + ".xml");
		}
	}
}
