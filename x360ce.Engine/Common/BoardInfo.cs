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
            string serialNumber = null;
            try
            {
                lock (searcherLock)
                {
                    var query = "SELECT SerialNumber, Index, MediaType FROM Win32_DiskDrive";
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                    ManagementObjectCollection moc = searcher.Get();
                    var items = moc.Cast<ManagementObject>().Select(x => new
                    {
                        Index = (uint)x["Index"],
                        MediaType = ((string)x["MediaType"] ?? "").Trim().ToLower(),
                        SerialNumber = (string)x["SerialNumber"],

                    });
                    // Get number of first hard drive.
                    var item = items.FirstOrDefault(x => !string.IsNullOrEmpty(x.SerialNumber) && x.MediaType.Contains("fixed"));
                    // if not found then get any non empty.
                    if (item == null) item = items.FirstOrDefault(x => !string.IsNullOrEmpty(x.SerialNumber));
                    moc.Dispose();
                    searcher.Dispose();
                    if (item != null) serialNumber = item.SerialNumber;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("BoardInfo Win32_DiskDrive SerialNumber read failed: {0}", ex.Message));
            }
            return serialNumber;
        }

        static Guid? _DiskDriveIdGuid;

        /// <summary>
        /// This anonymous guid will be used as link betwen disk and game settings on online database.
        /// </summary>
        public static Guid GetDiskDriveIdGuid()
        {
            if (_DiskDriveIdGuid.HasValue) return _DiskDriveIdGuid.Value;
            var diskId = GetDiskDriveID();
            var serialBytes = System.Text.Encoding.ASCII.GetBytes(diskId);
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(serialBytes);
            _DiskDriveIdGuid = new Guid(retVal);
            return _DiskDriveIdGuid.Value;
        }

        static object searcherLock = new object();


    }
}
