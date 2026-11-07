// @under-test: App.v4/Common/CloudClient.cs, App.v4/Common/CloudItem.cs, Engine/Common/KeyValueList.cs, Engine/Common/CloudHelper.Security.cs
// @area: cloud   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using JocysCom.ClassLibrary.Web.Services;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// How the program sends its queue to the cloud when the service fails, measured without a server.
	/// </summary>
	/// <remarks>
	/// One program sent the same Delete every five seconds for four hours, adding its keys to the message on every
	/// try, until the message was 3 MB. The program sends a message with each key once, waits longer after each failed
	/// try, stops after its last try or at once when the service refuses the message, and sends no message bigger than
	/// the service takes. A Delete is always sent: the program cannot know which of its records the cloud holds.
	/// </remarks>
	[TestClass]
	public class CloudClientTest
	{
		static readonly string ServerKey = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud).RsaNewKeys(2048).Public;

		static readonly string UserKey = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User).RsaNewKeys(2048).Public;

		static Options NewOptions()
		{
			return new Options
			{
				UserRsaPublicKey = UserKey,
				CloudRsaPublicKey = ServerKey,
				ComputerId = Guid.NewGuid(),
				ProfileId = Guid.NewGuid(),
			};
		}

		/// <summary>A client whose service is the given function and which saves nothing.</summary>
		static CloudClient Client(Func<CloudMessage, CloudMessage> execute)
		{
			return new CloudClient { Execute = execute, SaveOptions = () => { } };
		}

		/// <summary>A device as a program holds one that was never sent: no ID, no computer, no checksum.</summary>
		static UserDevice NeverSynced()
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Twin USB Gamepad", InstanceName = "Twin USB Gamepad" };
		}

		static CloudItem DeleteItem()
		{
			var message = new CloudMessage(CloudAction.Delete) { UserDevices = new[] { NeverSynced() } };
			return new CloudItem { Date = DateTime.Now, Message = message };
		}

		static WebException NoConnection()
		{
			return new WebException("Unable to connect to the remote server", WebExceptionStatus.ConnectFailure);
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("A message sent again after failed tries holds each key once, built for that try")]
		public void Every_try_sends_each_key_once()
		{
			var sent = new List<string[]>();
			var client = Client(m =>
			{
				sent.Add(m.Values.Select(x => (string)x.Key).ToArray());
				throw NoConnection();
			});
			var item = DeleteItem();
			var o = NewOptions();
			for (var i = 0; i < item.Retries; i++)
				Assert.IsNull(client.Send(item, o), "A failed try returned a reply.");
			Assert.AreEqual(item.Retries, sent.Count, "Not every try was sent.");
			var first = sent[0].OrderBy(x => x).ToArray();
			CollectionAssert.Contains(first, CloudKey.RsaPublicKey);
			CollectionAssert.Contains(first, CloudKey.RandomPassword);
			CollectionAssert.Contains(first, CloudKey.ComputerId);
			CollectionAssert.Contains(first, CloudKey.ProfileId);
			foreach (var keys in sent)
			{
				Assert.AreEqual(keys.Length, keys.Distinct().Count(), "A key repeats: " + string.Join(", ", keys));
				CollectionAssert.AreEqual(first, keys.OrderBy(x => x).ToArray(), "A try sent other keys than the first.");
			}
			Assert.AreEqual(first.Length, item.Message.Values.Count, "The message kept keys of earlier tries.");
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("Keys set twice in a message are held once, with the newest value")]
		public void A_key_set_again_replaces_its_value()
		{
			var values = new KeyValueList();
			values.Add(CloudKey.ComputerId, "first");
			values.Add(CloudKey.ComputerId, "second");
			values.UpsertRandomPassword(ServerKey);
			values.UpsertRandomPassword(ServerKey);
			Assert.AreEqual(1, values.Count(x => Equals(x.Key, CloudKey.ComputerId)));
			Assert.AreEqual(1, values.Count(x => Equals(x.Key, CloudKey.RandomPassword)));
			Assert.AreEqual("second", values.GetValue<string>(CloudKey.ComputerId));
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("After each failed try the item waits longer, never less than the first wait, and stops after its last try")]
		public void The_wait_grows_and_sending_stops_at_the_limit()
		{
			var calls = 0;
			var client = Client(m => { calls++; throw NoConnection(); });
			var item = DeleteItem();
			var o = NewOptions();
			var items = new[] { item };
			var waits = new List<TimeSpan>();
			for (var i = 0; i < 10 && item.State != CloudState.Stopped; i++)
			{
				// The moment the queue looks again: when the wait is over.
				var later = item.NextTry > DateTime.Now ? item.NextTry : DateTime.Now;
				Assert.AreSame(item, CloudClient.Due(items, later), "The item was not due after its wait.");
				var before = DateTime.Now;
				client.Send(item, o);
				if (item.State == CloudState.Stopped)
					break;
				Assert.AreEqual(CloudState.Error, item.State);
				Assert.IsNull(CloudClient.Due(items, item.NextTry.AddMilliseconds(-1)), "The item was due before its wait was over.");
				waits.Add(item.NextTry - before);
			}
			Assert.AreEqual(item.Retries, calls, "Sent more or fewer times than the item has tries.");
			Assert.AreEqual(CloudState.Stopped, item.State, "The item did not stop after its last try.");
			Assert.IsNull(CloudClient.Due(items, DateTime.MaxValue), "A stopped item is still sent.");
			Assert.IsInstanceOfType(item.Error, typeof(WebException), "The stopped item lost its reason.");
			Assert.AreEqual(item.Retries - 1, waits.Count);
			Assert.IsTrue(waits[0] >= CloudItem.RetryWaits[0], "The first wait is shorter than the queue's own gap: " + waits[0]);
			for (var i = 1; i < waits.Count; i++)
				Assert.IsTrue(waits[i] > waits[i - 1], string.Format("Wait {0} ({1}) is not longer than the one before ({2}).", i + 1, waits[i], waits[i - 1]));
			// Retry gives the item all its tries again, at once.
			item.Retry();
			Assert.AreSame(item, CloudClient.Due(items, DateTime.Now));
			Assert.AreEqual(0, item.Try);
		}

		[TestMethod, TestCategory("cloud")]
		[Description("Each wait gets up to a fifth more at random, and never less than its step")]
		public void Waits_have_jitter_above_their_step()
		{
			for (var tries = 1; tries <= 5; tries++)
			{
				var step = CloudItem.RetryWaits[Math.Min(tries, CloudItem.RetryWaits.Length) - 1];
				Assert.AreEqual(step, CloudItem.RetryWait(tries, 0));
				var most = CloudItem.RetryWait(tries, 1);
				Assert.IsTrue(most > step && most <= TimeSpan.FromTicks(step.Ticks * 6 / 5), "Wait " + most + " for step " + step);
			}
			CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2) }, CloudItem.RetryWaits);
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("A message the service refuses is not sent again")]
		public void A_refused_message_stops_at_once()
		{
			var calls = 0;
			var client = Client(m => { calls++; return new CloudMessage { ErrorCode = (int)CloudErrorCode.Error, ErrorMessage = "ProfileId value is empty" }; });
			var item = DeleteItem();
			Assert.IsNull(client.Send(item, NewOptions()));
			Assert.AreEqual(1, calls);
			Assert.AreEqual(CloudState.Stopped, item.State);
			Assert.AreEqual("ProfileId value is empty", item.Error.Message);
			Assert.IsNull(CloudClient.Due(new[] { item }, DateTime.MaxValue), "A refused message would be sent again.");
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("When the service's key changed, the new key from its reply is kept and the message is sent once more")]
		public void A_new_service_key_is_taken_and_tried_once()
		{
			var newKey = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud).RsaNewKeys(2048).Public;
			var saved = 0;
			var tries = new List<string>();
			Func<bool, CloudClient> client = secondFails => new CloudClient
			{
				SaveOptions = () => saved++,
				Execute = m =>
				{
					tries.Add(m.Values.GetValue<string>(CloudKey.RandomPassword));
					if (tries.Count == 1 || secondFails)
					{
						var reply = new CloudMessage { ErrorCode = (int)CloudErrorCode.UnableToDecrypt, ErrorMessage = "Server: Unable to decrypt" };
						reply.Values.Add(CloudKey.RsaPublicKey, newKey);
						return reply;
					}
					return new CloudMessage();
				},
			};
			var o = NewOptions();
			var item = DeleteItem();
			Assert.IsNotNull(client(false).Send(item, o), "The second try with the new key failed.");
			Assert.AreEqual(2, tries.Count, "The message was not sent once more.");
			Assert.AreEqual(newKey, o.CloudRsaPublicKey, "The new key was not kept.");
			Assert.AreEqual(1, saved, "The new key was not saved.");
			Assert.AreEqual(CloudState.Done, item.State);
			// A service that cannot read the message with its own key either gets it no more.
			tries.Clear();
			o = NewOptions();
			item = DeleteItem();
			Assert.IsNull(client(true).Send(item, o));
			Assert.AreEqual(2, tries.Count, "Sent more than once more.");
			Assert.AreEqual(CloudState.Stopped, item.State);
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("A message bigger than the service takes is not sent and stops with the reason")]
		public void An_oversized_message_is_not_sent()
		{
			var calls = 0;
			var client = Client(m => { calls++; return new CloudMessage(); });
			var game = Game(0);
			game.Comment = new string('x', CloudClient.MaxMessageBytes);
			var item = new CloudItem { Date = DateTime.Now, Message = new CloudMessage(CloudAction.Insert) { UserGames = new[] { game } } };
			Assert.IsNull(client.Send(item, NewOptions()));
			Assert.AreEqual(0, calls, "The message was sent.");
			Assert.AreEqual(CloudState.Stopped, item.State);
			StringAssert.StartsWith(item.Error.Message, "Not sent:");
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("A Delete of a record without an ID, a computer or a checksum is sent like any other")]
		public void A_delete_of_a_record_without_an_id_is_sent()
		{
			CloudMessage sent = null;
			var client = Client(m => { sent = m; return new CloudMessage { ErrorMessage = "UserDevices: 0 deleted." }; });
			var item = DeleteItem();
			var device = item.Message.UserDevices[0];
			Assert.AreEqual(Guid.Empty, device.ComputerId);
			Assert.AreEqual(Guid.Empty, device.Checksum);
			Assert.IsNotNull(client.Send(item, NewOptions()));
			Assert.IsNotNull(sent, "The Delete was not sent.");
			Assert.AreEqual(CloudAction.Delete, sent.Action);
			Assert.AreSame(device, sent.UserDevices.Single());
			Assert.AreEqual(CloudState.Done, item.State);
		}

		[TestMethod, TestCategory("cloud"), TestCategory("webservice"), TestCategory("v4")]
		[Description("Against the service: the program fetches its key, and a Delete of a record the cloud does not hold is done")]
		public void A_delete_the_cloud_cannot_match_is_done()
		{
			WebServiceTarget.Require();
			var saved = 0;
			var client = new CloudClient
			{
				Execute = m => { using (var ws = WebServiceTarget.Client()) return ws.Execute(m); },
				SaveOptions = () => saved++,
			};
			var o = NewOptions();
			o.CloudRsaPublicKey = null;
			var item = DeleteItem();
			var reply = client.Send(item, o);
			Assert.IsNotNull(reply, item.Error == null ? "No reply." : item.Error.Message);
			Assert.AreEqual(CloudState.Done, item.State);
			StringAssert.Contains(reply.ErrorMessage ?? "", "UserDevices: 0 deleted.");
			Assert.IsFalse(string.IsNullOrEmpty(o.CloudRsaPublicKey), "The service's key was not fetched.");
			Assert.AreEqual(1, saved, "The service's key was not saved.");
		}

		[TestMethod, TestCategory("cloud")]
		[Description("A stopped item lets the items behind it go; one that waits holds them back, so they keep their order")]
		public void Stopped_items_are_passed_and_waiting_items_hold_the_queue()
		{
			var now = DateTime.Now;
			var stopped = new CloudItem { State = CloudState.Stopped };
			var waiting = new CloudItem { State = CloudState.Error, NextTry = now.AddMinutes(1) };
			var fresh = new CloudItem();
			Assert.AreSame(fresh, CloudClient.Due(new[] { stopped, fresh }, now));
			Assert.IsNull(CloudClient.Due(new[] { stopped, waiting, fresh }, now));
			Assert.AreSame(waiting, CloudClient.Due(new[] { stopped, waiting, fresh }, waiting.NextTry));
			Assert.IsNull(CloudClient.Due(new CloudItem[0], now));
		}

		[TestMethod, TestCategory("cloud")]
		[Description("Failures that can pass are tried again; the service's refusals and wrong addresses are not")]
		public void Failures_that_can_pass_are_told_apart()
		{
			Assert.IsTrue(CloudClient.IsTransient(NoConnection()));
			Assert.IsTrue(CloudClient.IsTransient(new WebException("timed out", WebExceptionStatus.Timeout)));
			Assert.IsTrue(CloudClient.IsTransient(new WebException("no name", WebExceptionStatus.NameResolutionFailure)));
			Assert.IsTrue(CloudClient.IsTransient(new System.IO.IOException("connection reset")));
			Assert.IsTrue(CloudClient.IsTransient(new SoapException("database down", SoapException.ServerFaultCode)));
			Assert.IsFalse(CloudClient.IsTransient(new SoapException("bad request", SoapException.ClientFaultCode)));
			Assert.IsFalse(CloudClient.IsTransient(new WebException("certificate", WebExceptionStatus.TrustFailure)));
			Assert.IsFalse(CloudClient.IsTransient(new WebException("no status")));
			Assert.IsFalse(CloudClient.IsTransient(new InvalidOperationException("Client found response content type of 'text/html'")));
			Assert.IsFalse(CloudClient.IsTransient(new Exception("ProfileId value is empty")));
		}

		/// <summary>A game as the program stores one, with values as long as real ones get.</summary>
		static UserGame Game(int i)
		{
			var name = "Some Rather Long Game Title Number " + i;
			return new UserGame
			{
				GameId = Guid.NewGuid(),
				ComputerId = Guid.NewGuid(),
				ProfileId = Guid.NewGuid(),
				FileName = "SomeRatherLongGameTitle" + i + "-Win64-Shipping.exe",
				FileProductName = name,
				FileVersion = "4.27.2.0",
				FullPath = @"C:\Program Files (x86)\Steam\steamapps\common\" + name + @"\Binaries\Win64\SomeRatherLongGameTitle" + i + "-Win64-Shipping.exe",
				CompanyName = "A Game Company Name Ltd.",
				DInputFile = "",
				Comment = "Supports XInput only. Set Pad 1 to the wheel and turn off its force feedback if the game shakes the screen.",
				XInputPath = @"C:\Program Files (x86)\Steam\steamapps\common\" + name + @"\Binaries\Win64",
				IsEnabled = true,
				DateCreated = DateTime.Now,
				DateUpdated = DateTime.Now,
				Checksum = Guid.NewGuid(),
			};
		}

		/// <summary>A device with every value filled, as the stuck program's device was.</summary>
		static UserDevice Device()
		{
			var device = UserDeviceTest.FixedDevice();
			device.InstanceGuid = Guid.NewGuid();
			device.HidManufacturer = "Microsoft Corporation";
			device.HidDescription = "Controller (XBOX 360 For Windows)";
			device.HidDeviceId = @"HID\VID_045E&PID_028E&IG_00\8&2B8F4C7A&0&0000";
			device.HidDevicePath = @"\\?\hid#vid_045e&pid_028e&ig_00#8&2b8f4c7a&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
			device.HidParentDeviceId = @"USB\VID_045E&PID_028E\00B5E2A1";
			device.HidClassGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");
			device.HidClassDescription = "Human Interface Devices";
			device.DevManufacturer = "Microsoft";
			device.DevDescription = "Xbox 360 Controller for Windows";
			device.DevDeviceId = @"USB\VID_045E&PID_028E\00B5E2A1";
			device.DevDevicePath = @"\\?\usb#vid_045e&pid_028e#00b5e2a1#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";
			device.DevParentDeviceId = @"USB\ROOT_HUB30\5&1A2B3C4D&0&0";
			device.DevClassDescription = "Xbox 360 Peripherals";
			device.DateCreated = DateTime.Now;
			device.DateUpdated = DateTime.Now;
			device.Checksum = Guid.NewGuid();
			return device;
		}

		[TestMethod, TestCategory("cloud"), TestCategory("critical")]
		[Description("The biggest upload a person makes is well under the size limit")]
		public void The_largest_upload_is_well_under_the_limit()
		{
			var o = NewOptions();
			// Upload To Cloud sends each kind of record in one message, so the game list is the biggest. The program's
			// game list knows about 120 games; 1,000 is far more than anybody has.
			var games = new CloudMessage(CloudAction.Insert) { UserGames = Enumerable.Range(0, 1000).Select(Game).ToArray() };
			var devices = new CloudMessage(CloudAction.Insert) { UserDevices = Enumerable.Range(0, 100).Select(x => Device()).ToArray() };
			CloudClient.Secure(games, o);
			CloudClient.Secure(devices, o);
			var gamesBytes = CloudClient.MessageBytes(games);
			var devicesBytes = CloudClient.MessageBytes(devices);
			Console.WriteLine("1,000 games: {0:N0} bytes; 100 devices: {1:N0} bytes; limit: {2:N0} bytes.", gamesBytes, devicesBytes, CloudClient.MaxMessageBytes);
			Assert.IsTrue(gamesBytes * 2 < CloudClient.MaxMessageBytes, "1,000 games come to more than half the limit: " + gamesBytes);
			Assert.IsTrue(devicesBytes * 2 < CloudClient.MaxMessageBytes, "100 devices come to more than half the limit: " + devicesBytes);
			// The service refuses a request over 4 MB unread; the limit leaves room for the SOAP envelope around it.
			Assert.IsTrue(CloudClient.MaxMessageBytes < 4 * 1024 * 1024 - 64 * 1024, "The limit lets through messages the service refuses.");
		}

		#region The queue's own thread

		/// <summary>A client whose queue runs on its own thread, as the program's does, sending with the given function.</summary>
		/// <param name="network">Says whether a network is up; null for this computer's own.</param>
		static CloudClient Running(System.Windows.Forms.UserControl page, Func<CloudMessage, CloudMessage> execute, Func<bool> network = null)
		{
			var client = Client(execute);
			var o = NewOptions();
			client.ReadOptions = () => o;
			if (network != null)
				client.ReadNetworkAvailable = network;
			client.StartServer(null, page);
			return client;
		}

		[TestMethod, TestCategory("cloud")]
		[Description("With nothing to send the queue sleeps: nothing wakes it until an item is added")]
		public void An_empty_queue_sleeps()
		{
			using (var page = new System.Windows.Forms.UserControl())
			using (var client = Running(page, m => { throw new AssertFailedException("Nothing was queued, and something was sent."); }))
			{
				var timer = client.TasksTimer;
				Assert.AreEqual(default(DateTime), timer.NextRunTime, "A wake is set for an empty queue.");
				Thread.Sleep(TimeSpan.FromSeconds(6));
				Assert.AreEqual(0L, timer.ThreadCount, "The empty queue woke up.");
			}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("An item added to the queue is sent at once")]
		public void An_added_item_is_sent_at_once()
		{
			var sent = new ManualResetEventSlim();
			using (var page = new System.Windows.Forms.UserControl())
			using (var client = Running(page, m => { sent.Set(); return new CloudMessage(); }))
			{
				var item = DeleteItem();
				client.TasksTimer.DoActionNow(item);
				Assert.IsTrue(sent.Wait(2000), "The item was not sent at once.");
				Assert.IsTrue(SpinWait.SpinUntil(() => client.TasksTimer.Queue.Count == 0, 2000), "The sent item stayed in the queue.");
				Assert.AreEqual(CloudState.Done, item.State);
			}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("An item whose try failed is sent again when its wait is over, not up to a sleep later")]
		public void A_failed_item_is_sent_again_when_its_wait_is_over()
		{
			var tries = new List<DateTime>();
			var again = new ManualResetEventSlim();
			using (var page = new System.Windows.Forms.UserControl())
			using (var client = Running(page, m =>
			{
				lock (tries)
				{
					tries.Add(DateTime.Now);
					if (tries.Count == 1)
						throw NoConnection();
				}
				again.Set();
				return new CloudMessage();
			}))
			{
				var item = DeleteItem();
				client.TasksTimer.DoActionNow(item);
				Assert.IsTrue(SpinWait.SpinUntil(() => item.State == CloudState.Error, 2000), "The first try did not fail.");
				var due = item.NextTry;
				Assert.IsTrue(again.Wait(TimeSpan.FromSeconds(15)), "The item was not sent again.");
				var late = tries[1] - due;
				Console.WriteLine("Due {0:HH:mm:ss.fff}, sent again {1:HH:mm:ss.fff}: {2:N0} ms late.", due, tries[1], late.TotalMilliseconds);
				Assert.IsTrue(late >= TimeSpan.FromMilliseconds(-20), "The item was sent again before its wait was over.");
				Assert.IsTrue(late < TimeSpan.FromMilliseconds(500), "The item was sent again " + late.TotalMilliseconds + " ms after its wait was over.");
			}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("An item waiting after a failed try is sent at once when the network comes back")]
		public void A_waiting_item_is_sent_when_the_network_comes_back()
		{
			var tries = 0;
			var again = new ManualResetEventSlim();
			// The network as this test says it is, not as this computer's is.
			var up = 1;
			using (var page = new System.Windows.Forms.UserControl())
			using (var client = Running(page, m =>
			{
				if (Interlocked.Increment(ref tries) == 1)
					throw NoConnection();
				again.Set();
				return new CloudMessage();
			}, () => Volatile.Read(ref up) == 1))
			{
				// The first look at the network, which the queue does on a worker as it starts.
				Assert.IsTrue(SpinWait.SpinUntil(() => client.IsNetworkAvailable, 10000), "The network was not read as the queue started.");
				var item = DeleteItem();
				client.TasksTimer.DoActionNow(item);
				Assert.IsTrue(SpinWait.SpinUntil(() => item.State == CloudState.Error && !client.TasksTimer.IsRunning, 2000), "The first try did not fail.");
				// The network goes, and Windows says so: nothing is sent.
				Volatile.Write(ref up, 0);
				client.NetworkInformation_NetworkAvailabilityChanged(null, null);
				Assert.IsFalse(client.IsNetworkAvailable, "The network was not read again.");
				// It comes back, and Windows says so.
				Volatile.Write(ref up, 1);
				client.NetworkInformation_NetworkAvailabilityChanged(null, null);
				Assert.IsTrue(client.IsNetworkAvailable, "The network was not read again.");
				Assert.IsTrue(again.Wait(1000), "The waiting item was not sent when the network came back.");
				Assert.AreEqual(2, tries, "The item was not sent once more.");
			}
		}

		#endregion
	}
}
