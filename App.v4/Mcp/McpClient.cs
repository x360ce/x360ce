using System.IO;
using System.Net;
using System.Text;

namespace x360ce.App.Mcp
{
	/// <summary>The client side of the program's own door. Port and token come from the program's options.</summary>
	public static class McpClient
	{
		public static string Post(int port, string token, string body)
		{
			// The same host the listener binds: http.sys routes by the Host header.
			var request = (HttpWebRequest)WebRequest.Create("http://localhost:" + port + "/mcp/");
			// Straight to this machine. A system proxy that does not bypass local addresses would
			// otherwise carry a loopback call out and back, or nowhere.
			request.Proxy = null;
			// A call lasts as long as the action: a button that opens a window answers when it closes.
			request.Timeout = System.Threading.Timeout.Infinite;
			request.ReadWriteTimeout = System.Threading.Timeout.Infinite;
			request.Method = "POST";
			request.ContentType = "application/json; charset=utf-8";
			request.Headers["Authorization"] = "Bearer " + token;
			var bytes = Encoding.UTF8.GetBytes(body);
			request.ContentLength = bytes.Length;
			using (var stream = request.GetRequestStream())
				stream.Write(bytes, 0, bytes.Length);
			using (var response = (HttpWebResponse)request.GetResponse())
			using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
				return reader.ReadToEnd();
		}
	}
}
