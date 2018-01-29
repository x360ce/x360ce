using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace Nefarius.ViGEm.Client.Targets
{
    /// <inheritdoc />
    /// <summary>
    ///     Represents an emulated wired Sony DualShock 4 Controller.
    /// </summary>
    public class DualShock4Controller : ViGEmTarget
    {
        private ViGEmClient.PVIGEM_DS4_NOTIFICATION _notificationCallback;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Nefarius.ViGEm.Client.Targets.DualShock4Controller" /> class bound
        ///     to a <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" />.
        /// </summary>
        /// <param name="client">The <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> this device is attached to.</param>
        public DualShock4Controller(ViGEmClient client) : base(client)
        {
            NativeHandle = ViGEmClient.vigem_target_ds4_alloc();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Nefarius.ViGEm.Client.Targets.DualShock4Controller" /> class bound
        ///     to a <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> overriding the default Vendor and Product IDs with the
        ///     provided values.
        /// </summary>
        /// <param name="client">The <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> this device is attached to.</param>
        /// <param name="vendorId">The Vendor ID to use.</param>
        /// <param name="productId">The Product ID to use.</param>
        public DualShock4Controller(ViGEmClient client, ushort vendorId, ushort productId) : this(client)
        {
            VendorId = vendorId;
            ProductId = productId;
        }

        /// <summary>
        ///     Submits an <see cref="DualShock4Report"/> to this device which will update its state.
        /// </summary>
        /// <param name="report">The <see cref="DualShock4Report"/> to submit.</param>
        public void SendReport(DualShock4Report report)
        {
            // Convert managed to unmanaged structure
            var submit = new ViGEmClient.DS4_REPORT
            {
                wButtons = report.Buttons,
                bSpecial = report.SpecialButtons,
                bThumbLX = report.LeftThumbX,
                bThumbLY = report.LeftThumbY,
                bThumbRX = report.RightThumbX,
                bThumbRY = report.RightThumbY,
                bTriggerL = report.LeftTrigger,
                bTriggerR = report.RightTrigger
            };

            var error = ViGEmClient.vigem_target_ds4_update(Client.NativeHandle, NativeHandle, submit);

            switch (error)
            {
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                    throw new VigemBusNotFoundException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET:
                    throw new VigemInvalidTargetException();
            }
        }

        public override void Connect()
        {
            base.Connect();

            //
            // Callback to event
            // 
            _notificationCallback = (client, target, motor, smallMotor, color) => FeedbackReceived?.Invoke(this,
                new DualShock4FeedbackReceivedEventArgs(motor, smallMotor,
                    new LightbarColor(color.Red, color.Green, color.Blue)));

            var error = ViGEmClient.vigem_target_ds4_register_notification(Client.NativeHandle, NativeHandle,
                _notificationCallback);

            switch (error)
            {
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
                    throw new VigemBusNotFoundException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET:
                    throw new VigemInvalidTargetException();
                case ViGEmClient.VIGEM_ERROR.VIGEM_ERROR_CALLBACK_ALREADY_REGISTERED:
                    throw new VigemCallbackAlreadyRegisteredException();
            }
        }

        public override void Disconnect()
        {
            ViGEmClient.vigem_target_ds4_unregister_notification(NativeHandle);

            base.Disconnect();
        }

        public event DualShock4FeedbackReceivedEventHandler FeedbackReceived;
    }

    public delegate void DualShock4FeedbackReceivedEventHandler(object sender, DualShock4FeedbackReceivedEventArgs e);
}