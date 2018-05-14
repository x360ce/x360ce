using System.IO;
using System.Text;

using System.IO.Compression;

namespace JocysCom.ClassLibrary.Configuration
{
	public class SettingsHelper
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
			return dstStream.ToArray();
		}

		#endregion

		#region Writing

		public static bool IsDifferent(string name, byte[] bytes)
		{
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
				var byteHash = Security.MD5Helper.GetGuid(bytes);
				var fileHash = Security.MD5Helper.GetGuidFromFile(fi.FullName);
				isDifferent = !byteHash.Equals(fileHash);
			}
			return isDifferent;
		}

		public static void WriteIfDifferent(string name, byte[] bytes)
		{
			var isDifferent = IsDifferent(name, bytes);
			if (isDifferent)
			{
				File.WriteAllBytes(name, bytes);
			}
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
