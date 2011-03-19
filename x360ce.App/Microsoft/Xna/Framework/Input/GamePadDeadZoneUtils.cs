namespace Microsoft.Xna.Framework.Input
{
	using Microsoft.Xna.Framework;
	using System;

	internal static class GamePadDeadZoneUtils
	{
		private const int LeftStickDeadZoneSize = 0x1ea9;
		private const int RightStickDeadZoneSize = 0x21f1;
		private const int TriggerDeadZoneSize = 30;

		internal static Vector2 ApplyLeftStickDeadZone(int x, int y, GamePadDeadZone deadZoneMode)
		{
			return ApplyStickDeadZone(x, y, deadZoneMode, 0x1ea9);
		}

		private static float ApplyLinearDeadZone(float value, float maxValue, float deadZoneSize)
		{
			if (value < -deadZoneSize)
			{
				value += deadZoneSize;
			}
			else if (value > deadZoneSize)
			{
				value -= deadZoneSize;
			}
			else
			{
				return 0f;
			}
			float num = value / (maxValue - deadZoneSize);
			return MathHelper.Clamp(num, -1f, 1f);
		}

		internal static Vector2 ApplyRightStickDeadZone(int x, int y, GamePadDeadZone deadZoneMode)
		{
			return ApplyStickDeadZone(x, y, deadZoneMode, 0x21f1);
		}

		private static Vector2 ApplyStickDeadZone(int x, int y, GamePadDeadZone deadZoneMode, int deadZoneSize)
		{
			Vector2 vector;
			if (deadZoneMode == GamePadDeadZone.IndependentAxes)
			{
				vector.X = ApplyLinearDeadZone((float)x, 32767f, (float)deadZoneSize);
				vector.Y = ApplyLinearDeadZone((float)y, 32767f, (float)deadZoneSize);
				return vector;
			}
			if (deadZoneMode == GamePadDeadZone.Circular)
			{
				float num3 = (float)Math.Sqrt((double)((x * x) + (y * y)));
				float num2 = ApplyLinearDeadZone(num3, 32767f, (float)deadZoneSize);
				float num = (num2 > 0f) ? (num2 / num3) : 0f;
				vector.X = MathHelper.Clamp(x * num, -1f, 1f);
				vector.Y = MathHelper.Clamp(y * num, -1f, 1f);
				return vector;
			}
			vector.X = ApplyLinearDeadZone((float)x, 32767f, 0f);
			vector.Y = ApplyLinearDeadZone((float)y, 32767f, 0f);
			return vector;
		}

		internal static float ApplyTriggerDeadZone(int value, GamePadDeadZone deadZoneMode)
		{
			if (deadZoneMode == GamePadDeadZone.None)
			{
				return ApplyLinearDeadZone((float)value, 255f, 0f);
			}
			return ApplyLinearDeadZone((float)value, 255f, 30f);
		}
	}
}

