using System;
using System.Collections.Generic;
using System.Text;

namespace x360ce.App.XnaInput
{
    public enum FakeWmi
    {
        /// <summary>
        /// Disabled
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// Enabled: USB
        /// </summary>
        EnabledUsb = 1,
        /// <summary>
        /// Enabled: USB + HID
        /// </summary>
        EnabledUsbAndHid = 2
    }
}
