// @under-test: App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/Common/DInput/DInputHelper.XInputLibrarry.cs, App.v4/Common/DInput/DInputHelper.cs, Engine/SharpDX.XInput/Controller.x360ce.All.cs, App.v4/MainForm.cs, App.v4/Controls/PadControl.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The XInput view's reads, sixty a second, make nothing and are never waited for.</summary>
	/// <remarks>
	/// Windows' own XInput library is loaded for these tests. They read the four places and send nothing.
	/// </remarks>
	[TestClass]
	public class XInputDisplayReadTest
	{
		internal static void WithSystemXInput(Action action)
		{
			lock (Controller.XInputLock)
			{
				Exception error;
				Controller.ReLoadLibrary(EngineHelper.GetMsXInputLocation().FullName, out error);
				Assert.IsTrue(Controller.IsLoaded, "Windows' own XInput library did not load: " + error);
			}
			try
			{
				action();
			}
			finally
			{
				lock (Controller.XInputLock)
					Controller.FreeLibrary();
			}
		}

		/// <summary>The thread the display reads run on, or null before the first read.</summary>
		static Thread DisplayReader(DInputHelper helper)
		{
			var field = typeof(DInputHelper).GetField("_displayReader", BindingFlags.NonPublic | BindingFlags.Instance);
			return field == null ? null : (Thread)field.GetValue(helper);
		}

		/// <summary>Starts the helper's clock, as starting the engine does, so a display read falls due every 16 ms.</summary>
		internal static void StartClock(DInputHelper helper)
		{
			((Stopwatch)typeof(DInputHelper).GetField("watch", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper)).Start();
		}

		/// <summary>The display read's state, read without boxing, so a loop that watches it makes nothing.</summary>
		internal static readonly Func<DInputHelper, int> DisplayRead = ReadField<int>("_displayRead");

		/// <summary>The display read's states: nothing asked, asked of the reader, answered and not yet taken.</summary>
		internal static readonly int Idle = Constant("DisplayReadIdle");
		internal static readonly int Asked = Constant("DisplayReadAsked");
		internal static readonly int Answered = Constant("DisplayReadAnswered");

		static int Constant(string name)
		{
			return (int)typeof(DInputHelper).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
		}

		static Func<DInputHelper, T> ReadField<T>(string name)
		{
			var helper = Expression.Parameter(typeof(DInputHelper), "helper");
			var field = typeof(DInputHelper).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
			return Expression.Lambda<Func<DInputHelper, T>>(Expression.Field(helper, field), helper).Compile();
		}

		/// <summary>Waits until the reader has answered, and fails when it has not within <paramref name="milliseconds"/>.</summary>
		/// <remarks>Makes nothing while it waits, unless it fails.</remarks>
		internal static void WaitForAnswer(DInputHelper helper, int milliseconds = 2000)
		{
			var start = Environment.TickCount;
			while (DisplayRead(helper) != Answered)
			{
				if (unchecked(Environment.TickCount - start) > milliseconds)
					Assert.Fail("The display reader did not answer within " + milliseconds + " ms.");
				Thread.SpinWait(20);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("Reading a place through the loaded library makes nothing")]
		public void A_place_read_makes_nothing()
		{
			WithSystemXInput(() =>
			{
				State state;
				const int reads = 1000;
				var allocated = Allocations.FewestBytes(10, () =>
				{
					for (var i = 0; i < reads; i++)
						Controller.XInputGetState(i & 3, out state);
				});
				Assert.IsTrue(allocated < reads, reads + " place reads handed the collector " + allocated + " bytes; the XInput view reads four places sixty times a second.");
			});
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "Engine", "SharpDX.XInput", "Controller.x360ce.All.cs"));
			var free = source.Substring(source.IndexOf("public static void FreeLibrary()"));
			Assert.IsTrue(free.IndexOf("_getState = null;") >= 0 && free.IndexOf("_getState = null;") < free.IndexOf("NativeMethods.FreeLibrary("),
				"The state function is kept after its library is let go of.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("The display read hands nothing to the collector and never waits for a read")]
		public void The_display_read_makes_nothing()
		{
			var helper = new DInputHelper();
			StartClock(helper);
			var retrieve = EngineSteps.RetrieveXiStates(helper);
			var game = new UserGame { FileName = "display-read.exe", FileProductName = "Display read" };
			Thread reader = null;
			WithSystemXInput(() =>
			{
				// The reader is made on the first read.
				var warm = Stopwatch.StartNew();
				while (warm.ElapsedMilliseconds < 2000)
				{
					retrieve(game, true);
					Thread.SpinWait(2000);
				}
				reader = DisplayReader(helper);
				// Timed by the tick count, which makes nothing, so a window counts only the reads.
				const int windowMs = 1000;
				var allocated = Allocations.FewestBytes(5, () =>
				{
					var start = Environment.TickCount;
					while (unchecked(Environment.TickCount - start) < windowMs)
					{
						retrieve(game, true);
						Thread.SpinWait(2000);
					}
				});
				const int reads = windowMs / 16;
				Console.WriteLine("About " + reads + " reads, " + allocated + " bytes.");
				Assert.IsTrue(helper.XiStatesRead, "The display read was not asked for.");
				Assert.IsTrue(allocated < reads * 16,
					"About " + reads + " display reads handed the collector " + allocated + " bytes; each read must hand it less than 16.");
			});
			// One background thread for the life of the engine, ended with it.
			Assert.IsNotNull(reader, "No reader thread was made for the display read.");
			Assert.AreSame(reader, DisplayReader(helper), "A reader thread was made for more than one read.");
			Assert.IsTrue(reader.IsBackground, "The display reader keeps the program from ending.");
			helper.Dispose();
			Assert.IsTrue(reader.Join(1000), "The display reader outlives the engine.");
			var step6 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step6.RetrieveXiStates.cs"));
			var method = step6.Substring(step6.IndexOf("void RetrieveXiStates("));
			method = method.Substring(0, method.IndexOf("\n\t\t}"));
			Assert.IsFalse(method.Contains("RanWithin("), "The input thread waits for the display read.");
			Assert.IsFalse(method.Contains("XInputLock"), "The input thread takes the XInput lock for the display read.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("Reading the four places, and asking for a read and taking its answer, make nothing, one read at a time")]
		public void Each_display_read_makes_nothing()
		{
			var helper = new DInputHelper();
			var read = EngineSteps.ReadDisplayStates(helper);
			var ask = EngineSteps.AskDisplayRead(helper);
			// With the clock not started no read falls due, so this only takes the answer.
			var take = EngineSteps.RetrieveXiStates(helper);
			var game = new UserGame { FileName = "each-display-read.exe", FileProductName = "Each display read" };
			const int cycles = 1000;
			try
			{
				WithSystemXInput(() =>
				{
					// The reader is made on the first ask.
					var warm = Stopwatch.StartNew();
					while (warm.ElapsedMilliseconds < 2000)
					{
						read();
						AskAndTake(helper, ask, take, game);
					}
					var reading = Allocations.FewestBytes(10, () =>
					{
						for (var i = 0; i < cycles; i++)
							read();
					});
					var asking = Allocations.FewestBytes(10, () =>
					{
						for (var i = 0; i < cycles; i++)
							AskAndTake(helper, ask, take, game);
					});
					Console.WriteLine(cycles + " reads: " + reading + " bytes; " + cycles + " asks and takes: " + asking + " bytes.");
					Assert.IsTrue(reading < cycles, cycles + " reads of the four places handed the collector " + reading + " bytes.");
					Assert.IsTrue(asking < cycles, cycles + " asks and takes handed the collector " + asking + " bytes.");
				});
			}
			finally
			{
				helper.Dispose();
			}
		}

		/// <summary>Asks the reader for one read, waits for its answer, and takes it as a pass does.</summary>
		static void AskAndTake(DInputHelper helper, Action ask, EngineSteps.XiRetrieve take, UserGame game)
		{
			ask();
			WaitForAnswer(helper);
			take(game, true);
			if (DisplayRead(helper) != Idle)
				Assert.Fail("The answer was not taken.");
		}
	}
}
