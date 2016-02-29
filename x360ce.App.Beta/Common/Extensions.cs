using System;
using System.Collections.Generic;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public static class Extensions
	{

		public static string ToString(this Summary s)
		{
			var result = s.ProductName;
			if (!string.IsNullOrEmpty(s.FileName)) result += " | " + s.FileName;
			if (!string.IsNullOrEmpty(s.FileProductName)) result += " | " + s.FileProductName;
			return result;
		}


		public static string ToString(this Setting s)
		{
			var result = s.ProductName;
			if (!string.IsNullOrEmpty(s.FileName)) result += " | " + s.FileName;
			if (!string.IsNullOrEmpty(s.FileProductName)) result += " | " + s.FileProductName;
			return result;
		}

	}

}
