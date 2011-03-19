namespace Microsoft.Xna.Framework.Input
{
	using System.Globalization;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct GamePadDPad
	{
		internal ButtonState _up;
		internal ButtonState _right;
		internal ButtonState _down;
		internal ButtonState _left;
		public GamePadDPad(ButtonState upValue, ButtonState downValue, ButtonState leftValue, ButtonState rightValue)
		{
			this._up = upValue;
			this._right = rightValue;
			this._down = downValue;
			this._left = leftValue;
		}

		public ButtonState Up
		{
			get
			{
				return this._up;
			}
		}
		public ButtonState Down
		{
			get
			{
				return this._down;
			}
		}
		public ButtonState Right
		{
			get
			{
				return this._right;
			}
		}
		public ButtonState Left
		{
			get
			{
				return this._left;
			}
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
			return (this == ((GamePadDPad)obj));
		}

		public override int GetHashCode()
		{
			return Helpers.SmartGetHashCode(this);
		}

		public override string ToString()
		{
			string str = string.Empty;
			if (this._up == ButtonState.Pressed)
			{
				str = str + ((str.Length != 0) ? " " : "") + "Up";
			}
			if (this._down == ButtonState.Pressed)
			{
				str = str + ((str.Length != 0) ? " " : "") + "Down";
			}
			if (this._left == ButtonState.Pressed)
			{
				str = str + ((str.Length != 0) ? " " : "") + "Left";
			}
			if (this._right == ButtonState.Pressed)
			{
				str = str + ((str.Length != 0) ? " " : "") + "Right";
			}
			if (str.Length == 0)
			{
				str = "None";
			}
			return string.Format(CultureInfo.CurrentCulture, "{{DPad:{0}}}", new object[] { str });
		}

		public static bool operator ==(GamePadDPad left, GamePadDPad right)
		{
			return ((((left._up == right._up) && (left._down == right._down)) && (left._left == right._left)) && (left._right == right._right));
		}

		public static bool operator !=(GamePadDPad left, GamePadDPad right)
		{
			if (((left._up == right._up) && (left._down == right._down)) && (left._left == right._left))
			{
				return (left._right != right._right);
			}
			return true;
		}
	}
}

