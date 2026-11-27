using System;

namespace x360ce.Plugins
{
	/// <summary>A plugin's words in the language the person chose in x360ce, and English where there is no translation.</summary>
	/// <remarks>
	/// Write each text a person reads as <c>PluginText.T("MyPlugin.Key", "English words")</c>, with both as literals and the
	/// key starting with the plugin's name. When x360ce lists the texts to translate, it finds every such call in the
	/// plugin's DLL, so a key made at run time is never translated. Build a page's words in a method and call it again
	/// on <see cref="Changed"/>, so the page follows a change of language at once.
	/// </remarks>
	public static class PluginText
	{
		/// <summary>The words for a key in the language chosen: its translation of <paramref name="english"/>, or the English.</summary>
		public static string T(string key, string english)
		{
			var translate = Translate;
			return translate == null ? english : translate(key, english);
		}

		/// <summary>Set by x360ce: the translation of a key's English, or the English itself.</summary>
		public static Func<string, string, string> Translate { get; set; }

		/// <summary>Raised by x360ce on the interface thread after the person chose another language.</summary>
		public static event EventHandler Changed;

		/// <summary>Called by x360ce after the language changed: raises <see cref="Changed"/>.</summary>
		public static void OnChanged()
		{
			Changed?.Invoke(null, EventArgs.Empty);
		}
	}
}
