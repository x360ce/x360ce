using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace JocysCom.ClassLibrary.Security
{

	/// <summary>
	/// UTF-8 is used as a default encoding (smaller Internet messaging size).
	/// </summary>
	public static class AESHelper
	{

		#region Shared Functions

		/// <summary>
		/// Generate salt from password.
		/// </summary>
		/// <param name="password">Password string.</param>
		/// <returns>Salt bytes.</returns>
		private static byte[] SaltFromPassword(string password)
		{
			var passwordBytes = Encoding.UTF8.GetBytes(password);
			HMAC algorithm;
			switch (AesSaltAlgorithm)
			{
				case "HMACSHA1": algorithm = new HMACSHA1(); break;
				case "HMACSHA256": algorithm = new HMACSHA256(); break;
				default: algorithm = new HMACSHA256(); break;
			}
			algorithm.Key = passwordBytes;
			var salt = algorithm.ComputeHash(passwordBytes);
			algorithm.Dispose();
			return salt;
		}

		const string _prefix = "AppEncryption_";
		public const string AesSaltAlgorithmKeyName = _prefix + "AesSaltAlgorithm";

		static string _AesSaltAlgorithm;
		public static string AesSaltAlgorithm
		{
			get
			{
				if (string.IsNullOrEmpty(_AesSaltAlgorithm))
				{
					_AesSaltAlgorithm =
						ConfigurationManager.AppSettings[AesSaltAlgorithmKeyName]
						?? "HMACSHA256";
				}
				return _AesSaltAlgorithm;
			}
			set { _AesSaltAlgorithm = value; }
		}

		private static ICryptoTransform GetTransform(string password, bool encrypt)
		{
			// Create an instance of the AES class. 
			var provider = Aes.Create();
			// Calculate salt to make it harder to guess key by using a dictionary attack.
			var salt = SaltFromPassword(password);
			// Generate Secret Key from the password and salt.
			// Note: Set number of iterations to 10 in order for JavaScript example to work faster.
			// Rfc2898DeriveBytes generator based on HMACSHA1 by default.
			// Ability to specify HMAC algorithm is available since .NET 4.7.2
			var secretKey = new Rfc2898DeriveBytes(password, salt, 10, HashAlgorithmName.SHA1);
			// 32 bytes (256 bits) for the secret key and
			// 16 bytes (128 bits) for the initialization vector (IV).
			var key = secretKey.GetBytes(provider.KeySize / 8);
			var iv = secretKey.GetBytes(provider.BlockSize / 8);
			secretKey.Dispose();
			// Create a cryptor from the existing SecretKey bytes.
			var cryptor = encrypt
				? provider.CreateEncryptor(key, iv)
				: provider.CreateDecryptor(key, iv);
			return cryptor;
		}

		/// <summary>
		/// Encrypt/Decrypt with Write method.
		/// </summary>
		/// <param name="cryptor"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private static byte[] CipherStreamWrite(ICryptoTransform cryptor, byte[] input)
		{
			var inputBuffer = new byte[input.Length];
			// Copy data bytes to input buffer.
			System.Buffer.BlockCopy(input, 0, inputBuffer, 0, inputBuffer.Length);
			// Create a MemoryStream to hold the output bytes.
			// SUPPRESS: CWE-404: Improper Resource Shutdown or Release
			// Note: False Positive: cryptoStream.Close() will close underlying MemoryStream automatically.
			var stream = new MemoryStream();
			// Create a CryptoStream through which we are going to be processing our data.
			var cryptoStream = new CryptoStream(stream, cryptor, CryptoStreamMode.Write);
			// Start the encrypting or decrypting process.
			cryptoStream.Write(inputBuffer, 0, inputBuffer.Length);
			// Finish encrypting or decrypting.
			cryptoStream.FlushFinalBlock();
			// Convert data from a memoryStream into a byte array.
			var outputBuffer = stream.ToArray();
			cryptoStream.Close();
			// Underlying streams will be closed by default.
			//stream.Close();
			return outputBuffer;
		}

		#endregion

		#region AES-256 Encryption

		/// <summary>
		/// Encrypt string with AES-256 by using password.
		/// </summary>
		/// <param name="password">Password string.</param>
		/// <param name="s">String to encrypt.</param>
		/// <returns>Encrypted Base64 string.</returns>
		public static string EncryptString(string password, string s)
		{
			// Turn input strings into a byte array.
			var bytes = Encoding.UTF8.GetBytes(s);
			// Get encrypted bytes.
			var encryptedBytes = Encrypt(password, bytes);
			// Convert encrypted data into a base64-encoded string.
			var base64String = System.Convert.ToBase64String(encryptedBytes);
			// Return encrypted string.
			return base64String;
		}

		/// <summary>
		/// Encrypt string with AES-256 by using password.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="bytes">Bytes to encrypt.</param>
		/// <returns>Encrypted bytes.</returns>
		public static byte[] Encrypt(string password, byte[] bytes)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			var encryptor = GetTransform(password, true);
			var encryptedBytes = CipherStreamWrite(encryptor, bytes);
			encryptor.Dispose();
			// Return encrypted bytes.
			return encryptedBytes;
		}

		#endregion

		#region AES-256 Decryption

		/// <summary>
		/// Decrypt string with AES-256 by using password key.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="base64String">Encrypted Base64 string.</param>
		/// <returns>Decrypted string.</returns>
		public static string DecryptString(string password, string base64String)
		{
			// Convert Base64 string into a byte array. 
			var encryptedBytes = System.Convert.FromBase64String(base64String);
			var bytes = Decrypt(password, encryptedBytes);
			// Convert decrypted data into a string. 
			string s = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			// Return decrypted string.   
			return s;
		}

		/// <summary>
		/// Decrypt string with AES-256 by using password key.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="encryptedBytes">Encrypted bytes.</param>
		/// <returns>Decrypted bytes.</returns>
		public static byte[] Decrypt(string password, byte[] bytes)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			var decryptor = GetTransform(password, false);
			var decryptedBytes = CipherStreamWrite(decryptor, bytes);
			decryptor.Dispose();
			// Return encrypted bytes.
			return decryptedBytes;
		}

		#endregion

		#region Base64


		/// <summary>
		/// Decrypt string with AES-256 by using password key.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="base64String">Encrypted Base64 string.</param>
		/// <returns>Decrypted string.</returns>
		public static string DecryptFromBase64(string password, string base64String)
		{
			// Convert Base64 string into a byte array.
			var encryptedBytes = System.Convert.FromBase64String(base64String);
			var bytes = Decrypt(password, encryptedBytes);
			// Convert decrypted data into a string.
			var s = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			// Return decrypted string.
			return s;
		}

		/// <summary>
		/// Encrypt string with AES-256 by using password.
		/// </summary>
		/// <param name="password">Password string.</param>
		/// <param name="s">String to encrypt.</param>
		/// <returns>Encrypted Base64 string.</returns>
		public static string EncryptToBase64(string password, string s)
		{
			// Turn input strings into a byte array.
			var bytes = Encoding.UTF8.GetBytes(s);
			// Get encrypted bytes.
			var encryptedBytes = Encrypt(password, bytes);
			// Convert encrypted data into a base64-encoded string.
			var base64String = System.Convert.ToBase64String(encryptedBytes);
			// Return encrypted string.
			return base64String;
		}

		#endregion

		/// <summary>
		/// Encrypt file.
		/// </summary>
		/// <param name="password">Password used to protect the file.</param>
		/// <param name="inputFile">Unencrypted input file.</param>
		/// <param name="outputFile">Encrypted output file.</param>
		/// <param name="compress">Compress file before encryption</param>
		public static void EncryptFile(string password, string inputFile, string outputFile, bool compress = false)
		{
			var encryptor = GetTransform(password, true);
			// Open the file streams.
			// SUPPRESS: CWE-73: External Control of File Name or Path
			// Note: False Positive. File path is not externally controlled by the user.
			var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
			// SUPPRESS: CWE-73: External Control of File Name or Path
			// Note: False Positive. File path is not externally controlled by the user.
			var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
			try
			{
				var cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
				var gz = compress
					? new DeflateStream(cs, CompressionMode.Compress, true)
					: null;
				int read;
				// 4096 buffer preferable because the CPU cache can hold such amounts.
				var buffer = new byte[0x1000];
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					if (gz != null)
						// Write data to the DeflateStream, which in turn writes to the underlying CryptoStream.
						gz.Write(buffer, 0, read);
					else
						// Write data to the CryptoStream.
						cs.Write(buffer, 0, read);
				}
				if (gz != null)
					// Will close underlying stream 'cs' and 'output'.
					gz.Close();
				// Will close underlying stream 'output'.
				cs.Close();
			}
			catch
			{
				throw;
			}
			finally
			{
				input.Close();
			}
			encryptor.Dispose();
		}

		/// <summary>
		/// Decrypt file.
		/// </summary>
		/// <param name="password">Password used to protect the file.</param>
		/// <param name="inputFile">Encrypted input file.</param>
		/// <param name="outputFile">Decrypted output file.</param>
		/// <param name="decompress">Decompress file after decryption</param>
		// SUPPRESS: CWE-73: External Control of File Name or Path
		// Note: False Positive. File path is not externally controlled by the user.
		public static void DecryptFile(string password, string inputFile, string outputFile, bool decompress = false)
		{
			var decryptor = GetTransform(password, false);
			// Open the file streams.
			// SUPPRESS: CWE-73: External Control of File Name or Path
			// Note: False Positive. File path is not externally controlled by the user.
			var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
			// SUPPRESS: CWE-73: External Control of File Name or Path
			// Note: False Positive. File path is not externally controlled by the user.
			var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
			try
			{
				var cs = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
				var gz = decompress
					? new DeflateStream(cs, CompressionMode.Decompress, true)
					: null;
				int read;
				// 4096 buffer preferable because the CPU cache can hold such amounts.
				var buffer = new byte[0x1000];
				if (decompress)
				{
					// Read data from the DeflateStream, which in turn reads from the underlying CryptoStream.
					while ((read = gz.Read(buffer, 0, buffer.Length)) > 0)
						output.Write(buffer, 0, read);
				}
				else
				{
					// Read data from the CryptoStream.
					while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
						output.Write(buffer, 0, read);
				}
				if (gz != null)
					// Will close underlying stream 'cs' and 'input'.
					gz.Close();
				// Will close underlying stream 'input'.
				cs.Close();
			}
			catch
			{
				throw;
			}
			finally
			{
				output.Close();
			}
			decryptor.Dispose();
		}

	}
}
