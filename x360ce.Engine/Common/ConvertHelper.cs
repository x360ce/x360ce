using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public class ConvertHelper
	{

		/// <summary>Get XInput thumb value by DINput value</summary>
		/// <remarks>Used to create graphs pictures.</remarks>
		public static short GetThumbValue(ushort dInputValue, int deadZone, int antiDeadZone, int linear)
		{
			// Convert DInput range (ushort[0;65535]) to XInput range (ushort[-32768;32767]).
			var xInput = ConvertRange(ushort.MinValue, ushort.MaxValue, short.MinValue, short.MaxValue, dInputValue);

			//        [ 32768 steps | 32768 steps ]
			// DInput [ 0     32767 | 32768 65535 ] 
			// XInput [ 32768    -1 | 0     32767 ]
			//
			//int xInput = *(targetAxis[i]);
			//int deadZone = (int)device.axisdeadzone[i];
			//int antiDeadZone = (int)device.antideadzone[i];
			//int linear = (int)device.axislinear[i];
			int max = 32767;
			// If deadzone value is set then...
			bool invert = xInput < 0;
			// Convert [-32768;-1] -> [32767;0]
			if (invert) xInput = -1 - xInput;
			//if  invert 
			if (deadZone > 0)
			{
				if (xInput > deadZone)
				{
					// [deadZone;32767] => [0;32767];
					xInput = (int)((float)(xInput - deadZone) / (float)(max - deadZone) * (float)max);
				}
				else
				{
					xInput = 0;
				}
			}
			// If anti-deadzone value is set then...
			if (antiDeadZone > 0)
			{
				if (xInput > 0)
				{
					// [0;32767] => [antiDeadZone;32767];
					xInput = (int)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
				}
			}
			// If linear value is set then...
			if (linear != 0 && xInput > 0)
			{
				// [antiDeadZone;32767] => [0;32767];
				float xInputF = (float)(xInput - antiDeadZone) / (float)(max - antiDeadZone) * (float)max;
				float linearF = (float)linear / 100f;
				xInputF = ConvertToFloat((short)xInputF);
				float x = -xInputF;
				if (linearF < 0f) x = 1f + x;
				float v = ((float)Math.Sqrt(1f - x * x));
				if (linearF < 0f) v = 1f - v;
				xInputF = xInputF + (2f - v - xInputF - 1f) * Math.Abs(linearF);
				xInput = ConvertToShort(xInputF);
				// [0;32767] => [antiDeadZone;32767];
				xInput = (int)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
			}
			// Convert [32767;0] -> [-32768;-1]
			if (invert) xInput = -1 - xInput;
			//*(targetAxis[i]) = (SHORT)clamp(xInput, min, max);
			return (short)xInput;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value">[-1.0;1.0]</param>
		/// <param name="strength">[-1.0;1.0]</param>
		/// <returns>[-1.0;1.0]</returns>
		static float GetValue(float value, float param)
		{
			var x = value;
			if (value > 0f) x = 0f - x;
			if (param < 0f) x = 1f + x;
			var v = ((float)Math.Sqrt(1f - x * x));
			if (param < 0f) v = 1f - v;
			if (value > 0f) v = 2f - v;
			var val = value + (v - value - 1f) * Math.Abs(param);
			return val;
		}

		/// <summary>Convert short [-32768;32767] to float range [-1.0f;1.0f].</summary>
		public static float ConvertToFloat(short value)
		{
			return ConvertRangeF(short.MinValue, short.MaxValue, -1f, 1f, value);
		}

		/// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
		public static short ConvertToShort(float value)
		{
			return (short)ConvertRangeF(-1f, 1f, short.MinValue, short.MaxValue, value);
		}

		/// <summary>Convert float [-1.0f;1.0f] to ushort range [0;65535].</summary>
		public static ushort ConvertToUShort(float value)
		{
			return (ushort)ConvertRangeF(-1f, 1f, ushort.MinValue, ushort.MaxValue, value);
		}

		/// <summary>Convert value from [x1;y1] range to [x2;y2] range.</summary>
		public static int ConvertRange(int oldMin, int oldMax, int newMin, int newMax, int value)
		{
			var oldRange = (float)(oldMax - oldMin);
			var newRange = (float)(newMax - newMin);
			var scale = newRange / oldRange;
			return (int)Math.Round(newMin + ((value - oldMin) * scale));
		}

		public static float ConvertRangeF(float oldMin, float oldMax, float newMin, float newMax, float value)
		{
			var oldRange = oldMax - oldMin;
			var newRange = newMax - newMin;
			var scale = newRange / oldRange;
			var newValue = newMin + ((value - oldMin) * scale);
			if (newValue > newMax)
				return newMax;
			if (newValue < newMin)
				return newMin;
			return newValue;
		}

		public static int DeadZone(int val, int min, int max, int lowerDZ, int upperDZ)
		{
			if (val < lowerDZ) return min;
			if (val > upperDZ) return max;
			return val;
		}

		public static int Clamp(int val, int min, int max)
		{
			if (val < min) return min;
			if (val > max) return max;
			return val;
		}


	}
}
