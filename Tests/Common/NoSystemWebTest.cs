// @under-test: Engine/JocysCom/Web/Services/SoapHttpClientBase.cs, Engine/Common/WebServiceClient.cs, Engine/JocysCom/Mcp/McpServer.cs
// @area: cloud   @layer: unit
using JocysCom.ClassLibrary.Mcp;
using JocysCom.ClassLibrary.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The programs run without ASP.NET: Cloud goes through System.Net, and JSON through what .NET Framework carries outside System.Web.</summary>
	[TestClass]
	public class NoSystemWebTest
	{
		const string Envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body>";
		const string EnvelopeEnd = "</soap:Body></soap:Envelope>";

		/// <summary>A server for one request on this computer, which answers with the text given and keeps what it was sent.</summary>
		sealed class FakeService : IDisposable
		{
			readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
			readonly Thread _thread;
			public string Head;
			public string Body;

			public FakeService(string answer)
			{
				_listener.Start();
				_thread = new Thread(() => Serve(answer)) { IsBackground = true };
				_thread.Start();
			}

			public string Url => "http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port + "/x360ce.asmx";

			void Serve(string answer)
			{
				using (var client = _listener.AcceptTcpClient())
				{
					var stream = client.GetStream();
					var all = new MemoryStream();
					var buffer = new byte[65536];
					var continued = false;
					while (true)
					{
						var count = stream.Read(buffer, 0, buffer.Length);
						if (count == 0)
							break;
						all.Write(buffer, 0, count);
						var text = Encoding.UTF8.GetString(all.ToArray());
						var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
						if (end < 0)
							continue;
						Head = text.Substring(0, end);
						if (!continued && Head.IndexOf("100-continue", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							continued = true;
							var more = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
							stream.Write(more, 0, more.Length);
						}
						var length = int.Parse(Head.Split('\n').First(x => x.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase)).Split(':')[1].Trim());
						var at = Encoding.UTF8.GetByteCount(text.Substring(0, end + 4));
						if (all.Length - at < length)
							continue;
						Body = text.Substring(end + 4);
						break;
					}
					var reply = Encoding.UTF8.GetBytes(answer);
					stream.Write(reply, 0, reply.Length);
				}
			}

			public void Dispose()
			{
				_thread.Join(2000);
				_listener.Stop();
			}
		}

		static string Http(string status, string type, string body)
		{
			return "HTTP/1.1 " + status + "\r\nContent-Type: " + type + "\r\nConnection: close\r\nContent-Length: " + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n" + body;
		}

		static string Fault(string code, string text)
		{
			return Envelope + "<soap:Fault><faultcode>soap:" + code + "</faultcode><faultstring>" + text + "</faultstring><detail /></soap:Fault>" + EnvelopeEnd;
		}

		[TestMethod, TestCategory("cloud")]
		[Description("Neither the program nor the library refers to System.Web, so ASP.NET cannot load")]
		public void Nothing_refers_to_System_Web()
		{
			foreach (var assembly in new[] { typeof(WebServiceClient).Assembly, typeof(App.MainForm).Assembly })
			{
				var web = assembly.GetReferencedAssemblies().Select(x => x.Name).Where(x => x.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase)).ToArray();
				Assert.AreEqual(0, web.Length, assembly.GetName().Name + " refers to " + string.Join(", ", web));
			}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("A call is sent as the web service has always read it")]
		public void A_call_is_the_message_the_service_reads()
		{
			var answer = Envelope + "<SignInResponse xmlns=\"http://x360ce.com/\"><SignInResult><ErrorMessage>welcome</ErrorMessage></SignInResult></SignInResponse>" + EnvelopeEnd;
			using (var service = new FakeService(Http("200 OK", "text/xml; charset=utf-8", answer)))
			using (var ws = new WebServiceClient { Url = service.Url })
			{
				var result = ws.SignIn("user <&>", "pass");
				Assert.AreEqual("welcome", result.ErrorMessage);
				Assert.AreEqual(Envelope + "<SignIn xmlns=\"http://x360ce.com/\"><username>user &lt;&amp;&gt;</username><password>pass</password></SignIn>" + EnvelopeEnd, service.Body);
				StringAssert.Contains(service.Head, "SOAPAction: \"http://x360ce.com/SignIn\"");
				StringAssert.Contains(service.Head, "Content-Type: text/xml; charset=utf-8");
			}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("A type a method both takes and returns is read and written")]
		public void A_type_taken_and_returned_works()
		{
			var answer = Envelope + "<ExecuteResponse xmlns=\"http://x360ce.com/\"><ExecuteResult><ErrorMessage>done</ErrorMessage></ExecuteResult></ExecuteResponse>" + EnvelopeEnd;
			using (var service = new FakeService(Http("200 OK", "text/xml", answer)))
			using (var ws = new WebServiceClient { Url = service.Url })
				Assert.AreEqual("done", ws.Execute(new CloudMessage()).ErrorMessage);
		}

		[TestMethod, TestCategory("cloud")]
		[Description("A fault says which side it blames, which decides whether the queue tries again")]
		public void A_fault_says_which_side_it_blames()
		{
			foreach (var pair in new[] { new { Code = "Server", Expected = SoapException.ServerFaultCode }, new { Code = "Client", Expected = SoapException.ClientFaultCode } })
				using (var service = new FakeService(Http("500 Internal Server Error", "text/xml; charset=utf-8", Fault(pair.Code, "said so"))))
				using (var ws = new WebServiceClient { Url = service.Url })
				{
					var fault = Assert.ThrowsExactly<SoapException>(() => ws.SignOut());
					Assert.AreEqual(pair.Expected, fault.Code);
					Assert.AreEqual("said so", fault.Message);
				}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("An answer that is no SOAP says what came back, and an HTTP error keeps its status")]
		public void An_answer_that_is_no_soap_is_named()
		{
			using (var service = new FakeService(Http("200 OK", "text/html", "<html>hello</html>")))
			using (var ws = new WebServiceClient { Url = service.Url })
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => ws.SignOut()).Message, "<html>hello</html>");
			foreach (var status in new[] { "400 Bad Request", "404 Not Found" })
				using (var service = new FakeService(Http(status, "text/html", "<html>no</html>")))
				using (var ws = new WebServiceClient { Url = service.Url })
				{
					var error = Assert.ThrowsExactly<WebException>(() => ws.SignOut());
					Assert.AreEqual(WebExceptionStatus.ProtocolError, error.Status);
					Assert.AreEqual(int.Parse(status.Split(' ')[0]), (int)((HttpWebResponse)error.Response).StatusCode, "The queue reads the status to decide whether to try again.");
				}
		}

		[TestMethod, TestCategory("cloud")]
		[Description("JSON reads into dictionaries, arrays, text, longs, doubles, true or false and null")]
		public void Json_reads_into_plain_shapes()
		{
			var map = (Dictionary<string, object>)McpServer.FromJson("{\"a b\":1,\"\":2.5,\"type\":\"t\\u00fc\",\"item\":[true,null,{}],\"id\":9223372036854775807}");
			Assert.AreEqual(1L, map["a b"]);
			Assert.AreEqual(2.5, map[""]);
			Assert.AreEqual("t\u00fc", map["type"]);
			var items = (object[])map["item"];
			Assert.AreEqual(true, items[0]);
			Assert.IsNull(items[1]);
			Assert.IsInstanceOfType(items[2], typeof(Dictionary<string, object>));
			Assert.AreEqual(long.MaxValue, map["id"]);
			Assert.IsNull(McpServer.FromJson(" "), "A blank message reads as nothing.");
		}

		[TestMethod, TestCategory("cloud")]
		[Description("What is not JSON, or nests without end, is refused and not read")]
		public void Json_refuses_what_is_not_json()
		{
			foreach (var text in new[] { "{", "{\"a\":}", "abc", "{} x", "\"\\u12\"", new string('[', 150) + new string(']', 150) })
				Assert.ThrowsExactly<ArgumentException>(() => McpServer.FromJson(text), text);
		}

		[TestMethod, TestCategory("cloud")]
		[Description("JSON is written so a path, an address and any text survive, and a value that refers to itself is refused")]
		public void Json_writes_text_that_survives()
		{
			var text = "C:\\Games\\O'Brien's \"Game\"\r\n\t\u0001 http://a/b <&> \u00fc";
			Assert.AreEqual(text, McpServer.FromJson(McpServer.ToJson(text)));
			StringAssert.Contains(McpServer.ToJson("http://a/b"), "http://a/b", "An address must stay readable where a person pastes it.");
			Assert.AreEqual("{\"n\":5,\"d\":1.5,\"b\":true,\"z\":null,\"l\":[1,2]}", McpServer.ToJson(new Dictionary<string, object> { { "n", 5 }, { "d", 1.5 }, { "b", true }, { "z", null }, { "l", new List<int> { 1, 2 } } }));
			var loop = new Dictionary<string, object>();
			loop["self"] = loop;
			Assert.ThrowsExactly<InvalidOperationException>(() => McpServer.ToJson(loop));
		}
	}
}
