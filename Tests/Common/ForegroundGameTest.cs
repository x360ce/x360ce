// @under-test: App.v4/Global.cs, Engine/Common/ForegroundWindowHook.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>Choosing the listed game that runs, on each change of the window in front, opens only what may be one.</summary>
	/// <remarks>
	/// A change of the window in front used to open every running process, about three hundred, to read its file
	/// name. A process is now opened only when its name is a listed game's file name, and the full path it reads
	/// decides, so the same games are found running.
	/// </remarks>
	[TestClass]
	public class ForegroundGameTest
	{
		static UserGame Game(string path)
		{
			return new UserGame { FullPath = path, FileName = Path.GetFileName(path) };
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("Only processes named as a listed game are opened, and the same games are found running as by opening them all")]
		public void Only_processes_named_as_a_listed_game_are_opened()
		{
			var processes = Process.GetProcesses();
			try
			{
				var self = Process.GetCurrentProcess();
				var selfPath = ForegroundWindowHook.GetProcessFileName(self);
				if (string.IsNullOrEmpty(selfPath))
					Assert.Inconclusive("This process cannot read its own file name.");
				var games = new List<UserGame>
				{
					Game(selfPath),
					// Named as programs that run, in a folder they are not in: opened, and not found running.
					Game(@"C:\Games\svchost.exe"),
					Game(@"C:\Games\explorer.exe"),
				};
				for (var i = 0; i < 20; i++)
					games.Add(Game(@"C:\Games\Not running " + i + @"\game" + i + ".exe"));
				var names = new HashSet<string>(games.Select(x => Path.GetFileNameWithoutExtension(x.FullPath)), StringComparer.OrdinalIgnoreCase);
				var opened = new List<int>();
				Func<Process, string> open = p => { opened.Add(p.Id); return ForegroundWindowHook.GetProcessFileName(p); };
				var watch = Stopwatch.StartNew();
				var paths = Global.RunningGamePaths(games, processes, open);
				var listed = watch.Elapsed.TotalMilliseconds;
				var named = processes.Where(x => names.Contains(x.ProcessName)).Select(x => x.Id).ToArray();
				Console.WriteLine("{0} processes, {1} named as a listed game, {2} opened in {3:N1} ms.", processes.Length, named.Length, opened.Count, listed);
				CollectionAssert.AreEquivalent(named, opened, "Processes not named as a listed game were opened: " + opened.Count + " of " + processes.Length + ".");
				// Opening every process, as a change of the window in front used to.
				watch.Restart();
				var all = processes.Select(ForegroundWindowHook.GetProcessFileName).Distinct().ToArray();
				Console.WriteLine("Opening all {0} processes took {1:N1} ms.", processes.Length, watch.Elapsed.TotalMilliseconds);
				Func<string[], string[]> running = found => games.Where(g => found.Any(p => string.Equals(g.FullPath, p, StringComparison.OrdinalIgnoreCase)))
					.Select(g => g.FullPath).ToArray();
				CollectionAssert.AreEqual(running(all), running(paths), "Another set of games was found running.");
				CollectionAssert.AreEqual(new[] { selfPath }, running(paths), "Only this process runs of the listed games.");
				opened.Clear();
				Assert.AreEqual(0, Global.RunningGamePaths(new UserGame[0], processes, open).Length);
				Assert.AreEqual(0, opened.Count, "With no game listed besides x360ce, processes were opened.");
			}
			finally
			{
				foreach (var p in processes)
					p.Dispose();
			}
		}
	}
}
