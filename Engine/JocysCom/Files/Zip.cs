using System.IO;
using System.IO.Compression;

namespace JocysCom.ClassLibrary.Files
{

	public static class Zip
	{

		private const int bufSize = 4096;

		private static void CopyStream(Stream source, Stream target)
		{
			byte[] buf = new byte[bufSize];
			int bytesRead = 0;
			while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
				target.Write(buf, 0, bytesRead);
		}

		public static void UnGZipFile(string zipFileName, string fileName)
		{
			using (FileStream inStream = new FileStream(zipFileName, FileMode.Open, FileAccess.Read))
			using (FileStream outStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
			using (Stream source = new GZipStream(inStream, CompressionMode.Decompress, true))
				CopyStream(source, outStream);
		}

		public static void GZipFile(string fileName)
		{
			using (var inStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			using (var outStream = new FileStream(fileName + ".gz", FileMode.OpenOrCreate, FileAccess.Write))
			using (Stream destination = new GZipStream(outStream, CompressionMode.Compress, true))
				CopyStream(inStream, destination);
		}

		/// <summary>
		/// Compress file into the ZIP archive.
		/// </summary>
		/// <param name="fileName">The name of the file to compress.</param>
		/// <param name="zipFileName">A relative or absolute path of the ZIP file.</param>
		/// <param name="filenameInZip">New name of the file inside the ZIP.</param>
		public static void ZipFile(string fileName, string zipFileName = null, string fileNameInZip = null)
		{
			// If archive file name is not specified.
			if (string.IsNullOrEmpty(zipFileName))
				zipFileName = fileName + ".zip";
			// If file name inside archive is not specified.
			if (string.IsNullOrEmpty(fileNameInZip))
				fileNameInZip = System.IO.Path.GetFileName(fileName);
			// Create zip file for writing.
			var zip = ZipStorer.Create(zipFileName, string.Empty);
			// Add file to the zip.
			zip.AddFile(ZipStorer.Compression.Store, fileName, fileNameInZip, string.Empty);
			zip.Close();
		}

		/// <summary>
		/// Extract file from the ZIP archive.
		/// </summary>
		/// <param name="zipFileName">A relative or absolute path of the ZIP file.</param>
		/// <param name="fileNameInZip">File name inside the ZIP to extract.</param>
		/// <param name="fileName">The name of the new file to extract to (Optional).</param>
		public static bool UnZipFile(string zipFileName, string fileNameInZip = null, string fileName = null)
		{
			// Open an existing zip file for reading.
			var zip = ZipStorer.Open(zipFileName, FileAccess.Read);
			// Read the central directory collection
			var dir = zip.ReadCentralDir();
			var success = false;
			// Look for the desired file.
			foreach (ZipStorer.ZipFileEntry entry in dir)
			{
				if (string.IsNullOrEmpty(fileNameInZip) || string.Compare(entry.FilenameInZip, fileNameInZip, true) == 0)
				{
					var outName = string.IsNullOrEmpty(fileName)
						? entry.FilenameInZip
						: fileName;
					// Create file info to get rooted path.
					var fi = new FileInfo(outName);
					// File found, extract it
					zip.ExtractFile(entry, fi.FullName);
					success = true;
					break;
				}
			}
			zip.Close();
			return success;
		}

		/// <summary>
		/// Compress files into the ZIP archive.
		/// </summary>
		/// <param name="sourceFolder">The name of the file to compress.</param>
		/// <param name="zipFileName">A relative or absolute path of the ZIP file.</param>
		public static void ZipFiles(string sourceFolder, string zipFileName)
		{
			// Create zip file for writing.
			var zip = ZipStorer.Create(zipFileName, string.Empty);
			var di = new DirectoryInfo(sourceFolder);
			var files = di.GetFiles("*.*", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				var fileNameInZip = file.FullName.Substring(di.FullName.Length);
				// Add file to the zip.
				zip.AddFile(ZipStorer.Compression.Store, file.FullName, fileNameInZip, string.Empty);
			}
			zip.Close();
		}


		/// <summary>
		/// Extract files from the ZIP archive.
		/// </summary>
		/// <param name="zipFileName">A relative or absolute path of the ZIP file.</param>
		/// <param name="destinationFolder">Destination folder.</param>
		public static bool UnZipFiles(string zipFileName, string destinationFolder)
		{
			// Open an existing zip file for reading.
			var zip = ZipStorer.Open(zipFileName, FileAccess.Read);
			// Read the central directory collection
			var dir = zip.ReadCentralDir();
			var success = false;
			// Look for the desired file.
			foreach (ZipStorer.ZipFileEntry entry in dir)
			{
				var fileName = System.IO.Path.Combine(destinationFolder, entry.FilenameInZip);
				zip.ExtractFile(entry, fileName);
				success = true;
			}
			zip.Close();
			return success;
		}

	}
}
