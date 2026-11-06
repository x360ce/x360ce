using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace x360ce.App
{
	/// <summary>
	/// Installs or updates HID Hide from the latest release its authors publish on GitHub, only when the person asks: one
	/// request for the latest release, one for its setup. The setup runs only when it is the file the release describes
	/// and Windows finds it signed by <see cref="Publisher"/>.
	/// </summary>
	/// <remarks>
	/// The setup is run shown, as the virtual bus driver's is (<c>VirtualDriverInstaller.RunModernSetup</c>): its
	/// authors document no silent switches, and Windows asks for administrator rights for it. Everything here waits for
	/// the network or for the setup, so it runs on a worker thread, never on the interface's and never on the engine's.
	/// The requests go through <see cref="UpdateClient.Http"/>, which says only the program's name and version; a test
	/// hands in the answers.
	/// </remarks>
	public class HidHideSetupClient
	{
		/// <summary>The latest full release; GitHub leaves out drafts and pre-releases.</summary>
		public const string LatestUrl = "https://api.github.com/repos/nefarius/HidHide/releases/latest";

		/// <summary>The organisation HID Hide's setup must be signed by.</summary>
		public const string Publisher = "Nefarius Software Solutions e.U.";

		public HidHideSetupClient(Func<UpdateRequest, UpdateResponse> transport = null)
		{
			Transport = transport ?? new UpdateClient(typeof(HidHideSetupClient).Assembly.GetName().Version).Http;
		}

		public readonly Func<UpdateRequest, UpdateResponse> Transport;

		/// <summary>The folder a download is put in, in a folder of its own that is deleted afterwards.</summary>
		public string TempFolder = Path.GetTempPath();

		/// <summary>1 while a setup is downloaded or runs, so the Options page and the Issues tab never start two.</summary>
		static int _Busy;

		/// <summary>The latest release, with the setup for a processor.</summary>
		/// <param name="architecture">x64, arm64 or x86, as <see cref="MachineArchitecture"/> names it.</param>
		/// <param name="error">Why there is no answer, when null is returned.</param>
		/// <returns>
		/// The release, its <see cref="UpdateRelease.Url"/> null when it has no setup for the processor; null when GitHub did
		/// not say. Never throws.
		/// </returns>
		public UpdateRelease Latest(string architecture, out string error)
		{
			error = null;
			try
			{
				var response = Transport(new UpdateRequest { Url = LatestUrl });
				if (response.StatusCode != 200)
				{
					error = "GitHub answered " + response.StatusCode + " when asked for the latest HID Hide.";
					return null;
				}
				UpdateClient.Release release;
				var serializer = new DataContractJsonSerializer(typeof(UpdateClient.Release));
				using (var stream = new MemoryStream(response.Body ?? new byte[0]))
					release = (UpdateClient.Release)serializer.ReadObject(stream);
				Version version;
				// A tag such as "v1.5.230.0".
				if (release == null || !JocysCom.ClassLibrary.Helper.TryParseVersion(release.Tag, out version))
				{
					error = "GitHub's answer names no HID Hide version.";
					return null;
				}
				var assets = release.Assets ?? new UpdateClient.Asset[0];
				var name = SetupFor(assets.Select(x => x.Name), architecture);
				var setup = assets.FirstOrDefault(x => x.Name == name);
				return new UpdateRelease
				{
					Version = version,
					Page = release.Page,
					Url = setup == null ? null : setup.Url,
					Size = setup == null ? 0 : setup.Size,
					Sha256 = setup == null ? null : UpdateRelease.Sha256OfDigest(setup.Digest),
				};
			}
			catch (Exception ex)
			{
				error = "The latest HID Hide could not be asked for: " + ex.Message;
				return null;
			}
		}

		/// <summary>
		/// The setup for a processor among a release's files, named as HID Hide names them, such as
		/// HidHide_1.5.230_x64.exe; null when there is none.
		/// </summary>
		/// <param name="architecture">x64, arm64 or x86.</param>
		public static string SetupFor(IEnumerable<string> names, string architecture)
		{
			if (names == null || string.IsNullOrEmpty(architecture))
				return null;
			var ending = "_" + architecture + ".exe";
			return names.FirstOrDefault(x => x != null
				&& x.StartsWith("HidHide", StringComparison.OrdinalIgnoreCase)
				&& x.EndsWith(ending, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>The processor Windows runs on, as HID Hide names its setups: x64, arm64 or x86.</summary>
		/// <remarks>Windows' own answer, not this process's: a 32-bit process on 64-bit Windows is told both.</remarks>
		public static string MachineArchitecture()
		{
			var processor = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ?? Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "";
			switch (processor.ToUpperInvariant())
			{
				case "AMD64":
					return "x64";
				case "ARM64":
					return "arm64";
				case "X86":
					return "x86";
				default:
					return processor.ToLowerInvariant();
			}
		}

		/// <summary>Whether the version HID Hide's setup recorded is older than a release's; true when it is not known.</summary>
		public static bool Outdated(string installed, Version latest)
		{
			if (latest == null)
				return false;
			Version version;
			if (!JocysCom.ClassLibrary.Helper.TryParseVersion(installed, out version))
				return true;
			return Full(version) < Full(latest);
		}

		/// <summary>A version with all four parts, so that 1.5.230 and 1.5.230.0 are equal.</summary>
		static Version Full(Version v)
		{
			return new Version(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));
		}

		/// <summary>Installs the latest HID Hide for this computer's processor. Never throws: what went wrong is said.</summary>
		/// <param name="progress">Told each step as it starts; may be null. Called on this thread.</param>
		/// <returns>Null when the setup ran to its end, so the caller says what it finds installed; otherwise why not.</returns>
		public string Install(Action<string> progress)
		{
			Say(progress, "Asking GitHub for the latest HID Hide...");
			string error;
			var release = Latest(MachineArchitecture(), out error);
			return release == null ? error : Install(release, progress);
		}

		/// <summary>
		/// Downloads a release's setup into a folder of its own, checks it, runs it shown, waits for it, and deletes it.
		/// Never throws: what went wrong is said.
		/// </summary>
		/// <param name="release">The release <see cref="Latest"/> found.</param>
		/// <param name="progress">Told each step as it starts; may be null. Called on this thread.</param>
		/// <returns>Null when the setup ran to its end, so the caller says what it finds installed; otherwise why not.</returns>
		public string Install(UpdateRelease release, Action<string> progress)
		{
			if (string.IsNullOrEmpty(release.Url))
				return string.Format("HID Hide {0} has no setup for this computer's processor ({1}). Its release page lists what it has.",
					release.Version, MachineArchitecture().ToUpperInvariant());
			if (Interlocked.CompareExchange(ref _Busy, 1, 0) != 0)
				return "HID Hide's setup is already running.";
			var folder = Path.Combine(TempFolder, "x360ce-HidHide-" + Guid.NewGuid().ToString("N"));
			try
			{
				Say(progress, string.Format("Downloading HID Hide {0} ({1:N1} MB)...", release.Version, release.Size / 1024m / 1024m));
				string path;
				try
				{
					path = Download(release, folder, progress);
				}
				catch (InvalidDataException ex)
				{
					return "HID Hide's setup was refused: " + ex.Message;
				}
				catch (Exception ex)
				{
					return "HID Hide's setup could not be downloaded: " + ex.Message;
				}
				Say(progress, "Windows asks for administrator rights, then HID Hide's setup opens. Follow it through to the end.");
				var exitCode = JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(path, null, true, true, ProcessWindowStyle.Normal);
				return ExplainExit(exitCode);
			}
			finally
			{
				Delete(folder);
				Interlocked.Exchange(ref _Busy, 0);
			}
		}

		/// <summary>
		/// Downloads a release's setup into a folder and returns its path, once it is the file the release describes and
		/// signed by <see cref="Publisher"/>.
		/// </summary>
		/// <exception cref="InvalidDataException">It is not; the message says how. Nothing is left on disk.</exception>
		/// <exception cref="WebException">GitHub did not send it.</exception>
		public string Download(UpdateRelease release, string folder, Action<string> progress = null)
		{
			Uri address;
			if (!Uri.TryCreate(release.Url, UriKind.Absolute, out address) || address.Scheme != Uri.UriSchemeHttps)
				throw new InvalidDataException("its address is not an HTTPS one: " + release.Url);
			var response = Transport(new UpdateRequest { Url = release.Url, TimeoutMs = UpdateClient.DownloadTimeoutMs });
			if (response.StatusCode != 200 || response.Body == null)
				throw new WebException("GitHub answered " + response.StatusCode + ".");
			Say(progress, "Checking the setup's size, SHA-256 and signature...");
			var mismatch = release.Mismatch(response.Body);
			if (mismatch != null)
				throw new InvalidDataException(mismatch);
			Directory.CreateDirectory(folder);
			var path = Path.Combine(folder, FileName(address));
			File.WriteAllBytes(path, response.Body);
			var refusal = SignatureRefusal(path);
			if (refusal != null)
			{
				File.Delete(path);
				throw new InvalidDataException(refusal);
			}
			return path;
		}

		/// <summary>The setup's own file name, as its address ends; a plain one when that is not a file name.</summary>
		static string FileName(Uri address)
		{
			var name = Uri.UnescapeDataString(address.Segments.Last());
			return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
				? name
				: "HidHideSetup.exe";
		}

		/// <summary>
		/// Why a file may not run as HID Hide's setup, or null when it may: Windows must find its Authenticode signature valid
		/// and its chain trusted, and the certificate must name <see cref="Publisher"/> as its organisation.
		/// </summary>
		public static string SignatureRefusal(string path)
		{
			var signer = CertificateHelper.GetSigner(path);
			if (signer == null)
				return "it carries no signature Windows trusts.";
			var organisation = CertificateHelper.OrganisationOf(signer);
			if (!string.Equals(organisation, Publisher, StringComparison.Ordinal))
				return string.Format("it is signed by {0}, not {1}.", organisation ?? signer, Publisher);
			return null;
		}

		/// <summary>What the setup's exit code means; null when it finished.</summary>
		/// <remarks>The codes are Windows Installer's, which the setup passes on.</remarks>
		public static string ExplainExit(int exitCode)
		{
			switch (exitCode)
			{
				case 0:
					return null;
				case -1:
					return "Windows was not allowed to run HID Hide's setup as administrator, so nothing changed.";
				case 1223:
				case 1602:
					return "HID Hide's setup was cancelled.";
				case 1641:
				case 3010:
					return "Restart Windows to finish installing HID Hide.";
				default:
					return string.Format("HID Hide's setup ended with code {0}.", exitCode);
			}
		}

		static void Say(Action<string> progress, string text)
		{
			if (progress != null)
				progress(text);
		}

		/// <summary>Deletes the download's folder; one still held by a setup that left a program running stays for Windows to clear.</summary>
		static void Delete(string folder)
		{
			try
			{
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
