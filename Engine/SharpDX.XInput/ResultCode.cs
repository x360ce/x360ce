namespace SharpDX.XInput
{
    using SharpDX;
    using SharpDX.Win32;
    using System;

    public sealed class ResultCode
    {
        public static readonly Result NotConnected = ErrorCodeHelper.ToResult(ErrorCode.DeviceNotConnected);
    }
}

