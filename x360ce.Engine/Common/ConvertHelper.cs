using JocysCom.ClassLibrary.Collections;
using System;

namespace x360ce.Engine
{
	public class ConvertHelper
	{

		/// <summary>Get XInput thumb value by DInput value</summary>
		/// <remarks>Used to create graphs pictures.</remarks>
		
		public static float GetThumbValue(float dInputValue, float deadZone, float antiDeadZone, float linear, bool isInverted, bool isHalf, bool isThumb = true)
		{
			// Limit values.
			dInputValue = Math.Max(ushort.MinValue, Math.Min(dInputValue, ushort.MaxValue));
			deadZone = Math.Max(0, Math.Min(deadZone, ushort.MaxValue));
			antiDeadZone = Math.Max(0, Math.Min(antiDeadZone, ushort.MaxValue));
			linear = Math.Max(-100f, Math.Min(linear, 100f));

			// Check DInputValue.
			if (dInputValue < ushort.MinValue)
				throw new ArgumentOutOfRangeException(nameof(linear), $"DInputValue can't be less than {ushort.MinValue}!");
			if (dInputValue > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(linear), $"DInputValue can't be greater than {ushort.MaxValue}!");
			// Check DeadZone.
			if (deadZone < 0)
				throw new ArgumentOutOfRangeException(nameof(linear), $"DeadZone can't be less than {0}!");
			if (deadZone > short.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(linear), $"DeadZone can't be greater than {short.MaxValue}!");
			// Check AntiDeadZone.
			if (antiDeadZone < 0)
				throw new ArgumentOutOfRangeException(nameof(linear), $"AntiDeadZone can't be less than {0}!");
			if (antiDeadZone > short.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(linear), $"AntiDeadZone can't be greater than {short.MaxValue}!");
			// Check Linear sensitivity.
			if (linear < -100f)
				throw new ArgumentOutOfRangeException(nameof(linear), "Linear sensitivity can't be less than -100!");
			if (linear > 100f)
				throw new ArgumentOutOfRangeException(nameof(linear), "Linear sensitivity can't be greater than 100!");
			//
			//        [  32768 steps | 32768 steps ]
			// DInput [      0 32767 | 32768 65535 ] 
			// XInput [ -32768    -1 | 0     32767 ]
			//
			var dih = 32768f;
			var dInput = (float)dInputValue;
			// If source axis must be inverted then...
			if (isInverted)
				dInput = (float)ushort.MaxValue - dInput;
			// if only upper half axis must be used then...
			// Note: half axis is ignored if destination is thumb.
			if (isHalf && !isThumb)
			{
				// Limit minimum value.
				if (dInput < dih)
					dInput = dih;
				// Convert half Dinput range [32768;65535] range to DInput range (ushort[0;65535])
				dInput = ConvertRangeF(dInput, dih, ushort.MaxValue, 0f, ushort.MaxValue);
			}
			var min = isThumb ? -32768f : 0f;
			var max = isThumb ? 32767f : 255f;

			// Convert DInput range(ushort[0; 65535]) to XInput thumb range(ushort[min; max]).
			var xInput = ConvertRangeF(dInput, ushort.MinValue, ushort.MaxValue, min, max);
			// Check if value is negative (only thumb).
			bool invert = xInput < 0f;
			// Convert [-32768;-1] -> [32767;0]
			if (invert)
				xInput = -1f - xInput;

			var deadZoneApplied = false;
			// If deadzone value is set then...
			if (deadZone > 0f)
			{
				deadZoneApplied = xInput <= deadZone;
				xInput = deadZoneApplied
					? 0f
					// Convert range [deadZone;max] => [0;max];
					: ConvertRangeF(xInput, deadZone, max, 0f, max);
			}
			// If anti-deadzone value is set then...
			if (antiDeadZone > 0f && xInput > 0f)
			{
				// Convert range [0;max] => [antiDeadZone;max];
				xInput = ConvertRangeF(xInput, 0f, max, antiDeadZone, max);
			}
			// If linear value is set then...
			if (linear != 0f && xInput > 0f)
			{
				// [antiDeadZone;32767] => [0;1f];
				var valueF = ConvertRangeF(xInput, antiDeadZone, max, 0f, 1f);
				var linearF = (float)linear / 100f;
				var x = -valueF;
				if (linearF < 0f) x = 1f + x;
				var v = (float)Math.Sqrt(1f - x * x);
				if (linearF < 0f) v = 1f - v;
				valueF = valueF + (2f - v - valueF - 1f) * Math.Abs(linearF);
				// [0;1f] => [antiDeadZone;max];
				xInput = ConvertRangeF(valueF, 0f, 1f, antiDeadZone, max);
			}
			// If inversion required (only thumb) and not in deadzone then...
			// Checking for deadzone prevents XInput value jittering between 0 and -1.
			if (invert && !deadZoneApplied)
				// Convert [32767;0] -> [-32768;-1]
				xInput = -1f - xInput;
			// Set negative center value (-1) to 0 for thumb.
			if (isThumb && xInput == -1)
				xInput = 0;
			// Return value.
			return xInput;
		}
		
		/// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
		public static short ConvertToShort(float value)
		{
			return (short)ConvertRangeF(value, -1f, 1f, short.MinValue, short.MaxValue);
		}

		/// <summary>Convert value from [x1;y1] range to [x2;y2] range.</summary>
		public static int ConvertRange(int oldValue, int oldMin, int oldMax, int newMin, int newMax)
		{
			var newValue = ConvertRangeF(oldValue, oldMin, oldMax, newMin, newMax);
			return (int)Math.Round(newValue, 0);
		}

		/// <summary>Convert value from [x1;y1] range to [x2;y2] range.</summary>
		public static float ConvertRangeF(float oldValue, float oldMin, float oldMax, float newMin, float newMax)
		{
			if (oldMin == oldMax)
				throw new ArgumentException($"The arguments {nameof(oldMin)} and {nameof(oldMax)} cannot be equal!");
			if (newMin == newMax)
				throw new ArgumentException($"The arguments {nameof(newMin)} and {nameof(newMax)} cannot be equal!");

			if (LimitRange(oldValue, oldMin, oldMax) != oldValue)
				throw new ArgumentOutOfRangeException($"Value {nameof(oldValue)} is out of {nameof(oldMin)} - {nameof(oldMax)} range!");

			var oldSize = oldMax - oldMin;
			var newSize = newMax - newMin;
			var position = (oldValue - oldMin) / oldSize;
			var newValue = position * newSize + newMin;
			return LimitRange(newValue, newMin, newMax);
		}

		/// <summary>
		/// Return true if value in range (inclusive).
		/// </summary>
		public static bool InRange(float value, float min, float max)
		{
			// If inverted then...
			return min > max
				? max <= value && value <= min
				: min <= value && value <= max;
		}

		/// <summary>
		/// Limit to range.
		/// </summary>
		public static float LimitRange(float value, float min, float max)
		{
			// If inverted then swap.
			if (min > max) (min, max) = (max, min);
			// Limit value between min and max.
			return Math.Max(min, Math.Min(max, value));

			// If inverted then swap.
			//if (min > max)
			//	(min, max) = (max, min);
			//if (value > max)
			//	return max;
			//if (value < min)
			//	return min;
			//return value;
		}

		public static int DeadZone(int value, int min, int max, int lowerDZ, int upperDZ)
		{
			if (value < lowerDZ)
				return min;
			if (value > upperDZ)
				return max;
			return value;
		}

	}
}
