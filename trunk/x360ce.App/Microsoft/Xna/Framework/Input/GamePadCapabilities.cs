namespace Microsoft.Xna.Framework.Input
{
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential), SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
	public struct GamePadCapabilities
	{
		private bool _connected;
		private XINPUT_CAPABILITIES _caps;
		internal GamePadCapabilities(ref XINPUT_CAPABILITIES caps, ErrorCodes result)
		{
			this._connected = result != ErrorCodes.NotConnected;
			this._caps = caps;
		}

		public GamePadType GamePadType
		{
			get
			{
				if (this._caps.Type == 3)
				{
					return (((GamePadType)(this._caps.Type << 8)) | ((GamePadType)this._caps.SubType));
				}
				return (GamePadType)this._caps.SubType;
			}
		}
		public bool IsConnected
		{
			get
			{
				return this._connected;
			}
		}
		public bool HasAButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.A)) != 0);
			}
		}
		public bool HasBackButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Back)) != 0);
			}
		}
		public bool HasBButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.B)) != 0);
			}
		}
		public bool HasDPadDownButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Down)) != 0);
			}
		}
		public bool HasDPadLeftButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Left)) != 0);
			}
		}
		public bool HasDPadRightButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Right)) != 0);
			}
		}
		public bool HasDPadUpButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Up)) != 0);
			}
		}
		public bool HasLeftShoulderButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.LeftShoulder)) != 0);
			}
		}
		public bool HasLeftStickButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.LeftThumb)) != 0);
			}
		}
		public bool HasRightShoulderButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.RightShoulder)) != 0);
			}
		}
		public bool HasRightStickButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.RightThumb)) != 0);
			}
		}
		public bool HasStartButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Start)) != 0);
			}
		}
		public bool HasXButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.X)) != 0);
			}
		}
		public bool HasYButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.Y)) != 0);
			}
		}
		public bool HasBigButton
		{
			get
			{
				return (((ushort)(this._caps.GamePad.Buttons & ButtonValues.BigButton)) != 0);
			}
		}
		public bool HasLeftXThumbStick
		{
			get
			{
				return (this._caps.GamePad.ThumbLX != 0);
			}
		}
		public bool HasLeftYThumbStick
		{
			get
			{
				return (this._caps.GamePad.ThumbLY != 0);
			}
		}
		public bool HasRightXThumbStick
		{
			get
			{
				return (this._caps.GamePad.ThumbRX != 0);
			}
		}
		public bool HasRightYThumbStick
		{
			get
			{
				return (this._caps.GamePad.ThumbRY != 0);
			}
		}
		public bool HasLeftTrigger
		{
			get
			{
				return (this._caps.GamePad.LeftTrigger != 0);
			}
		}
		public bool HasRightTrigger
		{
			get
			{
				return (this._caps.GamePad.RightTrigger != 0);
			}
		}
		public bool HasLeftVibrationMotor
		{
			get
			{
				return (this._caps.Vibration.LeftMotorSpeed != 0);
			}
		}
		public bool HasRightVibrationMotor
		{
			get
			{
				return (this._caps.Vibration.RightMotorSpeed != 0);
			}
		}
		public bool HasVoiceSupport
		{
			get
			{
				return ((this._caps.Flags & 4) != 0);
			}
		}
	}
}

