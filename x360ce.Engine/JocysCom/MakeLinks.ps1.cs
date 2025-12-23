using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

public class MakeLinks {

	/// <summary>
	/// Returns 'true' if both files are hard linked.
	/// </summary>
	public static bool IsHardLinked(string file1, string file2) {
		return GetFileId(file1) == GetFileId(file2);
	}

	/// <summary>
	/// Update creation, last write, and last access time of the link file to match the target file.
	/// </summary>
	public static void UpdateLinkTimeFromFile(string link, string file) {
		var fi = new FileInfo(file);
		UpdateLinkTime(link, fi.CreationTime, fi.LastWriteTime, fi.LastAccessTime);
	}


	#region Native Methods

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
	/// </summary>
	[DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
	static extern SafeFileHandle CreateFile(
		string lpFileName,
		int dwDesiredAccess,
		FileShare dwShareMode,
		uint lpSecurityAttributes,
		FileMode dwCreationDisposition,
		int dwFlagsAndAttributes,
		IntPtr hTemplateFile
	);

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle
	/// </summary>
	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	static extern bool SetFileInformationByHandle(
		SafeFileHandle hFile,
		int FileInformationClass,
		ref FILE_BASIC_INFO lpFileInformation,
		uint dwBufferSize
	);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool GetFileInformationByHandle(
		SafeFileHandle hFile,
		out BY_HANDLE_FILE_INFORMATION lpFileInformation);

	struct BY_HANDLE_FILE_INFORMATION {
		public uint FileAttributes;
		public ComTypes.FILETIME CreationTime;
		public ComTypes.FILETIME LastAccessTime;
		public ComTypes.FILETIME LastWriteTime;
		public uint VolumeSerialNumber;
		public uint FileSizeHigh;
		public uint FileSizeLow;
		public uint NumberOfLinks;
		public uint FileIndexHigh;
		public uint FileIndexLow;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct FILE_BASIC_INFO {
		public long CreationTime;
		public long LastAccessTime;
		public long LastWriteTime;
		public long ChangeTime;
		public uint FileAttributes;
	}

	const int FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
	const int GENERIC_WRITE = 0x40000000;
	const int FileBasicInfo = 0;

	#endregion

	#region Helper functions

	static ulong GetFileId(string fileName) {
		BY_HANDLE_FILE_INFORMATION objectFileInfo;
		var fi = new FileInfo(fileName);
		var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		GetFileInformationByHandle(fs.SafeFileHandle, out objectFileInfo);
		fs.Close();
		var fileIndex = ((ulong)objectFileInfo.FileIndexHigh << 32) + (ulong)objectFileInfo.FileIndexLow;
		return fileIndex;
	}

	static void UpdateLinkTime(string link, DateTime created, DateTime updated, DateTime accessed) {
		SafeFileHandle handle = CreateFile(
			link,
			GENERIC_WRITE,
			FileShare.ReadWrite | FileShare.Delete,
			0,
			FileMode.Open,
			FILE_FLAG_OPEN_REPARSE_POINT,
			IntPtr.Zero
		);
		var basicInfo = new FILE_BASIC_INFO()
		{
			CreationTime = created.ToFileTime(),
			LastAccessTime = accessed.ToFileTime(),
			LastWriteTime = updated.ToFileTime(),
			ChangeTime = updated.ToFileTime(),
			FileAttributes = 0
		};
		if (!SetFileInformationByHandle(handle, FileBasicInfo, ref basicInfo, (uint)Marshal.SizeOf<FILE_BASIC_INFO>()))
			throw new Win32Exception(Marshal.GetLastWin32Error());
	}

	#endregion

}
