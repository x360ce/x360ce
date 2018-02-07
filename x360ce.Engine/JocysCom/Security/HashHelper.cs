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

		public static Guid GetGuidFromFile(HashAlgorithm algorithm, string path,
			object sender = null,
			ProgressChangedEventHandler progressHandler = null,
			RunWorkerCompletedEventHandler completedHandler = null
		)
		{
			byte[] buffer;
			byte[] oldBuffer;
			int bytesRead;
			int oldBytesRead;
			long size;
			long totalBytesRead = 0;
			int _progress = -1;
			using (var stream = System.IO.File.OpenRead(path))
			{
				size = stream.Length;
				buffer = new byte[4096];
				bytesRead = stream.Read(buffer, 0, buffer.Length);
				totalBytesRead += bytesRead;
				do
				{
					oldBytesRead = bytesRead;
					oldBuffer = buffer;
					buffer = new byte[4096];
					bytesRead = stream.Read(buffer, 0, buffer.Length);
					totalBytesRead += bytesRead;
					if (bytesRead == 0)
					{
						algorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
					}
					else
					{
						algorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
					}
					var progress = (int)((double)totalBytesRead * 100 / size);
					if (_progress != progress && progressHandler != null)
					{
						_progress = progress;
						progressHandler(sender, new ProgressChangedEventArgs(progress, null));
					}
				} while (bytesRead != 0);
				byte[] guidBytes = new byte[16];
				Array.Copy(algorithm.Hash, guidBytes, guidBytes.Length);
				Guid guid = new Guid(guidBytes);
				if (completedHandler != null)
					completedHandler(sender, new RunWorkerCompletedEventArgs(guid, null, false));
				return guid;
			}
		}

	}
}
