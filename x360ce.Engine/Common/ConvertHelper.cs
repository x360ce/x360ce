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
		public static float GetThumbValue(float dInputValue, float deadZone, float antiDeadZone, float linear, bool inverted, bool half, bool thumb = true)
		{
			//
			//        [  32768 steps | 32768 steps ]
			// DInput [      0 32767 | 32768 65535 ] 
			// XInput [ -32768    -1 | 0     32767 ]
			//
			var dih = 32768f;
			var dInput = (float)dInputValue;
			// If source axis must be inverted then...
			if (inverted)
				dInput = (float)ushort.MaxValue - dInput;
			// if only upper half axis must be used then...
			// Note: half axis is ignored if destination is thumb.
			if (half && !thumb)
			{
				// Limit minimum value.
				if (dInput < dih)
					dInput = dih;
				// Convert half Dinput range [32768;65535] range to DInput range (ushort[0;65535])
				dInput = ConvertRangeF(dih, ushort.MaxValue, 0f, ushort.MaxValue, dInput);
			}
			var min = thumb ? -32768f : 0f;
			var max = thumb ? 32767f : 255f;

			// Convert DInput range(ushort[0; 65535]) to XInput thumb range(ushort[min; max]).
			var xInput = ConvertRangeF(ushort.MinValue, ushort.MaxValue, min, max, dInput);
			// Check if value is negative (only thumb).
			bool invert = xInput < 0f;
			// Convert [-32768;-1] -> [32767;0]
			if (invert)
				xInput = -1f - xInput;
			// If deadzone value is set then...
			if (deadZone > 0f)
			{
				xInput = (xInput > deadZone)
					// Convert range [deadZone;max] => [0;max];
					? xInput = ConvertRangeF(deadZone, max, 0f, max, xInput)
					: xInput = 0f;
			}
			// If anti-deadzone value is set then...
			if (antiDeadZone > 0f && xInput > 0f)
			{
				// Convert range [0;max] => [antiDeadZone;max];
				xInput = ConvertRangeF(0f, max, antiDeadZone, max, xInput);
			}
			// If linear value is set then...
			if (linear != 0f && xInput > 0f)
			{
				// [antiDeadZone;32767] => [0;1f];
				var valueF = ConvertRangeF(antiDeadZone, max, 0f, 1f, xInput);
				var linearF = (float)linear / 100f;
				var x = -valueF;
				if (linearF < 0f) x = 1f + x;
				var v = ((float)Math.Sqrt(1f - x * x));
				if (linearF < 0f) v = 1f - v;
				valueF = valueF + (2f - v - valueF - 1f) * Math.Abs(linearF);
				// [0;1f] => [antiDeadZone;max];
				xInput = ConvertRangeF(0f, 1f, antiDeadZone, max, valueF);
			}
			// If inversion required (only thumb) then...
			if (invert)
				// Convert [32767;0] -> [-32768;-1]
				xInput = -1f - xInput;
			return xInput;
		}

		/// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
		public static short ConvertToShort(float value)
		{
			return (short)ConvertRangeF(-1f, 1f, short.MinValue, short.MaxValue, value);
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
            if (newRange > 0)
            {
                // Limit range.
                if (newValue > newMax)
                    return newMax;
                if (newValue < newMin)
                    return newMin;
            }
            else
            {
                // Limit range.
                if (newValue > newMin)
                    return newMin;
                if (newValue < newMax)
                    return newMax;
            }
            return newValue;
		}

		public static int DeadZone(int val, int min, int max, int lowerDZ, int upperDZ)
		{
			if (val < lowerDZ) return min;
			if (val > upperDZ) return max;
			return val;
		}

	}
}
