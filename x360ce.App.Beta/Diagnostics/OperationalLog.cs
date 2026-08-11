using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace x360ce.App.Diagnostics
{
	/// <summary>Small dependency-free JSON-lines log available before WPF startup.</summary>
	public sealed class OperationalLog : IDisposable
	{
		readonly object syncRoot = new object();
		readonly StreamWriter writer;
		bool disposed;

		public OperationalLog(string folder, int maxFiles = 10)
		{
			if (string.IsNullOrWhiteSpace(folder))
				throw new ArgumentNullException(nameof(folder));
			SessionId = Guid.NewGuid().ToString("N");
			Directory.CreateDirectory(folder);
			CurrentFilePath = Path.Combine(folder, "x360ce-" + SessionId + ".jsonl");
			writer = new StreamWriter(CurrentFilePath, true, new UTF8Encoding(false)) { AutoFlush = true };
			DeleteOldFiles(folder, Math.Max(1, maxFiles));
			Write("application_session_started", fields: new Dictionary<string, object>
			{
				["processArchitecture"] = Environment.Is64BitProcess ? "x64" : "x86",
				["osVersion"] = Environment.OSVersion.VersionString,
			});
		}

		public static OperationalLog Current { get; private set; }
		public string SessionId { get; }
		public string CurrentFilePath { get; }

		public static OperationalLog InitializeDefault()
		{
			if (Current != null)
				return Current;
			var folder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"x360ce", "Logs");
			Current = new OperationalLog(folder);
			return Current;
		}

		public IDisposable Measure(string stage)
		{
			if (string.IsNullOrWhiteSpace(stage))
				throw new ArgumentNullException(nameof(stage));
			Write("startup_stage_started", fields: new Dictionary<string, object> { ["stage"] = stage });
			return new StageScope(this, stage);
		}

		public void Write(string eventName, string level = "info", IDictionary<string, object> fields = null)
		{
			if (string.IsNullOrWhiteSpace(eventName))
				throw new ArgumentNullException(nameof(eventName));
			lock (syncRoot)
			{
				if (disposed)
					return;
				var values = new SortedDictionary<string, object>(StringComparer.Ordinal)
				{
					["timestamp"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
					["level"] = level,
					["event"] = eventName,
					["sessionId"] = SessionId,
					["processId"] = Process.GetCurrentProcess().Id,
					["threadId"] = Thread.CurrentThread.ManagedThreadId,
				};
				if (fields != null)
				{
					foreach (var pair in fields)
						values[pair.Key] = pair.Value;
				}
				writer.WriteLine(ToJson(values));
			}
		}

		public void WriteException(string eventName, Exception exception, IDictionary<string, object> fields = null)
		{
			var values = fields == null
				? new Dictionary<string, object>()
				: new Dictionary<string, object>(fields);
			values["exceptionType"] = exception?.GetType().FullName;
			values["hresult"] = exception?.HResult;
			values["message"] = exception?.Message;
			values["stackTrace"] = exception?.StackTrace;
			Write(eventName, "error", values);
		}

		static string ToJson(IEnumerable<KeyValuePair<string, object>> values)
		{
			var text = new StringBuilder("{");
			var first = true;
			foreach (var pair in values)
			{
				if (!first)
					text.Append(',');
				first = false;
				text.Append('"').Append(Escape(pair.Key)).Append("\":").Append(ToJsonValue(pair.Value));
			}
			return text.Append('}').ToString();
		}

		static string ToJsonValue(object value)
		{
			if (value == null)
				return "null";
			if (value is bool boolean)
				return boolean ? "true" : "false";
			if (value is byte || value is sbyte || value is short || value is ushort ||
				value is int || value is uint || value is long || value is ulong ||
				value is float || value is double || value is decimal)
				return Convert.ToString(value, CultureInfo.InvariantCulture);
			return "\"" + Escape(Convert.ToString(value, CultureInfo.InvariantCulture)) + "\"";
		}

		static string Escape(string value)
		{
			if (value == null)
				return string.Empty;
			return value.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n")
				.Replace("\t", "\\t");
		}

		void DeleteOldFiles(string folder, int maxFiles)
		{
			try
			{
				var oldFiles = new DirectoryInfo(folder).GetFiles("x360ce-*.jsonl")
					.Where(x => !string.Equals(x.FullName, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(x => x.LastWriteTimeUtc)
					.Skip(maxFiles - 1)
					.ToArray();
				foreach (var file in oldFiles)
					file.Delete();
			}
			catch (Exception)
			{
				// Logging and retention must never prevent the application from starting.
			}
		}

		public void Dispose()
		{
			lock (syncRoot)
			{
				if (disposed)
					return;
				writer.WriteLine(ToJson(new SortedDictionary<string, object>
				{
					["timestamp"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
					["level"] = "info",
					["event"] = "application_session_ended",
					["sessionId"] = SessionId,
				}));
				writer.Dispose();
				disposed = true;
			}
		}

		sealed class StageScope : IDisposable
		{
			readonly OperationalLog owner;
			readonly string stage;
			readonly Stopwatch stopwatch = Stopwatch.StartNew();
			bool isDisposed;

			public StageScope(OperationalLog owner, string stage)
			{
				this.owner = owner;
				this.stage = stage;
			}

			public void Dispose()
			{
				if (isDisposed)
					return;
				stopwatch.Stop();
				owner.Write("startup_stage_completed", fields: new Dictionary<string, object>
				{
					["stage"] = stage,
					["durationMs"] = stopwatch.Elapsed.TotalMilliseconds,
				});
				isDisposed = true;
			}
		}
	}
}
