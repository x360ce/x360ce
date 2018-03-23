using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SP_DEVINSTALL_PARAMS
    {
        public int cbSize;
        public int Flags;
        public int FlagsEx;
        public IntPtr HwndParent;
        public IntPtr InstallMsgHandler;
        public IntPtr InstallMsgHandlerContext;
        public IntPtr FileQueue;
        public ulong CallInstallReserved;
        public int Reserved;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DriverPath;
        public void Initialize()
        {
            cbSize = Marshal.SizeOf(typeof(SP_DEVINSTALL_PARAMS));
        }
    }

}
