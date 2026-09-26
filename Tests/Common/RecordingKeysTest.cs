// @under-test: App.v4/Controls/PadControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A key pressed while recording is for the recorder, not for the page's buttons.
	/// </summary>
	/// <remarks>
	/// The buttons on a controller page answer to their underlined letters without Alt whenever
	/// nothing else takes the key. Mapping the L key opened Load Preset over the recording, and the
	/// recording never finished.
	/// </remarks>
	[TestClass]
	public class RecordingKeysTest
	{
		/// <summary>The page with its key handling reachable.</summary>
		class Page : PadControl
		{
			public Page() : base(MapTo.Controller1) { }
			public bool Mnemonic(char key) => ProcessMnemonic(key);
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		public void While_recording_a_letter_presses_no_button()
		{
			// Every button's own letter does something a test cannot afford - a dialog, a cleared
			// preset - so the page is asked with a letter no button owns, and what it answers says
			// whether the key was kept from them: taken while recording, passed on otherwise.
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new Page())
				{
					form.Controls.Add(page);
					form.Show();
					Application.DoEvents();
					page._Imager.Recorder.Recording = true;
					try
					{
						Assert.IsTrue(page.Mnemonic('Z'), "A letter pressed while recording was passed on to the buttons.");
					}
					finally
					{
						page._Imager.Recorder.Recording = false;
					}
					Assert.IsFalse(page.Mnemonic('Z'), "A letter no button owns was kept from them when not recording.");
				}
			});
		}
	}
}
