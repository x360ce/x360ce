using System;
using System.IO;
using System.Text;
using x360ce.Engine;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// What was done through the door, one line per action, so a person can read afterwards what
	/// an assistant did with the program: every tool call with its arguments and outcome, every
	/// refusal, and every opening and closing of the door.
	/// </summary>
	public static class McpLog
	{
		const long RollAt = 5 * 1024 * 1024;
		static readonly object Gate = new object();

		/// <summary>Where the log lives: beside the settings unless a test points it elsewhere.</summary>
		public static string Folder;

		/// <summary>Beside the settings, so it travels with them and the Options page can open it.</summary>
		public static string Path
		{
			get { return System.IO.Path.Combine(Folder ?? EngineHelper.AppDataPath, "x360ce.AiAccess.log"); }
		}

		/// <summary>Appends one dated line. A log that cannot be written must not stop the action it records, so a failure to write is the one thing not logged.</summary>
		public static void Write(string message)
		{
			var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + Flat(message ?? "") + "\r\n";
			try
			{
				lock (Gate)
				{
					var file = new FileInfo(Path);
					if (file.Exists && file.Length > RollAt)
					{
						var old = Path + ".old";
						if (File.Exists(old))
							File.Delete(old);
						file.MoveTo(old);
					}
					File.AppendAllText(Path, line, Encoding.UTF8);
				}
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		/// <summary>One line, and no more of it than a reader needs: a whole tree read is summarised by its length.</summary>
		public static string Clip(string text, int max)
		{
			if (text == null)
				return "";
			text = Flat(text);
			return text.Length <= max ? text : text.Substring(0, max) + "... (" + text.Length + " chars)";
		}

		/// <summary>One line, and never the token: a read of the Options page carries it, and a log is for reading afterwards by anyone.</summary>
		static string Flat(string text)
		{
			return Token.Replace(text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " "), "<token>");
		}

		static readonly System.Text.RegularExpressions.Regex Token = new System.Text.RegularExpressions.Regex("[0-9a-f]{64}");
	}
}
