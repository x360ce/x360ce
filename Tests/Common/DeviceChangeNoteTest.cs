// @under-test: App.v4/MainForm.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A device the current game uses that disconnects or connects again is named in a note over the game, once per
	/// change of the device list, and nothing else is.
	/// </summary>
	/// <remarks>
	/// The window is usually minimised while a game runs, so a pad that dropped out showed only as a game that
	/// stopped answering.
	/// </remarks>
	[TestClass]
	public class DeviceChangeNoteTest
	{
		static UserDevice Device(string name, bool online)
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), InstanceName = name, ProductName = name + " product", IsOnline = online };
		}

		static string Note(Dictionary<Guid, bool> last, UserDevice[] devices, UserDevice[] mapped)
		{
			bool anyGone;
			return MainForm.DeviceChangeNote(last, devices, mapped, out anyGone);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A start says nothing, a mapped device that goes and comes back is named each time, and no change says nothing")]
		public void A_mapped_device_is_named_when_it_goes_and_comes_back()
		{
			var last = new Dictionary<Guid, bool>();
			var wheel = Device("Wheel", true);
			var all = new[] { wheel, Device("Keyboard", true) };
			var mapped = new[] { wheel };
			Assert.IsNull(Note(last, all, mapped), "The first look only learns the states.");
			Assert.AreEqual(2, last.Count);

			wheel.IsOnline = false;
			bool anyGone;
			Assert.AreEqual("Wheel disconnected", MainForm.DeviceChangeNote(last, all, mapped, out anyGone));
			Assert.IsTrue(anyGone);
			Assert.IsNull(Note(last, all, mapped), "The same state again is not a change.");

			wheel.IsOnline = true;
			Assert.AreEqual("Wheel connected", MainForm.DeviceChangeNote(last, all, mapped, out anyGone));
			Assert.IsFalse(anyGone);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device the game does not use is never named, and its state is still kept for when it is mapped")]
		public void An_unmapped_device_is_not_named_but_its_state_is_kept()
		{
			var last = new Dictionary<Guid, bool>();
			var pad = Device("Pad", true);
			var all = new[] { pad };
			Note(last, all, new UserDevice[0]);
			pad.IsOnline = false;
			Assert.IsNull(Note(last, all, new UserDevice[0]), "Not mapped: not named.");
			Assert.IsNull(Note(last, all, new[] { pad }), "Mapped while away: nothing changed since the last look.");
			pad.IsOnline = true;
			Assert.AreEqual("Pad connected", Note(last, all, new[] { pad }));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Several changes in one look make one note, the ones that went first; a device without an instance name goes by its product")]
		public void Several_changes_make_one_note()
		{
			var last = new Dictionary<Guid, bool>();
			var a = Device("A", true);
			var b = Device("B", true);
			var c = Device("C", false);
			var all = new[] { a, b, c };
			Note(last, all, all);
			a.IsOnline = false;
			b.IsOnline = false;
			c.IsOnline = true;
			c.InstanceName = "";
			bool anyGone;
			Assert.AreEqual("A, B disconnected. C product connected", MainForm.DeviceChangeNote(last, all, all, out anyGone));
			Assert.IsTrue(anyGone);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The note is asked for ahead of the check that skips the rest while the window is minimised")]
		public void The_note_is_asked_for_while_the_window_is_minimised()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			var start = source.IndexOf("private void DHelper_DevicesUpdated(", StringComparison.Ordinal);
			Assert.IsTrue(start > 0, "Handler not found.");
			var note = source.IndexOf("ControlsHelper.BeginInvoke(NoteMappedDeviceChanges)", start, StringComparison.Ordinal);
			var minimised = source.IndexOf("if (!FormEventsEnabled)", start, StringComparison.Ordinal);
			Assert.IsTrue(note > 0 && minimised > 0 && note < minimised,
				"While a game runs the window is minimised, and the handler stops at that check; the note must be asked for before it.");
		}
	}
}
