using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;

namespace x360ce.Engine
{
	public partial class CloudHelper
	{

		public static JocysCom.WebSites.Engine.Security.Data.User GetUser(CloudMessage input, out string error)
		{
			var values = input.Values;
			error = null;
			if (values == null)
			{
				error = "Input message is null";
				return null;
			}
			var randomPasswordEncrypted = values.GetValue<string>(CloudKey.RandomPassword);
			if (string.IsNullOrEmpty(randomPasswordEncrypted)) return null;
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			input.Values.DecryptRandomPassword(rsa.RsaPublicKeyValue, rsa.RsaPrivateKeyValue);
			// Try to get user by username.
			var username = values.GetValue<string>(CloudKey.Username, null, true);
			var password = values.GetValue<string>(CloudKey.Password, null, true);
			if (string.IsNullOrEmpty(username))
			{
				error = "Username is empty";
				return null;
			}
			if (string.IsNullOrEmpty(password))
			{
				error = "Password is empty";
				return null;
			}
			// If user password is valid then...
			if (!Membership.ValidateUser(username, password))
			{
				error = "Invalid user credentials";
			}
			// Return user.
			return JocysCom.WebSites.Engine.Security.Data.User.GetUser(username);
		}

		public static Guid? GetComputerId(CloudMessage input, out string error)
		{
			var values = input.Values;
			error = null;
			if (values == null)
			{
				error = "Input message is null";
				return null;
			}
			var randomPasswordEncrypted = values.GetValue<string>(CloudKey.RandomPassword);
			if (string.IsNullOrEmpty(randomPasswordEncrypted)) return null;
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			input.Values.DecryptRandomPassword(rsa.RsaPublicKeyValue, rsa.RsaPrivateKeyValue);
			// Try to get computer id.
			var computerId = input.Values.GetValue(CloudKey.ComputerId, Guid.Empty, true);
			if (computerId == Guid.Empty)
			{
				error = "DiskId is empty";
				return null;
			}
			return computerId;
		}

		/// <summary>Get secure user command.</summary>
		public static void ApplySecurity(CloudMessage message, string localRsaPublicKey = null, string remoteRsaPublicKey = null, string username = null, string password = null)
		{
			if (!string.IsNullOrEmpty(localRsaPublicKey))
			{
				// Include local RSA public key which will be used by remote side to encrypt reply data.
				message.Values.Add(CloudKey.RsaPublicKey, localRsaPublicKey);
			}
			if (!string.IsNullOrEmpty(remoteRsaPublicKey))
			{
				// Use cloud RSA key to generate random AES-256 password inside.
				message.Values.AddRandomPassword(remoteRsaPublicKey);
			}
			if (!string.IsNullOrEmpty(username))
			{
				// Add encrypted username.
				message.Values.Add(CloudKey.Username, username, true);
			}
			if (!string.IsNullOrEmpty(password))
			{
				// Add encrypted password.
				message.Values.Add(CloudKey.Password, password, true);
			}
		}


	}
}
