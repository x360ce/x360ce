#nullable disable
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// Serves McpServer over HTTP on the loopback address, to callers that present the token, and to this computer's own
	/// programs without it when the person trusts local connections; a web page always needs the token.
	/// </summary>
	public static class McpListener
	{
		/// <summary>The address that keeps the door on this computer. The default.</summary>
		public const string LoopbackAddress = "127.0.0.1";
		/// <summary>The address that opens the door to every network the computer is on.</summary>
		public const string AnyAddress = "0.0.0.0";

		static HttpListener _listener;
		static string _address;
		static string _token;
		static bool _trustLocal;

		/// <summary>What every program tells a caller without the token.</summary>
		public const string NoToken = "Send the token from the program's AI assistant access settings as Authorization: Bearer <token>. "
			+ "A program on this computer can call the program with -Ai instead, which needs no token.";

		/// <summary>
		/// What a caller without the token is told, as the body of the refusal. A program that finds the door without
		/// the token is pointed at the command line, which needs none, rather than left to look for the token on disk.
		/// </summary>
		public static string Unauthorised = NoToken;

		/// <summary>A new token: 32 random bytes as 64 lower-case hex digits, the shape the log hides.</summary>
		public static string NewToken()
		{
			var bytes = new byte[32];
			using (var random = RandomNumberGenerator.Create())
				random.GetBytes(bytes);
			return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
		}

		public static bool IsRunning { get { return _listener != null; } }

		/// <summary>Why the door is not usable, as one sentence with its own remedy. Null after a start that worked.</summary>
		public static string LastError;

		/// <summary>True when the last start failed only because Windows has no URL reservation for every network.</summary>
		public static bool NeedsUrlReservation;

		/// <summary>
		/// What the person does when every network needs a URL reservation, given the prefix to reserve: the end of the
		/// sentence in LastError. A program that makes the reservation itself, elevated, points at where it does.
		/// </summary>
		public static Func<string, string> UrlReservationRemedy = prefix =>
			"Run as Administrator: netsh http add urlacl url=" + prefix + " user=\"" + Environment.UserDomainName + "\\" + Environment.UserName + "\"";

		/// <summary>The prefix http.sys is asked for: the loopback name a standard user may bind, or every address, which needs a reservation.</summary>
		public static string Prefix(string address, int port)
		{
			return (address == AnyAddress ? "http://+:" : "http://localhost:") + port + "/mcp/";
		}

		/// <summary>
		/// Whether a program on this computer may call without the token: only when the person trusts local connections
		/// and the door listens on the loopback name. On every network the token is always needed, whatever the setting
		/// says, and a web page needs it always (<see cref="IsAuthorised"/>).
		/// </summary>
		public static bool TrustsLocal(string address, bool trustLocal)
		{
			return trustLocal && address != AnyAddress;
		}

		/// <summary>
		/// Whether a request is let in. One that carries an Authorization header is let in by the token alone, so a
		/// wrong token is refused either way, and a door without a token lets nobody in by one. One without the header
		/// is let in when the door trusts local connections, the request comes from this computer, and it carries no
		/// Origin header. http.sys hands the loopback name a request from any address that names it, so the caller's
		/// own address is what says local. A browser names the page behind every request it sends to another origin,
		/// a page served from this computer too, while a program or an assistant's command sends no Origin; so a web
		/// page needs the token whatever the person trusts.
		/// </summary>
		/// <param name="authorization">The request's Authorization header, or null when it has none.</param>
		/// <param name="caller">The address the request came from, or null when it is not known.</param>
		/// <param name="address">The address the door listens on.</param>
		/// <param name="token">The door's token.</param>
		/// <param name="trustLocal">Whether the person trusts local connections.</param>
		/// <param name="origin">The request's Origin header, or null when it has none.</param>
		public static bool IsAuthorised(string authorization, IPAddress caller, string address, string token, bool trustLocal, string origin = null)
		{
			if (authorization != null)
				return !string.IsNullOrEmpty(token) && authorization == "Bearer " + token;
			return TrustsLocal(address, trustLocal) && IsOnThisComputer(caller) && string.IsNullOrEmpty(origin);
		}

		/// <summary>
		/// Whether a caller may reach the door at all. On the loopback name only this computer's programs may: http.sys
		/// hands that name a request from another computer that names it, so the caller's own address decides. On every
		/// network anyone may, and the token decides.
		/// </summary>
		/// <param name="caller">The address the request came from, or null when it is not known.</param>
		/// <param name="address">The address the door listens on.</param>
		public static bool IsReachable(IPAddress caller, string address)
		{
			return address == AnyAddress || IsOnThisComputer(caller);
		}

		/// <summary>True for a loopback address, also when it arrives as an IPv4 address mapped into IPv6.</summary>
		static bool IsOnThisComputer(IPAddress caller)
		{
			if (caller == null)
				return false;
			return IPAddress.IsLoopback(caller.IsIPv4MappedToIPv6 ? caller.MapToIPv4() : caller);
		}

		/// <summary>
		/// Opens the door on the address and port. False, with the reason in LastError, when it cannot. With
		/// <paramref name="trustLocal"/>, this computer's own programs need no token on the loopback address.
		/// </summary>
		public static bool Start(string address, int port, string token, bool trustLocal = false)
		{
			Stop();
			NeedsUrlReservation = false;
			if (port < 1024 || port > 49151)
			{
				LastError = "port " + port + " is outside 1024 to 49151. Choose a port in that range " + McpCatalog.SettingsPlace + ".";
				return false;
			}
			var listener = new HttpListener();
			// localhost is the host a standard user may bind without a URL reservation, where
			// 127.0.0.1 is refused on some Windows versions. http.sys still hands it a request from
			// another computer that names localhost, so Answer refuses callers that are not on this
			// computer. Every network is the + wildcard, the only form http.sys takes for that, and
			// it needs a reservation made once as Administrator.
			listener.Prefixes.Add(Prefix(address, port));
			try
			{
				listener.Start();
			}
			catch (HttpListenerException ex)
			{
				// Another program holds the port, or Windows has no reservation. The sentence says
				// which and what to do; a crash report would say neither.
				NeedsUrlReservation = address == AnyAddress && ex.ErrorCode == 5;
				LastError = NeedsUrlReservation
					? "listening on every network needs a one-time permission from Windows. " + UrlReservationRemedy(Prefix(address, port))
					: "port " + port + " could not be opened (" + ex.Message + "). Choose another port " + McpCatalog.SettingsPlace + ".";
				return false;
			}
			_address = address;
			_token = token;
			_trustLocal = trustLocal;
			_listener = listener;
			LastError = null;
			listener.BeginGetContext(OnRequest, listener);
			McpLog.Write("door opened at " + Prefix(address, port) + " with " + McpCatalog.Level() + " access"
				+ (TrustsLocal(address, trustLocal) ? ", local connections need no token" : ""));
			return true;
		}

		public static void Stop()
		{
			var listener = _listener;
			_listener = null;
			if (listener != null)
			{
				listener.Close();
				McpLog.Write("door closed");
			}
		}

		/// <summary>
		/// Runs on a pool thread, which no crash reporter watches. Everything is caught and answered,
		/// so a broken client, a fault in a tool, or a stop that lands mid-request ends in a response
		/// or in silence rather than in the program.
		/// </summary>
		static void OnRequest(IAsyncResult ar)
		{
			var listener = (HttpListener)ar.AsyncState;
			HttpListenerContext context;
			try
			{
				context = listener.EndGetContext(ar);
				listener.BeginGetContext(OnRequest, listener);
			}
			catch (ObjectDisposedException) { return; }
			catch (HttpListenerException) { return; }
			try
			{
				Answer(context);
			}
			catch (Exception ex)
			{
				McpLog.Write("request failed: " + McpLog.Clip(ex.Message, 300));
				try { Write(context.Response, 500, McpServer.Error(null, -32603, "Internal error.")); }
				catch (Exception) { }
			}
		}

		/// <summary>
		/// Whether a request's Origin is a page on this computer. A browser names the page that sends a request, so a
		/// web site the person has open cannot reach the door through a name that points here.
		/// </summary>
		public static bool IsLocalOrigin(string origin)
		{
			Uri uri;
			return Uri.TryCreate(origin, UriKind.Absolute, out uri) && uri.IsLoopback;
		}

		static void Answer(HttpListenerContext context)
		{
			var origin = context.Request.Headers["Origin"];
			if (!string.IsNullOrEmpty(origin) && !IsLocalOrigin(origin))
			{
				McpLog.Write("refused: origin " + McpLog.Clip(origin, 100) + ", from " + context.Request.RemoteEndPoint);
				Write(context.Response, 403, "");
				return;
			}
			var caller = context.Request.RemoteEndPoint;
			if (!IsReachable(caller == null ? null : caller.Address, _address))
			{
				McpLog.Write("refused: not on this computer, from " + caller);
				Write(context.Response, 403, "");
				return;
			}
			var authorised = IsAuthorised(context.Request.Headers["Authorization"], caller == null ? null : caller.Address, _address, _token, _trustLocal, origin);
			if (!authorised || context.Request.HttpMethod != "POST")
			{
				if (!authorised)
					McpLog.Write("refused: no valid token, from " + context.Request.RemoteEndPoint);
				Write(context.Response, authorised ? 405 : 401, authorised ? "" : McpServer.ToJson(new Dictionary<string, object> { { "error", Unauthorised } }));
				return;
			}
			string body;
			// JSON is UTF-8 whatever the request says; a missing charset would otherwise mean the system code page.
			using (var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8))
				body = reader.ReadToEnd();
			var answer = McpServer.Handle(body);
			Write(context.Response, answer.Length == 0 ? 202 : 200, answer);
		}

		static void Write(HttpListenerResponse response, int status, string body)
		{
			var bytes = Encoding.UTF8.GetBytes(body);
			response.StatusCode = status;
			response.ContentType = "application/json; charset=utf-8";
			response.ContentLength64 = bytes.Length;
			response.OutputStream.Write(bytes, 0, bytes.Length);
			response.Close();
		}
	}
}
