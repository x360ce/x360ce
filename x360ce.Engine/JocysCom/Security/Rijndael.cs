using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace JocysCom.ClassLibrary.Security
{
	public class Rijndael
	{

		#region Shared Functions

		/// <summary>
		/// Generate salt from password.
		/// </summary>
		/// <param name="password">Password string.</param>
		/// <returns>Salt bytes.</returns>
		private static byte[] SaltFromPassword(string password)
		{
			byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
			System.Security.Cryptography.HMACSHA1 hmac;
			hmac = new System.Security.Cryptography.HMACSHA1(passwordBytes);
			byte[] salt = hmac.ComputeHash(passwordBytes);
			return salt;
		}

		private static ICryptoTransform GetTransform(string password, bool encrypt)
		{
			// Create an instance of the Rihndael class. 
			RijndaelManaged cipher = new System.Security.Cryptography.RijndaelManaged();
			// Calculate salt to make it harder to guess key by using a dictionary attack.
			byte[] salt = SaltFromPassword(password);
			// Generate Secret Key from the password and salt.
			// Note: Set number of iterations to 10 in order for JavaScript example to work faster.
			Rfc2898DeriveBytes secretKey = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10);
			// Create a encryptor from the existing SecretKey bytes by using
			// 32 bytes (256 bits) for the secret key and
			// 16 bytes (128 bits) for the initialization vector (IV).
			byte[] key = secretKey.GetBytes(32);
			byte[] iv = secretKey.GetBytes(16);
			ICryptoTransform cryptor = null;
			if (encrypt)
			{
				cryptor = cipher.CreateEncryptor(key, iv);
			}
			else
			{
				cryptor = cipher.CreateDecryptor(key, iv);
			}
			return cryptor;
		}

		/// <summary>
		/// Encrypt/Decrypt with Write method.
		/// </summary>
		/// <param name="cryptor"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private static byte[] CipherStreamWrite(System.Security.Cryptography.ICryptoTransform cryptor, byte[] input)
		{
			byte[] inputBuffer = new byte[input.Length];
			// Copy data bytes to input buffer.
			System.Buffer.BlockCopy(input, 0, inputBuffer, 0, inputBuffer.Length);
			// Create a MemoryStream to hold the output bytes.
			System.IO.MemoryStream stream = new System.IO.MemoryStream();
			// Create a CryptoStream through which we are going to be processing our data.
			System.Security.Cryptography.CryptoStreamMode mode;
			mode = System.Security.Cryptography.CryptoStreamMode.Write;
			System.Security.Cryptography.CryptoStream cryptoStream;
			cryptoStream = new System.Security.Cryptography.CryptoStream(stream, cryptor, mode);
			// Start the crypting process.
			cryptoStream.Write(inputBuffer, 0, inputBuffer.Length);
			// Finish crypting.
			cryptoStream.FlushFinalBlock();
			// Convert data from a memoryStream into a byte array.
			byte[] outputBuffer = stream.ToArray();
			// Close both streams.
			stream.Close();
			cryptoStream.Close();
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
			byte[] bytes = System.Text.Encoding.Unicode.GetBytes(s);
			// Get encrypted bytes.
			byte[] encryptedBytes = Encrypt(password, bytes);
			// Convert encrypted data into a base64-encoded string.
			string base64String = System.Convert.ToBase64String(encryptedBytes);
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
			// Create a encryptor.
			ICryptoTransform encryptor = GetTransform(password, true);
			// Return encrypted bytes.
			return CipherStreamWrite(encryptor, bytes);
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
			byte[] encryptedBytes = System.Convert.FromBase64String(base64String);
			byte[] bytes = Decrypt(password, encryptedBytes);
			// Convert decrypted data into a string. 
			string s = System.Text.Encoding.Unicode.GetString(bytes, 0, bytes.Length);
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
			// Create a encryptor.
			ICryptoTransform decryptor = GetTransform(password, false);
			// Return encrypted bytes.
			return CipherStreamWrite(decryptor, bytes);
		}

		#endregion

		public static void EncryptFile(string key, string fileLocation, string fileDestination)
		{
			RijndaelManaged RijndaelCipher = new RijndaelManaged();
			// First we are going to open the file streams 
			FileStream fsIn = new FileStream(fileLocation, FileMode.Open, FileAccess.Read);
			FileStream fsOut = new FileStream(fileDestination, FileMode.Create, FileAccess.Write);
			byte[] Salt = Encoding.ASCII.GetBytes(key.Length.ToString());
			PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(key, Salt);
			ICryptoTransform Encryptor = RijndaelCipher.CreateEncryptor(SecretKey.GetBytes(32), SecretKey.GetBytes(16));
			CryptoStream cryptoStream = new CryptoStream(fsOut, Encryptor, CryptoStreamMode.Write);
			int ByteData;
			while ((ByteData = fsIn.ReadByte()) != -1)
			{
				cryptoStream.WriteByte((byte)ByteData);
			}
			cryptoStream.Close();
			fsIn.Close();
			fsOut.Close();
		}

	}
}