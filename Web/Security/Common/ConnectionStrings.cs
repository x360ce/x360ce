using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace JocysCom.Web.Security
{
	/// <summary>
	/// Summary description for ConnectionStrings
	/// </summary>
	public static class ConnectionStrings
	{

		public static void Encrypt()
		{
			//EncryptConnectionStrings("DataProtectionConfigurationProvider");
			Encrypt("RSAProtectedConfigurationProvider");
		}

		public static void Decrypt()
		{
			//EncryptConnectionStrings("DataProtectionConfigurationProvider");
			Decrypt("RSAProtectedConfigurationProvider");
		}

		public static void Encrypt(string protectionProvider)
		{
			// Open the web.config file.
			Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.HttpContext.Current.Request.ApplicationPath);
			// Indicate the section to protect.
			ConfigurationSection section = config.GetSection("connectionStrings");
			// If sections is not protected.
			if (!section.SectionInformation.IsProtected)
			{
				section.SectionInformation.ProtectSection(protectionProvider);
			}
			// Apply protection and update.
			config.Save();
		}

		public static void Decrypt(string protectionProvider)
		{
			// Open the web.config file.
			Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.HttpContext.Current.Request.ApplicationPath);
			// Indicate the section to unprotect.
			ConfigurationSection section = config.GetSection("connectionStrings");
			// If sections is protected.
			if (section.SectionInformation.IsProtected)
			{
				section.SectionInformation.UnprotectSection();
			}
			// Remove protection and update.
			config.Save();
		}


	}
}