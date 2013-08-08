using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using x360ce.Web.Data;

namespace x360ce.Web
{
	public class SearchResult
	{
		public Setting[] Settings { get; set; }
		public Summary[] Summaries { get; set; }
		public PadSetting[] PadSettings { get; set; }
	}
}