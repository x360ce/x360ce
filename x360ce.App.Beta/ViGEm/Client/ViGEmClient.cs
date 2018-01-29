using System;
using Nefarius.ViGEm.Client.Exceptions;

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
			Init_x360ce();
			NativeHandle = vigem_alloc();
            var error = vigem_connect(NativeHandle);

            switch (error)
            {
                case VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED:
                    throw new VigemAlreadyConnectedException();
                case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                    throw new VigemBusNotFoundException();
                case VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED:
                    throw new VigemBusAccessFailedException();
                case VIGEM_ERROR.VIGEM_ERROR_BUS_VERSION_MISMATCH:
                    throw new VigemBusVersionMismatchException();
            }
        }

        /// <summary>
        ///     Gets the <see cref="PVIGEM_CLIENT"/> identifying the bus connection.
        /// </summary>
        internal PVIGEM_CLIENT NativeHandle { get; }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                vigem_disconnect(NativeHandle);
                vigem_free(NativeHandle);

                disposedValue = true;
            }
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
