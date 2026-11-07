using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App
{
	public enum CloudState
	{
		None = 0,
		Busy = 1,
		Done = 2,
		/// <summary>The last try failed; the item is sent again when its wait is over.</summary>
		Error = 3,
		/// <summary>The item failed for good, or on every try it had; it is sent again only when the person retries it.</summary>
		Stopped = 4,
	}
}
