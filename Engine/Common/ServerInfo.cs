using System;

namespace x360ce.Engine
{
	/// <summary>What the web service says about itself when asked whether it is there and working.</summary>
	/// <remarks>
	/// One small answer that proves the whole path: the address reaches an x360ce service, the
	/// service runs, and its database answers. The Options page's Test button asks for it.
	/// </remarks>
	public class ServerInfo
	{
		/// <summary>Version of the service assembly.</summary>
		public string Version { get; set; }
		/// <summary>The service's clock, in UTC.</summary>
		public DateTime UtcTime { get; set; }
		/// <summary>Name of the database the service uses, or empty when it did not answer.</summary>
		public string Database { get; set; }
		/// <summary>The database's clock, in UTC; the default when it did not answer.</summary>
		public DateTime DatabaseUtcTime { get; set; }
		/// <summary>Why the database did not answer, or empty.</summary>
		public string Error { get; set; }
	}
}
