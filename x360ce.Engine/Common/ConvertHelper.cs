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
		public static float GetThumbValue(float dInputValue, float deadZone, float antiDeadZone, float linear)
		{
			// Convert DInput range (ushort[0;65535]) to XInput range (ushort[-32768;32767]).
			var xInput = ConvertRangeF(ushort.MinValue, ushort.MaxValue, short.MinValue, short.MaxValue, dInputValue);
			//
			//        [ 32768 steps | 32768 steps ]
			// DInput [ 0     32767 | 32768 65535 ] 
			// XInput [ 32768    -1 | 0     32767 ]
			//
			var max = 32767f;
			// Check if value is negative.
			bool invert = xInput < 0;
			// Convert [-32768;-1] -> [32767;0]
			if (invert) xInput = -1 - xInput;
			// If deadzone value is set then...
			if (deadZone > 0)
			{
				xInput = (xInput > deadZone)
					// Convert range [deadZone;32767] => [0;32767];
					? xInput = ConvertRangeF(deadZone, max, 0, max, xInput)
					: xInput = 0;
			}
			// If anti-deadzone value is set then...
			if (antiDeadZone > 0 && xInput > 0)
			{
					// Convert range [0;32767] => [antiDeadZone;32767];
					xInput = ConvertRangeF(0, max, antiDeadZone, max, xInput);
			}
			// If linear value is set then...
			if (linear != 0 && xInput > 0)
			{
				// [antiDeadZone;32767] => [0;1f];
				var valueF = ConvertRangeF(antiDeadZone, max, 0, 1, xInput);
				var linearF = (float)linear / 100f;
				var x = -valueF;
				if (linearF < 0f) x = 1f + x;
				var v = ((float)Math.Sqrt(1f - x * x));
				if (linearF < 0f) v = 1f - v;
				valueF = valueF + (2f - v - valueF - 1f) * Math.Abs(linearF);
				// [0;1f] => [antiDeadZone;32767];
				xInput = ConvertRangeF(0, 1, antiDeadZone, max, valueF);
			}
			// If inversion required then...
			if (invert)
				// Convert [32767;0] -> [-32768;-1]
				xInput = -1 - xInput;
			return xInput;
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
			var newValue = ConvertRangeF(oldMin, oldMax, newMin, newMax, value);
			return (int)Math.Round(newValue, 0);
		}

		public static float ConvertRangeF(float oldMin, float oldMax, float newMin, float newMax, float value)
		{
			var oldRange = oldMax - oldMin;
			var newRange = newMax - newMin;
			var scale = newRange / oldRange;
			var newValue = newMin + ((value - oldMin) * scale);
			// Limit range.
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
