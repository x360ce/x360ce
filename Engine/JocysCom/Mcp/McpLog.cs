#nullable disable
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Mcp
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

		/// <summary>Where the log lives: the program's settings folder, which each program sets. A test may point it elsewhere.</summary>
		public static string Folder;

		/// <summary>The log's file name. Each program sets its own, because programs may share a settings folder.</summary>
		public static string FileName = "AiAccess.log";

		/// <summary>Beside the settings, so it travels with them and the settings page can open it. In the local application data folder when the program set no folder.</summary>
		public static string Path
		{
			get { return System.IO.Path.Combine(Folder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FileName); }
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

		/// <summary>Opens the log in the program Windows uses for it, writing a first line when there is no log yet.</summary>
		public static void Open()
		{
			if (!File.Exists(Path))
				Write("log opened from the settings");
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path) { UseShellExecute = true });
		}

		/// <summary>One line, and no more of it than a reader needs: a whole tree read is summarised by its length.</summary>
		public static string Clip(string text, int max)
		{
			if (text == null)
				return "";
			text = Flat(text);
			return text.Length <= max ? text : text.Substring(0, max) + "... (" + text.Length + " chars)";
		}

		/// <summary>
		/// One line, and never a token: a read of the settings page carries it, and a log is for reading afterwards by
		/// anyone. A secret token is masked, so the line still says which kind it was; a token made before tokens had a
		/// prefix, 64 hex digits, is replaced.
		/// </summary>
		static string Flat(string text)
		{
			text = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
			text = Security.TokenHelper.SecretTokenPattern.Replace(text, m => Security.TokenHelper.IsSecretToken(m.Value) ? Security.TokenHelper.MaskSecret(m.Value) : m.Value);
			return HexToken.Replace(text, "<token>");
		}

		static readonly Regex HexToken = new Regex("[0-9a-f]{64}");
	}
}
