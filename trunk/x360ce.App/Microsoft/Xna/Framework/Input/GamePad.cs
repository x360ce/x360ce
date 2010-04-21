namespace Microsoft.Xna.Framework.Input
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Diagnostics;

    public static class GamePad
    {
        private static bool[] _disconnected = new bool[4];
        private static long[] _lastReadTime = new long[4];
        internal const string XinputNativeDll = "xinput1_3.dll";

        public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
        {
            XINPUT_CAPABILITIES pCaps = new XINPUT_CAPABILITIES();
            ErrorCodes success = ErrorCodes.Success;
            if (ThrottleDisconnectedRetries(playerIndex))
            {
                success = ErrorCodes.NotConnected;
            }
            else
            {
                success = UnsafeNativeMethods.GetCaps(playerIndex, 1, out pCaps);
                ResetThrottleState(playerIndex, success);
            }
            if ((success != ErrorCodes.Success) && (success != ErrorCodes.NotConnected))
            {
                throw new InvalidOperationException(FrameworkResources.InvalidController);
            }
            return new GamePadCapabilities(ref pCaps, success);
        }

        public static GamePadState GetState(PlayerIndex playerIndex)
        {
            return GetState(playerIndex, GamePadDeadZone.IndependentAxes);
        }

        public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
        {
            XINPUT_STATE pState = new XINPUT_STATE();
            ErrorCodes success = ErrorCodes.Success;
            if (ThrottleDisconnectedRetries(playerIndex))
            {
                success = ErrorCodes.NotConnected;
            }
            else
            {
                success = UnsafeNativeMethods.GetState(playerIndex, out pState);
                ResetThrottleState(playerIndex, success);
            }
            if ((success != ErrorCodes.Success) && (success != ErrorCodes.NotConnected))
            {
                throw new InvalidOperationException(FrameworkResources.InvalidController);
            }
            return new GamePadState(ref pState, success, deadZoneMode);
        }

        private static void ResetThrottleState(PlayerIndex playerIndex, ErrorCodes result)
        {
            if ((playerIndex >= PlayerIndex.One) && (playerIndex <= PlayerIndex.Four))
            {
                if (result == ErrorCodes.NotConnected)
                {
                    _disconnected[(int) playerIndex] = true;
                    _lastReadTime[(int) playerIndex] = Stopwatch.GetTimestamp();
                }
                else
                {
                    _disconnected[(int) playerIndex] = false;
                }
            }
        }

        public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            XINPUT_VIBRATION xinput_vibration;
            xinput_vibration.LeftMotorSpeed = (short) (leftMotor * 65535f);
            xinput_vibration.RightMotorSpeed = (short) (rightMotor * 65535f);
            ErrorCodes success = ErrorCodes.Success;
            if (ThrottleDisconnectedRetries(playerIndex))
            {
                success = ErrorCodes.NotConnected;
            }
            else
            {
                success = UnsafeNativeMethods.SetState(playerIndex, ref xinput_vibration);
                ResetThrottleState(playerIndex, success);
            }
            if (success == ErrorCodes.Success)
            {
                return true;
            }
            if (((success != ErrorCodes.Success) && (success != ErrorCodes.NotConnected)) && (success != ErrorCodes.Busy))
            {
                throw new InvalidOperationException(FrameworkResources.InvalidController);
            }
            return false;
        }

        private static bool ThrottleDisconnectedRetries(PlayerIndex playerIndex)
        {
            if (((playerIndex >= PlayerIndex.One) && (playerIndex <= PlayerIndex.Four)) && _disconnected[(int) playerIndex])
            {
                long timestamp = Stopwatch.GetTimestamp();
                for (int i = 0; i < 4; i++)
                {
                    if (_disconnected[i])
                    {
                        long num3 = timestamp - _lastReadTime[i];
                        long frequency = Stopwatch.Frequency;
                        if ((PlayerIndex)i != playerIndex)
                        {
                            frequency /= 4L;
                        }
                        if ((num3 >= 0L) && (num3 <= frequency))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}

