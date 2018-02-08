using System;
using System.Text;
using System.Xml;
using System.Security.Cryptography.Xml;
using System.Configuration;

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

		public Encryption()
		{
			_prefix = "AppEncryption";
		}

		public Encryption(string prefix)
		{
			_prefix = prefix;
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

		System.Security.Cryptography.MD5 _HashProvider;
		public System.Security.Cryptography.MD5 HashProvider
		{
			get
			{
				return _HashProvider = _HashProvider ?? new System.Security.Cryptography.MD5CryptoServiceProvider();
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
			{
				hash = HashProvider.ComputeHash(bytes);
			}
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
				if (_MacProvider == null)
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
			if (hashKeyBytes == null)
			{
				lock (MacProviderLock)
				{
					// Use default from config file.
					hash = MacProvider.ComputeHash(bytes);
				}
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
					if (_RsaProvider == null)
					{

						//Problem Solution: http://support.microsoft.com/default.aspx?scid=KB;EN-US;322371
						System.Security.Cryptography.RSACryptoServiceProvider.UseMachineKeyStore = true;
						// Create a new CspParameters object to specify a key container.
						System.Security.Cryptography.CspParameters cspParams = new System.Security.Cryptography.CspParameters();
						cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";
						cspParams.Flags = System.Security.Cryptography.CspProviderFlags.UseMachineKeyStore;
						_RsaProvider = new System.Security.Cryptography.RSACryptoServiceProvider();
						// If webconfig data is not available then return.
						if (RsaPublicKeyValue == null) return _RsaProvider;
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
				}
				return _RsaProvider;
			}
		}

		System.Security.Cryptography.HashAlgorithm _RsaSignatureHashAlgorithm;
		/// <summary>
		/// RSA Signature algorithm. SHA1 is default.
		/// </summary>
		public System.Security.Cryptography.HashAlgorithm RsaSignatureHashAlgorithm
		{
			get
			{
				if (_RsaSignatureHashAlgorithm == null)
				{
					_RsaSignatureHashAlgorithm = new System.Security.Cryptography.SHA1Managed();
					//_RsaSignatureHashAlgorithm = new System.Security.Cryptography.MD5CryptoServiceProvider();
				}
				return _RsaSignatureHashAlgorithm;
			}
		}

		/// <summary>
		/// Encrypts data with the System.Security.Cryptography.RSA algorithm.
		/// </summary>
		/// <param name="bytes"> The data to be encrypted.</param>
		/// <returns>The encrypted data.</returns>
		public string RsaEncrypt(byte[] bytes)
		{
			byte[] encrypted;
			lock (RsaProviderLock)
			{
				// Enable OAEP padding for better security.
				// Disable for compatibility.
				encrypted = this.RsaProvider.Encrypt(bytes, false);
			}
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
			lock (RsaProviderLock)
			{
				// Enable OAEP padding for better security.
				// Disable for compatibility.
				decrypted = RsaProvider.Decrypt(bytes, false);
			}
			return System.Text.Encoding.UTF8.GetString(decrypted);
		}

		/// <summary>
		/// Private RSA key is required to sign data.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		/// <remarks>Private RSA key is required to sign data.</remarks>
		string RsaGenerateSignature(byte[] bytes)
		{
			//byte[] hash = RsaSignatureHashAlgorithm.ComputeHash(bytes);
			//byte[] sign = RsaProvider.SignHash(hash, System.Security.Cryptography.CryptoConfig.MapNameToOID("SHA1"));
			byte[] sign;
			lock (RsaProviderLock)
			{
				sign = RsaProvider.SignData(bytes, RsaSignatureHashAlgorithm);
			}
			string signature = System.Convert.ToBase64String(sign);
			return signature;
		}

		/// <summary>
		/// Computes the hash value of the specified byte array using the specified hash
		/// algorithm, and signs the resulting hash value.
		/// </summary>
		/// <param name="bytes">The input data for which to compute the hash.</param>
		/// <returns> The System.Security.Cryptography.RSA signature in base64 format for the specified data.</returns>
		/// <remarks>Private RSA key is required to sign data.</remarks>
		public string RsaSignData(byte[] bytes)
		{
			string signature = RsaGenerateSignature(bytes);
			return signature;
		}

		/// <summary>
		/// Computes the hash value of the specified byte array using the specified hash
		/// algorithm, and signs the resulting hash value.
		/// </summary>
		/// <param name="text">The input data for which to compute the hash.</param>
		/// <returns> The System.Security.Cryptography.RSA signature in base64 format for the specified data.</returns>
		public string RsaSignData(string text)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			return RsaSignData(bytes);
		}

		/// <summary>
		/// Computes the hash value of the specified file.
		/// </summary>
		/// <param name="text">Name of file to compute the hash.</param>
		/// <returns>The System.Security.Cryptography.RSA signature in base64 format for the specified data.</returns>
		public string RsaSignFile(string fileName)
		{
			System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
			if (!fi.Exists) return null;
			if (fi.Extension.ToLower() == ".xml")
			{
				// Create a new XML document.
				XmlDocument doc = new XmlDocument();
				// Load an XML file into the XmlDocument object.
				doc.PreserveWhitespace = true;
				doc.Load(fileName);
				// Sign document.
				XmlElement sign = RsaSignData(doc);
				// Save the document.
				doc.Save(fileName);
				// Save signature to *.rsa file.
				// return signature.
				return sign.InnerText;
			}
			else
			{
				string signFile = System.IO.Path.Combine(fi.FullName, ".rsa");
				string sign = RsaSignData(System.IO.File.ReadAllBytes(fi.FullName));
				System.IO.File.WriteAllText(signFile, sign);
				return sign;
			}
		}

		/// <summary>
		/// Computes the hash value of the specified System.Xml.XmlDocument object.
		/// </summary>
		/// <param name="doc">The System.Xml.XmlDocument object to sign.</param>
		/// <returns>The System.Security.Cryptography.RSA signature as XmlElement</returns>
		public XmlElement RsaSignData(XmlDocument doc)
		{
			// Create a SignedXml object.
			SignedXml signedXml = new SignedXml(doc);
			// Add the key to the SignedXml document.
			signedXml.SigningKey = RsaProvider;
			// Create a reference to be signed.
			Reference reference = new Reference();
			reference.Uri = "";
			// Add an enveloped transformation to the reference.
			XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
			reference.AddTransform(env);
			// Add the reference to the SignedXml object.
			signedXml.AddReference(reference);
			// Compute the signature.
			signedXml.ComputeSignature();
			// Get the XML representation of the signature and save
			// it to an XmlElement object.
			XmlElement xmlDigitalSignature = signedXml.GetXml();
			// Append the element to the XML document.
			doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
			return xmlDigitalSignature;
		}

		/// <summary>
		/// Verifies the specified signature data by comparing it to the signature computed
		/// for the specified data.
		/// </summary>
		/// <param name="text"> The data that was signed.</param>
		/// <param name="signature">The base64 signature data to be verified.</param>
		/// <returns>True if the signature verifies as valid; otherwise, false.</returns>
		public bool RsaVerifyData(string text, string signature)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			return RsaVerifyData(bytes, signature);
		}

		/// <summary>
		/// Verifies the specified signature data by comparing it to the signature computed
		/// for the specified data.
		/// </summary>
		/// <param name="text"> The data that was signed.</param>
		/// <param name="signature">The base64 signature data to be verified.</param>
		/// <returns>True if the signature verifies as valid; otherwise, false.</returns>
		public bool RsaVerifyData(byte[] bytes, string signature)
		{
			//byte[] hash = SHA1.ComputeHash(bytes);
			//byte[] sign = RsaProvider.SignatureAlgorithm
			//VerifyHash(hash, System.Security.Cryptography.CryptoConfig.MapNameToOID("SHA1"));
			string actualSignature = RsaGenerateSignature(bytes);
			return actualSignature.Equals(signature);
		}

		/// <summary>
		/// Determines whether the System.Security.Cryptography.Xml.SignedXml.Signature
		/// property verifies for the specified key.
		/// </summary>
		/// <param name="doc">The System.Xml.XmlDocument object to verify.</param>
		/// <returns>
		/// True if the System.Security.Cryptography.Xml.SignedXml.Signature property
		/// verifies for the specified key; otherwise, false.
		/// </returns>
		public bool RsaVerifyData(XmlDocument doc)
		{
			// Create a new SignedXml object and pass it
			// the XML document class.
			SignedXml signedXml = new SignedXml(doc);
			// Find the "Signature" node and create a new
			// XmlNodeList object.
			XmlNodeList nodeList = doc.GetElementsByTagName("Signature");
			// Throw an exception if no signature was found.
			if (nodeList.Count <= 0)
			{
				throw new System.Security.Cryptography.CryptographicException("Verification failed: No Signature was found in the document.");
			}
			// This example only supports one signature for
			// the entire XML document.  Throw an exception 
			// if more than one signature was found.
			if (nodeList.Count >= 2)
			{
				throw new System.Security.Cryptography.CryptographicException("Verification failed: More that one signature was found for the document.");
			}
			// Load the first <signature> node.  
			signedXml.LoadXml((XmlElement)nodeList[0]);
			// Check the signature and return the result.
			return signedXml.CheckSignature(RsaProvider);
		}


		/// <summary>
		/// Computes the hash value of the specified file.
		/// </summary>
		/// <param name="text">Name of file to compute the hash.</param>
		/// <returns>True if the signature verifies as valid; otherwise, false.</returns>
		public bool RsaVerifyFile(string fileName)
		{
			System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
			if (!fi.Exists) return false;
			if (fi.Extension.ToLower() == ".xml")
			{
				// Create a new XML document.
				XmlDocument doc = new XmlDocument();
				// Load an XML file into the XmlDocument object.
				doc.PreserveWhitespace = true;
				doc.Load(fileName);
				// Verify document.
				return RsaVerifyData(doc);
			}
			else
			{
				string signFile = System.IO.Path.Combine(fi.FullName, ".rsa");
				string signature = System.IO.File.ReadAllText(signFile);
				return RsaVerifyData(System.IO.File.ReadAllBytes(fi.FullName), signature);
			}
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
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			string pattern = "<add key=\"{0}\" value=\"{1}\"/>\r\n";
			sb.Append(String.Format(pattern, RsaPublicKeyName, keys.Public));
			sb.Append(String.Format(pattern, RsaPrivateKeyName, keys.Private));
			return sb.ToString();
		}

		public void RsaNewKeysSave()
		{
			RsaNewKeysSave(2048);
		}

		public void UpsertKey(System.Configuration.Configuration config, string name, object value)
		{
			if (config.AppSettings.Settings[HmacHashKeyName] == null)
				config.AppSettings.Settings.Add(HmacHashKeyName, string.Format("{0}", value));
			else
				config.AppSettings.Settings[RsaPublicKeyName].Value = string.Format("{0}", value);
		}

		public void RsaNewKeysSave(int keySize)
		{
			Keys keys = RsaNewKeys(keySize);
			// This is needed for Entity Framework connections.
			System.Configuration.Configuration config;
			// Get the configuration file.
			config = System.Web.HttpContext.Current == null || System.Web.HttpContext.Current.Request.PhysicalPath.Equals(string.Empty)
				? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				: System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
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
