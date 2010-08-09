using System;
using System.Collections.Generic;
using System.Text;

namespace x360ce.App
{
        public enum FakeDi
        {
            /// <summary>
            /// Disabled.
            /// </summary>
            Disabled = 0,
            /// <summary>
            ///  Enabled: Callback.
            /// </summary>
            EnabledCallback = 1,
            /// <summary>
            /// Enabled: Callback + DevInfo.
            /// </summary>
            EnabledCallbackAndDevInfo = 2,
			// Block all except Keyboard and Mouse.
			BlockAllExceptKbdAndMouse = 3
		}
}
