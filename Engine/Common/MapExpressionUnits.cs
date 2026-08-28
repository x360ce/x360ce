using System;

namespace x360ce.Engine
{
	/// <summary>
	/// Translates between the units a formula is written in and the units a device reports and
	/// expects.
	/// </summary>
	/// <remarks>
	/// A formula is written in plain units, so that somebody writing one is thinking about the
	/// control rather than about how it happens to be stored. A stick reads -1 to 1 and rests at 0.
	/// A pedal or trigger reads 0 to 1 and rests at 0. A button reads 0 or 1. Everything a formula
	/// produces is in the same units, so the result can be applied to any destination.
	///
	/// DirectInput reports an axis as a whole number from 0 to 65535 with the middle at 32767, which
	/// is a detail of the interface rather than anything a player should have to know. Turning it
	/// into the plain units happens here, once, in one place.
	/// </remarks>
	public static class MapExpressionUnits
	{

		/// <summary>Largest value DirectInput reports for an axis or a slider.</summary>
		public const int RawMax = ushort.MaxValue;

		/// <summary>The middle of an axis, where a stick sits when nobody is touching it.</summary>
		public const int RawCentre = 32767;

		/// <summary>
		/// Fills the values a compiled formula expects, in the order it expects them.
		/// </summary>
		/// <param name="expression">The formula, whose references give the order.</param>
		/// <param name="state">The controller's current state.</param>
		/// <param name="values">Buffer to fill. Must hold at least one value per reference.</param>
		/// <returns>False when nothing could be read, in which case the buffer is untouched.</returns>
		/// <param name="isThumb">
		/// True when the answer drives a stick, false when it drives a trigger or a button.
		/// </param>
		public static bool TryFill(MapExpression expression, CustomDiState state, float[] values, bool isThumb)
		{
			if (expression == null || state == null || values == null)
				return false;
			var references = expression.References;
			if (values.Length < references.Count)
				return false;
			for (int i = 0; i < references.Count; i++)
				values[i] = Read(references[i], state, isThumb);
			return true;
		}

		/// <summary>What a formula answers for one reading of its sources.</summary>
		/// <remarks>
		/// The device loop and the chart on the mapping page both need this and used to work it out
		/// separately: the loop through the formula, the chart through the dead zone settings the
		/// formula replaces. So the chart drew the wrong shape, and drew nothing at all once a formula
		/// was switched on. Asking the same question in one place is the point.
		/// </remarks>
		/// <param name="buffer">Room for the sources, at least MaxReferences long.</param>
		public static bool TryEvaluate(MapExpression expression, CustomDiState state, bool isThumb, float[] buffer, out float value)
		{
			value = 0f;
			if (!TryFill(expression, state, buffer, isThumb))
				return false;
			value = expression.Evaluate(buffer);
			return true;
		}

		/// <summary>What a formula answers when one named source is at a given reading.</summary>
		/// <remarks>
		/// Used to draw the line on the mapping page, which shows what this control alone produces
		/// across its whole travel. Everything else the formula names is left at rest, which is what
		/// the line has always meant: the response to this control and nothing else.
		/// </remarks>
		/// <param name="raw">Reading of the swept source, in DirectInput units.</param>
		public static bool TrySweep(MapExpression expression, MapReference swept, int raw, bool isThumb, float[] buffer, out float value)
		{
			value = 0f;
			if (expression == null || buffer == null)
				return false;
			var references = expression.References;
			if (buffer.Length < references.Count)
				return false;
			for (int i = 0; i < references.Count; i++)
				buffer[i] = Equals(references[i], swept)
					? Convert(references[i], raw, isThumb)
					: 0f;
			value = expression.Evaluate(buffer);
			return true;
		}

		/// <summary>One reading turned into the units a formula is written in.</summary>
		/// <remarks>Split out of Read so a reading can come from a sweep instead of a device.</remarks>
		public static float Convert(MapReference reference, int raw, bool isThumb)
		{
			switch (reference.Type)
			{
				case 'a':
					return isThumb ? Centred(raw) : Whole(raw);
				case 'x':
					return Positive(raw);
				case 's':
					return Whole(raw);
				case 'h':
					return Positive(raw);
				default:
					// A button, a hat direction or the clock: not something a sweep moves.
					return 0f;
			}
		}

		/// <summary>The source a chart sweeps for this formula, or null when it has none.</summary>
		/// <remarks>
		/// The first source that moves through a range. A formula naming only buttons has no line to
		/// draw, because a button has two readings rather than a travel.
		/// </remarks>
		public static MapReference? GetSweptSource(MapExpression expression)
		{
			if (expression == null)
				return null;
			foreach (var reference in expression.References)
				if (reference.Type == 'a' || reference.Type == 'x' || reference.Type == 's' || reference.Type == 'h')
					return reference;
			return null;
		}

