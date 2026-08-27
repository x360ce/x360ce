using System;
using System.Globalization;

namespace x360ce.Engine
{
	/// <summary>
	/// Writes a row's existing dead zone, anti dead zone and sensitivity out as an expression.
	/// </summary>
	/// <remarks>
	/// An expression replaces those three settings for its row rather than being layered on top of
	/// them. Layering has no defensible order, and the two work in different units: the settings are in
	/// the destination's units and an expression is in normalised ones.
	///
	/// Replacing them would lose somebody's tuning at the moment they switch the row over, so switching
	/// over writes the tuning out as the formula that produces it. Nothing is lost, the result is
	/// unchanged until they edit it, and they are shown their own configuration written in the syntax
	/// they are about to use, which teaches it better than any help page.
	/// </remarks>
	public static class MapExpressionSeed
	{

		/// <summary>Largest value a thumb stick reports, and the unit its dead zones are given in.</summary>
		public const float ThumbMax = 32767f;

		/// <summary>Largest value a trigger reports, and the unit its dead zones are given in.</summary>
		public const float TriggerMax = 255f;

		/// <summary>
		/// A stored mapping written the way a formula names the same control.
		/// </summary>
		/// <param name="stored">The mapping as it is kept, such as "a1", "x3" or "1".</param>
		/// <returns>The same control as a formula names it, or null when it has no plain form.</returns>
		/// <remarks>
		/// The two notations disagree about buttons and the disagreement is silent. Storage writes a
		/// button as a bare number, so button one is kept as "1". Inside a formula "1" is the number
		/// one, which is a value that never changes rather than a button anybody can press. Handing a
		/// stored button straight to a formula therefore produces something that reads as sensible,
		/// compiles, runs, and is not the control the person chose.
		///
		/// A stick pushed the other way is stored as a negative number and becomes a minus sign, which
		/// is exactly what it means. Nothing else has a plain opposite: half of an axis read backwards,
		/// or a button counted as released, cannot be said with the sources a formula has. Those give
		/// nothing back rather than something close, because a formula that is nearly right is worse
		/// than no formula at all.
		/// </remarks>
		public static string AsExpressionSource(string stored)
		{
			if (string.IsNullOrEmpty(stored))
				return null;
			var text = stored.Trim();
			if (text.Length == 0 || MapExpression.IsExpression(text))
				return null;
			// A letter at the front names the kind. Without one it is a button, which is stored bare.
			var letter = char.IsLetter(text[0]) ? char.ToLowerInvariant(text[0]) : 'b';
			var digits = char.IsLetter(text[0]) ? text.Substring(1) : text;
			if (MapExpression.SourceLetters.IndexOf(letter) < 0)
				return null;
			int index;
			if (!int.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out index))
				return null;
			if (index == 0)
				return null;
			var source = letter.ToString() + Math.Abs(index).ToString(CultureInfo.InvariantCulture);
			if (index > 0)
				return source;
			// Only a whole axis has a plain opposite: the same stick pushed the other way.
			return letter == 'a' ? "-" + source : null;
		}

		/// <summary>
		/// The expression that reproduces a row's current settings, including the leading prefix.
		/// </summary>
		/// <param name="source">Source the row reads, written as it is stored, such as "a1".</param>
		/// <param name="deadZone">Dead zone in the destination's units.</param>
		/// <param name="antiDeadZone">Anti dead zone in the destination's units.</param>
		/// <param name="linear">Sensitivity, as the interface stores it, -100 to 100.</param>
		/// <param name="destinationMax">
		/// <see cref="ThumbMax"/> or <see cref="TriggerMax"/>, whichever the row drives.
		/// </param>
		/// <remarks>
		/// The shortest expression that produces the same result is written, so a row with nothing set
		/// seeds as the bare source rather than as arithmetic that cancels itself out. A person opening
		/// a formula for the first time should see something they can read.
		/// </remarks>
		public static string FromSettings(string source, float deadZone, float antiDeadZone, float linear, float destinationMax)
		{
			if (string.IsNullOrEmpty(source))
				return MapExpression.Prefix;
			if (destinationMax <= 0f)
				destinationMax = ThumbMax;
			var dead = Clamp01(deadZone / destinationMax);
			var anti = Clamp01(antiDeadZone / destinationMax);
			var curve = Math.Max(-1f, Math.Min(1f, linear / 100f));
			// Nothing set, so the value passes through and the formula says exactly that.
			if (dead <= 0f && anti <= 0f && curve == 0f)
				return MapExpression.Prefix + source;
			// Everything below works on the distance from the centre, and the direction is put back at
			// the end. Without that a dead zone would only work one way.
			var size = "abs(" + source + ")";
			// Named rather than spelled out. The same three steps written as arithmetic came to over
			// three hundred characters for a fully tuned row, because each part had to be repeated for
			// every place it appeared - too long to store, and far too long to learn anything from.
			if (dead > 0f)
				size = "deadzone(" + size + "," + N(dead) + ")";
			if (curve != 0f)
				size = "curve(" + size + "," + N(curve) + ")";
			if (anti > 0f)
				size = "antideadzone(" + size + "," + N(anti) + ")";
			var full = MapExpression.Prefix + "sign(" + source + ")*" + size;
			// A row's settings written out in full do not fit in sixteen characters, which is what a
			// mapping is stored in until the column is widened. Rather than offer something that
			// cannot be saved, the plain source is given: switching the row over then changes nothing
			// and loses nothing, because the settings it came from are still there to be read.
			return full.Length <= MapExpression.MaxLength
				? full
				: MapExpression.Prefix + source;
		}

		/// <summary>
		/// A number as the grammar reads it: a dot for the decimal mark, and no trailing noise.
		/// </summary>
		private static string N(float value)
		{
			var text = Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
			return text.Length == 0 ? "0" : text;
		}

		private static float Clamp01(float value)
		{
			if (float.IsNaN(value) || value < 0f)
				return 0f;
			// A dead zone of the whole travel would leave nothing to stretch, and dividing by what is
			// left would not be a number.
			return value > 0.99f ? 0.99f : value;
		}

	}
}
