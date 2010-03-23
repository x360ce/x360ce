using System;

namespace x360ce.App.XnaInput
{

	public class Controller
	{
		private static bool automaticallyPollState = true;
		private int playerIndex = 0;
		private XnaInput.GamePadState state = new XnaInput.GamePadState();
		private Vibration vibration = new Vibration();

		internal Controller(int playerIndex)
		{
			this.playerIndex = playerIndex;
		}

		public bool IsButtonDown(ButtonValues buttonsToCheck)
		{
			this.PollStateInternal();
			return ((this.state.Gamepad.Buttons & buttonsToCheck) != ButtonValues.None);
		}

		public bool PollState()
		{
			return (XInput.GetState(this.playerIndex, ref this.state) == 0);
		}

		private bool PollStateInternal()
		{
			return (AutomaticallyPollState && this.PollState());
		}

		public void SetMotorSpeeds(ushort leftMotor, ushort rightMotor)
		{
			this.vibration.LeftMotorSpeed = leftMotor;
			this.vibration.RightMotorSpeed = rightMotor;
			XInput.SetState(this.playerIndex, ref this.vibration);
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

		public ushort LeftMotorSpeed
		{
			get
			{
				return this.vibration.LeftMotorSpeed;
			}
			set
			{
				this.vibration.LeftMotorSpeed = value;
				XInput.SetState(this.playerIndex, ref this.vibration);
			}
		}

		public ushort RightMotorSpeed
		{
			get
			{
				return this.vibration.RightMotorSpeed;
			}
			set
			{
				this.vibration.RightMotorSpeed = value;
				XInput.SetState(this.playerIndex, ref this.vibration);
			}
		}

		public XnaInput.GamePadState State
		{
			get
			{
				this.PollStateInternal();
				return this.state;
			}
		}
	}
}

