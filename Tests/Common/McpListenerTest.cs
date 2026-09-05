// @under-test: App.v4/Mcp/McpServer.cs, App.v4/Mcp/McpClient.cs
// @area: mcp   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using x360ce.App;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>The door: loopback only, token required, the level read from the program each time, and a bad request answered rather than fatal.</summary>
	[TestClass]
	public class McpListenerTest
	{
		const int Port = 37361;
		const string List = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}";

		[TestMethod, TestCategory("mcp"), TestCategory("critical"), Timeout(30000)]
		[Description("A request with the token is answered; without it, refused; the level is read live; bad and non-ASCII input is answered; a bad port is recorded")]
		public void Token_gates_and_level_is_live()
		{
			McpServerTest.UseSample(AiAccess.Read);
			Assert.IsFalse(McpListener.Start(80, "secret"), "A port outside the range must be refused, not tried.");
			StringAssert.Contains(McpListener.LastError, "1024");
			Assert.IsTrue(McpListener.Start(Port, "secret"), McpListener.LastError);
			try
			{
				var refused = Assert.ThrowsExactly<WebException>(() => McpClient.Post(Port, "wrong", List));
				Assert.AreEqual(HttpStatusCode.Unauthorized, ((HttpWebResponse)refused.Response).StatusCode);
				Assert.IsFalse(McpClient.Post(Port, "secret", List).Contains("poke_value"), "A Configure tool was listed at Read.");
				McpCatalog.Level = () => AiAccess.Configure;
				Assert.IsTrue(McpClient.Post(Port, "secret", List).Contains("poke_value"), "The level was not read live.");
				StringAssert.Contains(McpClient.Post(Port, "secret", "{not json"), "-32700");
				var poked = McpClient.Post(Port, "secret", "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"poke_value\",\"arguments\":{\"value\":\"ü\"}}}");
				StringAssert.Contains(poked, "poked ü", "A non-ASCII value must arrive as sent; bodies are UTF-8.");
				Assert.AreEqual("", McpClient.Post(Port, "secret", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}"), "A notification has no answer.");
			}
			finally
			{
				McpListener.Stop();
			}
			Assert.IsFalse(McpListener.IsRunning);
		}
	}
}
