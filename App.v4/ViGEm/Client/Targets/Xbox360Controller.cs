using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace Nefarius.ViGEm.Client.Targets
{
    /// <inheritdoc />
    /// <summary>
    ///     Represents an emulated wired Microsoft Xbox 360 Controller.
    /// </summary>
    public class Xbox360Controller : ViGEmTarget
    {
        private ViGEmClient.PVIGEM_X360_NOTIFICATION _notificationCallback;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Nefarius.ViGEm.Client.Targets.Xbox360Controller" /> class bound to a
        ///     <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" />.
        /// </summary>
        /// <param name="client">The <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> this device is attached to.</param>
        public Xbox360Controller(ViGEmClient client) : base(client)
        {
            NativeHandle = ViGEmClient.NativeMethods.vigem_target_x360_alloc();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Nefarius.ViGEm.Client.Targets.Xbox360Controller" /> class bound to a
        ///     <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> overriding the default Vendor and Product IDs with the provided
        ///     values.
        /// </summary>
        /// <param name="client">The <see cref="T:Nefarius.ViGEm.Client.ViGEmClient" /> this device is attached to.</param>
        /// <param name="vendorId">The Vendor ID to use.</param>
        /// <param name="productId">The Product ID to use.</param>
        public Xbox360Controller(ViGEmClient client, ushort vendorId, ushort productId) : this(client)
        {
            VendorId = vendorId;
            ProductId = productId;
        }

        /// <summary>
        ///     Submits a state to this device, and says what the bus answered.
        /// </summary>
        /// <param name="report">The state to submit.</param>
        /// <returns>The bus's answer; <see cref="BusAnswers.Of"/> says what it means.</returns>
        /// <remarks>
        ///     Handed back rather than thrown. The engine sends up to a thousand of these a second a
        ///     controller, and an answer is a number to compare where an exception is an object to make
        ///     and unwind. Every answer comes back, so none is taken as success by being left off a list.
        /// </remarks>
        public VIGEM_ERROR SendReport(XUSB_REPORT report)
        {
            return ViGEmClient.NativeMethods.vigem_target_x360_update(Client.NativeHandle, NativeHandle, report);
        }

        /// <summary>
        ///     What the bus answered when this controller was registered for vibration: VIGEM_ERROR_NONE when it
        ///     takes vibration. <see cref="BusAnswers.Of"/> says what any other answer means.
        /// </summary>
        /// <remarks>
        ///     Set by each <see cref="Connect"/>. A refused registration leaves the controller on the bus, and the
        ///     game's buttons, sticks and triggers reach it; only the game's vibration does not.
        /// </remarks>
        public VIGEM_ERROR RumbleError { get; private set; } = VIGEM_ERROR.VIGEM_ERROR_NONE;

        /// <summary>
        ///     Brings this controller online, then registers it for vibration.
        /// </summary>
        /// <exception cref="ViGEmException">The bus refused the controller itself, and nothing is on the bus.</exception>
        /// <remarks>
        ///     Only the add throws. Once it has worked the controller is on the bus, so the registration's answer
        ///     is kept in <see cref="RumbleError"/> rather than thrown. Thrown, it would read as a failed plug of a
        ///     controller that is there: never remembered as ours, plugged again on top of itself, and holding
        ///     its place.
        /// </remarks>
        public override void Connect()
        {
            base.Connect();

            //
            // Callback to event
            // 
            _notificationCallback = (client, target, largeMotor, smallMotor, number) => FeedbackReceived?.Invoke(this,
                new Xbox360FeedbackReceivedEventArgs(largeMotor, smallMotor, number));

            RumbleError = ViGEmClient.NativeMethods.vigem_target_x360_register_notification(Client.NativeHandle, NativeHandle,
                _notificationCallback);
        }

        /// <summary>
        ///     Lets go of vibration, then takes this controller off the bus.
        /// </summary>
        /// <remarks>
        ///     Unregistering is asked for whatever the registration answered. It answers nothing, so it cannot stop
        ///     the removal after it; after a refused registration there is nothing registered for it to let go of.
        /// </remarks>
        public override void Disconnect()
        {
            ViGEmClient.NativeMethods.vigem_target_x360_unregister_notification(NativeHandle);

            base.Disconnect();
        }

        public event Xbox360FeedbackReceivedEventHandler FeedbackReceived;
    }

    public delegate void Xbox360FeedbackReceivedEventHandler(object sender, Xbox360FeedbackReceivedEventArgs e);
}
