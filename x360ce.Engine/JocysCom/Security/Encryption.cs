using System;
using System.Configuration;
using System.Text;

namespace JocysCom.ClassLibrary.Security
{
	/// <summary>
	/// Summary description for Encryption
	/// </summary>
	public partial class Encryption : IDisposable
	{

		static Encryption _Current;
		public static Encryption Current
		{
			set { _Current = value; }
			get { return _Current = _Current ?? new Encryption(); }
		}

		public Encryption(string prefix = null)
		{
			_prefix = prefix ?? "AppEncryption_";
		}

		string _prefix;

		/// <summary>Gets HMAC hash key name inside config file.</summary>
		public string HmacHashKeyName { get { return _prefix + "HmacHashKey"; } }

		/// <summary>RSA public key name inside config file.</summary>
		public string RsaPublicKeyName { get { return _prefix + "RsaPublicKey"; } }

		/// <summary>RSA private key name inside config file.</summary>
		public string RsaPrivateKeyName { get { return _prefix + "RsaPrivateKey"; } }

		/// <summary>RSA OAEP Padding key name inside config file.</summary>
		public string RsaUseOaepName { get { return _prefix + "RsaUseOaep"; } }

		string _HmacHashKeyValue;
		public string HmacHashKeyValue
		{
			get
			{
				if (string.IsNullOrEmpty(_HmacHashKeyValue))
				{
					_HmacHashKeyValue =
						ConfigurationManager.AppSettings[HmacHashKeyName]
						?? _prefix + "Hmac";
				}
				return _HmacHashKeyValue;
			}
			set { _HmacHashKeyValue = value; }
		}

		string _RsaPublicKeyValue;
		public string RsaPublicKeyValue
		{
			get
			{
				return _RsaPublicKeyValue = _RsaPublicKeyValue
					?? ConfigurationManager.AppSettings[RsaPublicKeyName];
			}
			set { _RsaPublicKeyValue = value; }
		}

		string _RsaPrivateKeyValue;
		public string RsaPrivateKeyValue
		{
			get
			{
				return _RsaPrivateKeyValue = _RsaPrivateKeyValue
					?? ConfigurationManager.AppSettings[RsaPrivateKeyName];
			}
			set { _RsaPrivateKeyValue = value; }
		}

		bool? _RsaUseOaepValue;
		public bool RsaUseOaepValue
		{
			get
			{
				if (!_RsaUseOaepValue.HasValue)
				{
					bool value;
					bool.TryParse(ConfigurationManager.AppSettings[RsaUseOaepName], out value);
					_RsaUseOaepValue = value;
				}
				return _RsaUseOaepValue.Value;
			}
			set { _RsaUseOaepValue = value; }
		}

		#region MD5

		object HashProviderLock = new object();

		System.Security.Cryptography.HashAlgorithm _HashProvider;
		public System.Security.Cryptography.HashAlgorithm HashProvider
		{
			get
			{
				return _HashProvider = _HashProvider ??
					System.Security.Cryptography.MD5.Create();
			}
		}


		/// <summary>
		/// Computes the MD5 hash value for the specified text. Use UTF-8 encoding to get bytes.
		/// </summary>
		/// <param name="text">The input to compute the hash code for.</param>
		/// <returns>The computed hash code as GUID.</returns>
		public Guid ComputeMd5Hash(string text)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			return ComputeMd5Hash(bytes);
		}

		/// <summary>
		/// Computes the MD5 hash value for the specified text.
		/// </summary>
		/// <param name="text">The input to compute the hash code for.</param>
		/// <param name="encoding">Encoding to get bytes.</param>
		/// <returns>The computed hash code as GUID.</returns>
		public Guid ComputeMd5Hash(string text, Encoding encoding)
		{
			byte[] bytes = encoding.GetBytes(text);
			return ComputeMd5Hash(bytes);
		}

