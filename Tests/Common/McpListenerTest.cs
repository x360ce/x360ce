// @under-test: Engine/JocysCom/Mcp/McpServer.cs, Engine/JocysCom/Mcp/McpClient.cs, Engine/JocysCom/Mcp/McpListener.cs
// @area: mcp   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using x360ce.App;
using x360ce.App.Mcp;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.Tests
{
	/// <summary>The door: loopback only, token required unless local connections are trusted, the level read from the program each time, and a bad request answered rather than fatal.</summary>
	[TestClass]
	public class McpListenerTest
	{
		internal const int Port = 37361;
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
				StringAssert.Contains(McpListener.LastError, "Press Fix on the Issues tab");
				StringAssert.Contains(McpListener.LastError, "netsh http add urlacl");
			}
			// The command the sentence gives is the one the Fix runs elevated: for every user, not only this one.
			var remedy = McpListener.UrlReservationRemedy(McpListener.Prefix(Options.AnyAddress, Port));
			StringAssert.Contains(remedy, "Press Fix on the Issues tab");
			StringAssert.Contains(remedy, "netsh http add urlacl url=http://+:" + Port + "/mcp/ sddl=D:(A;;GX;;;WD)");
			Assert.IsTrue(McpListener.Start(Options.LoopbackAddress, Port, "secret"), McpListener.LastError);
			Assert.IsFalse(McpListener.NeedsUrlReservation);
			try
			{
				var refused = Assert.ThrowsExactly<McpHttpException>(() => McpClient.Post(Port, "wrong", List));
				Assert.AreEqual((int)HttpStatusCode.Unauthorized, refused.Status);
				// Told nothing, a caller that found the door went looking for the token in the settings files.
				StringAssert.Contains(refused.Body, "x360ce.exe -Ai", "The refusal does not point at the way in that needs no token.");
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

		[TestMethod, TestCategory("mcp"), TestCategory("critical"), Timeout(30000)]
		[Description("Trusting local connections lets a call without a token in on the loopback door, and only then; a wrong token and a web page are refused either way")]
		public void Trusted_local_calls_need_no_token()
		{
			McpServerTest.UseSample(AiAccess.Read);
			try
			{
				// Every network needs the token whatever the setting says. Opening it needs a reservation Windows only
				// grants to an Administrator; where it opens, a call without the token is refused.
				if (McpListener.Start(Options.AnyAddress, Port, "secret", true))
					Assert.AreEqual(HttpStatusCode.Unauthorized, Post(null, null), "The door on every network let a call in without the token.");
				Assert.IsTrue(McpListener.Start(Options.LoopbackAddress, Port, "secret", true), McpListener.LastError);
				Assert.AreEqual(HttpStatusCode.OK, Post(null, null), "A call from this computer without a token was refused while local connections are trusted.");
				Assert.AreEqual(HttpStatusCode.OK, Post("secret", null), "The token stopped working.");
				Assert.AreEqual(HttpStatusCode.Unauthorized, Post("wrong", null), "A wrong token was let in because local connections are trusted.");
				Assert.AreEqual(HttpStatusCode.Forbidden, Post(null, "http://example.com"), "A page from another site reached the door without a token.");
				Assert.AreEqual(HttpStatusCode.Unauthorized, Post(null, "http://localhost:3000"), "A page served from this computer ran a tool without the token.");
				Assert.AreEqual(HttpStatusCode.OK, Post("secret", "http://localhost:3000"), "A page on this computer with the token was refused.");
				Assert.IsTrue(McpListener.Start(Options.LoopbackAddress, Port, "secret"), McpListener.LastError);
				Assert.AreEqual(HttpStatusCode.Unauthorized, Post(null, null), "A call without a token was let in with local connections not trusted.");
				Assert.AreEqual(HttpStatusCode.Unauthorized, Post("wrong", null));
			}
			finally
			{
				McpListener.Stop();
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The door's rule: no token only from a program on this computer, on the loopback door, with trust on; a web page, and every network, always need the token; a wrong or empty token never passes")]
		public void Every_network_always_needs_the_token()
		{
			var here = IPAddress.Loopback;
			var other = IPAddress.Parse("192.168.1.20");
			Assert.IsTrue(McpListener.IsAuthorised(null, here, Options.LoopbackAddress, "secret", true));
			Assert.IsFalse(McpListener.IsAuthorised(null, here, Options.LoopbackAddress, "secret", false), "No token needs trust.");
			Assert.IsFalse(McpListener.IsAuthorised(null, here, Options.AnyAddress, "secret", true), "On every network the token is always needed.");
			Assert.IsFalse(McpListener.IsAuthorised(null, other, Options.LoopbackAddress, "secret", true), "A call from another computer is not local.");
			Assert.IsFalse(McpListener.IsAuthorised("Bearer wrong", here, Options.LoopbackAddress, "secret", true), "A wrong token is refused even when trusted.");
			Assert.IsFalse(McpListener.IsAuthorised("Bearer ", here, Options.LoopbackAddress, null, false), "A door without a token lets nobody in by token.");
			Assert.IsTrue(McpListener.IsAuthorised("Bearer secret", other, Options.AnyAddress, "secret", false));
			Assert.IsFalse(McpListener.IsAuthorised(null, here, Options.LoopbackAddress, "secret", true, "http://localhost:3000"), "A web page needs the token whatever the person trusts.");
			Assert.IsTrue(McpListener.IsAuthorised("Bearer secret", here, Options.LoopbackAddress, "secret", true, "http://localhost:3000"));
			Assert.IsFalse(McpListener.TrustsLocal(Options.AnyAddress, true));
		}

		/// <summary>Posts the tools list with the token and an Origin header, as a browser page would, and returns the status.</summary>
		static HttpStatusCode PostWithOrigin(string origin)
		{
			return Post("secret", origin);
		}

		/// <summary>Posts the tools list to the door on <see cref="Port"/>, with the token and the Origin header when given, and returns the status.</summary>
		internal static HttpStatusCode Post(string token, string origin)
		{
			var request = (HttpWebRequest)WebRequest.Create("http://localhost:" + Port + "/mcp/");
			request.Method = "POST";
			request.Proxy = null;
			request.ContentType = "application/json";
			if (token != null)
				request.Headers["Authorization"] = "Bearer " + token;
			if (origin != null)
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
