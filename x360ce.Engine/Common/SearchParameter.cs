using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace x360ce.Engine
{
	public class SearchParameter
	{
		public Guid ProductGuid { get; set; }
		public Guid InstanceGuid { get; set; }
		public string FileName { get; set; }
		public string FileProductName { get; set; }
	}
}