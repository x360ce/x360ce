using System.Runtime.InteropServices;

namespace SharpDX.XInput
{
	internal partial class XInput910 : IXInput
	{
		/// <summary>Reloads settings from INI file.</summary>
		public int Reset()
		{
			var result = (Native.Reset());
			return result;
		}

		private static partial class Native
		{
			[DllImport("xinput9_1_0.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "Reset")]
			public static extern int Reset();

		}
	}
}
