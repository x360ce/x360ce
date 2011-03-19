namespace Microsoft.Xna.Framework.Input
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct GamePadTriggers
	{
		internal float _left;
		internal float _right;
		public GamePadTriggers(float leftTrigger, float rightTrigger)
		{
			this._left = leftTrigger;
			this._right = rightTrigger;
			this._left = Math.Min(this._left, 1f);
			this._left = Math.Max(this._left, 0f);
			this._right = Math.Min(this._right, 1f);
			this._right = Math.Max(this._right, 0f);
		}

		public float Left
		{
			get
			{
				return this._left;
			}
		}
		public float Right
		{
			get
			{
				return this._right;
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
			return (this == ((GamePadTriggers)obj));
		}

		public override int GetHashCode()
		{
			return Helpers.SmartGetHashCode(this);
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "{{Left:{0} Right:{1}}}", new object[] { this._left, this._right });
		}

		public static bool operator ==(GamePadTriggers left, GamePadTriggers right)
		{
			return ((left._left == right._left) && (left._right == right._right));
		}

		public static bool operator !=(GamePadTriggers left, GamePadTriggers right)
		{
			if (left._left == right._left)
			{
				return !(left._right == right._right);
			}
			return true;
		}
	}
}

