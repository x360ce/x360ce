#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JocysCom.ClassLibrary.Files;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// OpenAI's Secure MCP Tunnel, which lets ChatGPT on the web reach the door. ChatGPT runs on OpenAI's servers and
	/// cannot call this computer. OpenAI's tunnel-client, run here, keeps an outbound HTTPS connection to OpenAI, takes
	/// ChatGPT's calls from it and passes them to the door with the door's token.
	/// </summary>
	/// <remarks>
	/// tunnel-client comes from github.com/openai/tunnel-client. It is downloaded once into <see cref="ClientFolder"/>,
	/// one folder per release, and checked against the release's SHA-256 sums. Making or deleting a tunnel needs an
	/// OpenAI admin key, used for that one call and never kept. Running a tunnel needs an API key, which the program
	/// keeps encrypted by <see cref="Protect"/>. Keys reach tunnel-client through its own environment only, never
	/// through its command line, which other programs can read.
	/// </remarks>
	public static class McpTunnel
	{
		#region What each program sets

		/// <summary>The name a new tunnel gets at OpenAI, and the start of its description. Set by the program.</summary>
		public static string Name = "mcp";

		/// <summary>Where tunnel-client is kept, one folder per release. Set by the program; a folder in the local application data folder when not set.</summary>
		public static string Folder;

		#endregion

		/// <summary>The OpenAI Platform page that shows the organization ID.</summary>
		public const string OrganizationPage = "https://platform.openai.com/settings/organization/general";

		/// <summary>The OpenAI Platform page that makes admin keys.</summary>
		public const string AdminKeysPage = "https://platform.openai.com/settings/organization/admin-keys";

		/// <summary>The OpenAI Platform page that makes API keys, such as the restricted one a tunnel runs with.</summary>
		public const string ApiKeysPage = "https://platform.openai.com/settings/organization/api-keys";

		/// <summary>Where ChatGPT adds a custom MCP server.</summary>
		public const string ChatGptPage = "https://chatgpt.com/plugins";

		/// <summary>Where tunnel-client is published.</summary>
		public const string Releases = "https://github.com/openai/tunnel-client/releases";

		/// <summary>What the tunnel's state says while it is not wanted.</summary>
		public const string Off = "Off.";

		const string ClientFile = "tunnel-client.exe";
		const string HealthFile = "health-url.txt";
		const string Entropy = "JocysCom.ClassLibrary.Mcp.McpTunnel";

		/// <summary>The variable the door's Authorization header is passed in, which tunnel-client adds to every call it passes on.</summary>
		const string HeaderVariable = "MCP_SERVER_AUTHORIZATION";

		/// <summary>OpenAI makes a new tunnel ready 25 to 30 seconds after it is made.</summary>
		static readonly TimeSpan ReadyAfterCreate = TimeSpan.FromSeconds(30);

		static readonly Regex TunnelIdPattern = new Regex("^tunnel_[0-9a-f]{32}$");
		static readonly Regex OrganizationPattern = new Regex("^org-[A-Za-z0-9]+$");
		static readonly HttpClient Health = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(3) };

		static readonly object Gate = new object();
		static Process _process;
		static string _wanted;
		static int _generation;
		static DateTime _created;

		/// <summary>Whether the tunnel is open, why not, or what it waits for.</summary>
		public static string Status { get; private set; } = Off;

		/// <summary>tunnel-client's own status page while it runs, or null.</summary>
		public static string StatusPage { get; private set; }

		/// <summary>Raised on any thread after <see cref="Status"/> or <see cref="StatusPage"/> changed.</summary>
		public static event EventHandler Changed;

		/// <summary>Where tunnel-client is kept, one folder per release.</summary>
		public static string ClientFolder
		{
			get { return Folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JocysCom", "TunnelClient"); }
		}

		/// <summary>Whether text is a tunnel ID, as ChatGPT asks for it.</summary>
		public static bool IsTunnelId(string text)
		{
			return text != null && TunnelIdPattern.IsMatch(text);
		}

		/// <summary>Whether text is an organization ID.</summary>
		public static bool IsOrganization(string text)
		{
			return text != null && OrganizationPattern.IsMatch(text);
		}

		#region Keys

		/// <summary>Encrypts a key for this Windows account with Windows DPAPI: another account or another computer cannot read it.</summary>
		public static string Protect(string key)
		{
			if (string.IsNullOrEmpty(key))
				return "";
			var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(Entropy), DataProtectionScope.CurrentUser);
			return System.Convert.ToBase64String(data);
		}

		/// <summary>The key <see cref="Protect"/> encrypted; empty when there is none, or when another Windows account encrypted it.</summary>
		public static string Unprotect(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";
			try
			{
				var data = ProtectedData.Unprotect(System.Convert.FromBase64String(text), Encoding.UTF8.GetBytes(Entropy), DataProtectionScope.CurrentUser);
				return Encoding.UTF8.GetString(data);
			}
			catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
			{
				return "";
			}
		}

		#endregion

		#region Making and deleting tunnels

		/// <summary>Makes a tunnel in the organization and returns its ID. The admin key is used for this call only.</summary>
		public static string Create(string adminKey, string organization)
		{
			if (!IsOrganization(organization))
				throw new InvalidOperationException("The organization ID starts with org-. Find it on the OpenAI Platform.");
			if (string.IsNullOrEmpty(adminKey))
				throw new InvalidOperationException("Making a tunnel needs an admin key from the OpenAI Platform.");
			var json = Admin("tunnels create --name " + Quote(Name) + " --description " + Quote(Name + " on " + Environment.MachineName)
				+ " --organization-id " + organization, AdminVariables(adminKey));
			var id = Field(json, "id");
			if (!IsTunnelId(id))
				throw new InvalidOperationException("OpenAI answered without a tunnel ID.");
			_created = DateTime.UtcNow;
			return id;
		}

		/// <summary>Deletes a tunnel at OpenAI. The admin key is used for this call only.</summary>
		public static void Delete(string adminKey, string id)
		{
			if (!IsTunnelId(id))
				throw new InvalidOperationException("There is no tunnel ID to delete.");
			if (string.IsNullOrEmpty(adminKey))
				throw new InvalidOperationException("Deleting a tunnel needs an admin key from the OpenAI Platform.");
			Admin("tunnels delete " + id + " --confirm", AdminVariables(adminKey));
		}

		static Dictionary<string, string> AdminVariables(string adminKey)
		{
			return new Dictionary<string, string> { { "OPENAI_ADMIN_KEY", adminKey } };
		}

		static Dictionary<string, string> ApiKeyVariables(string apiKey)
		{
			return new Dictionary<string, string> { { "CONTROL_PLANE_API_KEY", apiKey } };
		}

		/// <summary>A tunnel-client admin command's JSON answer. Throws the message OpenAI gave when it fails.</summary>
		static string Admin(string arguments, Dictionary<string, string> variables)
		{
			using (var process = Process.Start(StartInfo(Client(), "admin " + arguments + " --json", variables)))
			{
				var error = process.StandardError.ReadToEndAsync();
				var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				if (process.ExitCode == 0)
					return output;
				var message = Field(output, "message");
				throw new InvalidOperationException(string.IsNullOrEmpty(message) ? (output + " " + error.Result).Trim() : message);
			}
		}

		/// <summary>
		/// Starts tunnel-client with keys only it sees. Keys this program's own environment holds are taken away first,
		/// so one set for another purpose cannot stand in for the one meant.
		/// </summary>
		static ProcessStartInfo StartInfo(string exe, string arguments, Dictionary<string, string> variables)
		{
			var info = new ProcessStartInfo(exe, arguments)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				WorkingDirectory = Path.GetDirectoryName(exe),
			};
			foreach (var name in new[] { "OPENAI_ADMIN_KEY", "OPENAI_API_KEY", "CONTROL_PLANE_API_KEY" })
				info.EnvironmentVariables.Remove(name);
			foreach (var pair in variables)
				info.EnvironmentVariables[pair.Key] = pair.Value;
			return info;
		}

		static string Quote(string text)
		{
			return "\"" + text.Replace("\"", "") + "\"";
		}

		/// <summary>A string field of a JSON answer, or null.</summary>
		static string Field(string json, string name)
		{
			var m = Regex.Match(json ?? "", "\"" + name + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
			return m.Success ? Regex.Unescape(m.Groups[1].Value) : null;
		}

		/// <summary>The strings of a JSON answer's array field.</summary>
		static string[] Values(string json, string name)
		{
			var m = Regex.Match(json ?? "", "\"" + name + "\"\\s*:\\s*\\[([^\\]]*)\\]");
			return m.Success
				? Regex.Matches(m.Groups[1].Value, "\"([^\"]*)\"").Cast<Match>().Select(x => x.Groups[1].Value).ToArray()
				: new string[0];
		}

		#endregion

		#region tunnel-client

		/// <summary>tunnel-client, downloaded first when no release of it is here yet.</summary>
		static string Client()
		{
			return LocalClient() ?? Download();
		}

		/// <summary>The newest release already downloaded, or null.</summary>
		static string LocalClient()
		{
			var folder = new DirectoryInfo(ClientFolder);
			if (!folder.Exists)
				return null;
			return folder.GetDirectories("v*")
				.Select(d => new { Path = Path.Combine(d.FullName, ClientFile), Version = ReleaseVersion(d.Name) })
				.Where(x => x.Version != null && File.Exists(x.Path))
				.OrderBy(x => x.Version)
				.Select(x => x.Path)
				.LastOrDefault();
		}

		static Version ReleaseVersion(string tag)
		{
			Version version;
			return Version.TryParse(tag.TrimStart('v'), out version) ? version : null;
		}

		/// <summary>Downloads the latest release for this computer's processor, and uses it only when it matches the sum the release lists.</summary>
		static string Download()
		{
#if NETFRAMEWORK
			System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
#endif
			using (var http = new HttpClient())
			{
				// GitHub answers no request without a User-Agent.
				http.DefaultRequestHeaders.UserAgent.ParseAdd(Name + "-tunnel");
				var latest = http.GetStringAsync("https://api.github.com/repos/openai/tunnel-client/releases/latest").Result;
				var tag = Field(latest, "tag_name");
				if (string.IsNullOrEmpty(tag) || ReleaseVersion(tag) == null || tag.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					throw new InvalidOperationException("GitHub did not say which tunnel-client is the latest.");
				var processor = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ?? Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
				var name = "tunnel-client-" + tag + "-windows-" + (string.Equals(processor, "ARM64", StringComparison.OrdinalIgnoreCase) ? "arm64" : "amd64") + ".zip";
				var zip = http.GetByteArrayAsync(Releases + "/download/" + tag + "/" + name).Result;
				var sums = http.GetStringAsync(Releases + "/download/" + tag + "/SHA256SUMS.txt").Result;
				var line = Regex.Match(sums, "^([0-9a-f]{64})\\s+\\*?" + Regex.Escape(name) + "\\s*$", RegexOptions.Multiline);
				if (!line.Success)
					throw new InvalidOperationException("The release's SHA256SUMS.txt does not list " + name + ".");
				using (var sha = SHA256.Create())
					if (!string.Equals(BitConverter.ToString(sha.ComputeHash(zip)).Replace("-", ""), line.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
						throw new InvalidOperationException(name + " does not match its SHA-256 sum, so it was not used.");
				var temp = Path.Combine(ClientFolder, "download-" + Guid.NewGuid().ToString("N"));
				var folder = Path.Combine(ClientFolder, tag);
				Directory.CreateDirectory(temp);
				try
				{
					var file = Path.Combine(temp, name);
					File.WriteAllBytes(file, zip);
					var files = Path.Combine(temp, "files");
					Zip.UnZipFiles(file, files);
					if (!File.Exists(Path.Combine(files, ClientFile)))
						throw new InvalidOperationException(name + " holds no " + ClientFile + ".");
					if (Directory.Exists(folder))
						Directory.Delete(folder, true);
					Directory.Move(files, folder);
				}
				finally
				{
					Directory.Delete(temp, true);
				}
				return Path.Combine(folder, ClientFile);
			}
		}

		#endregion

		#region Opening and closing

		/// <summary>
		/// Opens or closes the tunnel to match the settings: it is open while the door is, the tunnel is ticked, and a
		/// tunnel ID and an API key are kept. A tunnel already open with the same settings is left as it is.
		/// </summary>
		public static void Apply(IAiTunnelSettings settings)
		{
			var key = Unprotect(settings.TunnelApiKey);
			var why = !settings.TunnelEnabled ? Off
				: !settings.Enabled ? "Waits for AI assistant access, which is off."
				: !IsTunnelId(settings.TunnelId) ? "Waits for a tunnel ID."
				: key.Length > 0 ? null
				: string.IsNullOrEmpty(settings.TunnelApiKey) ? "Waits for an API key."
				: "The API key was encrypted for another Windows account. Paste it again.";
			if (why != null)
			{
				Stop(why);
				return;
			}
			var id = settings.TunnelId;
			var port = settings.Port;
			var token = settings.Token;
			var wanted = id + "\n" + key + "\n" + port + "\n" + token;
			int generation;
			lock (Gate)
			{
				if (wanted == _wanted)
					return;
				_wanted = wanted;
				generation = ++_generation;
				Kill();
			}
			SetStatus(generation, "Opening.", null);
			Task.Run(() => Run(generation, id, key, port, token));
		}

		/// <summary>Closes the tunnel, and says why.</summary>
		public static void Stop(string status = Off)
		{
			int generation;
			lock (Gate)
			{
				_wanted = null;
				generation = ++_generation;
				Kill();
			}
			SetStatus(generation, status, null);
		}

		static void Run(int generation, string id, string key, int port, string token)
		{
			try
			{
				if (LocalClient() == null)
					SetStatus(generation, "Downloading tunnel-client from " + Releases + ".", null);
				var exe = Client();
				var wait = _created + ReadyAfterCreate - DateTime.UtcNow;
				if (wait > TimeSpan.Zero)
				{
					SetStatus(generation, "Waiting for OpenAI to make the new tunnel ready.", null);
					Thread.Sleep(wait);
				}
				// A key from another organization cannot run the tunnel. Asking first says so plainly.
				string tunnel;
				try
				{
					tunnel = Admin("tunnels get " + id, ApiKeyVariables(key));
				}
				catch (InvalidOperationException ex)
				{
					throw new InvalidOperationException("OpenAI refused the API key for " + id + ": " + ex.Message);
				}
				var organizations = string.Join(", ", Values(tunnel, "organization_ids"));
				var health = Path.Combine(ClientFolder, HealthFile);
				File.Delete(health);
				var variables = ApiKeyVariables(key);
				variables["CONTROL_PLANE_TUNNEL_ID"] = id;
				variables["MCP_SERVER_URL"] = McpListener.Prefix(McpListener.LoopbackAddress, port);
				variables[HeaderVariable] = "Bearer " + token;
				// Every call carries the token, tunnel-client's own probes too, so the door checks it even where programs
				// on this computer need none: calls through the tunnel come from the internet.
				variables["MCP_EXTRA_HEADERS"] = "Authorization: env:" + HeaderVariable;
				variables["MCP_DISCOVERY_EXTRA_HEADERS"] = "Authorization: env:" + HeaderVariable;
				variables["HEALTH_LISTEN_ADDR"] = "127.0.0.1:0";
				variables["HEALTH_URL_FILE"] = health;
				var process = new Process { StartInfo = StartInfo(exe, "run", variables), EnableRaisingEvents = true };
				string last = null;
				DataReceivedEventHandler keep = (s, e) =>
				{
					if (!string.IsNullOrWhiteSpace(e.Data))
						last = e.Data.Trim();
				};
				process.OutputDataReceived += keep;
				process.ErrorDataReceived += keep;
				process.Exited += (s, e) => Fail(generation, "Closed: " + (last ?? "tunnel-client stopped."));
				lock (Gate)
				{
					if (generation != _generation)
						return;
					StopStrays();
					process.Start();
					_process = process;
				}
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				string page = null;
				for (var i = 0; i < 60 && generation == _generation && !process.HasExited; i++)
				{
					Thread.Sleep(1000);
					if (page == null && File.Exists(health))
						page = File.ReadAllText(health).Trim().TrimEnd('/');
					if (page != null && Ready(page))
					{
						SetStatus(generation, "Open. ChatGPT reaches " + Name + " through " + id + ", organization " + organizations + ".", page + "/ui");
						return;
					}
				}
				if (!process.HasExited)
					SetStatus(generation, "Not ready yet. " + (last ?? ""), page == null ? null : page + "/ui");
			}
			catch (Exception ex)
			{
				var inner = ex is AggregateException ? ex.InnerException : ex;
				Fail(generation, "Not open: " + inner.Message);
			}
		}

		static bool Ready(string page)
		{
			try
			{
				using (var response = Health.GetAsync(page + "/readyz").Result)
					return response.IsSuccessStatusCode;
			}
			catch (AggregateException)
			{
				return false;
			}
		}

		/// <summary>Says why the tunnel is not open, and lets the next <see cref="Apply"/> try again.</summary>
		static void Fail(int generation, string status)
		{
			lock (Gate)
			{
				if (generation == _generation)
					_wanted = null;
			}
			SetStatus(generation, status, null);
		}

		static void SetStatus(int generation, string status, string page)
		{
			lock (Gate)
			{
				if (generation != _generation)
					return;
				Status = status;
				StatusPage = page;
			}
			var changed = Changed;
			if (changed != null)
				changed(null, EventArgs.Empty);
		}

		static void Kill()
		{
			var process = _process;
			_process = null;
			if (process == null)
				return;
			try
			{
				if (!process.HasExited)
					process.Kill();
			}
			catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
			{
				// It ended on its own meanwhile.
			}
			process.Dispose();
		}

		/// <summary>Closes tunnel-client copies started from this folder by a run of the program that ended without closing them: two would serve one tunnel.</summary>
		static void StopStrays()
		{
			var folder = Path.GetFullPath(ClientFolder) + Path.DirectorySeparatorChar;
			foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ClientFile)))
			{
				using (process)
				{
					try
					{
						if (process.MainModule.FileName.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
							process.Kill();
					}
					catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
					{
						// Ended meanwhile, or another account's.
					}
				}
			}
		}

		#endregion
	}
}
