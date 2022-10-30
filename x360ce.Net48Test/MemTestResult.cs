using System;
using System.Diagnostics;

namespace x360ce.Net48Test
{
	public class MemTestResult
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
