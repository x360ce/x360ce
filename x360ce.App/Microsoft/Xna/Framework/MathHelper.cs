namespace Microsoft.Xna.Framework
{
	using System;

	public static class MathHelper
	{
		// Fields
		public const float E = 2.718282f;
		public const float Log10E = 0.4342945f;
		public const float Log2E = 1.442695f;
		public const float Pi = 3.141593f;
		public const float PiOver2 = 1.570796f;
		public const float PiOver4 = 0.7853982f;
		public const float TwoPi = 6.283185f;

		// Methods
		public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
		{
			return ((value1 + (amount1 * (value2 - value1))) + (amount2 * (value3 - value1)));
		}

		public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
		{
			float num = amount * amount;
			float num2 = amount * num;
			return (0.5f * ((((2f * value2) + ((-value1 + value3) * amount)) + (((((2f * value1) - (5f * value2)) + (4f * value3)) - value4) * num)) + ((((-value1 + (3f * value2)) - (3f * value3)) + value4) * num2)));
		}

		public static float Clamp(float value, float min, float max)
		{
			value = (value > max) ? max : value;
			value = (value < min) ? min : value;
			return value;
		}

		public static float Distance(float value1, float value2)
		{
			return Math.Abs((float)(value1 - value2));
		}

		public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
		{
			float num3 = amount;
			float num = num3 * num3;
			float num2 = num3 * num;
			float num7 = ((2f * num2) - (3f * num)) + 1f;
			float num6 = (-2f * num2) + (3f * num);
			float num5 = (num2 - (2f * num)) + num3;
			float num4 = num2 - num;
			return ((((value1 * num7) + (value2 * num6)) + (tangent1 * num5)) + (tangent2 * num4));
		}

		public static float Lerp(float value1, float value2, float amount)
		{
			return (value1 + ((value2 - value1) * amount));
		}

		public static float Max(float value1, float value2)
		{
			return Math.Max(value1, value2);
		}

		public static float Min(float value1, float value2)
		{
			return Math.Min(value1, value2);
		}

		public static float SmoothStep(float value1, float value2, float amount)
		{
			float num = Clamp(amount, 0f, 1f);
			return Lerp(value1, value2, (num * num) * (3f - (2f * num)));
		}

		public static float ToDegrees(float radians)
		{
			return (radians * 57.29578f);
		}

		public static float ToRadians(float degrees)
		{
			return (degrees * 0.01745329f);
		}

		public static float WrapAngle(float angle)
		{
			angle = (float)Math.IEEERemainder((double)angle, 6.2831854820251465);
			if (angle <= -3.141593f)
			{
				angle += 6.283185f;
				return angle;
			}
			if (angle > 3.141593f)
			{
				angle -= 6.283185f;
			}
			return angle;
		}
	}
}

