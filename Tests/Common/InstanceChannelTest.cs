// @under-test: Engine/Common/InstanceChannel.cs
// @area: admin   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Two copies of the program talk over a named pipe, one JSON message to a line.
	/// </summary>
	/// <remarks>
	/// The copy running as Administrator reports to the one that started it, and the two may be
	/// different versions. A newer copy may send types and fields an older one has never heard of, and
	/// the older one has to read past them, not stop.
	/// </remarks>
	[TestClass]
	public class InstanceChannelTest
	{
		static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

		static string NewName()
		{
			return "x360ce.test." + Guid.NewGuid().ToString("N");
		}

		/// <summary>A listening end and a connected end on the same pipe.</summary>
		static void OnPair(Action<InstanceChannel, InstanceChannel> test)
		{
			var name = NewName();
			using (var listener = InstanceChannel.Listen(name))
			{
				var connecting = Task.Run(() => InstanceChannel.Connect(name, Wait));
				Assert.IsTrue(listener.WaitForConnection(Wait), "Nobody connected.");
				using (var connected = connecting.Result)
					test(listener, connected);
			}
		}

		/// <summary>Writes lines as they are, the way a copy that knows more than this one might.</summary>
		static void OnRawSender(Action<InstanceChannel, StreamWriter> test)
		{
			var name = NewName();
			using (var listener = InstanceChannel.Listen(name))
			using (var raw = new NamedPipeClientStream(".", name, PipeDirection.InOut))
			{
				raw.Connect((int)Wait.TotalMilliseconds);
				Assert.IsTrue(listener.WaitForConnection(Wait), "Nobody connected.");
				using (var writer = new StreamWriter(raw, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" })
					test(listener, writer);
			}
		}

		[TestMethod, TestCategory("admin"), TestCategory("critical")]
		[Description("A message sent either way arrives with its type, text and values")]
		public void A_message_arrives_either_way()
		{
			OnPair((listener, connected) =>
			{
				Assert.IsTrue(connected.Send(new InstanceMessage
				{
					Type = "Progress",
					Text = "Switching off the Xbox controller",
					Values = new Dictionary<string, string> { { "Step", "2" } },
				}));
				var received = listener.Receive(Wait);
				Assert.IsNotNull(received, "Nothing arrived.");
				Assert.AreEqual("Progress", received.Type);
				Assert.AreEqual("Switching off the Xbox controller", received.Text);
				Assert.AreEqual("2", received.Values["Step"]);
				Assert.IsTrue(listener.Send(new InstanceMessage { Type = "Exit" }));
				Assert.AreEqual("Exit", connected.Receive(Wait)?.Type, "The answer did not come back.");
			});
		}

		[TestMethod, TestCategory("admin"), TestCategory("critical")]
		[Description("Fields and value shapes a newer copy adds are read past, and what is known still arrives")]
		public void What_a_newer_copy_adds_is_read_past()
		{
			OnRawSender((listener, writer) =>
			{
				writer.WriteLine("{\"Type\":\"Result\",\"Priority\":3,\"Trace\":{\"Steps\":[1,2]},"
					+ "\"Values\":{\"Ok\":\"true\",\"Place\":4,\"Detail\":{\"Why\":\"later\"}},\"Text\":\"Done\"}");
				var received = listener.Receive(Wait);
				Assert.IsNotNull(received, "A message with extra fields was lost.");
				Assert.AreEqual("Result", received.Type);
				Assert.AreEqual("Done", received.Text);
				Assert.AreEqual("true", received.Values["Ok"]);
				Assert.AreEqual("4", received.Values["Place"], "A number among the values was not read as text.");
				Assert.IsTrue(received.Values.ContainsKey("Detail"), "A value of a newer shape was dropped.");
			});
		}

		[TestMethod, TestCategory("admin")]
		[Description("A line that is not a message is skipped, and the next one still arrives")]
		public void A_line_that_is_no_message_is_skipped()
		{
			OnRawSender((listener, writer) =>
			{
				writer.WriteLine("not a message at all");
				writer.WriteLine("[1,2,3]");
				writer.WriteLine("{\"Text\":\"no type\"}");
				writer.WriteLine("{\"Type\":\"Ready\"}");
				var received = listener.Receive(Wait);
				Assert.AreEqual("Ready", received?.Type, "The message after the bad lines did not arrive.");
			});
		}

		[TestMethod, TestCategory("admin")]
		[Description("When the other copy goes, waiting ends at once and sending says it failed")]
		public void When_the_other_copy_goes_waiting_ends()
		{
			var name = NewName();
			using (var listener = InstanceChannel.Listen(name))
			{
				var connected = Task.Run(() => InstanceChannel.Connect(name, Wait));
				Assert.IsTrue(listener.WaitForConnection(Wait));
				connected.Result.Dispose();
				var watch = System.Diagnostics.Stopwatch.StartNew();
				Assert.IsNull(listener.Receive(TimeSpan.FromSeconds(30)), "A message came from a copy that has gone.");
				Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(5), "Waiting went on after the other copy had gone.");
				Assert.IsFalse(listener.IsConnected);
				Assert.IsFalse(listener.Send(new InstanceMessage { Type = "Exit" }), "Sending to a copy that has gone said it worked.");
			}
		}

		[TestMethod, TestCategory("admin")]
		[Description("Waiting with nothing sent gives up at the time given")]
		public void Waiting_with_nothing_sent_gives_up()
		{
			OnPair((listener, connected) =>
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();
				Assert.IsNull(listener.Receive(TimeSpan.FromMilliseconds(300)));
				Assert.IsTrue(watch.Elapsed >= TimeSpan.FromMilliseconds(250), "Waiting ended before the time given.");
				Assert.IsTrue(connected.IsConnected, "Waiting with nothing sent broke the connection.");
			});
		}

		[TestMethod, TestCategory("admin")]
		[Description("Sending to a copy that has stopped reading gives up instead of waiting for ever")]
		public void Sending_to_a_copy_that_stopped_reading_gives_up()
		{
			var name = NewName();
			using (var listener = InstanceChannel.Listen(name))
			// Connected and never read from, the way a copy that has hung would behave.
			using (var raw = new NamedPipeClientStream(".", name, PipeDirection.InOut))
			{
				raw.Connect((int)Wait.TotalMilliseconds);
				Assert.IsTrue(listener.WaitForConnection(Wait));
				var big = new InstanceMessage { Type = "Progress", Text = new string('x', 64 * 1024) };
				var sending = Task.Run(() =>
				{
					// The pipe fills within a few messages; the one that does not fit must come back false.
					for (var i = 0; i < 1000; i++)
						if (!listener.Send(big))
							return i;
					return -1;
				});
				Assert.IsTrue(sending.Wait(TimeSpan.FromSeconds(30)), "A send to a copy that does not read never came back.");
				Assert.IsTrue(sending.Result >= 0, "Every send said it worked, though nothing was read.");
				Assert.IsFalse(listener.IsConnected, "The channel stayed open after a send was given up on.");
			}
		}

		[TestMethod, TestCategory("admin")]
		[Description("Connecting where nobody listens gives up at the time given")]
		public void Connecting_where_nobody_listens_gives_up()
		{
			Assert.IsNull(InstanceChannel.Connect(NewName(), TimeSpan.FromMilliseconds(300)));
		}
	}
}
