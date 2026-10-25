#nullable disable

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Security
{
	/// <summary>
	/// Secret tokens, such as API keys and access tokens, in the shape GitHub's tokens made common: a prefix that says who
	/// issued the token and what it opens, a random part, and a checksum; and any secret masked for showing.
	/// </summary>
	/// <remarks>
	/// A token reads <c>{prefix}-{random}{checksum}</c>, such as <c>sk-x360ce-mcp-</c> and 36 letters and digits: 30 random
	/// Base62 characters, about 178 bits, then 6 Base62 characters of checksum, the first 32 bits of the SHA-256 of all
	/// before them. Base62 keeps the token one word, which a double-click selects whole and no separator splits. The
	/// checksum lets a program, a log or a secret scanner tell a token from a typo or a look-alike without asking the
	/// server that issued it; it proves nothing about who made the token. No standard defines a token's shape: RFC 8959
	/// only wraps a secret in a secret-token: URI, and a prefix with a checksum is the practice of GitHub, npm and others.
	/// </remarks>
	public static partial class TokenHelper
	{
		const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		const int SecretRandomLength = 30;
		const int SecretChecksumLength = 6;

		/// <summary>What is not a letter or digit of a prefix: each run of it becomes one '-'.</summary>
		static readonly Regex SecretPrefixSeparator = new Regex("[^a-z0-9]+");

		/// <summary>What looks like a secret token in text; <see cref="IsSecretToken"/> tells a real one by its checksum.</summary>
		public static readonly Regex SecretTokenPattern = new Regex("\\b[a-z0-9]+(?:-[a-z0-9]+)*-[0-9A-Za-z]{36}\\b");

		/// <summary>
		/// Text made a token's prefix: lower-case letters and digits in words joined by '-', which tell it from the random
		/// part. "SK_Test-" becomes "sk-test"; a user name such as "Evo.Jocys" becomes "evo-jocys".
		/// </summary>
		public static string ToSecretPrefix(string text)
		{
			return SecretPrefixSeparator.Replace((text ?? "").ToLowerInvariant(), "-").Trim('-');
		}

		/// <summary>A new secret token.</summary>
		/// <param name="prefix">
		/// Who issued the token and what it opens: "sk" for a secret key, the program, the service, such as sk-x360ce-mcp.
		/// Made a prefix with <see cref="ToSecretPrefix"/>.
		/// </param>
		public static string NewSecretToken(string prefix)
		{
			prefix = ToSecretPrefix(prefix);
			if (prefix.Length == 0)
				throw new ArgumentException("A token needs a prefix of letters or digits, such as sk-x360ce-mcp.", nameof(prefix));
			var sb = new StringBuilder(prefix).Append('-');
			var end = sb.Length + SecretRandomLength;
			var bytes = new byte[64];
			using (var random = RandomNumberGenerator.Create())
			{
				while (sb.Length < end)
				{
					random.GetBytes(bytes);
					// 248 is four times 62: a byte from 248 up is left out, so every character is as likely as any other.
					for (var i = 0; i < bytes.Length && sb.Length < end; i++)
						if (bytes[i] < 248)
							sb.Append(Base62[bytes[i] % 62]);
				}
			}
			return sb.Append(SecretChecksum(sb.ToString())).ToString();
		}

		/// <summary>True for a secret token whose checksum is right: one that could have been issued, not a typo or a look-alike.</summary>
		public static bool IsSecretToken(string token)
		{
			if (string.IsNullOrEmpty(token))
				return false;
			var match = SecretTokenPattern.Match(token);
			if (!match.Success || match.Index != 0 || match.Length != token.Length)
				return false;
			var body = token.Substring(0, token.Length - SecretChecksumLength);
			return string.Equals(token.Substring(body.Length), SecretChecksum(body), StringComparison.Ordinal);
		}

		/// <summary>
		/// A secret as it may be shown, in a list, a log or a message: its prefix, which says what it is, then ****, then
		/// its last characters, which tell two secrets apart, such as sk-x360ce-mcp-****Ab12.
		/// </summary>
		/// <remarks>
		/// At most the last four characters show, as Stripe, OpenAI and PCI DSS's "last four" show them, and fewer of a
		/// shorter secret: one for each eight characters after the prefix, so seven in eight stay hidden, and none of a
		/// secret shorter than eight. The **** is the same whatever the length, which it does not give away.
		/// </remarks>
		/// <param name="secret">The secret, such as a token, an API key or a password.</param>
		/// <param name="prefix">
		/// What the secret starts with that says what it is, such as "sk-aip-", kept as it is. Without one, the prefix of a
		/// secret token (<see cref="IsSecretToken"/>) is kept, and of any other secret nothing.
		/// </param>
		public static string MaskSecret(string secret, string prefix = null)
		{
			if (string.IsNullOrEmpty(secret))
				return "";
			if (string.IsNullOrEmpty(prefix) || !secret.StartsWith(prefix, StringComparison.Ordinal))
				prefix = IsSecretToken(secret) ? secret.Substring(0, secret.LastIndexOf('-') + 1) : "";
			var rest = secret.Length - prefix.Length;
			var shown = Math.Min(4, rest / 8);
			return prefix + "****" + secret.Substring(secret.Length - shown);
		}

		/// <summary>The first 32 bits of the SHA-256 of the text, in six Base62 characters.</summary>
		static string SecretChecksum(string body)
		{
			byte[] hash;
			using (var sha = SHA256.Create())
				hash = sha.ComputeHash(Encoding.UTF8.GetBytes(body));
			var value = (uint)hash[0] << 24 | (uint)hash[1] << 16 | (uint)hash[2] << 8 | hash[3];
			var chars = new char[SecretChecksumLength];
			for (var i = chars.Length - 1; i >= 0; i--)
			{
				chars[i] = Base62[(int)(value % 62)];
				value /= 62;
			}
			return new string(chars);
		}
	}
}
