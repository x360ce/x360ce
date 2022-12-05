using System;
using System.Diagnostics;

namespace x360ce.Tests
{
	public class MemoryTestResult
	{
		public Type Type { get; set; }
		public long? MemObjectSize { get; set; }
		public long? MemDifference { get; set; }
		public Exception Exception { get; set; }
		public string Message { get; set; } = "";
		public bool IsAlive { get; set; }
		public TraceLevel Level { get; set; } = TraceLevel.Info;
		public long Duration { get; set; }

	}
}
