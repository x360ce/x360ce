// @under-test: Engine/JocysCom/Common/HiResTimer.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// What the device timer does on a Windows without the multimedia timer library.
	/// </summary>
	/// <remarks>
	/// Such a Windows exists: a stripped installation reported "Unable to load DLL 'Winmm.dll'"
	/// from the very first attempt to start polling, and the program stopped there. A coarser
	/// timer is worth more than none, so the ordinary one is used instead and the rest of the
	/// program never knows.
	/// </remarks>
	[TestClass]
	public class HiResTimerFallbackTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The timer keeps ticking without the multimedia timer library")]
		public void Ticks_without_the_multimedia_timer()
		{
			var available = HiResTimer.MultimediaTimerAvailable;
			HiResTimer.MultimediaTimerAvailable = false;
			try
			{
				var ticks = 0;
				using (var timer = new HiResTimer(10, "fallback"))
				{
					timer.Elapsed += (s, e) => Interlocked.Increment(ref ticks);
					timer.Start();
					Thread.Sleep(500);
					timer.Stop();
				}
				var counted = ticks;
				Thread.Sleep(100);
				Assert.IsTrue(counted >= 5,
					"The timer ticked " + counted + " times in half a second without the multimedia " +
					"library, so a Windows without it would poll nothing.");
				Assert.AreEqual(counted, ticks, "The timer went on ticking after it was stopped.");
			}
			finally
			{
				HiResTimer.MultimediaTimerAvailable = available;
			}
		}
	}
}
