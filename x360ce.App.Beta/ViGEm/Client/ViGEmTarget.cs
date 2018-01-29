using System;
using Nefarius.ViGEm.Client.Exceptions;

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
                ViGEmClient.vigem_target_set_vid(NativeHandle, VendorId);
                ViGEmClient.vigem_target_set_pid(NativeHandle, ProductId);
            }

            var error = ViGEmClient.vigem_target_add(Client.NativeHandle, NativeHandle);

            switch (error)
            {
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                    throw new VigemBusNotFoundException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_TARGET_UNINITIALIZED:
                    throw new VigemTargetUninitializedException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED:
                    throw new VigemAlreadyConnectedException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT:
                    throw new VigemNoFreeSlotException();
            }
        }

        /// <summary>
        ///     Takes this device offline by removing it from the bus.
        /// </summary>
        public virtual void Disconnect()
        {
            var error = ViGEmClient.vigem_target_remove(Client.NativeHandle, NativeHandle);

            switch (error)
            {
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                    throw new VigemBusNotFoundException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_TARGET_UNINITIALIZED:
                    throw new VigemTargetUninitializedException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN:
                    throw new VigemTargetNotPluggedInException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED:
                    throw new VigemRemovalFailedException();
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

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                ViGEmClient.vigem_target_free(NativeHandle);

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