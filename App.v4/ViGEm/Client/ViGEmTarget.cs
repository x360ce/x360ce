using System;

namespace Nefarius.ViGEm.Client
{
    using PVIGEM_TARGET = IntPtr;

    /// <summary>
    ///     Provides a managed wrapper around a generic emulation target.
    /// </summary>
    public abstract class ViGEmTarget : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ViGEmTarget"/> bound to a <see cref="ViGEmClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="ViGEmClient"/> this device is attached to.</param>
        protected ViGEmTarget(ViGEmClient client)
        {
            Client = client;
        }

        /// <summary>
        ///     Gets the <see cref="ViGEmClient"/> this <see cref="ViGEmTarget"/> is bound to.
        /// </summary>
        protected ViGEmClient Client { get; }

        protected PVIGEM_TARGET NativeHandle { get; set; }

        /// <summary>
        /// The number the bus knows this controller by, which also appears in its name in Windows.
        /// </summary>
        /// <remarks>
        /// This is the clear reference to a controller this program created. The bus does not record
        /// which program created what, and Windows has no field saying so either, so without this
        /// there is no way to tell this program's own controller from one left behind by a run that
        /// died. Getting that wrong means offering to remove the controller currently in use.
        /// </remarks>
        public uint Serial
        {
            get
            {
                return NativeHandle == IntPtr.Zero
                    ? 0
                    : ViGEmClient.NativeMethods.vigem_target_get_index(NativeHandle);
            }
        }

        /// <summary>
        ///     Gets the Vendor ID this device will present to the system.
        /// </summary>
        public ushort VendorId { get; protected set; }

        /// <summary>
        ///     Gets the Product ID this device will present to the system.
        /// </summary>
        public ushort ProductId { get; protected set; }

        /// <summary>
        ///     Brings this device online by attaching it to the bus.
        /// </summary>
        public virtual void Connect()
        {
            if (VendorId > 0 && ProductId > 0)
            {
                ViGEmClient.NativeMethods.vigem_target_set_vid(NativeHandle, VendorId);
                ViGEmClient.NativeMethods.vigem_target_set_pid(NativeHandle, ProductId);
            }

            var error = ViGEmClient.NativeMethods.vigem_target_add(Client.NativeHandle, NativeHandle);
            switch (error)
            {
                case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                case VIGEM_ERROR.VIGEM_ERROR_TARGET_UNINITIALIZED:
                case VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED:
                case VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT:
                    throw new ViGEmException(error);
            }
        }

        /// <summary>
        ///     Takes this device offline by removing it from the bus.
        /// </summary>
        public virtual void Disconnect()
        {
            var error = ViGEmClient.NativeMethods.vigem_target_remove(Client.NativeHandle, NativeHandle);
            switch (error)
            {
                case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                case VIGEM_ERROR.VIGEM_ERROR_TARGET_UNINITIALIZED:
                case VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN:
                case VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED:
					throw new ViGEmException(error);
			}
		}

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    try { Disconnect(); } catch { }

                // The driver library can be gone before this runs. The application is often
                // started from a temporary folder that is cleaned while it is still open, and
                // the finalizer runs later still. A library that cannot be loaded means there is
                // nothing left to release, and the exception would end the process, because
                // nothing catches what escapes the finalizer thread.
                try { ViGEmClient.NativeMethods.vigem_target_free(NativeHandle); }
                catch (DllNotFoundException) { }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~ViGEmTarget()
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
