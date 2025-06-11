using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace x360ce.Engine
{
	public partial class EngineHelper
	{

		/// <summary>
		/// Get first 8 numbers of GUID.
		/// </summary>
		/// <remarks>Instance GUID or Setting GUID (MD5 checksum) is always random.</remarks>
		public static string GetID(Guid guid)
		{
			return guid.ToString("N").Substring(0, 8).ToUpper();
		}

		static Guid UpdateChecksum(IChecksum item, System.Security.Cryptography.MD5CryptoServiceProvider md5)
		{
			string s = JocysCom.ClassLibrary.Runtime.RuntimeHelper.GetDataMembersString(item);
			var bytes = Encoding.Unicode.GetBytes(s);
			var cs = new Guid(md5.ComputeHash(bytes));
			if (item.Checksum != cs)
				item.Checksum = cs;
			return cs;
		}

		/// <summary>
		/// Update checksums of objects and return total checksum.
		/// </summary>
		/// <remarks>Last GUID will be summary checksum.</remarks>
		public static List<Guid> UpdateChecksums<T>(T[] list) where T : IChecksum
		{
			var result = new List<Guid>();
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			for (int i = 0; i < list.Length; i++)
			{
				var checksum = UpdateChecksum(list[i], md5);
				result.Add(checksum);
			}
			if (list.Length > 0)
			{   // Order to make sure that it won't influence total checksum.
				result = result.OrderBy(x => x).ToList();
				int size = 16;
				var total = new byte[list.Length * size];
				for (int i = 0; i < list.Length; i++)
				{
					Array.Copy(list[i].Checksum.ToByteArray(), 0, total, i * size, size);
				}
				result.Add(new Guid(md5.ComputeHash(total)));
			}
			return result;
		}

	}
}
