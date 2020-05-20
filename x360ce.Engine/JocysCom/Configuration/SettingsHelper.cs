using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace JocysCom.ClassLibrary.Configuration
{
	public static partial class SettingsHelper
	{
		#region Compression

		public static byte[] Compress(byte[] bytes)
		{
			int numRead;
			var srcStream = new MemoryStream(bytes);
			var dstStream = new MemoryStream();
			srcStream.Position = 0;
			var stream = new GZipStream(dstStream, CompressionMode.Compress);
			byte[] buffer = new byte[0x1000];
			while (true)
			{
				numRead = srcStream.Read(buffer, 0, buffer.Length);
				if (numRead == 0) break;
				stream.Write(buffer, 0, numRead);
			}
			stream.Close();
			srcStream.Close();
			return dstStream.ToArray();
		}

		public static byte[] Decompress(byte[] bytes)
		{
			int numRead;
			var srcStream = new MemoryStream(bytes);
			var dstStream = new MemoryStream();
			srcStream.Position = 0;
			var stream = new GZipStream(srcStream, CompressionMode.Decompress);
			var buffer = new byte[0x1000];
			while (true)
			{
				numRead = stream.Read(buffer, 0, buffer.Length);
				if (numRead == 0) break;
				dstStream.Write(buffer, 0, numRead);
			}
			dstStream.Close();
			stream.Close();
			return dstStream.ToArray();
		}

		#endregion

		#region Writing

		public static bool IsDifferent(string name, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			var fi = new FileInfo(name);
			var isDifferent = false;
			// If file doesn't exists or file size is different then...
			if (!fi.Exists || fi.Length != bytes.Length)
			{
				isDifferent = true;
			}
			else
			{
				// Compare checksums.
				var byteHash = Security.SHA256Helper.GetGuid(bytes);
				var fileHash = Security.SHA256Helper.GetGuidFromFile(fi.FullName);
				isDifferent = !byteHash.Equals(fileHash);
			}
			return isDifferent;
		}

		public static bool WriteIfDifferent(string name, byte[] bytes)
		{
			var isDifferent = IsDifferent(name, bytes);
			if (isDifferent)
				File.WriteAllBytes(name, bytes);
			return isDifferent;
		}

		public static string ReadFileContent(string name, out Encoding encoding)
		{
			using (var reader = new System.IO.StreamReader(name, true))
			{
				encoding = reader.CurrentEncoding;
				return reader.ReadToEnd();
			}
		}

		/// <summary>
		/// Get file content with encoding header.
		/// </summary>
		public static byte[] GetFileContentBytes(string content, Encoding encoding = null)
		{
			var ms = new MemoryStream();
			// Encoding header will be added to content.
			var sw = new StreamWriter(ms, encoding);
			sw.Write(content);
			sw.Flush();
			var bytes = ms.ToArray();
			sw.Dispose();
			return bytes;
		}

		#endregion


	}
}
