using System;

namespace Nefarius.ViGEm.Client
{
	using PVIGEM_CLIENT = IntPtr;

	/// <summary>
	///     Represents a managed gateway to a compatible emulation bus.
	/// </summary>
	public partial class ViGEmClient : IDisposable
	{
		public VIGEM_ERROR Initialize()
		{
			try
			{
				_NativeHandle = NativeMethods.vigem_alloc();
				var error = NativeMethods.vigem_connect(NativeHandle);
				//switch (error)
				//{
				//	case VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED:
				//	case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
				//	case VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED:
				//	case VIGEM_ERROR.VIGEM_ERROR_BUS_VERSION_MISMATCH:
				//		throw new ViGEmException(error);
				//}
				return error;
			}
			catch (DllNotFoundException ex)
			{
				// System.DllNotFoundException:
				// Unable to load DLL 'vigemclient.dll':
				// The specified module could not be found.
				// (Exception from HRESULT: 0x8007007E)
				if (ex.HResult == unchecked((int)0x8007007E))
				{
					// Probably "Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019" is missing.
					// You can find official download links on Microsoft Page:
					// https://support.microsoft.com/en-gb/help/2977003/the-latest-supported-visual-c-downloads
					// Direct links:
					// 32-bit: https://aka.ms/vs/16/release/vc_redist.x86.exe
					// 64-bit: https://aka.ms/vs/16/release/vc_redist.x64.exe
					// You can also find it here:
					// https://visualstudio.microsoft.com/downloads/
					// Under "Other Tools and Frameworks", "Microsoft Visual C++ Redistributable for Visual Studio 2019"
				}
				throw;
			}
			catch (Exception)
			{
				throw;
			}

		}


		/// <summary>
		///     Gets the <see cref="PVIGEM_CLIENT"/> identifying the bus connection.
		/// </summary>
		internal PVIGEM_CLIENT NativeHandle { get { return _NativeHandle; } }
		private PVIGEM_CLIENT _NativeHandle;

		#region ■ IDisposable Support

		// To detect redundant calls
		public bool IsDisposed;
		public bool Disposing;

		protected virtual void Dispose(bool disposing)
		{
			if (IsDisposed || Disposing)
				return;
			Disposing = true;
			if (disposing)
			{
				UnplugAllControllers();
			}
			NativeMethods.vigem_disconnect(NativeHandle);
			NativeMethods.vigem_free(NativeHandle);
			IsDisposed = true;
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~ViGEmClient()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
