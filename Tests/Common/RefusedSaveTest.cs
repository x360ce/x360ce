// @under-test: App.v4/Common/SettingsManager.cs, App.v4/MainForm.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Windows.Forms;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// A settings file Windows will not let this account write is asked about, never a crash.
	/// </summary>
	/// <remarks>
	/// The Save button wrote every settings file directly, so a file in C:\ProgramData that belonged
	/// to another account ended the program (reported against 4.22.21.0, x360ce.UserSettings.xml).
	/// The program and game lists already asked; now every save goes through the same question.
	/// </remarks>
	[TestClass]
	public class RefusedSaveTest
	{
		Func<Exception, DialogResult> _ask;

		[TestInitialize]
		public void Before() { _ask = SettingsManager.AskAfterFailedSave; }

		[TestCleanup]
		public void After() { SettingsManager.AskAfterFailedSave = _ask; }

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		public void A_refused_save_is_asked_about_and_tried_again_when_asked()
		{
			var tries = 0;
			var asked = 0;
			SettingsManager.AskAfterFailedSave = ex => { asked++; return DialogResult.Retry; };
			SettingsManager.SaveOrAsk(() =>
			{
				tries++;
				if (tries == 1)
					throw new UnauthorizedAccessException("Access to the path 'x360ce.UserSettings.xml' is denied.");
			});
			Assert.AreEqual(1, asked, "The refusal was not put to the person.");
			Assert.AreEqual(2, tries, "Try again did not save again.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		public void A_refused_save_left_as_it_is_does_not_throw()
		{
			SettingsManager.AskAfterFailedSave = ex => DialogResult.Cancel;
			SettingsManager.SaveOrAsk(() => { throw new IOException("The file is in use."); });
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		public void Other_failures_are_not_mistaken_for_a_refused_file()
		{
			SettingsManager.AskAfterFailedSave = ex => { Assert.Fail("A fault in the program was asked about as a refused file."); return DialogResult.Cancel; };
			Assert.ThrowsExactly<InvalidOperationException>(() => SettingsManager.SaveOrAsk(() => { throw new InvalidOperationException("A fault."); }));
		}
	}
}
