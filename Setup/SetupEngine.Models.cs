using System.IO;

namespace x360ce.Setup
{
	public class DetectedGameInfo
	{
		public string FilePath { get; set; }
		public string FileName => Path.GetFileName(FilePath);
		public string DirectoryPath => Path.GetDirectoryName(FilePath);
		public bool Is64Bit { get; set; }
	}

	public class DetectedControllerInfo
	{
		public string Name { get; set; }
		public string InstanceGuid { get; set; }
		public string ProductGuid { get; set; }
		public int VendorId { get; set; }
		public int ProductId { get; set; }
		public int PlayerIndex { get; set; }
		public bool IsOnline { get; set; }
	}
}
