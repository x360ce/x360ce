using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

	public partial class NativeMethods
	{
		[DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		internal static extern object CoGetObject(
		string pszName,
		[In] ref BIND_OPTS3 pBindOptions,
		[In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

	}
}
