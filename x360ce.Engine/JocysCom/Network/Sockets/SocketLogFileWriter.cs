using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace JocysCom.ClassLibrary.Network.Sockets
{

	// Create a log file writer, so you can see the flow easily.
	// It can be printed. Makes it easier to figure out complex program flow.
	// The log StreamWriter uses a buffer. So it will only work right if you close
	// the server console properly at the end of the test.
	public class SocketLogFileWriter : IO.LogFileWriter
	{

		public SocketLogFileWriter(SocketSettings settings) : base(settings.ConfigPrefix)
		{
			// Store settings.
			Settings = settings;
		}

		SocketSettings Settings;

		public void WriteError(string format, params object[] args) { if (Settings.LogErrors || Settings.LogFlow) WriteLine(format, args); }
		public void WriteFlow(string format, params object[] args) { if (Settings.LogFlow) WriteLine(format, args); }

		public void WriteFlowWithData(byte[] bytes, int offset, int size, string format, params object[] args)
		{
			List<string> message = new List<string>();
			if (Settings.LogFlow) message.Add(args != null && args.Length > 0 ? string.Format(format, args) : format);
			if (Settings.LogData) message.Add(GetHexBlock(bytes, offset, size));
			if (message.Count > 0) WriteLine(string.Join(":\r\n", message.ToArray()));
		}

		public string GetHexBlock(byte[] bytes, int offset, int size)
		{
			var hex = BytesToStringBlock(bytes, false, true, true, offset, size);
			hex = IdentText(20, hex, ' ');
			return hex;
		}

		#region Thread Info

		HashSet<int> managedThreadIds = new HashSet<int>();
		HashSet<Thread> managedThreads = new HashSet<Thread>();
		// Create lock for HashSet of thread references.
		private object threadHashSetLock = new object();
		Process theProcess;

		/// <summary>
		/// Get thread info.
		/// </summary>
		/// <returns></returns>
		internal string GetThreadInfo()
		{
			var sb = new StringBuilder();
			bool newThreadChecker = false;
			lock (threadHashSetLock)
			{
				if (theProcess is null) theProcess = Process.GetCurrentProcess();
				if (managedThreadIds.Add(Thread.CurrentThread.ManagedThreadId))
				{
					managedThreads.Add(Thread.CurrentThread);
					newThreadChecker = true;
				}
			}
			if (newThreadChecker)
			{
				// Display managed threads
				// Note that there is NOT a 1:1 ratio between managed threads 
				// and system (native) threads.
				var nonLiveManagedThreadIds = managedThreads.Cast<Thread>().Where(x => !x.IsAlive).Select(x => x.ManagedThreadId).OrderBy(x => x).ToArray();
				AppendIds(sb, "Managed", nonLiveManagedThreadIds);
				// Managed threads below are still being used now.
				if (nonLiveManagedThreadIds.Length > 0) sb.AppendLine();
				var liveManagedThreadIds = managedThreads.Cast<Thread>().Where(x => x.IsAlive).Select(x => x.ManagedThreadId).OrderBy(x => x).ToArray();
				AppendIds(sb, "ManagedLive", liveManagedThreadIds);
				// Display system threads
				// Note that there is NOT a 1:1 ratio between managed threads 
				// and system (native) threads.
				if (liveManagedThreadIds.Length > 0) sb.AppendLine();
				var liveThreadIds = theProcess.Threads.Cast<ProcessThread>().Select(x => x.Id).OrderBy(x => x).ToArray();
				AppendIds(sb, "System", liveThreadIds);
			}
			return sb.ToString();
		}

		public static void AppendIds(StringBuilder sb, string title, int[] list)
		{
			sb.AppendLine(title + "[" + list.Length.ToString() + "]:");
			for (int i = 0; i < list.Length; i++)
			{
				if (i > 0)
				{
					if (i % 8 == 0) sb.AppendLine();
					else sb.Append(", ");
				}
				sb.Append(string.Format("{0,6}", list[i]));
			}
		}

		public static string WrapText(string s, int width)
		{
			var sb = new System.Text.StringBuilder(string.Empty);
			var i = 0;
			foreach (var c in s)
			{
				if (i > 0 && i % width == 0) sb.Append(Environment.NewLine);
				sb.Append(c);
				i++;
			}
			return sb.ToString().Trim('\r', '\n');
		}

		static string IdentText(int tabs, string s, char ident = '\t')
		{
			if (tabs == 0) return s;
			if (s is null) s = string.Empty;
			var sb = new System.Text.StringBuilder();
			var tr = new System.IO.StringReader(s);
			string prefix = string.Empty;
			for (int i = 0; i < tabs; i++) prefix += ident;
			string line;
			while ((line = tr.ReadLine()) != null)
			{
				if (sb.Length > 0) sb.AppendLine();
				if (tabs > 0) sb.Append(prefix);
				sb.Append(line);
			}
			return sb.ToString();
		}

		static string BytesToStringBlock(byte[] bytes, bool addIndex, bool addHex, bool addText, int offset = 0, int size = -1)
		{
			var builder = new StringBuilder();
			var hx = new StringBuilder();
			var ch = new StringBuilder();
			var lineIndex = 0;
			List<string> lines = new List<string>();
			var length = size == -1 ? bytes.Length : size;
			for (int i = 1; i <= length; i++)
			{
				var modulus = i % 16;
				hx.Append(bytes[i - 1 + offset].ToString("X2")).Append(" ");
				var c = (char)bytes[i - 1 + offset];
				if (System.Char.IsControl(c)) ch.Append(".");
				else ch.Append(c);
				// If line ended.
				if ((modulus == 0 && i > 1) || (i == length))
				{
					if (addIndex)
					{
						builder.Append((lineIndex * 16).ToString("X8"));
						if (addHex || addText) builder.Append(": ");
					}
					if (addHex)
					{
						if (hx.Length < 50) hx.Append(' ', 50 - hx.Length);
						builder.Append(hx.ToString());
						if (addText) builder.Append(" | ");
					}
					if (addText)
					{
						builder.Append(ch.ToString());
						lines.Add(builder.ToString());
						builder.Clear();
					}
					hx.Clear();
					ch.Clear();
					lineIndex++;
				}
			}
			return string.Join(Environment.NewLine, lines);
		}

		// Use this for testing, when there is NOT a UserToken yet. Use in SocketListener
		// method or Initialize().
		internal void WriteThreads(string methodName, UserToken token = null)
		{
			if (!Settings.LogThreads) return;
			var sb = new StringBuilder();
			sb.Append(methodName + ": ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);
			if (token != null) sb.Append(", TokenId = " + token.TokenId);
			sb.AppendLine();
			sb.Append(GetThreadInfo());
			// Indent with spaces.
			var s = IdentText(20, sb.ToString(), ' ').TrimEnd('\r', '\n');
			WriteLine(s);
		}

		#endregion

		#region IDisposable

		public new void Dispose()
		{
			base.Dispose();
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private new void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				Settings = null;
			}
		}

		#endregion

	}
}


