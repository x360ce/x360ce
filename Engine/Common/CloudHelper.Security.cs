using System;
using System.Linq;
using System.Web.Security;

namespace x360ce.Engine
{
	public partial class CloudHelper
	{

		/// <summary>
		/// Decrypt message and get user if supplied user name and password is valid.
		/// </summary>
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
			if (string.IsNullOrEmpty(randomPasswordEncrypted))
				return null;
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			input.Values.DecryptRandomPassword(rsa.RsaPublicKeyValue, rsa.RsaPrivateKeyValue);
			// Try to get user by user name.
			var username = values.GetValue<string>(CloudKey.Username, null, true);
			var password = values.GetValue<string>(CloudKey.Password, null, true);
			if (string.IsNullOrEmpty(username))
			{
				error = "User name is empty";
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

		/// <summary>
		/// Decrypt message and get cloud key value as GUID.
		/// </summary>
		public static Guid? GetGuidId(string cloudKey, CloudMessage input, out string error)
		{
			var values = input.Values;
			error = null;
			if (values == null)
			{
				error = "Input message is null";
				return null;
			}
			var randomPasswordEncrypted = values.GetValue<string>(CloudKey.RandomPassword);
			if (string.IsNullOrEmpty(randomPasswordEncrypted))
				return null;
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			try
			{
				input.Values.DecryptRandomPassword(rsa.RsaPublicKeyValue, rsa.RsaPrivateKeyValue);
			}
			catch (Exception ex)
			{
				ex.Data.Add(nameof(CloudErrorCode), CloudErrorCode.UnableToDecrypt);
				throw;
			}
			// Try to get computer id.
			var guidId = input.Values.GetValue(cloudKey, Guid.Empty, true);
			if (guidId == Guid.Empty)
			{
				error = string.Format("{0} value is empty", cloudKey);
				return null;
			}
			return guidId;
		}

		/// <summary>Get secure user command.</summary>
		public static void ApplySecurity(CloudMessage message, string localRsaPublicKey = null, string remoteRsaPublicKey = null, string username = null, string password = null)
		{
			// Add local RSA public key which will be used by remote side to encrypt reply password.
			if (!string.IsNullOrEmpty(localRsaPublicKey))
				message.Values.Add(CloudKey.RsaPublicKey, localRsaPublicKey, false, true);
			// Create and add random pasword and encrypt with remote RSA public key.
			// Password will be used to encrypt sensitive data with AES-256 symetric encryption.
			if (!string.IsNullOrEmpty(remoteRsaPublicKey))
				message.Values.UpsertRandomPassword(remoteRsaPublicKey);
			// Add encrypted user name.
			if (!string.IsNullOrEmpty(username))
				message.Values.Add(CloudKey.Username, username, true, true);
			// Add encrypted x360ce site password.
			if (!string.IsNullOrEmpty(password))
				message.Values.Add(CloudKey.Password, password, true, true);
		}


	}
}