		/// <summary>One control's value, in the units a formula is written in.</summary>
		/// <remarks>
		/// A control the device does not have reads as nought rather than failing. A formula naming
		/// axis nine on a device with four is describing something that is simply not there, and a
		/// control that is not there is not being moved.
		/// </remarks>
		/// <param name="isThumb">
		/// True when the answer drives a stick, false when it drives a trigger or a button.
		///
		/// Only an axis is read differently by what it drives, and that is not this program inventing
		/// a rule: it is the rule the program already follows everywhere else. The same axis handed to
		/// a trigger covers the whole of the trigger's travel, and handed to a stick covers both
		/// directions from the middle. A trigger rests at one end and a stick rests in the middle, so
		/// there is no single reading that is right for both.
		///
		/// Reading a trigger as though it rested in the middle is what made "=a5*2" hold the trigger
		/// fully down while nobody was touching it: at rest it read minus one, which doubled to minus
		/// two, which is past the end.
		/// </param>
		public static float Read(MapReference reference, CustomDiState state, bool isThumb)
		{
			if (state == null)
				return 0f;
			var index = reference.Index - 1;
			if (index < 0)
				return 0f;
			switch (reference.Type)
			{
				case 'a':
					// For a stick the middle becomes nought and the ends become -1 and 1. For a trigger
					// the whole travel becomes nought to one, because that is where a trigger rests.
					return index < state.Axis.Length
						? (isThumb ? Centred(state.Axis[index]) : Whole(state.Axis[index]))
						: 0f;
				case 'x':
					// The same stick read as one half, so each half can drive something different.
					return index < state.Axis.Length
						? Positive(state.Axis[index])
						: 0f;
				case 's':
					// A pedal or throttle: rests at one end, so nought there and one at the other.
					return index < state.Sliders.Length
						? Whole(state.Sliders[index])
						: 0f;
				case 'h':
					return index < state.Sliders.Length
						? Positive(state.Sliders[index])
						: 0f;
				case 'b':
					return index < state.Buttons.Length && state.Buttons[index]
						? 1f
						: 0f;
				case 'd':
					// A hat direction, which arrives already turned into a button.
					return index < state.Buttons.Length && state.Buttons[index]
						? 1f
						: 0f;
				case 'p':
					return index < state.Buttons.Length && state.Buttons[index]
						? 1f
						: 0f;
				default:
					// The clock, which is not a control and has no index.
					return reference.Type == MapExpression.TimeType
						? Milliseconds()
						: 0f;
			}
		}

		/// <summary>
		/// How long the program has been running, in whole milliseconds.
		/// </summary>
		/// <remarks>
		/// Given as a plain count so that a person can divide it into whatever they need: by a
		/// thousand for seconds, by sixty thousand for minutes.
		///
		/// Counted from when the program started rather than from a date, because a formula only ever
		/// cares how much time has passed. A count of milliseconds since a date is far too large a
		/// number to hold in the values a formula works in, and would arrive already rounded to
		/// something coarser than a second.
		///
		/// The values a formula works in hold whole numbers exactly up to about sixteen million, which
		/// is a little under five hours. After that the count is still correct but stops being exact to
		/// the millisecond, and anything oscillating quickly will slow its step. Something repeating on
		/// a minute or an hour is unaffected.
		/// </remarks>
		public static float Milliseconds()
		{
			return (float)Clock.Elapsed.TotalMilliseconds;
		}

		/// <summary>Started once, when the program first asks the time of anything.</summary>
		private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

		/// <summary>Raw 0 to 65535 as -1 to 1, with the middle at nought.</summary>
		private static float Centred(int raw)
		{
			return Clamp((raw - RawCentre) / (float)RawCentre, -1f, 1f);
		}

		/// <summary>Raw 0 to 65535 as 0 to 1.</summary>
		private static float Whole(int raw)
		{
			return Clamp(raw / (float)RawMax, 0f, 1f);
		}

		/// <summary>The upper half of a raw range as 0 to 1, with the lower half resting at nought.</summary>
		private static float Positive(int raw)
		{
			return raw <= RawCentre
				? 0f
				: Clamp((raw - RawCentre) / (float)RawCentre, 0f, 1f);
		}

		#region Destinations

		/// <summary>A formula's answer as a trigger position, which only travels one way.</summary>
		/// <remarks>
		/// The sign is dropped rather than clamped away, so a formula written for a stick still does
		/// something sensible when somebody puts it on a trigger.
		/// </remarks>
		public static byte ToTrigger(float value)
		{
			if (float.IsNaN(value))
				return 0;
			var size = value < 0f ? -value : value;
			// Rounded, not cut short. Half of a trigger's travel doubled is a whole one, and cutting
			// short reports that as one step less than full, for ever.
			return size >= 1f ? byte.MaxValue : (byte)Math.Round(size * byte.MaxValue, MidpointRounding.AwayFromZero);
		}

		/// <summary>A formula's answer as a stick position, which travels both ways.</summary>
		public static short ToThumb(float value)
		{
			if (float.IsNaN(value))
				return 0;
			if (value >= 1f)
				return short.MaxValue;
			if (value <= -1f)
				return short.MinValue;
			return (short)Math.Round(value * short.MaxValue, MidpointRounding.AwayFromZero);
		}

		/// <summary>Whether a formula's answer counts as a button being held.</summary>
		/// <remarks>
		/// Half way is the point, which is the obvious place and makes a formula that answers 0 or 1
		/// behave exactly as it reads.
		/// </remarks>
		public static bool IsPressed(float value)
		{
			return value >= 0.5f;
		}

		#endregion

		private static float Clamp(float value, float low, float high)
		{
			if (float.IsNaN(value))
				return low < 0f ? 0f : low;
			return value < low ? low : value > high ? high : value;
		}

	}
}
