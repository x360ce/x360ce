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
			var item = new Program();
			var fi = new FileInfo(fileName);
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			item.Comment = vi.Comments;
			item.DateCreated = DateTime.Now;
			item.DateUpdated = item.DateCreated;
			item.FileName = fi.Name;
			item.FileProductName = EngineHelper.FixName(vi.ProductName, item.FileName);
			item.InstanceCount = 0;
			item.IsEnabled = true;
			item.ProgramId = Guid.NewGuid();
			item.XInputMask = 0;
			item.HookMask = 0;
			item.XInputMask = 0;
			item.DInputMask = 0;
			item.DInputFile = "";
			item.FakeVID = 0;
			item.FakePID = 0;
			item.Timeout = -1;
			item.Weight = 1;
			return item;
		}

	}
}
