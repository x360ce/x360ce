using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.Engine;

namespace x360ce.App
{
	public class Options
	{
		public Options()
		{
			AllowOnlyOneCopy = true;
			InternetFeatures = true;
			InternetAutoLoad = true;
			InternetAutoSave = true;
		}
		/// <summary>
		/// Avoid deserialization duplicates by using separate method.
		/// </summary>
		public void InitDefaults()
		{
			if (string.IsNullOrEmpty(InternetDatabaseUrl))
				InternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";
			if (InternetDatabaseUrls == null)
				InternetDatabaseUrls = new List<string>();
			if (InternetDatabaseUrls.Count == 0)
			{
				InternetDatabaseUrls.Add("http://www.x360ce.com/webservices/x360ce.asmx");
				InternetDatabaseUrls.Add("http://localhost:20360/webservices/x360ce.asmx");
			}
			if (GameScanLocations == null)
				GameScanLocations = new List<string>() { };
			if (string.IsNullOrEmpty(DiskId))
			{
				DiskId = Engine.BoardInfo.GetDiskId();
			}
			if (ComputerId == Guid.Empty)
			{
				ComputerId = Engine.BoardInfo.GetHashedDiskId();
			}
		}

		public bool CheckAndFixUserRsaKeys()
		{
			// If user RSA keys are missing then...
			if (string.IsNullOrEmpty(UserRsaPublicKey))
			{
				// Create new RSA keys which will be used to send encrypted credentials.
				var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User);
				var keys = rsa.RsaNewKeys(2048);
				UserRsaPublicKey = keys.Public;
				UserRsaPrivateKey = keys.Private;
				return true;
			}
			return false;
		}

		public bool AllowOnlyOneCopy { get; set; }

		public bool ShowProgramsTab { get; set; }
		public bool ShowSettingsTab { get; set; }
		public bool ShowDevicesTab { get; set; }
		public bool ShowIniTab { get; set; }

		public bool InternetFeatures { get; set; }
		public bool InternetAutoLoad { get; set; }
		public bool InternetAutoSave { get; set; }
		public string InternetDatabaseUrl { get; set; }
		public List<string> InternetDatabaseUrls { get; set; }
		public List<string> GameScanLocations { get; set; }
		public string DiskId { get; set; }
		public Guid ComputerId { get; set; }

		public string LoginEnabled { get; set; }

		public bool IncludeProductsInsideINI { get; set; }

		public string UserRsaPublicKey { get; set; }
		public string UserRsaPrivateKey { get; set; }

		public string CloudRsaPublicKey { get; set; }

		public string Username { get; set; }
		public string Password { get; set; }
		public bool MinimizeToTray { get; set; }
		public bool ExcludeSupplementalDevices { get; set; }
		public bool ExcludeVirtualDevices { get; set; }
        public bool CheckForUpdates { get; set; }

    }
}
