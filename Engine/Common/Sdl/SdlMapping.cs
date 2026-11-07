using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	/// <summary>How an SDL controller GUID is written.</summary>
	public enum SdlGuidForm
	{
		/// <summary>A form the ids cannot be read from: the row matches only a GUID written the same way.</summary>
		Other,
		/// <summary>SDL 2.0.5 and later: bus, name CRC, vendor, product, version, driver, each number little-endian.</summary>
		Standard,
		/// <summary>SDL before 2.0.5: DirectInput's product GUID, vendor and product followed by "PIDVID".</summary>
		ProductGuid,
		/// <summary>The one row SDL keeps for every controller it reads through XInput.</summary>
		XInput,
	}

	/// <summary>The GUID that starts an SDL controller row, and the ids it carries.</summary>
	/// <remarks>
	/// Standard form, 32 hex digits: <c>{bus}{crc}{vendor}0000{product}0000{version}{driver}{data}</c>, each two-byte
	/// number little-endian, as <c>03000000300f00000a01000000000000</c> for USB (bus 0x0003) vendor 0x0F30 product
	/// 0x010A. The CRC of the name and the version do not tell controllers apart for x360ce, so they are left out of
	/// <see cref="Key"/> and of matching. Older SDL wrote DirectInput's product GUID instead:
	/// <c>{vendor}{product}000000000000504944564944</c>, the last six bytes "PIDVID".
	/// </remarks>
	public sealed class SdlGuid
	{
		/// <summary>The GUID as written, in lower case.</summary>
		public string Text { get; private set; }

		public SdlGuidForm Form { get; private set; }

		/// <summary>The bus, 0x03 USB or 0x05 Bluetooth; nought when the form does not carry one.</summary>
		public int Bus { get; private set; }

		public int VendorId { get; private set; }

		public int ProductId { get; private set; }

		/// <summary>The GUID with what does not tell controllers apart cleared: the CRC of the name and the version. Two rows with one key are one row, the later kept.</summary>
		public string Key { get; private set; }

		/// <summary>Whether the GUID names a vendor and product.</summary>
		public bool HasIds
		{
			get { return VendorId != 0 || ProductId != 0; }
		}

		/// <summary>The GUID that starts the row SDL looks up for the "xinput" form.</summary>
		public const string XInputText = "xinput";

		/// <summary>The last six bytes of a DirectInput product GUID: "PIDVID".</summary>
		const string ProductGuidTail = "504944564944";

		/// <summary>Reads a GUID; null when the text is not one an SDL row starts with.</summary>
		public static SdlGuid Parse(string text)
		{
			var t = (text ?? "").Trim().ToLowerInvariant();
			if (t == XInputText)
				return new SdlGuid { Text = t, Form = SdlGuidForm.XInput, Key = t };
			if (t.Length != 32 || !t.All(Uri.IsHexDigit))
				return null;
			var g = new SdlGuid { Text = t, Form = SdlGuidForm.Other, Key = t };
			if (t.EndsWith(ProductGuidTail, StringComparison.Ordinal) && t.Substring(8, 12) == "000000000000")
			{
				g.Form = SdlGuidForm.ProductGuid;
				g.VendorId = Le16(t, 0);
				g.ProductId = Le16(t, 4);
			}
			else if (t.Substring(12, 4) == "0000" && t.Substring(20, 4) == "0000")
			{
				g.Form = SdlGuidForm.Standard;
				g.Bus = Le16(t, 0);
				g.VendorId = Le16(t, 8);
				g.ProductId = Le16(t, 16);
				g.Key = t.Substring(0, 4) + "0000" + t.Substring(8, 16) + "0000" + t.Substring(28, 4);
			}
			return g;
		}

		/// <summary>The standard GUID of a USB controller with these ids, as the database writes it: no CRC, no version.</summary>
		public static string ForDevice(int vendorId, int productId)
		{
			return string.Format(CultureInfo.InvariantCulture, "03000000{0}0000{1}000000000000", HexLe16(vendorId), HexLe16(productId));
		}

		/// <summary>The bus in words.</summary>
		public string BusName
		{
			get
			{
				switch (Bus)
				{
					case 0x03: return "USB";
					case 0x05: return "Bluetooth";
					default: return Bus == 0 ? "" : "bus 0x" + Bus.ToString("X2", CultureInfo.InvariantCulture);
				}
			}
		}

		static int Le16(string hex, int at)
		{
			var lo = int.Parse(hex.Substring(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var hi = int.Parse(hex.Substring(at + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return lo | hi << 8;
		}

		static string HexLe16(int value)
		{
			return (value & 0xFF).ToString("x2", CultureInfo.InvariantCulture) + ((value >> 8) & 0xFF).ToString("x2", CultureInfo.InvariantCulture);
		}

		public override string ToString()
		{
			return Text;
		}
	}

	/// <summary>One row of an SDL controller database: <c>GUID,name,field:binding,...,platform:Windows,</c>.</summary>
	/// <remarks>
	/// The format is SDL's, as <c>gamecontrollerdb.txt</c> from SDL_GameControllerDB and the
	/// <c>SDL_GAMECONTROLLERCONFIG</c> environment variable write it. A field names a part of the gamepad, such as
	/// <c>a</c>, <c>leftx</c> or <c>-lefty</c>, and its binding the control of the device, such as <c>b0</c>, <c>+a2</c>,
	/// <c>a1~</c> or <c>h0.1</c>. Other fields, such as <c>platform</c>, <c>face</c>, <c>hint</c> and <c>crc</c>, say
	/// something about the row. <see cref="SdlConverter"/> reads the bindings.
	/// </remarks>
	public sealed class SdlMapping
	{
		public SdlGuid Guid { get; private set; }

		/// <summary>The controller's name, as the row gives it.</summary>
		public string Name { get; private set; }

		/// <summary>The platform the row is for, such as "Windows"; null when the row names none, which SDL reads on every platform.</summary>
		public string Platform { get; private set; }

		/// <summary>Every field but the platform, in the order written.</summary>
		public IList<KeyValuePair<string, string>> Fields { get; private set; }

		/// <summary>The platform x360ce reads rows for.</summary>
		public const string WindowsPlatform = "Windows";

		const string PlatformField = "platform";

		/// <summary>Whether the row is one x360ce uses: for Windows, or for no platform in particular.</summary>
		public bool IsForWindows
		{
			get { return Platform == null || string.Equals(Platform, WindowsPlatform, StringComparison.OrdinalIgnoreCase); }
		}

		/// <summary>The binding of a field, or null when the row has none.</summary>
		public string Get(string field)
		{
			foreach (var f in Fields)
				if (f.Key == field)
					return f.Value;
			return null;
		}

		/// <summary>Reads one line; false for a comment, an empty line, or text that is not an SDL row.</summary>
		/// <remarks>A row starts with a GUID of 32 hex digits or "xinput", then a name, then name:value fields.</remarks>
		public static bool TryParse(string line, out SdlMapping mapping)
		{
			mapping = null;
			var t = (line ?? "").Trim().TrimStart('﻿');
			if (t.Length == 0 || t[0] == '#')
				return false;
			var parts = t.Split(',');
			if (parts.Length < 3)
				return false;
			var guid = SdlGuid.Parse(parts[0]);
			if (guid == null)
				return false;
			var fields = new List<KeyValuePair<string, string>>();
			string platform = null;
			for (var i = 2; i < parts.Length; i++)
			{
				var part = parts[i].Trim();
				if (part.Length == 0)
					continue;
				var colon = part.IndexOf(':');
				if (colon <= 0)
					return false;
				var key = part.Substring(0, colon);
				var value = part.Substring(colon + 1);
				if (key == PlatformField)
					platform = value;
				else
					fields.Add(new KeyValuePair<string, string>(key, value));
			}
			if (fields.Count == 0)
				return false;
			mapping = new SdlMapping
			{
				Guid = guid,
				Name = parts[1].Trim(),
				Platform = platform,
				Fields = fields.AsReadOnly(),
			};
			return true;
		}

		/// <summary>A row made from its parts; the name loses the commas SDL cannot carry in it.</summary>
		public static SdlMapping Create(string guid, string name, IEnumerable<KeyValuePair<string, string>> fields, string platform = WindowsPlatform)
		{
			return new SdlMapping
			{
				Guid = SdlGuid.Parse(guid),
				Name = (name ?? "").Replace(",", " ").Trim(),
				Platform = platform,
				Fields = fields.ToList().AsReadOnly(),
			};
		}

		/// <summary>The row as SDL writes it, the platform last.</summary>
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Guid == null ? "" : Guid.Text).Append(',').Append(Name).Append(',');
			foreach (var f in Fields)
				sb.Append(f.Key).Append(':').Append(f.Value).Append(',');
			if (Platform != null)
				sb.Append(PlatformField).Append(':').Append(Platform).Append(',');
			return sb.ToString();
		}
	}
}
