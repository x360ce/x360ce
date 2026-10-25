using JocysCom.ClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A real AutoHotkey v2 script and an engine channel of its own, driven at a thousand passes a second.</summary>
	/// <remarks>
	/// The script includes a copy of x360ce.ahk with only its <c>Name</c> changed, so it never meets a running x360ce; the
	/// layout test in <see cref="ScriptChannelTest"/> keeps the rest of the library equal to the engine's. Two devices are
	/// routed: <see cref="Device"/>, "Test pad", device 1 on Controller 1 (c1.d1), and <see cref="Device2"/>, "Second pad",
	/// device 1 on Controller 2 (c2.d1) and device 2 on Controller 1 (c1.d2),
	/// each with its sticks centred and its hats at rest. Makes a test inconclusive where AutoHotkey v2 is not installed.
	/// </remarks>
	public sealed class AutoHotkeyRun : IDisposable
	{
		public readonly ScriptChannel Channel;
		public readonly SourceState Device = Resting();
		public readonly SourceState Device2 = Resting();
		/// <summary>What each device's controller read on the last pass.</summary>
		public SourceState Mapped;
		public SourceState Mapped2;
		/// <summary>The folder the script and its library are written to, deleted with the run.</summary>
		public readonly string Folder;
		public readonly int Slot;
		public readonly int Slot2;
		public Process Process { get; private set; }

		readonly string _ahk;
		readonly string _script;
		readonly MemoryMappedFile _map;
		readonly MemoryMappedViewAccessor _view;
		readonly ManualResetEvent _wake = new ManualResetEvent(false);
		readonly HiResPacer _pacer;

		/// <param name="body">The script after its <c>#Include</c> of the library.</param>
		public AutoHotkeyRun(string body)
		{
			_ahk = FindAutoHotkey();
			if (_ahk == null)
				Assert.Inconclusive("AutoHotkey v2 is not installed.");
			var name = @"Local\X360CE.Script.Test." + Guid.NewGuid().ToString("N");
			Folder = Path.Combine(Path.GetTempPath(), "x360ce-autohotkey-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Folder);
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "AutoHotkey", "x360ce.ahk"));
			var library = source.Replace("static Name := \"Local\\X360CE.Script\"", "static Name := \"" + name + "\"");
			Assert.AreNotEqual(source, library, "The library's Name was not found to point it at the test's memory.");
			File.WriteAllText(Path.Combine(Folder, "x360ce.ahk"), library, new UTF8Encoding(false));
			_script = Path.Combine(Folder, "Test.ahk");
			File.WriteAllText(_script, "#Requires AutoHotkey v2.0\r\n#NoTrayIcon\r\n#Include %A_ScriptDir%\\x360ce.ahk\r\n" + body, new UTF8Encoding(false));
			Channel = new ScriptChannel(name);
			_map = MemoryMappedFile.OpenExisting(name);
			_view = _map.CreateViewAccessor(0, ScriptChannel.Size);
			_pacer = new HiResPacer(_wake);
			var slots = new int[2];
			Channel.Route(new[]
			{
				new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Test pad" },
				new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Second pad" },
			}, new[] { 0x01, 0x0102 }, slots);
			Slot = slots[0];
			Slot2 = slots[1];
			Mapped = Device;
			Mapped2 = Device2;
		}

		/// <summary>AutoHotkey v2 for this process's bitness, or null.</summary>
		public static string FindAutoHotkey()
		{
			string folder = null;
			using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
			using (var key = hive.OpenSubKey(@"SOFTWARE\AutoHotkey"))
				folder = key == null ? null : key.GetValue("InstallDir") as string;
			if (string.IsNullOrEmpty(folder))
				folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoHotkey");
			var path = Path.Combine(folder, "v2", Environment.Is64BitProcess ? "AutoHotkey64.exe" : "AutoHotkey32.exe");
			return File.Exists(path) ? path : null;
		}

		static SourceState Resting()
		{
			var state = new SourceState();
			for (var i = 0; i < state.Axis.Length; i++)
				state.Axis[i] = 32767;
			for (var i = 0; i < state.Povs.Length; i++)
				state.Povs[i] = -1;
			return state;
		}

		/// <summary>Starts the script, with its arguments, and passes until it connects and <paramref name="registered"/> is true.</summary>
		public void Start(Func<bool> registered, params string[] arguments)
		{
			var line = "\"" + _script + "\"";
			foreach (var argument in arguments)
				line += " \"" + argument + "\"";
			Process = Process.Start(new ProcessStartInfo(_ahk, line) { UseShellExecute = false });
			Assert.IsTrue(PassUntil(() => Channel.Connected && registered(), 10000),
				"The script did not connect and register its hotkeys within 10 seconds.");
		}

		/// <summary>One engine pass, then a wait until the next is due.</summary>
		public void Pass()
		{
			var now = Stopwatch.GetTimestamp();
			Channel.BeginPass(now);
			Mapped = Channel.Apply(Slot, Device) ?? Device;
			Mapped2 = Channel.Apply(Slot2, Device2) ?? Device2;
			Channel.EndPass(now);
			_pacer.Wait(1);
		}

		/// <summary>Passes until <paramref name="done"/> is true, at most <paramref name="limit"/> times.</summary>
		public bool PassUntil(Func<bool> done, int limit)
		{
			for (var i = 0; i < limit; i++)
			{
				Pass();
				if (done())
					return true;
			}
			return false;
		}

		/// <summary>Passes <paramref name="count"/> times.</summary>
		public void Passes(int count)
		{
			for (var i = 0; i < count; i++)
				Pass();
		}

		/// <summary>The channel's counters, for a failure message: who wrote and read how much, and whether a message waits.</summary>
		public string Counters()
		{
			return string.Format("connected {0}, events written {1} read {2}, pending {3}, events dropped {4}, commands written {5} read {6}, heartbeat {7}, script running {8}",
				Channel.Connected,
				_view.ReadInt32(ScriptChannel.OffEventsWritten), _view.ReadInt32(ScriptChannel.OffEventsRead),
				_view.ReadInt32(ScriptChannel.OffPending), _view.ReadInt32(ScriptChannel.OffEventsDropped),
				_view.ReadInt32(ScriptChannel.OffCommandsWritten), _view.ReadInt32(ScriptChannel.OffCommandsRead),
				_view.ReadInt32(ScriptChannel.OffHeartbeat), Process != null && !Process.HasExited);
		}

		/// <summary>A value of a slot in the shared memory, as the script wrote it.</summary>
		public int SlotInt(int slot, int offset)
		{
			return _view.ReadInt32(ScriptChannel.OffSlots + slot * ScriptChannel.SlotSize + offset);
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool IsWindow(IntPtr hWnd);

		public void Dispose()
		{
			// The script that last said it is connected, which is not always the process started here: AutoHotkey can
			// restart a script as a new process. Asked to close first, so no test script outlives its test.
			var window = (IntPtr)_view.ReadInt64(ScriptChannel.OffScriptWindow);
			if (window != IntPtr.Zero && IsWindow(window))
			{
				PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
				for (var i = 0; i < 40 && IsWindow(window); i++)
					Thread.Sleep(50);
			}
			if (Process != null)
			{
				if (!Process.HasExited)
					Process.Kill();
				Process.WaitForExit(5000);
				Process.Dispose();
			}
			_pacer.Dispose();
			_wake.Dispose();
			_view.Dispose();
			_map.Dispose();
			Channel.Dispose();
			// A process just closed, or a virus scanner looking at its new files, can hold the folder a moment longer.
			for (var attempt = 0; attempt < 20; attempt++)
			{
				try
				{
					Directory.Delete(Folder, true);
					return;
				}
				catch (IOException)
				{
					Thread.Sleep(50);
				}
				catch (UnauthorizedAccessException)
				{
					Thread.Sleep(50);
				}
			}
		}
	}
}
