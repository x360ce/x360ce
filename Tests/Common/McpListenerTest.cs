// @under-test: Engine/Mcp/McpServer.cs, Engine/Mcp/McpClient.cs
// @area: mcp   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using x360ce.App;
using x360ce.App.Mcp;
using x360ce.Engine.Mcp;

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
			Assert.IsFalse(McpListener.Start(Options.LoopbackAddress, 80, "secret"), "A port outside the range must be refused, not tried.");
			StringAssert.Contains(McpListener.LastError, "1024");
			// Every network needs a reservation Windows only grants to an Administrator; without one the
			// door stays shut and the reason names the Fix. With one, it opens, and that is fine too.
			if (!McpListener.Start(Options.AnyAddress, Port, "secret"))
			{
				Assert.IsTrue(McpListener.NeedsUrlReservation, McpListener.LastError);
				StringAssert.Contains(McpListener.LastError, "netsh http add urlacl");
			}
			Assert.IsTrue(McpListener.Start(Options.LoopbackAddress, Port, "secret"), McpListener.LastError);
			Assert.IsFalse(McpListener.NeedsUrlReservation);
			try
			{
				var refused = Assert.ThrowsExactly<WebException>(() => McpClient.Post(Port, "wrong", List));
				Assert.AreEqual(HttpStatusCode.Unauthorized, ((HttpWebResponse)refused.Response).StatusCode);
				// Told nothing, a caller that found the door went looking for the token in the settings files.
				using (var reader = new System.IO.StreamReader(refused.Response.GetResponseStream()))
					StringAssert.Contains(reader.ReadToEnd(), "x360ce.exe -Ai", "The refusal does not point at the way in that needs no token.");
				const string Poke = "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"poke_value\",\"arguments\":{\"value\":\"x\"}}}";
				var poke = McpClient.Post(Port, "secret", Poke);
				StringAssert.Contains(poke, "\"isError\":true", "A Configure tool was allowed at Read.");
				StringAssert.Contains(poke, "needs Configure access");
				Assert.AreEqual(HttpStatusCode.Forbidden, PostWithOrigin("http://example.com"), "A page from another site reached the door.");
				Assert.AreEqual(HttpStatusCode.OK, PostWithOrigin("http://localhost:6274"), "A page on this computer was refused.");
				McpCatalog.Level = () => AiAccess.Configure;
				StringAssert.Contains(McpClient.Post(Port, "secret", Poke), "poked x", "The level was not read live.");
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

		/// <summary>Posts the tools list with the token and an Origin header, as a browser page would, and returns the status.</summary>
		static HttpStatusCode PostWithOrigin(string origin)
		{
			var request = (HttpWebRequest)WebRequest.Create("http://localhost:" + Port + "/mcp/");
			request.Method = "POST";
			request.Proxy = null;
			request.ContentType = "application/json";
			request.Headers["Authorization"] = "Bearer secret";
			request.Headers["Origin"] = origin;
			var body = Encoding.UTF8.GetBytes(List);
			using (var stream = request.GetRequestStream())
				stream.Write(body, 0, body.Length);
			try
			{
				using (var response = (HttpWebResponse)request.GetResponse())
					return response.StatusCode;
			}
			catch (WebException ex)
			{
				return ((HttpWebResponse)ex.Response).StatusCode;
			}
		}
	}
}
