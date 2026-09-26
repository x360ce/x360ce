// @under-test: App.v4/Common/DInput/ElevatedDevices.cs
// @area: admin   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Putting controllers in order asks for Administrator once, not once per controller step.
	/// </summary>
	/// <remarks>
	/// A real controller is switched off and later switched on again, and each used to start its own
	/// copy of the program as Administrator, so Windows asked twice. One copy now makes every switch
	/// the reorder needs, as the copy that started it asks, and answers over an instance channel.
	/// Here that copy runs on a thread with a switch that only writes down what it was asked.
	/// </remarks>
	[TestClass]
	public class ElevatedDevicesTest
	{
		const string A = @"USB\VID_045E&PID_02FF\TEST-A";
		const string B = @"USB\VID_045E&PID_02FF\TEST-B";

		/// <summary>Stands in for the copy started as Administrator: serves on a thread with a pretend switch.</summary>
		class PretendAdministrator
		{
			public int Starts;
			public readonly List<string> Switched = new List<string>();
			public Func<string, bool, bool> Answer = (id, on) => true;
			public Task Serving;

			public bool Start(string pipeName)
			{
				Starts++;
				Serving = Task.Run(() =>
				{
					using (var channel = InstanceChannel.Connect(pipeName, TimeSpan.FromSeconds(5)))
						ElevatedDevices.Serve(channel, (id, on) =>
						{
							lock (Switched)
								Switched.Add((on ? "on " : "off ") + id);
							return Answer(id, on);
						}, TimeSpan.FromSeconds(30));
				});
				return true;
			}
		}

		[TestMethod, TestCategory("admin"), TestCategory("critical")]
		[Description("Switching a controller off and on again asks for Administrator once")]
		public void Switching_off_and_on_again_asks_for_Administrator_once()
		{
			var admin = new PretendAdministrator();
			using (var devices = new ElevatedDevices(admin.Start))
			{
				string error;
				Assert.IsTrue(devices.Switch(false, new[] { A }, out error), "Switching off failed: " + error);
				Assert.IsTrue(devices.Switch(true, new[] { A }, out error), "Switching on failed: " + error);
			}
			Assert.AreEqual(1, admin.Starts, "Administrator was asked for more than once.");
			CollectionAssert.AreEqual(new[] { "off " + A, "on " + A }, admin.Switched);
			Assert.IsTrue(admin.Serving.Wait(TimeSpan.FromSeconds(5)), "The Administrator copy did not stop when told.");
		}

		[TestMethod, TestCategory("admin")]
		[Description("A controller Windows would not switch is reported, with why when Windows said")]
		public void A_controller_Windows_would_not_switch_is_reported()
		{
			var admin = new PretendAdministrator
			{
				Answer = (id, on) => id != B ? true : throw new InvalidOperationException("Device is in use."),
			};
			using (var devices = new ElevatedDevices(admin.Start))
			{
				string error;
				Assert.IsFalse(devices.Switch(false, new[] { A, B }, out error), "A refusal was reported as done.");
				StringAssert.Contains(error, "1 of 2", "The report does not say how many were switched.");
				StringAssert.Contains(error, "Device is in use.", "The report does not say why.");
			}
		}

		[TestMethod, TestCategory("admin")]
		[Description("A refused Administrator prompt fails at once instead of waiting for a copy that never comes")]
		public void A_refused_prompt_fails_at_once()
		{
			var starts = 0;
			using (var devices = new ElevatedDevices(pipeName => { starts++; return false; }))
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();
				string error;
				Assert.IsFalse(devices.Switch(false, new[] { A }, out error));
				Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(2), "A refusal was waited on.");
				Assert.IsFalse(string.IsNullOrEmpty(error), "A refusal said nothing.");
				Assert.IsFalse(devices.Switch(true, new[] { A }, out error), "A refusal was forgotten.");
			}
			Assert.AreEqual(1, starts, "Administrator was asked again after it was refused.");
		}

		[TestMethod, TestCategory("admin")]
		[Description("Progress from the Administrator copy is passed on, and messages it does not know are passed over")]
		public void Progress_is_passed_on_and_unknown_messages_passed_over()
		{
			// A newer Administrator copy, sending a type this copy has never heard of before its answer.
			Task serving = null;
			Func<string, bool> start = pipeName =>
			{
				serving = Task.Run(() =>
				{
					using (var channel = InstanceChannel.Connect(pipeName, TimeSpan.FromSeconds(5)))
					{
						var request = channel.Receive(TimeSpan.FromSeconds(5));
						channel.Send(new InstanceMessage { Type = "SomethingNewer", Text = "ignore me" });
						channel.Send(new InstanceMessage { Type = ElevatedDevices.Progress, Text = "Windows is removing it" });
						var result = new InstanceMessage { Type = ElevatedDevices.Result };
						result.Values["Id"] = request.Value("Id");
						result.Values["Done"] = "1";
						channel.Send(result);
						channel.Receive(TimeSpan.FromSeconds(5));
					}
				});
				return true;
			};
			var said = new List<string>();
			using (var devices = new ElevatedDevices(start) { Said = said.Add })
			{
				string error;
				Assert.IsTrue(devices.Switch(false, new[] { A }, out error), "The answer after an unknown message was lost: " + error);
			}
			CollectionAssert.AreEqual(new[] { "Windows is removing it" }, said);
			Assert.IsTrue(serving.Wait(TimeSpan.FromSeconds(5)));
		}
	}
}
