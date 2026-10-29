using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.App.Recording
{
	/// <summary>Makes text safe to post where anyone can read it: no user, PC, folder, address, key or device serial is left in it.</summary>
	/// <remarks>
	/// A recording runs every text that comes from outside the program's own words through <see cref="Clean"/>: product
	/// names, option values, file names, error messages. What the program writes itself, numbers and control names, needs
	/// none of it. Each rule keeps what a reader needs to find the cause: a path keeps its file name, a web address its host,
	/// and a device path its vendor and product ID.
	/// </remarks>
	public static class ShareSafe
	{
		/// <summary>Device paths and instance IDs, which hold a serial or a port; only the vendor and product ID are kept.</summary>
		static readonly Regex DevicePath = new Regex(@"(?:\\\\\?\\|\b(?:HID|USB|BTHENUM|BTHLEDEVICE|SWD|ROOT|XUSB)\\)[^\s""'<>]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		static readonly Regex VendorProduct = new Regex(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>Web addresses: the path and query can hold a key, so only the scheme and host are kept.</summary>
		static readonly Regex Url = new Regex(@"\b([a-z][a-z0-9+.-]*)://([^/\s:?#""'<>]+)[^\s""'<>]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>Paths on a drive or a share: the folders can hold the user's name, so only the last part is kept.</summary>
		static readonly Regex FilePath = new Regex(@"(?:\b[A-Za-z]:|\\\\[^\\/\s:*?""<>|]+)\\(?:[^\\/:*?""<>|\r\n]*\\)*([^\\/:*?""<>|\r\n]*)", RegexOptions.CultureInvariant);

		static readonly Regex Email = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant);

		/// <summary>Access tokens, API keys and other long runs of letters and digits.</summary>
		static readonly Regex Key = new Regex(@"\bsk-[A-Za-z0-9_-]{8,}|\b[A-Za-z0-9+/_-]{40,}={0,2}", RegexOptions.CultureInvariant);

		static readonly Regex AnyGuid = new Regex(@"\{?\b[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\b\}?", RegexOptions.CultureInvariant);

		/// <summary>The names of this user and this PC, longest first, each with what it is replaced with.</summary>
		static readonly KeyValuePair<Regex, string>[] Personal = PersonalNames();

		static KeyValuePair<Regex, string>[] PersonalNames()
		{
			var names = new List<KeyValuePair<string, string>>();
			Add(names, Environment.UserName, "<user>");
			Add(names, ProfileName(), "<user>");
			Add(names, Environment.MachineName, "<pc>");
			Add(names, Environment.UserDomainName, "<pc>");
			return names
				.OrderByDescending(x => x.Key.Length)
				.Select(x => new KeyValuePair<Regex, string>(new Regex(@"\b" + Regex.Escape(x.Key) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), x.Value))
				.ToArray();
		}

		/// <summary>The name of the user's profile folder, which can differ from the user name.</summary>
		static string ProfileName()
		{
			var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return string.IsNullOrEmpty(profile) ? null : Path.GetFileName(profile.TrimEnd('\\'));
		}

		/// <summary>Adds a name to replace. A name shorter than three letters is left alone, as it would change ordinary words.</summary>
		static void Add(List<KeyValuePair<string, string>> names, string name, string replacement)
		{
			if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
				return;
			name = name.Trim();
			if (names.Any(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)))
				return;
			names.Add(new KeyValuePair<string, string>(name, replacement));
		}

		/// <summary>The text with every user, PC, folder, address, key and device serial taken out.</summary>
		/// <param name="text">Any text; null gives null.</param>
		/// <param name="label">Names a GUID that stands for something known, such as a device's D1; null, or a null answer, writes it as &lt;id&gt;.</param>
		public static string Clean(string text, Func<Guid, string> label = null)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			// Device paths first: they hold backslashes the path rule would cut at the wrong place.
			text = DevicePath.Replace(text, m =>
			{
				var ids = VendorProduct.Match(m.Value);
				return ids.Success ? "VID_" + ids.Groups[1].Value.ToUpperInvariant() + "&PID_" + ids.Groups[2].Value.ToUpperInvariant() : "<device>";
			});
			text = Url.Replace(text, m => m.Groups[1].Value + "://" + m.Groups[2].Value);
			text = FilePath.Replace(text, m => m.Groups[1].Value);
			text = Email.Replace(text, "<email>");
			text = AnyGuid.Replace(text, m =>
			{
				Guid guid;
				var name = label != null && Guid.TryParse(m.Value, out guid) ? label(guid) : null;
				return name ?? "<id>";
			});
			text = Key.Replace(text, "<key>");
			foreach (var name in Personal)
				text = name.Key.Replace(text, name.Value);
			return text;
		}
	}
}
