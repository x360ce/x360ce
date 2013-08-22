using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace x360ce.Engine.Data
{
	public partial class Program
	{

		public static Program FromDisk(string fileName)
		{
			var program = new Program();
			var fi = new FileInfo(fileName);
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			program.FileName = fi.Name;
			// vi.FileDescription
			// vi.OriginalFilename
			// Assembly File Version (mandatory).
			//program.FileVersion = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
			program.FileProductName = vi.ProductName;
			// Assembly Product Version (optional).
			//program.ProductVersion = new Version(vi.ProductMajorPart, vi.ProductMinorPart, vi.ProductBuildPart, vi.ProductPrivatePart);
			//vi.LegalCopyright = 
			//program.Size = info.Length;
			//program.CompanyName = vi.CompanyName;
			//vi.Language = vi.Language;
			return program;
		}

	}
}