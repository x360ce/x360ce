using System;

namespace x360ce.Engine
{
	public class SearchParameter
	{
		public Guid ProductGuid { get; set; }
		public Guid InstanceGuid { get; set; }
		public string FileName { get; set; }
		public string FileProductName { get; set; }

		public bool IsEmpty()
		{
			return
				string.IsNullOrEmpty(FileName) &&
				string.IsNullOrEmpty(FileProductName) &&
				InstanceGuid == Guid.Empty &&
				ProductGuid == Guid.Empty;
		}
	}
}
