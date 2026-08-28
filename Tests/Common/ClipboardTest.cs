// @under-test: Engine/JocysCom/Controls/ControlsHelper.Windows.cs
// @area: clipboard   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Copying must not be able to end the program.
	/// </summary>
	/// <remarks>
	/// Only one process owns the clipboard at a time, and Windows refuses the operation while
	/// another holds it. That is ordinary: remote desktop clients, clipboard managers and office
	/// suites all take it briefly. A user reported losing the program to it, from the Copy Preset
	/// button, so the refusal is reproduced here rather than assumed.
	///
	/// What is tested is the copy itself, not the warning a button shows afterwards. The warning
	/// is a modal window, and a modal window opened where nobody can click it waits for ever.
	/// </remarks>
	[TestClass]
	public class ClipboardTest
	{

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool OpenClipboard(IntPtr hWndNewOwner);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool CloseClipboard();

		/// <summary>Runs the action on a thread the clipboard can be used from.</summary>
		private static T OnUiThread<T>(Func<T> action)
		{
			var result = default(T);
			Exception failure = null;
			var thread = new Thread(() =>
			{
				try { result = action(); }
				catch (Exception ex) { failure = ex; }
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(60)), "The clipboard call never returned.");
			if (failure != null)
				throw new AssertFailedException(failure.Message, failure);
			return result;
		}

		[TestMethod, TestCategory("clipboard"), TestCategory("critical")]
		[Description("A copy survives another program holding the clipboard")]
		public void A_copy_survives_another_program_holding_the_clipboard()
		{
			// Given: something else owns the clipboard and has not let go. Held on its own thread,
			// because the owner is a thread rather than a process and the copy has to be made from
			// somewhere that is not holding it.
			var opened = new ManualResetEventSlim();
			var release = new ManualResetEventSlim();
			var held = false;
			var holder = new Thread(() =>
			{
				held = OpenClipboard(IntPtr.Zero);
				opened.Set();
				release.Wait(TimeSpan.FromSeconds(30));
				if (held)
					CloseClipboard();
			});
			holder.SetApartmentState(ApartmentState.STA);
			holder.Start();
			Assert.IsTrue(opened.Wait(TimeSpan.FromSeconds(10)), "The holder thread never started.");
			if (!held)
				Assert.Inconclusive("The clipboard could not be taken, so a busy clipboard is not being tested.");

			try
			{
				// When: the program copies while it is held.
				var watch = System.Diagnostics.Stopwatch.StartNew();
				var copied = OnUiThread(() => ControlsHelper.CopyToClipboard("<PadSetting />"));
				watch.Stop();

				// Then: it is told no, and is still running to be told.
				Assert.IsFalse(copied,
					"The copy reported success while another program held the clipboard.");
				// And: it answered rather than waiting. The framework retries for about a second
				// before giving up, so a few seconds is generous; beyond that the call is blocked
				// on something, which for a copy means a window nobody is going to click.
				Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(8),
					"Reporting a busy clipboard took " + watch.Elapsed.TotalSeconds.ToString("0.0") +
					" seconds, so the call is waiting on something rather than returning.");
			}
			finally
			{
				release.Set();
				holder.Join(TimeSpan.FromSeconds(10));
			}
		}

		[TestMethod, TestCategory("clipboard"), TestCategory("critical")]
		[Description("A copy of ordinary text reaches the clipboard")]
		public void A_copy_of_ordinary_text_reaches_the_clipboard()
		{
			// The guard above is worthless if it swallows working copies too.
			var text = "<PadSetting id=\"" + Guid.NewGuid() + "\" />";
			var copied = OnUiThread(() => ControlsHelper.CopyToClipboard(text));
			if (!copied)
				Assert.Inconclusive("The clipboard was busy on this machine, so a working copy is not being tested.");
			Assert.AreEqual(text, OnUiThread(() => Clipboard.GetText()),
				"The text on the clipboard is not what was copied.");
		}

		[TestMethod, TestCategory("clipboard")]
		[Description("Copying nothing clears the clipboard instead of failing")]
		public void Copying_nothing_clears_the_clipboard_instead_of_failing()
		{
			// SetText refuses an empty string outright, which would report a failure that has
			// nothing to do with the clipboard being busy.
			var copied = OnUiThread(() => ControlsHelper.CopyToClipboard(""));
			if (!copied)
				Assert.Inconclusive("The clipboard was busy on this machine.");
			Assert.AreEqual("", OnUiThread(() => Clipboard.GetText()), "The clipboard was not cleared.");
		}

	}
}
