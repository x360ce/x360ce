using System;

namespace x360ce.Engine
{
	/// <summary>The centering spring's force at every steering angle, shaped like a car's steering and read at the car's speed.</summary>
	/// <remarks>
	/// The force towards the centre passes through four points, times the strength: nothing at the centre, half at
	/// Centre, all of it at Peak, and Lightening of it at the stop. A monotone cubic (Fritsch-Carlson) joins them, so
	/// between two points it rises or falls no more than they do. With Peak at nought, at or beyond the stop, or at or
	/// inside Centre, the points are the centre, half at Centre, and all of it at the stop. Beyond the stop the curve
	/// stays at its value there. The other side of the centre is its mirror. Angles are steering wheel degrees, counted
	/// against the wheel range, or against <see cref="ForceFeedbackState.DefaultRangeDegrees"/> when the range is left as
	/// it is.
	///
	/// The points describe the car at 100 km/h, the speed of the ISO 13674-1 weave test the measurements come from. A
	/// car's wheel gets stiffer about in proportion to its speed, so the curve is read at the angle times the speed over
	/// 100: force(angle) = strength x curve(angle x speed / 100). Below <see cref="MinimumSpeed"/> the steering axis
	/// inclination still centres the wheel, so that is the least speed used.
	///
	/// The force follows the wheel a few milliseconds late, and a curve too steep at the centre makes the wheel chase its
	/// own force. So the slope at the centre, after the speed, is never steeper than
	/// <see cref="ForceFeedbackState.SpringStiffnessLimit"/> percent over <see cref="ForceFeedbackState.SpringRamp"/> axis
	/// units: Centre is widened until it is not. Within <see cref="ForceFeedbackState.SpringDeadBand"/> of the centre
	/// there is no force.
	///
	/// The curve is built into a table of whole percents, one for every axis unit from the centre, when a setting
	/// changes. A poll looks its force up there, and makes nothing.
	/// </remarks>
	public class SpringCurve
	{
		/// <summary>The least speed, in km/h, the curve is read at.</summary>
		public const int MinimumSpeed = 10;

		/// <summary>The speed, in km/h, the curve's points describe.</summary>
		public const int PointsSpeed = 100;

		/// <summary>The force in whole percent at each distance from the centre, in axis units, up to the stop.</summary>
		readonly byte[] table = new byte[SpringCalibration.Center + 2];

		// The curve's points, their slopes, and the slopes between them, in degrees and shares of the strength. Kept,
		// so a build makes nothing either.
		readonly double[] xs = new double[4];
		readonly double[] ys = new double[4];
		readonly double[] ms = new double[4];
		readonly double[] ds = new double[3];
		int count;

		/// <summary>The settings the table was built for. A strength of -1 means none yet.</summary>
		int builtStrength = -1;
		int builtCentre;
		int builtPeak;
		int builtLightening;
		int builtRange;
		int builtSpeed;

		/// <summary>The speed, in km/h, the table was built for, as it was given.</summary>
		public int Speed { get { return builtSpeed; } }

		/// <summary>How far from the centre the force reaches half the strength, in axis units: Centre after the speed and the steepness cap.</summary>
		public int CentreUnits { get; private set; }

		/// <summary>Builds the table for these settings, unless it was built for them already.</summary>
		/// <remarks>Called on every poll; when nothing changed it compares six numbers and returns.</remarks>
		/// <param name="strength">The strongest force, in percent of the device's force.</param>
		/// <param name="centreDegrees">The angle of half the strength, at 100 km/h.</param>
		/// <param name="peakDegrees">The angle of the strongest force, at 100 km/h, where nought is the stop.</param>
		/// <param name="lighteningPercent">How much of the strongest force is left at the stop.</param>
		/// <param name="rangeDegrees">The wheel's range lock to lock, or nought when it is left as it is.</param>
		/// <param name="speed">The car's speed in km/h.</param>
		public void Set(int strength, int centreDegrees, int peakDegrees, int lighteningPercent, int rangeDegrees, int speed)
		{
			if (strength == builtStrength && centreDegrees == builtCentre && peakDegrees == builtPeak
				&& lighteningPercent == builtLightening && rangeDegrees == builtRange && speed == builtSpeed)
				return;
			builtStrength = strength;
			builtCentre = centreDegrees;
			builtPeak = peakDegrees;
			builtLightening = lighteningPercent;
			builtRange = rangeDegrees;
			builtSpeed = speed;
			Build();
		}

		/// <summary>The force for a wheel position, in percent, positive towards the high end of the axis.</summary>
		/// <param name="position">The steering axis, 0 to <see cref="SpringCalibration.AxisMax"/>.</param>
		public int Force(int position)
		{
			var offset = position - SpringCalibration.Center;
			var distance = offset < 0 ? -offset : offset;
			if (distance >= table.Length)
				distance = table.Length - 1;
			int percent = table[distance];
			return offset < 0 ? percent : -percent;
		}

