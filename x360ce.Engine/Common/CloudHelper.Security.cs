using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;

namespace x360ce.Engine
{
	public partial class CloudHelper
	{

		public static JocysCom.WebSites.Engine.Security.Data.User GetUser(CloudMessage message)
		{
			var values = message.Values;
			if (values == null) return null;
			var randomPasswordEncrypted = values.GetValue<string>(CloudKey.RandomPassword);
			if (string.IsNullOrEmpty(randomPasswordEncrypted)) return null;
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			message.Values.DecryptRandomPassword(rsa.RsaPublicKeyValue, rsa.RsaPrivateKeyValue);
			var username = values.GetValue<string>(CloudKey.Username, null, true);
			var password = values.GetValue<string>(CloudKey.Password, null, true);
			// If user password is not valid then return
			if (!Membership.ValidateUser(username, password))
			{
				return null;
			}
			var user = JocysCom.WebSites.Engine.Security.Data.User.GetUser(username);
			return user;
		}

		/// <summary>Get secure user command.</summary>
		public static CloudMessage NewMessage(CloudAction action, string localRsaPublicKey = null, string remoteRsaPublicKey = null, string username = null, string password = null)
		{
			var cmd = new CloudMessage(action);
			if (!string.IsNullOrEmpty(localRsaPublicKey))
			{
				// Include local RSA public key which will be used by remote side to encrypt reply data.
				cmd.Values.Add(CloudKey.RsaPublicKey, localRsaPublicKey);
			}
			if (!string.IsNullOrEmpty(remoteRsaPublicKey))
			{
				// Use cloud RSA key to generate random AES-256 password inside.
				cmd.Values.AddRandomPassword(remoteRsaPublicKey);
			}
			if (!string.IsNullOrEmpty(username))
			{
				// Add encrypted username.
				cmd.Values.Add(CloudKey.Username, username, true);
			}
			if (!string.IsNullOrEmpty(password))
			{
				// Add encrypted password.
				cmd.Values.Add(CloudKey.Password, password, true);
			}
			return cmd;
		}


	}
}
