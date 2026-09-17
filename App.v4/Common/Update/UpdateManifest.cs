using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.App
{
	/// <summary>
	/// What a release says about itself: the file `latest.json` the release script attaches
	/// beside `x360ce.zip`. GitHub serves it from a fixed address that always points at the
	/// newest full release, so this one small file is the whole answer to "is there a newer
	/// version", and the zip it names is checked against it before anything is installed.
	/// </summary>
	[DataContract]
	public class UpdateManifest
	{
		[DataMember(Name = "version", Order = 0)]
		public string Version { get; set; }

		/// <summary>Name of the release asset holding the program.</summary>
		[DataMember(Name = "file", Order = 1)]
		public string File { get; set; }

		[DataMember(Name = "size", Order = 2)]
		public long Size { get; set; }

		/// <summary>SHA-256 of the asset, lower-case hex.</summary>
		[DataMember(Name = "sha256", Order = 3)]
		public string Sha256 { get; set; }

		[DataMember(Name = "published", Order = 4)]
		public string Published { get; set; }

		/// <summary>Address of the release page, for the notes.</summary>
		[DataMember(Name = "notes", Order = 5)]
		public string Notes { get; set; }

		public Version ParsedVersion
		{
			get { return new Version(Version); }
		}

		/// <summary>Reads a manifest and refuses one that could not be acted on.</summary>
		/// <exception cref="FormatException">The text is not a manifest this program can use.</exception>
		public static UpdateManifest Parse(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				throw new FormatException("The manifest is empty.");
			UpdateManifest manifest;
			try
			{
				var serializer = new DataContractJsonSerializer(typeof(UpdateManifest));
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
					manifest = (UpdateManifest)serializer.ReadObject(stream);
			}
			catch (Exception ex)
			{
				throw new FormatException("The manifest could not be read: " + ex.Message, ex);
			}
			Version version;
			if (manifest == null || !System.Version.TryParse(manifest.Version, out version) || version.Build < 0 || version.Revision < 0)
				throw new FormatException("The manifest carries no usable version.");
			if (string.IsNullOrWhiteSpace(manifest.File))
				throw new FormatException("The manifest names no file.");
			if (manifest.Size <= 0)
				throw new FormatException("The manifest gives no file size.");
			if (manifest.Sha256 == null || !Regex.IsMatch(manifest.Sha256, "^[0-9a-fA-F]{64}$"))
				throw new FormatException("The manifest carries no SHA-256.");
			manifest.Sha256 = manifest.Sha256.ToLowerInvariant();
			return manifest;
		}

		/// <summary>Whether the bytes are the file the manifest describes.</summary>
		public bool Describes(byte[] data)
		{
			return data != null && data.LongLength == Size && Sha256Of(data) == Sha256;
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

		/// <summary>The highest version among the titles, ignoring any title not of the form; null when none is.</summary>
		public static Version HighestTitle(IEnumerable<string> titles)
		{
			Version highest = null;
			foreach (var title in titles ?? Enumerable.Empty<string>())
			{
				Version version;
				if (TryParseTitle(title, out version) && (highest == null || version > highest))
					highest = version;
			}
			return highest;
		}
	}
}