		/// <summary>
		/// Computes the MD5 hash value for the specified byte array.
		/// </summary>
		/// <param name="bytes">The input to compute the hash code for.</param>
		/// <returns>The computed hash code as GUID.</returns>
		/// <remarks>
		/// One instance of the MD5 Crypto Service Provider
		/// can't operate properly with multiple simultaneous threads.
		/// Use lock to solve this problem.
		/// </remarks>
		public Guid ComputeMd5Hash(byte[] bytes)
		{
			byte[] hash;
			lock (HashProviderLock)
				hash = HashProvider.ComputeHash(bytes);
			return new Guid(hash);
		}


		#endregion

		#region MD5HMAC

		object MacProviderLock = new object();

		System.Security.Cryptography.KeyedHashAlgorithm _MacProvider;
		public System.Security.Cryptography.KeyedHashAlgorithm MacProvider
		{
			get
			{
				if (_MacProvider is null)
				{
					// Create MD5HMAC hash provider.
					if (string.IsNullOrEmpty(HmacHashKeyValue))
						throw new InvalidOperationException("Application key '" + HmacHashKeyName + "' is not set!");
					byte[] hashKeyBytes = System.Text.Encoding.UTF8.GetBytes(HmacHashKeyValue);
					_MacProvider = new System.Security.Cryptography.HMACMD5();
					_MacProvider.Key = hashKeyBytes;
				}
				return _MacProvider;
			}
		}

