using System;

namespace Nefarius.ViGEm.Client
{
    using PVIGEM_CLIENT = IntPtr;

    /// <summary>
    ///     Represents a managed gateway to a compatible emulation bus.
    /// </summary>
    public partial class ViGEmClient : IDisposable
    {
        public ViGEmClient()
        {
            NativeHandle = NativeMethods.vigem_alloc();
            var error = NativeMethods.vigem_connect(NativeHandle);
            switch (error)
            {
                case VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED:
                case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                case VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED:
                case VIGEM_ERROR.VIGEM_ERROR_BUS_VERSION_MISMATCH:
                    throw new ViGEmException(error);
            }
        }

        /// <summary>
        ///     Gets the <see cref="PVIGEM_CLIENT"/> identifying the bus connection.
        /// </summary>
        internal PVIGEM_CLIENT NativeHandle { get; }

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
