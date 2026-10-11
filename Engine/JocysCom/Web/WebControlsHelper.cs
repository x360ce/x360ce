#nullable disable

using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Web
{
	// Members that do not depend on ASP.NET, so they build for every target. The web part is in WebControlsHelper.Web.cs.
	public static partial class WebControlsHelper
	{

		// Uniform Resource Identifier (URI): Generic Syntax
		// Appendix B.  Parsing a URI Reference with a Regular Expression
		// https://tools.ietf.org/html/rfc3986#appendix-B
		public static Regex UriSchemeRx = new Regex("^(([^:\\/?#]+):)", RegexOptions.Compiled);

		/// <summary>Returns true it link is absolute. For example, starts with  "https:" or "C:".</summary>
		public static bool IsAbsoluteLink(string link) =>
			UriSchemeRx.IsMatch(link);

	}
}
