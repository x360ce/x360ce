using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;

namespace JocysCom.ClassLibrary.Security
{

	/// <summary>
	/// UTF-8 is used as a default encoding (smaller Internet messaging size).
	/// </summary>
	public static class HashHelper
	{

		public static Guid GetGuid(KeyedHashAlgorithm algorithm, string key, string value, Encoding encoding = null)
		{
			// Important: Don’t Use Encoding.Default, because it is different on different machines and send data may be decoded as as gibberish.
			// Use UTF-8 or Unicode (UTF-16), used by SQL Server.
			if (encoding == null)
				encoding = Encoding.UTF8;
			byte[] keyBytes = encoding.GetBytes(key);
			byte[] valueBytes = encoding.GetBytes(value);
			return GetGuid(algorithm, keyBytes, valueBytes);
		}

		public static Guid GetGuid(KeyedHashAlgorithm algorithm, string key, byte[] value, Encoding encoding = null)
		{
			// Important: Don’t Use Encoding.Default, because it is different on different machines and send data may be decoded as as gibberish.
			// Use UTF-8 or Unicode (UTF-16), used by SQL Server.
			if (encoding == null)
				encoding = Encoding.UTF8;
			byte[] keyBytes = encoding.GetBytes(key);
			return GetGuid(algorithm, keyBytes, value);
		}

		public static Guid GetGuid(KeyedHashAlgorithm algorithm, byte[] key, byte[] bytes)
		{
			algorithm.Key = key;
			byte[] hash = algorithm.ComputeHash(bytes);
			byte[] guidBytes = new byte[16];
			Array.Copy(hash, guidBytes, guidBytes.Length);
			Guid guid = new Guid(guidBytes);
			return guid;
		}

		public static Guid GetGuid(HashAlgorithm algorithm, string value, Encoding encoding = null)
		{
			// Important: Don’t Use Encoding.Default, because it is different on different machines and send data may be decoded as as gibberish.
			// Use UTF-8 or Unicode (UTF-16), used by SQL Server.
			if (encoding == null)
				encoding = Encoding.UTF8;
			byte[] bytes = encoding.GetBytes(value);
			return GetGuid(algorithm, bytes);
		}

		public static Guid GetGuid(HashAlgorithm algorithm, byte[] bytes)
		{
			byte[] hash = algorithm.ComputeHash(bytes);
			byte[] guidBytes = new byte[16];
			Array.Copy(hash, guidBytes, guidBytes.Length);
			Guid guid = new Guid(guidBytes);
			return guid;
		}

		public static byte[] GetHashFromFile(HashAlgorithm algorithm, string path,
			object sender = null,
			ProgressChangedEventHandler progressHandler = null,
			RunWorkerCompletedEventHandler completedHandler = null
		)
		{
			// This method is equivalent to the FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read).
			// CWE-73: External Control of File Name or Path
			// Note: False Positive. File path is not externally controlled by the user.
			using (var stream = System.IO.File.OpenRead(path))
			{
				int _progress = -1;
				long totalBytes = stream.Length;
				long totalBytesRead = 0;
				// 4096 buffer preferable because the CPU cache can hold such amounts.
				var buffer = new byte[0x1000];
				bool done;
				int bytesRead;
				do
				{
					bytesRead = stream.Read(buffer, 0, buffer.Length);
					totalBytesRead += bytesRead;
					// True if reading of all bytes completed.
					done = totalBytesRead == totalBytes;
					// If more bytes left to read then...
					if (done)
						algorithm.TransformFinalBlock(buffer, 0, bytesRead);
					else
						algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
					var progress = (int)((double)totalBytesRead * 100 / totalBytes);
					var ev = progressHandler;
					if (_progress != progress && ev != null)
					{
						_progress = progress;
						ev(sender, new ProgressChangedEventArgs(progress, null));
					}
					// Continue if not done...
				} while (!done);
			}
			var hash = algorithm.Hash;
			var ev2 = completedHandler;
			if (ev2 != null)
				ev2(sender, new RunWorkerCompletedEventArgs(hash, null, false));
			return hash;
		}

		public static Guid GetGuidFromFile(HashAlgorithm algorithm, string path,
			object sender = null,
			ProgressChangedEventHandler progressHandler = null,
			RunWorkerCompletedEventHandler completedHandler = null
		)
		{
			var hash = GetHashFromFile(algorithm, path, sender, progressHandler, null);
			byte[] guidBytes = new byte[16];
			Array.Copy(hash, guidBytes, guidBytes.Length);
			Guid guid = new Guid(guidBytes);
			var ev2 = completedHandler;
			if (ev2 != null)
				ev2(sender, new RunWorkerCompletedEventArgs(guid, null, false));
			return guid;
		}
	}

}

