using System;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Security
{

	/// <summary>
	/// 256-bit hash.
	/// UTF-8 is used as a default encoding (smaller Internet messaging size).
	/// </summary>
	public partial class SHA256Helper
	{
		public static Guid GetGuid(string value)
		{
			var algorithm = System.Security.Cryptography.SHA256.Create();
			Guid guid = HashHelper.GetGuid(algorithm, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(long value)
		{
			var algorithm = System.Security.Cryptography.SHA256.Create();
			Guid guid = HashHelper.GetGuid(algorithm, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] bytes)
		{
			var algorithm = System.Security.Cryptography.SHA256.Create();
			Guid guid = HashHelper.GetGuid(algorithm, bytes);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuidFromFile(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = System.Security.Cryptography.SHA256.Create();
			Guid guid = HashHelper.GetGuidFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			return guid;
		}

		public static byte[] GetHashFromFile(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = System.Security.Cryptography.SHA256.Create();
			var hash = HashHelper.GetHashFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			return hash;
		}

		// HMAC

		public static Guid GetGuid(string key, string value)
		{
			var algorithm = System.Security.Cryptography.HMACSHA256.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, long value)
		{
			var algorithm = System.Security.Cryptography.HMACSHA256.Create();
			var guid = HashHelper.GetGuid(algorithm, key, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, byte[] value)
		{
			var algorithm = System.Security.Cryptography.HMACSHA256.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] key, byte[] value)
		{
			var algorithm = System.Security.Cryptography.HMACSHA256.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

	}
}
