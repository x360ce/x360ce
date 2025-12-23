using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Provides utilities for managing application settings, including file comparisons,
	/// data compression/decompression, and content transformation.
	/// </summary>
	public static partial class SettingsHelper
	{
		#region Compression

		/// <summary>
		/// Compresses the given byte array using GZip compression.
		/// </summary>
		/// <param name="bytes">The byte array to compress.</param>
		/// <returns>The compressed byte array.</returns>
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

		/// <summary>
		/// Decompresses a previously compressed byte array using GZip.
		/// </summary>
		/// <param name="bytes">The compressed byte array to decompress.</param>
		/// <returns>The original byte array.</returns>
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

		/// <summary>
		/// Determines if the file specified by the path is different based on its size or content checksum compared to the provided byte array.
		/// </summary>
		/// <param name="path">The path to the file to compare.</param>
		/// <param name="bytes">The byte array to compare against the file's contents.</param>
		/// <returns>True if the file is considered different; otherwise, false.</returns>
		public static bool IsDifferent(string path, byte[] bytes)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			var fileInfo = new FileInfo(path);
			// If the file does not exist or the size is different, then it is considered different.
			if (!fileInfo.Exists || fileInfo.Length != bytes.Length)
				return true;
			// Compare checksums.
			using (var algorithm = System.Security.Cryptography.SHA256.Create())
			{
				var byteHash = algorithm.ComputeHash(bytes);
				var fileBytes = File.ReadAllBytes(fileInfo.FullName);
				var fileHash = algorithm.ComputeHash(fileBytes);
				var isDifferent = !byteHash.SequenceEqual(fileHash);
				return isDifferent;
			}
		}

		/// <summary>
		/// Writes the specified byte array to a file at the given path if the current content of the file is different from the byte array. This comparison takes into account file size and checksum.
		/// </summary>
		/// <param name="path">The path where the file will be written.</param>
		/// <param name="bytes">The byte array to write.</param>
		/// <returns>True if the file was written; false if the contents were the same and no write occurred.</returns>
		public static bool WriteIfDifferent(string path, byte[] bytes)
		{
			var isDifferent = IsDifferent(path, bytes);
			if (isDifferent)
				File.WriteAllBytes(path, bytes);
			return isDifferent;
		}

		/// <summary>
		/// Reads the content of a file into a string using the detected encoding of the file.
		/// </summary>
		/// <param name="name">The path to the file to read.</param>
		/// <param name="encoding">The encoding used to read the file. Detected automatically if left null.</param>
		/// <returns>The content of the file as a string.</returns>
		public static string ReadFileContent(string name, out Encoding encoding)
		{
			using (var reader = new System.IO.StreamReader(name, true))
			{
				encoding = reader.CurrentEncoding;
				return reader.ReadToEnd();
			}
		}

		/// <summary>
		/// Converts a string content into a byte array with an optional encoding header.
		/// </summary>
		/// <param name="content">The string content to convert.</param>
		/// <param name="encoding">The encoding to use for the byte array. If null, the default encoding is used.</param>
		/// <returns>The byte array representation of the content, including the encoding header if specified.</returns>
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


		/// <summary>
		/// Saves the byte array to a file and appends a CRC32 checksum to the filename for integrity verification.
		/// This ensures that file contents are not tampered with and remain consistent between operations.
		/// </summary>
		/// <param name="name">The name of the file to save, without the checksum.</param>
		/// <param name="bytes">The byte array containing the data to be saved to the file.</param>
		/// <returns>FileInfo object representing the saved file, including its checksum in the filename.</returns>
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


		/// <summary>
		/// Calculates a CRC32 checksum for the given byte array.
		/// </summary>
		/// <param name="bytes">The byte array to calculate the checksum for.</param>
		/// <returns>The calculated CRC32 checksum as an unsigned 32-bit integer.</returns>
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
