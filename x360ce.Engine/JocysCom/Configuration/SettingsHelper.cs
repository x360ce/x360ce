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
		/// Returns true if the file at the specified path differs from the provided byte array (by existence, size, or SHA-256 checksum); otherwise false.
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
		/// Writes the byte array to the specified path only if its content differs (comparing size and SHA-256 checksum).
		/// To avoid truncating the file on disk-full or other IO errors, writes to a temporary file first and then renames it.
		/// </summary>
		/// <param name="path">The path where the file will be written.</param>
		/// <param name="bytes">The byte array to write.</param>
		/// <returns>True if the file was written; false if the contents were the same and no write occurred.</returns>
		public static bool WriteIfDifferent(string path, byte[] bytes)
		{
			if (!IsDifferent(path, bytes))
				return false;
			if (IsEnoughSpaceAvailable(path, bytes.Length))
			{
				File.WriteAllBytes(path, bytes);
				return true;
			}
			// Generate a temporary filename in the same directory for an atomic-like replacement.
			// Writing to the same directory helps avoid cross-volume moves.
			var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Directory not found.");
			var tempFileName = Path.Combine(directory, Path.GetRandomFileName());
			try
			{
				// Write all bytes to the temporary file first.
				File.WriteAllBytes(tempFileName, bytes);
				// If we have .NET 6 or later, we could do: File.Move(tempFileName, path, overwrite: true);
				// Otherwise, we can delete the existing file (if any) and then rename the temp file.
				if (File.Exists(path))
					File.Delete(path);
				// Rename the temp file to the final path (nearly atomic on Windows).
				File.Move(tempFileName, path);
				return true;
			}
			catch
			{
				// Clean up the temp file if something goes wrong.
				if (File.Exists(tempFileName))
					File.Delete(tempFileName);
				// Rethrow the exception or handle it as needed.
				throw;
			}
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

		/// <summary>
		/// Determines if the drive containing the specified path has enough free space to accommodate required bytes plus a buffer (10 MB or 5% of required size).
		/// </summary>
		/// <param name="path">The file path whose drive to check.</param>
		/// <param name="requiredBytes">The number of bytes intended to be written.</param>
		/// <returns>True if available free space exceeds requiredBytes plus buffer; otherwise, false.</returns>
		public static bool IsEnoughSpaceAvailable(string path, long requiredBytes)
		{
			// Convert a relative path to an absolute path
			var fullPath = Path.GetFullPath(path);
			// Extract the drive from the full path
			var driveRoot = Path.GetPathRoot(fullPath);
			if (string.IsNullOrEmpty(driveRoot))
				return false;
			var drive = new DriveInfo(driveRoot);
			// A rule of thumb buffer: 10 MB or 5% of file size, whichever is greater.
			long buffer = Math.Max(10 * 1024 * 1024, (long)(requiredBytes * 0.05));
			long totalNeeded = requiredBytes + buffer;
			return drive.AvailableFreeSpace > totalNeeded;
		}

		#endregion

		#region Saving 

		/// <summary>
		/// Saves the byte array to a file and appends a CRC32 (Cyclic Redundancy Check) checksum to the filename for integrity verification.
		/// Ensures file contents are not tampered with and remain consistent across operations.
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
		/// Calculates the 32-bit Cyclic Redundancy Check (CRC32) checksum for the given byte array.
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