		/// <summary>
		/// Computes the HMAC MD5 hash value for the specified text.
		/// </summary>
		/// <param name="text">The input to compute the hash code for.</param>
		/// <returns>The computed hash code as GUID.</returns>
		public Guid ComputeHash(string text, string hashKey = null)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			byte[] hashKeyBytes = null;
			if (!string.IsNullOrEmpty(hashKey))
			{
				hashKeyBytes = System.Text.Encoding.UTF8.GetBytes(hashKey);
			}
			return ComputeHash(bytes, hashKeyBytes);
		}

		/// <summary>
		/// Computes the HMAC MD5 hash value for the specified byte array.
		/// </summary>
		/// <param name="bytes">The input to compute the hash code for.</param>
		/// <returns>The computed hash code as GUID.</returns>
		/// <remarks>
		/// One instance of the MD5 Crypto Service Provider
		/// can't operate properly with multiple simultaneous threads.
		/// Use lock to solve this problem.
		/// </remarks>
		public Guid ComputeHash(byte[] bytes, byte[] hashKeyBytes = null)
		{
			byte[] hash;
			// If HMAC hash key is not supplied then...
			if (hashKeyBytes is null)
			{
				lock (MacProviderLock)
					// Use default from config file.
					hash = MacProvider.ComputeHash(bytes);
			}
			else
			{
				// Create MD5HMAC hash provider.
				var macProvider = new System.Security.Cryptography.HMACMD5();
				macProvider.Key = hashKeyBytes;
				hash = macProvider.ComputeHash(bytes);
			}
			return new Guid(hash);
		}

		#endregion

		#region RSA Security / Password Protection / Signature

		// Encrypt XML.
		// http://msdn.microsoft.com/en-us/library/ms229746.aspx

		object RsaProviderLock = new object();

		System.Security.Cryptography.RSACryptoServiceProvider _RsaProvider;
		public System.Security.Cryptography.RSACryptoServiceProvider RsaProvider
		{
			get
			{
				lock (RsaProviderLock)
				{
					if (_RsaProvider != null)
						return _RsaProvider;
					//Problem Solution: http://support.microsoft.com/default.aspx?scid=KB;EN-US;322371
					System.Security.Cryptography.RSACryptoServiceProvider.UseMachineKeyStore = true;
					// Create a new CspParameters object to specify a key container.
					System.Security.Cryptography.CspParameters cspParams = new System.Security.Cryptography.CspParameters();
					cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";
					cspParams.Flags = System.Security.Cryptography.CspProviderFlags.UseMachineKeyStore;
					_RsaProvider = new System.Security.Cryptography.RSACryptoServiceProvider();
					// If web.config data is not available then return.
					if (RsaPublicKeyValue is null) return _RsaProvider;
					byte[] privateKeyBytes = string.IsNullOrEmpty(RsaPrivateKeyValue)
						? new byte[0] : System.Convert.FromBase64String(RsaPrivateKeyValue);
					byte[] publicKeyBytes = string.IsNullOrEmpty(RsaPublicKeyValue)
						? new byte[0] : System.Convert.FromBase64String(RsaPublicKeyValue);
					// If private key was found then...
					byte[] rsaKeyBytes = (privateKeyBytes.Length > 0) ? privateKeyBytes : publicKeyBytes;
					//System.Security.Cryptography.RSAParameters rp = new System.Security.Cryptography.RSAParameters()
					try
					{
						// This line can fail due to missing user profile on windows.
						_RsaProvider.ImportCspBlob(rsaKeyBytes);
					}
					catch (Exception) { }
				}
				return _RsaProvider;
			}
		}

		/// <summary>
		/// RSA Signature algorithm. SHA1 is default.
		/// </summary>
		public System.Security.Cryptography.HashAlgorithm RsaSignatureHashAlgorithm
			=> _RsaSignatureHashAlgorithm = _RsaSignatureHashAlgorithm ?? System.Security.Cryptography.SHA256.Create();

		System.Security.Cryptography.HashAlgorithm _RsaSignatureHashAlgorithm;

		/// <summary>
		/// Encrypts data with the System.Security.Cryptography.RSA algorithm.
		/// </summary>
		/// <param name="bytes"> The data to be encrypted.</param>
		/// <returns>The encrypted data.</returns>
		public string RsaEncrypt(byte[] bytes)
		{
			byte[] encrypted;
			// Enable OAEP padding for better security.
			lock (RsaProviderLock)
				encrypted = this.RsaProvider.Encrypt(bytes, RsaUseOaepValue);
			return System.Convert.ToBase64String(encrypted);
		}

		/// <summary>
		/// Encrypts data with the System.Security.Cryptography.RSA algorithm.
		/// </summary>
		/// <param name="text"> The data to be encrypted.</param>
		/// <returns>The encrypted data.</returns>
		public string RsaEncrypt(string text)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			return RsaEncrypt(bytes);
		}

		/// <summary>
		/// Decrypts data with the System.Security.Cryptography.RSA algorithm.
		/// </summary>
		/// <param name="base64Text">The data to be decrypted.</param>
		/// <returns>The decrypted data, which is the original plain text before encryption.</returns>
		public string RsaDecrypt(string base64Text)
		{
			byte[] bytes = System.Convert.FromBase64String(base64Text);
			byte[] decrypted;
			// Enable OAEP padding for better security.
			lock (RsaProviderLock)
				decrypted = RsaProvider.Decrypt(bytes, RsaUseOaepValue);
			return System.Text.Encoding.UTF8.GetString(decrypted);
		}



		#endregion

		#region Generate new RSA keys

		// A RSA public key consists of two components as follows (All positive whole numbers):
		// * "Modulus" is the RSA modulus. An RSA Modulus is the number which is the product of two different odd prime numbers which by convention are called P and Q.[RSA02]
		// * "Exponent" is the RSA public exponent. Exponent is an integer between 3 and Modulus - 1, where the greatest common denominator of Exponent and the least common multiple of P - 1 and Q - 1 equal 1.[RSA02] 
		//
		// A RSA private key consists of seven components as follows (All positive whole numbers):
		// * "P"- The first factor of Modulus.
		// * "Q"- The second factor of Modulus.
		// * "Modulus"- The same as in the corresponding RSA public key.
		// * "D"- The RSA private exponent (Keep Secret!).
		// * "DP"- The first factor’s Chinese remainder theorem exponent.
		// * "DQ"- The second factor’s Chinese remainder theorem exponent.
		// * "InverseQ"- The (first) Chinese remainder theorem coefficient. 

		/// <summary>
		/// Get new public and private RSA key pair.
		/// </summary>
		/// <returns></returns>
		public string GetNewRsaKeys()
		{
			return GetNewRsaKeys(2048);
		}

		public struct Keys
		{
			public string Public;
			public string Private;
		}

		public Keys RsaNewKeys(int keySize)
		{
			Keys keys = new Keys();
			// Generate a public/private key pair.
			System.Security.Cryptography.RSACryptoServiceProvider rsa;
			rsa = new System.Security.Cryptography.RSACryptoServiceProvider(keySize);
			//-----------------------------------------------------
			//Save the public key information to an RSAParameters structure.
			System.Security.Cryptography.RSAParameters publicKeyInfo;
			publicKeyInfo = rsa.ExportParameters(false);
			string publicXml = rsa.ToXmlString(false);
			keys.Public = System.Convert.ToBase64String(rsa.ExportCspBlob(false));
			//-----------------------------------------------------
			//Save the public and private key information to an RSAParameters structure.
			System.Security.Cryptography.RSAParameters privateKeyInfo;
			privateKeyInfo = rsa.ExportParameters(true);
			string privateXml = rsa.ToXmlString(true);
			keys.Private = System.Convert.ToBase64String(rsa.ExportCspBlob(true));
			//-----------------------------------------------------
			//System.Security.Cryptography.X509Certificates.PublicKey pubKey;
			//System.Security.Cryptography.X509Certificates.PublicKey pvtKey;
			return keys;
		}

		/// <summary>
		/// Get new public and private RSA key pair for app.config file.
		/// </summary>
		/// <param name="keySize">Key size.</param>
		/// <returns></returns>
		public string GetNewRsaKeys(int keySize)
		{
			Keys keys = RsaNewKeys(keySize);
			var sb = new System.Text.StringBuilder();
			string pattern = "<add key=\"{0}\" value=\"{1}\"/>\r\n";
			sb.Append(string.Format(pattern, RsaPublicKeyName, keys.Public));
			sb.Append(string.Format(pattern, RsaPrivateKeyName, keys.Private));
			return sb.ToString();
		}

		public void RsaNewKeysSave()
		{
			RsaNewKeysSave(2048);
		}

		public void UpsertKey(System.Configuration.Configuration config, string name, object value)
		{
			if (config.AppSettings.Settings[name] is null)
				config.AppSettings.Settings.Add(name, string.Format("{0}", value));
			else
				config.AppSettings.Settings[name].Value = string.Format("{0}", value);
		}

		public void RsaNewKeysSave(int keySize)
		{
			Keys keys = RsaNewKeys(keySize);

#if NETFRAMEWORK

			// Get the configuration file.
			var config = System.Web.HttpRuntime.IISVersion is null
				? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				: System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
#else
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
#endif

			// Modify settings.
			UpsertKey(config, HmacHashKeyName, HmacHashKeyValue);
			UpsertKey(config, RsaPublicKeyName, keys.Public);
			UpsertKey(config, RsaPrivateKeyName, keys.Private);
			UpsertKey(config, RsaUseOaepName, RsaUseOaepValue);
			// Save the configuration file.
			config.Save(System.Configuration.ConfigurationSaveMode.Modified);
			// Reset values.
			HmacHashKeyValue = null;
			RsaPrivateKeyValue = null;
			RsaPublicKeyValue = null;
			// Force a reload of a changed section.
			System.Configuration.ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
			//Properties.Settings.Default.Reload();
			// Reload other settings.
			//Engine.Properties.PublicSettings.Default.Reload();
		}

		#endregion

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		//// NOTE: Leave out the finalizer altogether if this class doesn't 
		//// own unmanaged resources itself, but leave the other methods
		//// exactly as they are. 
		//~Encryption()
		//{
		//    // Finalizer calls Dispose(false)
		//    Dispose(false);
		//}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Free managed resources.
				lock (MacProviderLock)
				{
					if (_MacProvider != null)
					{
						_MacProvider.Dispose();
						_MacProvider = null;
					}
				}
				lock (RsaProviderLock)
				{
					if (_RsaProvider != null)
					{
						_RsaProvider.Dispose();
						_RsaProvider = null;
					}
				}
				if (_RsaSignatureHashAlgorithm != null)
				{
					_RsaSignatureHashAlgorithm.Dispose();
					_RsaSignatureHashAlgorithm = null;
				}
			}
		}

		#endregion

	}
}
