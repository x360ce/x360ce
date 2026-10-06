using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace x360ce.App
{
	/// <summary>
	/// The release an update check chose, as GitHub publishes it: the version its title carries, its page, and
	/// the address, size and SHA-256 of its x360ce.zip. The zip downloaded is checked against these before
	/// anything is installed.
	/// </summary>
	public class UpdateRelease
	{
		public Version Version { get; set; }

		/// <summary>Address of the release page, where its notes are.</summary>
		public string Page { get; set; }

		/// <summary>Address the zip is downloaded from: this release's own, never "latest". Null when the release has no zip.</summary>
		public string Url { get; set; }

		public long Size { get; set; }

		/// <summary>SHA-256 of the zip, lower-case hex, as GitHub computed it when the zip was uploaded. Null when GitHub shows none.</summary>
		public string Sha256 { get; set; }

		/// <summary>Whether the bytes are the file the release describes.</summary>
		public bool Describes(byte[] data)
		{
			return data != null && data.LongLength == Size && Sha256 != null && Sha256Of(data) == Sha256;
		}

		public static string Sha256Of(byte[] data)
		{
			using (var sha = SHA256.Create())
				return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
		}

		/// <summary>The fixed form every release title takes, such as "X360CE 4.21.30.0".</summary>
		public const string TitlePrefix = "X360CE ";
		static readonly Regex TitleForm = new Regex("^" + Regex.Escape(TitlePrefix) + @"(\d+\.\d+\.\d+\.\d+)$");

		/// <summary>The version a release title carries, when the title has the fixed form.</summary>
		public static bool TryParseTitle(string title, out Version version)
		{
			version = null;
			if (title == null)
				return false;
			var m = TitleForm.Match(title.Trim());
			return m.Success && System.Version.TryParse(m.Groups[1].Value, out version);
		}

		/// <summary>The lower-case hex of a GitHub asset digest such as "sha256:3f26...", or null for anything else.</summary>
		public static string Sha256OfDigest(string digest)
		{
			const string prefix = "sha256:";
			if (digest == null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				return null;
			var hex = digest.Substring(prefix.Length);
			return Regex.IsMatch(hex, "^[0-9a-fA-F]{64}$") ? hex.ToLowerInvariant() : null;
		}
	}
}
