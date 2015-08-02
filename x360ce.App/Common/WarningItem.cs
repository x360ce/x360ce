using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App
{
	public class WarningItem
	{
		public WarningItem(string name)
		{
			Name = name;
			FixName = "";
		}

		public string Name { get; }
		public string Description { get; set; }
		public string FixName { get; set; }
		public Action FixAction { get; set; }
	}
}
