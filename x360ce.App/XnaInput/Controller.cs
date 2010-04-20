using System;

namespace x360ce.App.XnaInput
{

	public class Controller
	{
		private static bool automaticallyPollState = true;
        private PlayerIndex playerIndex = PlayerIndex.One;
		private XnaInput.XINPUT_STATE state = new XnaInput.XINPUT_STATE();
		private XINPUT_VIBRATION vibration = new XINPUT_VIBRATION();

        internal Controller(PlayerIndex playerIndex)
		{
			this.playerIndex = playerIndex;
		}

		public bool IsButtonDown(ButtonValues buttonsToCheck)
		{
			this.PollStateInternal();
			return ((this.state.GamePad.Buttons & buttonsToCheck) != ButtonValues.None);
		}

		public bool PollState()
		{
			return (UnsafeNativeMethods.GetState(this.playerIndex, out this.state) == 0);
		}

		private bool PollStateInternal()
		{
			return (AutomaticallyPollState && this.PollState());
		}

		public void SetMotorSpeeds(short leftMotor, short rightMotor)
		{
			this.vibration.LeftMotorSpeed = leftMotor;
			this.vibration.RightMotorSpeed = rightMotor;
			UnsafeNativeMethods.SetState(this.playerIndex, out this.vibration);
		}

		public static bool AutomaticallyPollState
		{
			get
			{
				return automaticallyPollState;
			}
			set
			{
				automaticallyPollState = value;
			}
		}

		public bool IsConnected
		{
			get
			{
				return this.PollState();
			}
		}

		public short LeftMotorSpeed
		{
			get
			{
				return this.vibration.LeftMotorSpeed;
			}
			set
			{
				this.vibration.LeftMotorSpeed = value;
				UnsafeNativeMethods.SetState(this.playerIndex, out this.vibration);
			}
		}

		public short RightMotorSpeed
		{
			get
			{
				return this.vibration.RightMotorSpeed;
			}
			set
			{
				this.vibration.RightMotorSpeed = value;
				UnsafeNativeMethods.SetState(this.playerIndex, out this.vibration);
			}
		}

		public XnaInput.XINPUT_STATE State
		{
			get
			{
				this.PollStateInternal();
				return this.state;
			}
		}
	}
}

