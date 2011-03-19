namespace Microsoft.Xna.Framework.Input
{
	using Microsoft.Xna.Framework;
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct GamePadThumbSticks
	{
		internal Vector2 _left;
		internal Vector2 _right;
		public GamePadThumbSticks(Vector2 leftThumbstick, Vector2 rightThumbstick)
		{
			this._left = leftThumbstick;
			this._right = rightThumbstick;
			this._left = Vector2.Min(this._left, Vector2.One);
			this._left = Vector2.Max(this._left, -Vector2.One);
			this._right = Vector2.Min(this._right, Vector2.One);
			this._right = Vector2.Max(this._right, -Vector2.One);
		}

		public Vector2 Left
		{
			get
			{
				return this._left;
			}
		}
		public Vector2 Right
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
			return (this == ((GamePadThumbSticks)obj));
		}

		public override int GetHashCode()
		{
			return Helpers.SmartGetHashCode(this);
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "{{Left:{0} Right:{1}}}", new object[] { this._left, this._right });
		}

		public static bool operator ==(GamePadThumbSticks left, GamePadThumbSticks right)
		{
			return ((left._left == right._left) && (left._right == right._right));
		}

		public static bool operator !=(GamePadThumbSticks left, GamePadThumbSticks right)
		{
			if (!(left._left != right._left))
			{
				return (left._right != right._right);
			}
			return true;
		}
	}
}

