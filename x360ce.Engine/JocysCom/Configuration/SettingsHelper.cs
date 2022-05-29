using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
				var algorithm = System.Security.Cryptography.SHA256.Create();
				var byteHash = algorithm.ComputeHash(bytes);
				var fileBytes = File.ReadAllBytes(fi.FullName);
				var fileHash = algorithm.ComputeHash(fileBytes);
				isDifferent = !byteHash.SequenceEqual(fileHash);
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

		#region Saving 


		public static FileInfo SaveFileWithChecksum(string name, byte[] bytes)
		{
			var assembly = Assembly.GetEntryAssembly();
			var company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute))).Company;
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			// Get writable application folder.
			var specialFolder = Environment.SpecialFolder.CommonApplicationData;
			var folder = string.Format("{0}\\{1}\\{2}", Environment.GetFolderPath(specialFolder), company, product);
			var hash = ComputeCRC32Checksum(bytes);
			// Put file into sub folder because file name must match with LoadLibrary() argument. 
			var chName = string.Format("{0}.{1:X8}\\{0}", name, hash);
			var fileName = System.IO.Path.Combine(folder, "Temp", chName);
			var fi = new FileInfo(fileName);
			if (fi.Exists)
				return fi;
			if (!fi.Directory.Exists)
				fi.Directory.Create();
			File.WriteAllBytes(fileName, bytes);
			fi.Refresh();
			return fi;
		}

		public static uint ComputeCRC32Checksum(byte[] bytes)
		{
			uint poly = 0xedb88320;
			uint[] table = new uint[256];
			uint temp;
			for (uint i = 0; i < table.Length; ++i)
			{
				temp = i;
				for (int j = 8; j > 0; --j)
					temp = (temp & 1) == 1 ? (temp >> 1) ^ poly : temp >> 1;
				table[i] = temp;
			}
			uint crc = 0xffffffff;
			for (int i = 0; i < bytes.Length; ++i)
				crc = (crc >> 8) ^ table[(byte)(((crc) & 0xff) ^ bytes[i])];
			return ~crc;
		}

		#endregion


	}
}
