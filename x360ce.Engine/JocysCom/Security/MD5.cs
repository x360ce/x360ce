using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace JocysCom.ClassLibrary.Security
{
	public partial class MD5
	{
		public static Guid GetGuid(string value, System.Text.Encoding encoding = null)
		{
			if (encoding == null) encoding = System.Text.Encoding.Default;
			byte[] bytes = encoding.GetBytes(value);
			return GetGuid(bytes);
		}

		public static Guid GetGuid(string key, long value)
		{
			return GetGuid(BitConverter.GetBytes(value));
		}

		public static Guid GetGuid(byte[] bytes)
		{
			var md5 = System.Security.Cryptography.MD5.Create();
			Guid guid = new Guid(md5.ComputeHash(bytes));
			return guid;
		}

		public Guid GetGuidFromFile(string path)
		{
			byte[] buffer;
			byte[] oldBuffer;
			int bytesRead;
			int oldBytesRead;
			long size;
			long totalBytesRead = 0;
			int _progress = -1;
			using (var stream = System.IO.File.OpenRead(path))
			using (var md5 = System.Security.Cryptography.MD5.Create())
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
						md5.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
					}
					else
					{
						md5.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
					}
					var progress = (int)((double)totalBytesRead * 100 / size);
					var progressHandler = ProgressChanged;
					if (_progress != progress && progressHandler != null)
					{
						_progress = progress;
						progressHandler(this, new ProgressChangedEventArgs(progress, null));
					}
				} while (bytesRead != 0);
				var hash = new Guid(md5.Hash);
				var completeHandler = RunWorkerCompleted;
				if (completeHandler != null) completeHandler(this, new RunWorkerCompletedEventArgs(hash, null, false));
				return hash;
			}
		}

		public event RunWorkerCompletedEventHandler RunWorkerCompleted;
		public event ProgressChangedEventHandler ProgressChanged;

	}
}
