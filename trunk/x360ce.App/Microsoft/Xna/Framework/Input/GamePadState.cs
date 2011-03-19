namespace Microsoft.Xna.Framework.Input
{
	using Microsoft.Xna.Framework;
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct GamePadState
	{
		private const int _normalButtonMask = 0xfbff;
		private bool _connected;
		private int _packet;
		private GamePadThumbSticks _thumbs;
		private GamePadTriggers _triggers;
		private GamePadButtons _buttons;
		private GamePadDPad _dpad;
		private XINPUT_STATE _state;
		public GamePadState(GamePadThumbSticks thumbSticks, GamePadTriggers triggers, GamePadButtons buttons, GamePadDPad dPad)
		{
			this._packet = 0;
			this._connected = true;
			this._thumbs = thumbSticks;
			this._triggers = triggers;
			this._buttons = buttons;
			this._dpad = dPad;
			this._state = new XINPUT_STATE();
			this.FillInternalState();
		}

		public GamePadState(Vector2 leftThumbStick, Vector2 rightThumbStick, float leftTrigger, float rightTrigger, params Microsoft.Xna.Framework.Input.Buttons[] buttons)
		{
			this._packet = 0;
			this._connected = true;
			this._thumbs = new GamePadThumbSticks(leftThumbStick, rightThumbStick);
			this._triggers = new GamePadTriggers(leftTrigger, rightTrigger);
			Microsoft.Xna.Framework.Input.Buttons buttons2 = 0;
			if (buttons != null)
			{
				for (int i = 0; i < buttons.Length; i++)
				{
					buttons2 |= buttons[i];
				}
			}
			this._buttons = new GamePadButtons(buttons2);
			this._dpad = new GamePadDPad();
			this._dpad._down = ((buttons2 & Microsoft.Xna.Framework.Input.Buttons.DPadDown) != 0) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._up = ((buttons2 & Microsoft.Xna.Framework.Input.Buttons.DPadUp) != 0) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._left = ((buttons2 & Microsoft.Xna.Framework.Input.Buttons.DPadLeft) != 0) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._right = ((buttons2 & Microsoft.Xna.Framework.Input.Buttons.DPadRight) != 0) ? ButtonState.Pressed : ButtonState.Released;
			this._state = new XINPUT_STATE();
			this.FillInternalState();
		}

		private void FillInternalState()
		{
			this._state.PacketNumber = 0;
			if (this.Buttons.A == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.A));
			}
			if (this.Buttons.B == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.B));
			}
			if (this.Buttons.X == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.X));
			}
			if (this.Buttons.Y == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Y));
			}
			if (this.Buttons.Back == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Back));
			}
			if (this.Buttons.LeftShoulder == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.LeftShoulder));
			}
			if (this.Buttons.LeftStick == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.LeftThumb));
			}
			if (this.Buttons.RightShoulder == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.RightShoulder));
			}
			if (this.Buttons.RightStick == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.RightThumb));
			}
			if (this.Buttons.Start == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Start));
			}
			if (this.Buttons.BigButton == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.BigButton));
			}
			if (this.DPad.Up == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Up));
			}
			if (this.DPad.Down == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Down));
			}
			if (this.DPad.Right == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Right));
			}
			if (this.DPad.Left == ButtonState.Pressed)
			{
				this._state.GamePad.Buttons = (ButtonValues)((ushort)(this._state.GamePad.Buttons | ButtonValues.Left));
			}
			this._state.GamePad.LeftTrigger = (byte)(this._triggers._left * 255f);
			this._state.GamePad.RightTrigger = (byte)(this._triggers._right * 255f);
			this._state.GamePad.ThumbLX = (short)(this._thumbs._left.X * 32767f);
			this._state.GamePad.ThumbLY = (short)(this._thumbs._left.Y * 32767f);
			this._state.GamePad.ThumbRX = (short)(this._thumbs._right.X * 32767f);
			this._state.GamePad.ThumbRY = (short)(this._thumbs._right.Y * 32767f);
		}

		internal GamePadState(ref XINPUT_STATE pState, ErrorCodes result, GamePadDeadZone deadZoneMode)
		{
			this._state = pState;
			this._connected = result != ErrorCodes.NotConnected;
			this._packet = pState.PacketNumber;
			this._buttons._a = (((ushort)(pState.GamePad.Buttons & ButtonValues.A)) == 0x1000) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._b = (((ushort)(pState.GamePad.Buttons & ButtonValues.B)) == 0x2000) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._x = (((ushort)(pState.GamePad.Buttons & ButtonValues.X)) == 0x4000) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._y = (((ushort)(pState.GamePad.Buttons & ButtonValues.Y)) == 0x8000) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._start = (((ushort)(pState.GamePad.Buttons & ButtonValues.Start)) == 0x10) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._back = (((ushort)(pState.GamePad.Buttons & ButtonValues.Back)) == 0x20) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._leftStick = (((ushort)(pState.GamePad.Buttons & ButtonValues.LeftThumb)) == 0x40) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._rightStick = (((ushort)(pState.GamePad.Buttons & ButtonValues.RightThumb)) == 0x80) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._leftShoulder = (((ushort)(pState.GamePad.Buttons & ButtonValues.LeftShoulder)) == 0x100) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._rightShoulder = (((ushort)(pState.GamePad.Buttons & ButtonValues.RightShoulder)) == 0x200) ? ButtonState.Pressed : ButtonState.Released;
			this._buttons._bigButton = (((ushort)(pState.GamePad.Buttons & ButtonValues.BigButton)) == 0x800) ? ButtonState.Pressed : ButtonState.Released;
			this._triggers._left = GamePadDeadZoneUtils.ApplyTriggerDeadZone(pState.GamePad.LeftTrigger, deadZoneMode);
			this._triggers._right = GamePadDeadZoneUtils.ApplyTriggerDeadZone(pState.GamePad.RightTrigger, deadZoneMode);
			this._thumbs._left = GamePadDeadZoneUtils.ApplyLeftStickDeadZone(pState.GamePad.ThumbLX, pState.GamePad.ThumbLY, deadZoneMode);
			this._thumbs._right = GamePadDeadZoneUtils.ApplyRightStickDeadZone(pState.GamePad.ThumbRX, pState.GamePad.ThumbRY, deadZoneMode);
			this._dpad._down = (((ushort)(pState.GamePad.Buttons & ButtonValues.Down)) == 2) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._up = (((ushort)(pState.GamePad.Buttons & ButtonValues.Up)) == 1) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._left = (((ushort)(pState.GamePad.Buttons & ButtonValues.Left)) == 4) ? ButtonState.Pressed : ButtonState.Released;
			this._dpad._right = (((ushort)(pState.GamePad.Buttons & ButtonValues.Right)) == 8) ? ButtonState.Pressed : ButtonState.Released;
		}

		public GamePadButtons Buttons
		{
			get
			{
				return this._buttons;
			}
		}
		public GamePadDPad DPad
		{
			get
			{
				return this._dpad;
			}
		}
		public bool IsConnected
		{
			get
			{
				return this._connected;
			}
		}
		public int PacketNumber
		{
			get
			{
				return this._packet;
			}
		}
		public GamePadThumbSticks ThumbSticks
		{
			get
			{
				return this._thumbs;
			}
		}
		public GamePadTriggers Triggers
		{
			get
			{
				return this._triggers;
			}
		}
		public bool IsButtonDown(Microsoft.Xna.Framework.Input.Buttons button)
		{
			Microsoft.Xna.Framework.Input.Buttons buttons = ((Microsoft.Xna.Framework.Input.Buttons)this._state.GamePad.Buttons) & (Microsoft.Xna.Framework.Input.Buttons.Y | Microsoft.Xna.Framework.Input.Buttons.X | Microsoft.Xna.Framework.Input.Buttons.B | Microsoft.Xna.Framework.Input.Buttons.A | Microsoft.Xna.Framework.Input.Buttons.BigButton | Microsoft.Xna.Framework.Input.Buttons.RightShoulder | Microsoft.Xna.Framework.Input.Buttons.LeftShoulder | Microsoft.Xna.Framework.Input.Buttons.RightStick | Microsoft.Xna.Framework.Input.Buttons.LeftStick | Microsoft.Xna.Framework.Input.Buttons.Back | Microsoft.Xna.Framework.Input.Buttons.Start | Microsoft.Xna.Framework.Input.Buttons.DPadRight | Microsoft.Xna.Framework.Input.Buttons.DPadLeft | Microsoft.Xna.Framework.Input.Buttons.DPadDown | Microsoft.Xna.Framework.Input.Buttons.DPadUp);
			if (((button & Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft) == Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft) && (GamePadDeadZoneUtils.ApplyLeftStickDeadZone(this._state.GamePad.ThumbLX, this._state.GamePad.ThumbLY, GamePadDeadZone.IndependentAxes).X < 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight) == Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight) && (GamePadDeadZoneUtils.ApplyLeftStickDeadZone(this._state.GamePad.ThumbLX, this._state.GamePad.ThumbLY, GamePadDeadZone.IndependentAxes).X > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown) == Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown) && (GamePadDeadZoneUtils.ApplyLeftStickDeadZone(this._state.GamePad.ThumbLX, this._state.GamePad.ThumbLY, GamePadDeadZone.IndependentAxes).Y < 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp) == Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp) && (GamePadDeadZoneUtils.ApplyLeftStickDeadZone(this._state.GamePad.ThumbLX, this._state.GamePad.ThumbLY, GamePadDeadZone.IndependentAxes).Y > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft) == Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft) && (GamePadDeadZoneUtils.ApplyRightStickDeadZone(this._state.GamePad.ThumbRX, this._state.GamePad.ThumbRY, GamePadDeadZone.IndependentAxes).X < 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight) == Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight) && (GamePadDeadZoneUtils.ApplyRightStickDeadZone(this._state.GamePad.ThumbRX, this._state.GamePad.ThumbRY, GamePadDeadZone.IndependentAxes).X > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown) == Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown) && (GamePadDeadZoneUtils.ApplyRightStickDeadZone(this._state.GamePad.ThumbRX, this._state.GamePad.ThumbRY, GamePadDeadZone.IndependentAxes).Y < 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp) == Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp) && (GamePadDeadZoneUtils.ApplyRightStickDeadZone(this._state.GamePad.ThumbRX, this._state.GamePad.ThumbRY, GamePadDeadZone.IndependentAxes).Y > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.LeftTrigger) == Microsoft.Xna.Framework.Input.Buttons.LeftTrigger) && (GamePadDeadZoneUtils.ApplyTriggerDeadZone(this._state.GamePad.LeftTrigger, GamePadDeadZone.IndependentAxes) > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftTrigger;
			}
			if (((button & Microsoft.Xna.Framework.Input.Buttons.RightTrigger) == Microsoft.Xna.Framework.Input.Buttons.RightTrigger) && (GamePadDeadZoneUtils.ApplyTriggerDeadZone(this._state.GamePad.RightTrigger, GamePadDeadZone.IndependentAxes) > 0f))
			{
				buttons |= Microsoft.Xna.Framework.Input.Buttons.RightTrigger;
			}
			return ((button & buttons) == button);
		}

		public bool IsButtonUp(Microsoft.Xna.Framework.Input.Buttons button)
		{
			return !this.IsButtonDown(button);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj.GetType() != base.GetType())
			{
				return false;
			}
			return (this == ((GamePadState)obj));
		}

		public override int GetHashCode()
		{
			return (((this._thumbs.GetHashCode() ^ this._triggers.GetHashCode()) ^ (this._buttons.GetHashCode() ^ this._connected.GetHashCode())) ^ (this._dpad.GetHashCode() ^ this._packet.GetHashCode()));
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "{{IsConnected:{0}}}", new object[] { this._connected });
		}

		public static bool operator ==(GamePadState left, GamePadState right)
		{
			return (((((left._connected == right._connected) && (left._packet == right._packet)) && ((left._thumbs == right._thumbs) && (left._triggers == right._triggers))) && (left._buttons == right._buttons)) && (left._dpad == right._dpad));
		}

		public static bool operator !=(GamePadState left, GamePadState right)
		{
			if ((((left._connected == right._connected) && (left._packet == right._packet)) && (!(left._thumbs != right._thumbs) && !(left._triggers != right._triggers))) && !(left._buttons != right._buttons))
			{
				return (left._dpad != right._dpad);
			}
			return true;
		}
	}
}