		/// <summary>The strength Auto sets for a wheel's friction: enough for the curve to beat it from the stop to Centre, and a tenth more.</summary>
		/// <remarks>
		/// Beyond Centre the curve is at its lowest either at Centre, half the strength, or at the stop, Lightening of it.
		/// So the strength is the friction over the smaller of the two, and a tenth more, rounded up. With the peak at the
		/// stop Lightening plays no part, and the lowest is half.
		/// </remarks>
		/// <param name="frictionPercent">The weakest constant force that brings the wheel home, in percent.</param>
		/// <param name="lighteningPercent">How much of the strongest force is left at the stop.</param>
		/// <param name="peakDegrees">The angle of the strongest force, where nought is the stop.</param>
		/// <param name="rangeDegrees">The wheel's range lock to lock, or nought when it is left as it is.</param>
		public static int AutoStrength(int frictionPercent, int lighteningPercent, int peakDegrees, int rangeDegrees)
		{
			if (frictionPercent <= 0)
				return 0;
			var range = rangeDegrees > 0 ? rangeDegrees : ForceFeedbackState.DefaultRangeDegrees;
			var peakAtStop = peakDegrees <= 0 || peakDegrees * 2 >= range;
			var lowest = peakAtStop ? 50 : Math.Max(0, Math.Min(50, lighteningPercent));
			if (lowest == 0)
				return 100;
			return Math.Min(100, (frictionPercent * 110 + lowest - 1) / lowest);
		}

		void Build()
		{
			var strength = Math.Max(0, Math.Min(100, builtStrength));
			var range = builtRange > 0 ? builtRange : ForceFeedbackState.DefaultRangeDegrees;
			// The speed stretches or squeezes the angle the curve is read at.
			var scale = Math.Max(MinimumSpeed, builtSpeed) / (double)PointsSpeed;
			var degreesPerUnit = (double)range / SpringCalibration.AxisMax;
			// The slope at the centre is half the strength over Centre, read at the speed. Centre is widened until that
			// is no steeper than the limit.
			var capped = scale * strength / 2.0 * ForceFeedbackState.SpringRamp / ForceFeedbackState.SpringStiffnessLimit * degreesPerUnit;
			var centre = Math.Max(Math.Max(1, builtCentre), capped);
			CentreUnits = (int)Math.Round(centre / scale / degreesPerUnit);
			if (strength == 0)
			{
				Array.Clear(table, 0, table.Length);
				return;
			}
			Points(centre, range / 2.0, builtPeak, Math.Max(0, Math.Min(100, builtLightening)) / 100.0);
			Slopes();
			// Degrees of the curve per axis unit of the wheel.
			var step = degreesPerUnit * scale;
			var distance = 0;
			for (; distance <= ForceFeedbackState.SpringDeadBand; distance++)
				table[distance] = 0;
			// Each segment is a cubic in its own share of the way, t, from one point to the next, with the strength
			// taken in: force = ((c3 t + c2) t + c1) t + c0, where t grows by the same step every axis unit.
			for (var i = 0; i < count - 1; i++)
			{
				var h = xs[i + 1] - xs[i];
				var c0 = strength * ys[i];
				var c1 = strength * h * ms[i];
				var c2 = strength * (3 * (ys[i + 1] - ys[i]) - h * (2 * ms[i] + ms[i + 1]));
				var c3 = strength * (2 * (ys[i] - ys[i + 1]) + h * (ms[i] + ms[i + 1]));
				var perUnit = step / h;
				var from = xs[i] / h;
				for (; distance < table.Length && distance * step < xs[i + 1]; distance++)
				{
					var t = distance * perUnit - from;
					table[distance] = Percent(((c3 * t + c2) * t + c1) * t + c0);
				}
			}
			// Beyond the stop the curve stays at its value there.
			var stopPercent = Percent(strength * ys[count - 1]);
			for (; distance < table.Length; distance++)
				table[distance] = stopPercent;
		}

		/// <summary>A force in percent, rounded to a whole percent and kept within the device's force.</summary>
		static byte Percent(double force)
		{
			return force <= 0 ? (byte)0 : force >= 100 ? (byte)100 : (byte)(force + 0.5);
		}

		/// <summary>The curve's points: the centre, Centre, Peak when there is one, and the stop.</summary>
		void Points(double centre, double stop, double peak, double lightening)
		{
			xs[0] = 0;
			ys[0] = 0;
			if (centre >= stop)
			{
				// Centre at or beyond the stop: the wheel never reaches half the strength.
				xs[1] = stop;
				ys[1] = 0.5 * stop / centre;
				count = 2;
			}
			else if (peak <= centre || peak >= stop)
			{
				xs[1] = centre;
				ys[1] = 0.5;
				xs[2] = stop;
				ys[2] = 1;
				count = 3;
			}
			else
			{
				xs[1] = centre;
				ys[1] = 0.5;
				xs[2] = peak;
				ys[2] = 1;
				xs[3] = stop;
				ys[3] = lightening;
				count = 4;
			}
		}

		/// <summary>The slope at each point, by Fritsch and Carlson, so the curve between two points never overshoots them.</summary>
		/// <remarks>
		/// The mirror before the centre gives the centre the slope towards Centre; the flat stretch beyond the stop gives
		/// the stop a slope of nought, as it does any point where the curve turns.
		/// </remarks>
		void Slopes()
		{
			for (var i = 0; i < count - 1; i++)
				ds[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
			ms[0] = ds[0];
			for (var i = 1; i < count - 1; i++)
				ms[i] = ds[i - 1] * ds[i] <= 0 ? 0 : (ds[i - 1] + ds[i]) / 2;
			ms[count - 1] = 0;
			for (var i = 0; i < count - 1; i++)
			{
				if (ds[i] == 0)
				{
					ms[i] = 0;
					ms[i + 1] = 0;
					continue;
				}
				var a = ms[i] / ds[i];
				var b = ms[i + 1] / ds[i];
				var sum = a * a + b * b;
				if (sum > 9)
				{
					var t = 3 / Math.Sqrt(sum);
					ms[i] = t * a * ds[i];
					ms[i + 1] = t * b * ds[i];
				}
			}
		}
	}
}
