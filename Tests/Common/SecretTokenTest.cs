// @under-test: Engine/JocysCom/Security/TokenHelper.Secret.cs
// @area: security   @layer: unit
using JocysCom.ClassLibrary.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The shared secret tokens: a prefix that says who issued a token and what it opens, a random part and a checksum
	/// that tells a token from a typo without asking the issuer; and any secret masked so it can be shown.
	/// </summary>
	[TestClass]
	public class SecretTokenTest
	{
		[TestMethod, TestCategory("security"), TestCategory("critical")]
		[Description("A new token is the prefix, 30 random Base62 characters and a 6-character checksum, and no two are alike")]
		public void A_token_is_its_prefix_a_random_part_and_a_checksum()
		{
			var token = TokenHelper.NewSecretToken("sk-x360ce-mcp");
			StringAssert.StartsWith(token, "sk-x360ce-mcp-");
			var rest = token.Substring("sk-x360ce-mcp-".Length);
			Assert.AreEqual(36, rest.Length, "The random part and the checksum are not 36 characters.");
			Assert.IsTrue(rest.All(char.IsLetterOrDigit), "The token is not one word a double-click selects whole: " + token);
			Assert.IsTrue(TokenHelper.IsSecretToken(token), "A new token does not pass its own checksum.");
			var many = Enumerable.Range(0, 1000).Select(x => TokenHelper.NewSecretToken("sk-test")).ToArray();
			Assert.AreEqual(many.Length, many.Distinct().Count(), "Two new tokens are alike.");
			// Each of the 62 characters is drawn about as often: 30 000 characters, about 484 each.
			var counts = many.SelectMany(x => x.Substring("sk-test-".Length, 30)).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
			Assert.AreEqual(62, counts.Count, "Some Base62 characters are never drawn.");
			Assert.IsTrue(counts.Values.All(x => x > 350 && x < 650), "The random part favours some characters.");
		}

		[TestMethod, TestCategory("security"), TestCategory("critical")]
		[Description("The checksum tells a token from a typo, a changed prefix or a look-alike, without asking who issued it")]
		public void The_checksum_tells_a_token_from_a_typo()
		{
			var token = TokenHelper.NewSecretToken("sk-x360ce-mcp");
			var i = token.Length - 10;
			var typo = token.Substring(0, i) + (token[i] == 'a' ? 'b' : 'a') + token.Substring(i + 1);
			Assert.IsFalse(TokenHelper.IsSecretToken(typo), "A token with a character changed passes.");
			Assert.IsFalse(TokenHelper.IsSecretToken("sk-x360ce-ai" + token.Substring("sk-x360ce-mcp".Length)), "A token with another prefix passes.");
			Assert.IsFalse(TokenHelper.IsSecretToken(token.Substring(1)), "A token cut short passes.");
			Assert.IsFalse(TokenHelper.IsSecretToken(new string('a', 64)), "64 hex digits pass as a secret token.");
			Assert.IsFalse(TokenHelper.IsSecretToken(""));
			Assert.IsFalse(TokenHelper.IsSecretToken(null));
			var match = TokenHelper.SecretTokenPattern.Match("Authorization: Bearer " + token + ".");
			Assert.AreEqual(token, match.Value, "A token in a line of text is not found.");
		}

		[TestMethod, TestCategory("security")]
		[Description("A prefix is made lower-case words joined by '-', so it can be told from the random part, and one with no letter or digit is refused")]
		public void A_prefix_is_made_lower_case_words()
		{
			Assert.AreEqual("sk-x360ce", TokenHelper.ToSecretPrefix("SK-X360CE"));
			Assert.AreEqual("sk-aip", TokenHelper.ToSecretPrefix("sk-aip-"), "A configured prefix with its '-' is not taken as it is meant.");
			Assert.AreEqual("sk-avatar-evo-jocys", TokenHelper.ToSecretPrefix("sk-avatar-Evo.Jocys"), "A user name in a prefix is not made words.");
			Assert.AreEqual("sk-mcp", TokenHelper.ToSecretPrefix("sk__mcp  "));
			StringAssert.StartsWith(TokenHelper.NewSecretToken("SK_Test-"), "sk-test-");
			foreach (var bad in new[] { null, "", "-", " _. " })
				Assert.ThrowsExactly<ArgumentException>(() => TokenHelper.NewSecretToken(bad), "The prefix '" + bad + "' was taken.");
			Assert.IsTrue(TokenHelper.IsSecretToken(TokenHelper.NewSecretToken("sk")));
		}

		[TestMethod, TestCategory("security"), TestCategory("critical")]
		[Description("A masked secret keeps its prefix and shows at most its last four characters, fewer of a short one, and never its length")]
		public void A_masked_secret_shows_its_prefix_and_last_four_at_most()
		{
			var token = TokenHelper.NewSecretToken("sk-x360ce-mcp");
			Assert.AreEqual("sk-x360ce-mcp-****" + token.Substring(token.Length - 4), TokenHelper.MaskSecret(token));
			// One character shown for each eight after the prefix, at most four, so seven in eight stay hidden.
			Assert.AreEqual("****" + "0123", TokenHelper.MaskSecret(new string('x', 60) + "0123"), "A long secret does not show its last four.");
			Assert.AreEqual("****" + "23", TokenHelper.MaskSecret("abcdefghijklmn0123"), "An 18-character secret does not show two.");
			Assert.AreEqual("****" + "3", TokenHelper.MaskSecret("abcdef0123"), "A 10-character secret does not show one.");
			Assert.AreEqual("****", TokenHelper.MaskSecret("hunter2"), "A secret shorter than eight shows any of itself.");
			Assert.AreEqual("", TokenHelper.MaskSecret(""));
			Assert.AreEqual("", TokenHelper.MaskSecret(null));
			// A key of another shape keeps the prefix it is given: AiPlatform's sk-aip- keys.
			Assert.AreEqual("sk-aip-****A1b2", TokenHelper.MaskSecret("sk-aip-" + new string('Q', 39) + "A1b2", "sk-aip-"));
			Assert.AreEqual(TokenHelper.MaskSecret(new string('x', 40)).Length, TokenHelper.MaskSecret(new string('x', 400)).Length,
				"The mask gives the secret's length away.");
		}
	}
}
