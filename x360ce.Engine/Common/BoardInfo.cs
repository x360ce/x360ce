namespace x360ce.Engine
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Management;

	public class BoardInfo
	{
		/// <summary>
		/// Get manufacturer-allocated number used to identify the physical element.
		/// http://msdn.microsoft.com/en-us/library/aa394132%28VS.85%29.aspx
		/// </summary>
		static string GetDiskDriveID()
		{
			return GetClassValue<string>("Win32_DiskDrive", "SerialNumber");
		}

		static Guid? _DiskDriveIdGuid;

		/// <summary>
		/// This anonymous guid will be used as link betwen disk and game settings on online database.
		/// </summary>
		public static Guid GetDiskDriveIdGuid()
		{
			if (_DiskDriveIdGuid.HasValue) return _DiskDriveIdGuid.Value;
			var serialBytes = System.Text.Encoding.ASCII.GetBytes(GetDiskDriveID());
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] retVal = md5.ComputeHash(serialBytes);
			_DiskDriveIdGuid = new Guid(retVal);
			return _DiskDriveIdGuid.Value;
		}

		static object searcherLock = new object();

		static T GetClassValue<T>(string className, string paramName)
		{
			T value = default(T);
			try
			{
				lock (searcherLock)
				{
					var query = string.Format("SELECT {0} FROM {1}", paramName, className);
					ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
					ManagementObjectCollection moc = searcher.Get();
					foreach (ManagementObject mo in moc)
					{
						value = (T)mo[paramName];
					}
					moc.Dispose();
					searcher.Dispose();
				}
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("BoardInfo {0} {1} read failed: {2}", className, paramName, ex.Message));
			}
			return value;
		}
	
	
	}
}
