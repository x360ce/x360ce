// @under-test: App.v4 process memory, as a user's memory dump of it carries it
// @area: support   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// What a memory dump of a running x360ce weighs once zipped, against the 25 MB GitHub takes for a file
	/// attached to an issue, and what of the user's own it carries.
	/// </summary>
	/// <remarks>
	/// The three kinds a user can make without installing anything:
	/// <list type="bullet">
	/// <item>Full: Task Manager, Details, right-click x360ce.exe, Create memory dump file. All of the program's
	/// memory.</item>
	/// <item>Heap: what a Windows hang report keeps, or procdump -mp. The memory the program wrote, where .NET
	/// keeps its objects and its own record of each thread, so it is the smallest kind that still names the
	/// .NET method every thread is in.</item>
	/// <item>Mini: thread stacks and the module list. A .NET method on a stack cannot be named from it.</item>
	/// </list>
	/// A marker is put in the program's environment. Every Windows variable of the user's account reaches the
	/// program the same way, API keys other programs read among them, so a kind that carries the marker carries
	/// those too.
	///
	/// Measured on 4.27.19.0 Debug, 105 MB private once settled: Full 435 MB, zipped 141 MB; Heap 208 MB, zipped
	/// 60 MB; Mini 2 MB, zipped 1.1 MB. Only Mini fits, and only Full and Heap carry the marker, the Windows user
	/// name and the computer name. So a user's Task Manager dump goes by a share link, and never publicly.
	/// </remarks>
	[TestClass]
	public class DumpSizeTest
	{
		/// <summary>The largest file GitHub takes in an issue comment, other than an image or a video.</summary>
		const long GitHubFileLimit = 25L * 1024 * 1024;

		// MINIDUMP_TYPE flags, from dbghelp.h.
		const uint MiniDumpWithDataSegs = 0x1;
		const uint MiniDumpWithFullMemory = 0x2;
		const uint MiniDumpWithHandleData = 0x4;
		const uint MiniDumpWithUnloadedModules = 0x20;
		const uint MiniDumpWithPrivateReadWriteMemory = 0x200;
		const uint MiniDumpWithFullMemoryInfo = 0x800;
		const uint MiniDumpWithThreadInfo = 0x1000;

		/// <summary>As Task Manager and procdump -ma write it.</summary>
		const uint FullDump = MiniDumpWithFullMemory | MiniDumpWithHandleData | MiniDumpWithUnloadedModules
			| MiniDumpWithFullMemoryInfo | MiniDumpWithThreadInfo;

		/// <summary>As procdump -mp writes it.</summary>
		const uint HeapDump = MiniDumpWithDataSegs | MiniDumpWithPrivateReadWriteMemory | MiniDumpWithHandleData
			| MiniDumpWithUnloadedModules | MiniDumpWithFullMemoryInfo | MiniDumpWithThreadInfo;

		/// <summary>As procdump -mm writes it, with each thread's times and state added.</summary>
		const uint MiniDump = MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo;

		[DllImport("dbghelp.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeFileHandle hFile, uint dumpType,
			IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

		class DumpResult
		{
			public string Kind;
			public long DumpBytes;
			public long ZipBytes;
			public bool HasMarker;
			public bool HasUserName;
			public bool HasComputerName;
			public double Seconds;

			public bool Fits { get { return ZipBytes <= GitHubFileLimit; } }

			public override string ToString()
			{
				return string.Format("{0,-5} dump {1,7:N1} MB  zip {2,7:N1} MB  fits 25 MB: {3,-3}  marker: {4,-3}  user name: {5,-3}  computer name: {6,-3}  {7:N1} s",
					Kind, DumpBytes / 1048576.0, ZipBytes / 1048576.0, Fits ? "yes" : "no",
					HasMarker ? "yes" : "no", HasUserName ? "yes" : "no", HasComputerName ? "yes" : "no", Seconds);
			}
		}

		[TestMethod, TestCategory("support"), TestCategory("ui-interactive")]
		[Description("A dump of running x360ce, zipped, against the 25 MB GitHub takes in an issue, and what it carries")]
		public void Dump_of_running_program_zipped_against_GitHub_limit()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			if (Process.GetProcessesByName("x360ce").Length > 0)
				Assert.Inconclusive("Another x360ce instance is already running, so this test could not dump its own. "
					+ "Close it and run again.");
			var marker = "x360ce-dump-marker-" + Guid.NewGuid().ToString("N");
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.dump-size." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var info = new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
			};
			// Set for the program alone, as a variable of the user's account reaches it.
			info.EnvironmentVariables["X360CE_DUMP_MARKER"] = marker;
			Process process = null;
			try
			{
				process = Process.Start(info);
				Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));
				if (Is64Bit(process) != Environment.Is64BitProcess)
					Assert.Inconclusive("The test host and x360ce differ in bitness, and a dump is written only by a process "
						+ "of the same bitness. Run the tests as x64.");
				// Dumped once start-up has settled, as a program that froze while a game ran would be. A machine short
				// of memory may never hold it steady; the dump is then taken at the end of the wait all the same.
				var until = DateTime.UtcNow.AddSeconds(60);
				long privateBytes;
				while (true)
				{
					process.Refresh();
					var before = process.PrivateMemorySize64;
					Thread.Sleep(1500);
					process.Refresh();
					privateBytes = process.PrivateMemorySize64;
					if (Math.Abs(privateBytes - before) < 5L * 1024 * 1024 || DateTime.UtcNow > until)
						break;
				}
				Console.WriteLine("x360ce private memory when dumped: {0:N0} MB", privateBytes / 1048576);
				var full = Write(process, folder, "Full", FullDump, marker);
				var heap = Write(process, folder, "Heap", HeapDump, marker);
				var mini = Write(process, folder, "Mini", MiniDump, marker);
				Console.WriteLine(full);
				Console.WriteLine(heap);
				Console.WriteLine(mini);
				Assert.IsTrue(mini.Fits, "A Mini dump no longer fits in a GitHub issue: " + mini);
				Assert.IsFalse(mini.HasMarker, "A Mini dump now carries the program's environment: " + mini);
				Assert.IsTrue(full.HasMarker && heap.HasMarker,
					"Full and Heap dumps no longer carry the program's environment, so the advice to share them privately "
					+ "needs looking at again: " + full + "; " + heap);
			}
			finally
			{
				Ui.CloseApp(process);
				Directory.Delete(folder, true);
			}
		}

		static bool Is64Bit(Process process)
		{
			bool wow64;
			if (!IsWow64Process(process.Handle, out wow64))
				throw new System.ComponentModel.Win32Exception();
			return Environment.Is64BitOperatingSystem && !wow64;
		}

		static DumpResult Write(Process process, string folder, string kind, uint type, string marker)
		{
			var path = Path.Combine(folder, "x360ce." + kind + ".dmp");
			var watch = Stopwatch.StartNew();
			using (var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				if (!MiniDumpWriteDump(process.Handle, (uint)process.Id, file.SafeFileHandle, type,
					IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
					throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), kind + " dump was not written.");
			}
			watch.Stop();
			// Zipped as Windows' Send to, Compressed (zipped) folder does: one Deflate entry.
			var zip = path + ".zip";
			using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
				archive.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Optimal);
			return new DumpResult
			{
				Kind = kind,
				DumpBytes = new FileInfo(path).Length,
				ZipBytes = new FileInfo(zip).Length,
				HasMarker = ContainsText(path, marker),
				HasUserName = ContainsText(path, Environment.UserName),
				HasComputerName = ContainsText(path, Environment.MachineName),
				Seconds = watch.Elapsed.TotalSeconds,
			};
		}

		/// <summary>Whether the file holds the text, as Windows keeps it (UTF-16) or as 8-bit characters.</summary>
		static bool ContainsText(string path, string text)
		{
			var patterns = new[] { Encoding.Unicode.GetBytes(text), Encoding.ASCII.GetBytes(text) };
			var overlap = patterns.Max(x => x.Length) - 1;
			const int chunk = 4 * 1024 * 1024;
			var buffer = new byte[chunk + overlap];
			var carried = 0;
			using (var stream = File.OpenRead(path))
			{
				int read;
				while ((read = stream.Read(buffer, carried, chunk)) > 0)
				{
					var length = carried + read;
					if (patterns.Any(x => IndexOf(buffer, length, x) >= 0))
						return true;
					// The end of this chunk is kept, so a text that straddles two chunks is still found.
					carried = Math.Min(overlap, length);
					Buffer.BlockCopy(buffer, length - carried, buffer, 0, carried);
				}
			}
			return false;
		}

		static int IndexOf(byte[] buffer, int length, byte[] pattern)
		{
			var last = length - pattern.Length;
			var i = 0;
			while (i <= last)
			{
				i = Array.IndexOf(buffer, pattern[0], i, last - i + 1);
				if (i < 0)
					return -1;
				var j = 1;
				while (j < pattern.Length && buffer[i + j] == pattern[j])
					j++;
				if (j == pattern.Length)
					return i;
				i++;
			}
			return -1;
		}
	}
}
