using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct SP_DRVINFO_DATA
    {
        public int cbSize;
        public uint DriverType;
        public UIntPtr Reserved;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string MfgName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ProviderName;
        public System.Runtime.InteropServices.ComTypes.FILETIME DriverDate;
        public ulong DriverVersion;
        public void Initialize()
        {
            cbSize = Marshal.SizeOf(typeof(SP_DRVINFO_DATA));
        }
    }
}
