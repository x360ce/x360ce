using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using x360ce.Engine.Data;

namespace x360ce.Engine
{

    /// <summary>
    /// Class used to store current force feedback state of device.
    /// </summary>
    public class ForceFeedbackState
    {
        public ForceFeedbackState()
        {
            PeriodicForceL = new PeriodicForce();
            PeriodicForceR = new PeriodicForce();
            ConstantForceL = new ConstantForce();
            ConstantForceR = new ConstantForce();
            GUID_Force = EffectGuid.ConstantForce;
        }

        const uint INFINITE = 0xFFFFFFFF;
        const uint DIEB_NOTRIGGER = 0xFFFFFFFF;

        EffectParameters GetParameters()
        {
            var p = new EffectParameters();
            p.Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds;
            p.StartDelay = 0;
            p.Duration = unchecked((int)INFINITE);
            p.SamplePeriod = 0;
            p.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
            p.TriggerRepeatInterval = unchecked((int)INFINITE);
            return p;
        }

        // Effects this program made on the device are held here by reference. The device's own
        // list of created effects is shared by all three, so a position in it names nothing.

        // Left

        DeviceObjectItem actuatorL;
        internal EffectParameters paramsL;
        internal Effect effectL;

        /// <summary>Effects the device said it will never take. Asked once, then left alone, or every poll would ask again and every answer would be reported.</summary>
        readonly HashSet<Effect> unsupported = new HashSet<Effect>();
        /// <summary>The motor effects the device would not make with the current type, motors and directions. Asked for again when one of those changes, or by a new state.</summary>
        MotorEffects motorsRefused;
        public PeriodicForce PeriodicForceL;
        public ConstantForce ConstantForceL;

        // Right

        DeviceObjectItem actuatorR;
        internal EffectParameters paramsR;
        internal Effect effectR;
        public PeriodicForce PeriodicForceR;
        public ConstantForce ConstantForceR;

        // Centering spring: a constant force on the same actuator as the left motor, set by this
        // program from the spring's curve as the wheel moves.

        EffectParameters paramsS;
        Effect effectS;
        readonly ConstantForce springForce = new ConstantForce();
        string old_SpringEnable;
        string old_SpringStrength;
        string old_SpringCentre;
        string old_SpringPeak;
        string old_SpringLightening;
        string old_SpringDamping;
        string old_SpringSpeed;
        // The spring's settings, read once when one changes rather than parsed every poll.
        /// <summary>The spring strength in percent, or nought when the spring is off.</summary>
        int springStrength;
        int springCentre = int.Parse(SettingName.DefaultForceSpringCentre);
        int springPeak = int.Parse(SettingName.DefaultForceSpringPeak);
        int springLightening = int.Parse(SettingName.DefaultForceSpringLightening);
        int springSpeed = int.Parse(SettingName.DefaultForceSpringSpeed);
        /// <summary>The damping that goes with the spring, in DirectInput's units, or nought when the spring is off.</summary>
        int springDamper;
        /// <summary>The spring's force at every angle, built when a setting, the wheel range or the speed changes.</summary>
        public readonly SpringCurve Curve = new SpringCurve();
        /// <summary>The force the device holds now, so a poll that would set the same value sets nothing.</summary>
        int springMagnitude;
        /// <summary>The device refused the spring effect. Asked once, or every poll would ask and throw.</summary>
        bool springRefused;
        /// <summary>The error the device refused the spring effect with, or 0 while it has not, for bug report recordings.</summary>
        public int SpringRefusedError { get; private set; }

        // Damping that goes with the spring: the device itself resists the wheel's speed, so the
        // wheel does not swing past the centre and back.

        EffectParameters paramsD;
        Effect effectD;
        int damperCoefficient;
        /// <summary>The damping the device is holding right now, in its units, or nought when it holds none.</summary>
        public int DamperOnDevice { get { return effectD == null ? 0 : damperCoefficient; } }
        /// <summary>Which axis in the device state the spring's actuator moves, or -1 while unknown.</summary>
        public int SpringAxisIndex = -1;

        // Steering range, sent to the wheel itself rather than through DirectInput.

        string old_WheelRange;
        Joystick rangeDevice;
        /// <summary>The HID interface of the wheel the range is for, or null when it is sent to none.</summary>
        string rangePath;
        /// <summary>The range to send, in degrees.</summary>
        int rangeToSend;
        /// <summary>True while the range waits to be sent, at <see cref="rangeDueAt"/>.</summary>
        bool rangeDue;
        int rangeDueAt;
        /// <summary>Sends left before a wheel that will not take the range is left as it is.</summary>
        int rangeTriesLeft;
        /// <summary>True while a later send waits, at <see cref="rangeLaterAt"/>, behind the one due at <see cref="rangeDueAt"/>.</summary>
        bool rangeLaterDue;
        int rangeLaterAt;
        /// <summary>The shortest and the longest delay, in milliseconds, the range was asked to be sent again after, or -1 while none was.</summary>
        /// <remarks>Set by the interface, on any thread, and taken by the engine, without a lock.</remarks>
        int askedSoonestMs = -1;
        int askedLatestMs = -1;

        /// <summary>How often a range the wheel would not take is sent again, and how many times.</summary>
        public const int RangeRetryMs = 1000;
        public const int RangeTries = 5;

        /// <summary>How long after another program comes to the front the range is sent again, first and last.</summary>
        /// <remarks>
        /// Logitech's driver puts its own range on the wheel when a game takes it, a moment after the game comes to the front,
        /// and nothing tells when. The second send puts the chosen range back after the driver's.
        /// </remarks>
        public const int RangeAgainSoonMs = 500;
        public const int RangeAgainLateMs = 2000;

        /// <summary>Sends the range again: the wheel may have been set to another since.</summary>
        /// <remarks>
        /// Logitech's software puts its own range on the wheel when a program holds it, and the person sees the list still
        /// naming the range they chose. Choosing it again sends it, on the engine's next poll. Asked several times before the
        /// engine takes the asks, the range is sent after the shortest delay and again after the longest.
        /// </remarks>
        /// <param name="delayMs">How long after the engine's next poll the range is sent, in milliseconds.</param>
        public void SendRangeAgain(int delayMs = 0)
        {
            delayMs = Math.Max(0, delayMs);
            KeepAsk(ref askedSoonestMs, delayMs, true);
            KeepAsk(ref askedLatestMs, delayMs, false);
        }

        /// <summary>Puts the delay in the slot while the slot holds none, or a longer one for the soonest or a shorter one for the latest.</summary>
        static void KeepAsk(ref int slot, int delayMs, bool soonest)
        {
            while (true)
            {
                var held = Volatile.Read(ref slot);
                if (held >= 0 && (soonest ? held <= delayMs : held >= delayMs))
                    return;
                if (Interlocked.CompareExchange(ref slot, delayMs, held) == held)
                    return;
            }
        }

        /// <summary>Takes the range the spring's curve is counted against, and queues the range for the wheel when it is to be sent.</summary>
        /// <remarks>
        /// The range goes to the wheel once per setting and once per device, because a wheel plugged in again has forgotten
        /// it, and each time the device is held again, because Logitech's software puts its own range on the wheel then. Not
        /// before the device is known by its ids: they come from a read of the machine that finishes after the first polls, and
        /// a range sent while they were still nought was sent to nothing. Sent once the device settles, as its own centering
        /// is, and again while the wheel will not take it (<see cref="UpdateCentering"/>).
        /// </remarks>
        /// <param name="heldAgain">The device was held again since the last force update.</param>
        public void UpdateRange(UserDevice ud, Joystick device, PadSetting ps, bool heldAgain, int tickCount)
        {
            var range = ps.GetWheelRange();
            rangeDegrees = SpringRange(range, ud);
            if (ud.DevVendorId == 0 || !(Changed(ref old_WheelRange, ps.WheelRange) | rangeDevice != device | heldAgain))
                return;
            rangeDevice = device;
            rangeToSend = range;
            rangePath = range > 0 && LogitechWheel.SupportsRange(ud.DevVendorId, ud.DevProductId) ? ud.HidDevicePath : null;
            rangeDue = false;
            if (rangePath == null)
                rangeLaterDue = false;
            QueueRange(tickCount + (heldAgain ? CenteringSettleMs : 0));
        }

        /// <summary>Sends the range once it is due, and again a second later while the wheel will not take it.</summary>
        /// <remarks>
        /// A send asked for while another waits keeps both: the sooner is sent first and the later after it. Of three or more,
        /// the soonest and the latest are kept.
        /// </remarks>
        /// <param name="dueAt">When to send it, by <see cref="Environment.TickCount"/>.</param>
        void QueueRange(int dueAt)
        {
            if (rangePath == null)
                return;
            if (!rangeDue)
            {
                rangeDue = true;
                rangeDueAt = dueAt;
                rangeTriesLeft = RangeTries;
                return;
            }
            if (dueAt - rangeDueAt < 0)
            {
                var later = rangeDueAt;
                rangeDueAt = dueAt;
                rangeTriesLeft = RangeTries;
                dueAt = later;
            }
            else if (dueAt == rangeDueAt)
                return;
            if (!rangeLaterDue || dueAt - rangeLaterAt > 0)
            {
                rangeLaterDue = true;
                rangeLaterAt = dueAt;
            }
        }

        // The wheel's own centering, switched on its HID interface on the wheels that take it.

        /// <summary>The HID interface of the wheel whose own centering this state switched, or null.</summary>
        string centeringPath;
        /// <summary>Whether the wheel's own centering is to be on, or null before it was decided.</summary>
        bool? centeringOn;
        /// <summary>True while the decision waits to be sent, at <see cref="centeringDueAt"/>.</summary>
        bool centeringDue;
        int centeringDueAt;

        /// <summary>How long after an effect is made the wheel's own centering is switched.</summary>
        /// <remarks>
        /// The driver switches it off itself shortly after the first effect starts on a device held again, and a
        /// report sent before that is lost. Measured on a G27: sent at once it was lost, sent 100 ms later it held.
        /// </remarks>
        public const int CenteringSettleMs = 250;

        /// <summary>Sends the wheel's range and its own centering once each is due. Called every poll: flags and clock compares until then.</summary>
        public void UpdateCentering(int tickCount)
        {
            if (Volatile.Read(ref askedSoonestMs) >= 0 || Volatile.Read(ref askedLatestMs) >= 0)
            {
                var soonest = Interlocked.Exchange(ref askedSoonestMs, -1);
                var latest = Interlocked.Exchange(ref askedLatestMs, -1);
                if (soonest >= 0)
                    QueueRange(tickCount + soonest);
                if (latest >= 0)
                    QueueRange(tickCount + latest);
            }
            if (rangeDue && tickCount - rangeDueAt >= 0)
            {
                var taken = LogitechWheel.SetRange(rangePath, rangeToSend);
                RangeLastDegrees = rangeToSend;
                RangeLastError = taken ? 0 : LogitechWheel.LastError;
                Counts.RangeSends++;
                rangeDue = !taken && --rangeTriesLeft > 0;
                rangeDueAt = tickCount + RangeRetryMs;
                // A later send takes the place of a retry due after it, as it sends the same range.
                if (rangeLaterDue && (!rangeDue || rangeLaterAt - rangeDueAt <= 0))
                {
                    rangeLaterDue = false;
                    rangeDue = false;
                    QueueRange(rangeLaterAt);
                }
            }
            if (!centeringDue || tickCount - centeringDueAt < 0)
                return;
            centeringDue = false;
            SendCentering(centeringOn == true);
        }

        /// <summary>Switches the wheel's own centering on its HID interface, and keeps what came of it for a recording.</summary>
        void SendCentering(bool on)
        {
            var taken = LogitechWheel.SetCentering(centeringPath, on);
            CenteringLastOn = on;
            CenteringLastError = taken ? 0 : LogitechWheel.LastError;
            Counts.CenteringSends++;
        }

        #region Force Feedback

        const int DI_FFNOMINALMAX = 10000;

        // Force type changed by settings.
        string old_ForceType = "-1";
		string old_ForceSwapMotor = "-1";
        // Force parameters changed by settings.
        string old_LeftPeriod;
        string old_RightPeriod;
        string old_LeftStrength;
        string old_RightStrength;
        string old_LeftDirection;
        string old_RightDirection;
        string old_OveralStrength;
        /// <summary>The Force feedback enabled switch the last update was made with. Off with the spring on, the game's vibration is nought.</summary>
        string old_ForceEnable;
        // Speed sent by the game.
        short old_LeftMotorSpeed;
        short old_RightMotorSpeed;
        /// <summary>Latest normalised rumble drive (0 to 1) from the game, used for optional zero-telemetry understeer lightening.</summary>
        double rumbleDrive;
        /// <summary>Smoothed rumble envelope (0 to 1) updated on each spring poll.</summary>
        double rumbleEnvelope;
        long lastSpringClockMs = -1;

        /// <summary>The signed spring force in percent (-100 to +100) computed on the last <see cref="UpdateSpring"/> call.</summary>
        public int LastSpringPercent { get; private set; }

        /// <summary>The parts the spring's force was made of on the last poll that worked it out, for a recording. Engine thread only.</summary>
        public SpringTerms LastSpringTerms;

        /// <summary>The damper sent on the last poll, in DirectInput units (0 to 10000).</summary>
        public int LastDamper { get { return damperCoefficient; } }

        /// <summary>The effect a game's vibration is played with: constant (a push along the axis) or a periodic one (a shake).</summary>
        public string MotorEffectName
        {
            get
            {
                return GUID_Force == EffectGuid.ConstantForce ? "constant push" : GUID_Force == EffectGuid.Sine ? "sine shake"
                    : GUID_Force == EffectGuid.Square ? "square shake" : GUID_Force == EffectGuid.Triangle ? "triangle shake"
                    : GUID_Force == EffectGuid.SawtoothUp || GUID_Force == EffectGuid.SawtoothDown ? "sawtooth shake" : "other";
            }
        }

        /// <summary>The strength the left motor's effect is played at now, in DirectInput units (-10000 to 10000).</summary>
        public int MotorMagnitudeL
        {
            get { return effectL == null ? 0 : GUID_Force == EffectGuid.ConstantForce ? ConstantForceL.Magnitude : PeriodicForceL.Magnitude; }
        }

        /// <summary>The strength the right motor's effect is played at now, in DirectInput units.</summary>
        public int MotorMagnitudeR
        {
            get { return effectR == null ? 0 : GUID_Force == EffectGuid.ConstantForce ? ConstantForceR.Magnitude : PeriodicForceR.Magnitude; }
        }

        /// <summary>The last range and own centering sent to the wheel's HID interface, and why each was not taken (0 when it was).</summary>
        /// <remarks>Read by a recording through <see cref="CallCounts.RangeSends"/> and <see cref="CallCounts.CenteringSends"/>, which tell a new send.</remarks>
        public int RangeLastDegrees, RangeLastError;
        public bool CenteringLastOn;
        public int CenteringLastError;

        Guid GUID_Force;

        /// <summary>The last force update threw before it finished, so settings it recorded as sent may not have reached the device.</summary>
        bool interrupted;

        /// <summary>What force updates asked of the motor effects since this state was made, counted for a bug report recording.</summary>
        /// <remarks>Written by the engine thread only. Read by any thread: each count is one number, read whole.</remarks>
        public sealed class CallCounts
        {
            /// <summary>Force updates that reached the device.</summary>
            public int Updates;
            /// <summary>Motor effects made.</summary>
            public int Made;
            /// <summary>Effects started again after the device was held again, the spring's among them.</summary>
            public int Resumed;
            /// <summary>Changes sent to a motor effect.</summary>
            public int Changes;
            /// <summary>Of <see cref="Changes"/>, those sent with a start, because the driver did not say the effect was playing.</summary>
            public int Started;
            /// <summary>Steering ranges sent to the wheel, those it would not take included.</summary>
            public int RangeSends;
            /// <summary>The wheel's own centering switched on its HID interface, those it would not take included.</summary>
            public int CenteringSends;
            /// <summary>Gear changes found in the telemetry, see <see cref="GameRumblePercent"/>.</summary>
            public int GearChanges;
        }

        /// <summary>What force updates asked of the motor effects, for a recording.</summary>
        public readonly CallCounts Counts = new CallCounts();

        #region Game rumble while telemetry drives the spring

        /// <summary>How long after a gear change starts the game's rumble plays at the gear change's share, in milliseconds.</summary>
        public const long GearChangeRumbleMs = 150;

        /// <summary>The least fall of engine speed between two samples, as a share of the redline, that is a gear change while the car gains speed.</summary>
        public const float GearChangeRpmDrop = 0.15f;

        /// <summary>The gear of the last telemetry sample, or -1 when the game sends none.</summary>
        public int LastGear { get; private set; } = -1;

        /// <summary>The share of the game's rumble, in percent, that played for the last gear change found.</summary>
        public int LastGearChangeRumblePercent { get; private set; }

        /// <summary>When the last gear change was found, by the telemetry's clock in milliseconds, or 0 for none.</summary>
        public long LastGearChangeMs { get; private set; }

        long gearSampleMs;
        float gearLastRpm = float.NaN;
        float gearLastSpeed = float.NaN;

        /// <summary>
        /// The share of the game's rumble, in percent, that plays this poll while live telemetry drives the centering spring:
        /// 100 when the telemetry is not fresh, 0 when the game's rumble is ignored, and
        /// <paramref name="gearChangePercent"/> for <see cref="GearChangeRumbleMs"/> after a gear change starts. Engine thread
        /// only; makes nothing.
        /// </summary>
        /// <param name="telemetry">The pass's telemetry, or <see cref="WheelTelemetrySample.Inactive"/> while the spring or telemetry is off.</param>
        /// <param name="nowMs">Monotonic clock in milliseconds.</param>
        /// <param name="ignoreGame">True when the wheel's road and impact feel comes from telemetry alone.</param>
        /// <param name="gearChangePercent">The share of the game's rumble that plays while the gear changes, 0 to 100; 100 leaves it whole.</param>
        /// <remarks>
        /// A gear change is the telemetry's gear changing, or, from a game that sends no gear, the engine speed falling by
        /// <see cref="GearChangeRpmDrop"/> of the redline or more between two samples while the car gains speed.
        /// </remarks>
        public int GameRumblePercent(WheelTelemetrySample telemetry, long nowMs, bool ignoreGame, int gearChangePercent)
        {
            if (!telemetry.Active || WheelTelemetryMath.Freshness(telemetry.TimestampMs, nowMs) <= 0.0)
            {
                LastGear = -1;
                gearLastRpm = float.NaN;
                gearLastSpeed = float.NaN;
                return 100;
            }
            var share = Math.Max(0, Math.Min(100, gearChangePercent));
            if (telemetry.TimestampMs != gearSampleMs)
            {
                gearSampleMs = telemetry.TimestampMs;
                var changed = false;
                if (telemetry.Gear >= 0)
                    changed = LastGear >= 0 && telemetry.Gear != LastGear;
                else
                    changed = gearLastRpm - telemetry.RpmFraction >= GearChangeRpmDrop && telemetry.SpeedKmh > gearLastSpeed;
                LastGear = telemetry.Gear;
                gearLastRpm = telemetry.RpmFraction;
                gearLastSpeed = telemetry.SpeedKmh;
                if (changed)
                {
                    LastGearChangeMs = telemetry.TimestampMs;
                    LastGearChangeRumblePercent = ignoreGame ? 0 : share;
                    Counts.GearChanges++;
                }
            }
            if (ignoreGame)
                return 0;
            if (LastGearChangeMs > 0 && nowMs - LastGearChangeMs < GearChangeRumbleMs)
                return share;
            return 100;
        }

        /// <summary>The share of the game's rumble, in percent, the motors last played, so a share that changes is sent though the game sent nothing new.</summary>
        public int LastGameRumblePercent = 100;

        #endregion

        /// <summary>Takes every effect this state made off the device, before the state is dropped.</summary>
        /// <remarks>
        /// Disposed rather than stopped: disposing an effect takes its force off as a stop does, and asks the
        /// device nothing, so it cannot fail. A device the program has let go of no longer holds its effects, and
        /// DirectInput answers a stop or a status asked of one of them with DIERR_NOTDOWNLOADED.
        /// </remarks>
        public void StopDeviceForces()
        {
            Dispose(ref effectL);
            Dispose(ref effectR);
            Dispose(ref effectS);
            Dispose(ref effectD);
            // The effects took the wheel's own centering away, so it is given back with them.
            if (centeringPath != null)
                SendCentering(true);
            centeringPath = null;
            centeringOn = null;
            centeringDue = false;
        }

        static void Dispose(ref Effect effect)
        {
            if (effect == null)
                return;
            effect.Dispose();
            effect = null;
        }

        // Xbox One gamepads are equipped with a total of four independent vibration motors:
        // Two large motors located in the gamepad body:
        //	- Left  motor provides rough, high-amplitude vibration.
        //	- Right motor provides gentler, more subtle vibration.
        // Two small motors located inside each trigger,
        // that provide sharp bursts of vibration directly to the user's trigger fingers.

        /// <param name="heldAgain">
        /// The device was held again since the last update that finished, and forgot what was playing, so the effects this
        /// state holds are started again. Otherwise only the effects whose values changed are sent, as x360ce 5 does: with a
        /// driver that never says an effect is playing, every update would start both motors' effects again, the right one last.
        /// </param>
        public bool SetDeviceForces(UserDevice ud, Joystick device, PadSetting ps, Vibration v, bool heldAgain)
		{
			Changed(ref old_ForceEnable, ps.ForceEnable);
			var motorsChanged = Changed(ref old_ForceSwapMotor, ps.ForceSwapMotor);
			bool swapMotor = false;

			if (motorsChanged)
			{
				// Find and assign actuators.
				var actuators = ud.DeviceObjects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();
				DeviceObjectItem xMotor = null;
				// If actuator available then...
				if (actuators.Count > 0)
				{
					// Try to find left actuator.
					xMotor = actuators.FirstOrDefault(x => x.Type == ObjectGuid.XAxis);
					//var actuator = actuators[0];
					// If default actuator not found then take default.
					if (xMotor == null)
						xMotor = actuators[0];
					actuators.Remove(xMotor);
				}
				DeviceObjectItem yMotor = null;
				// If actuator available then...
				if (actuators.Count > 0)
				{
					// Try to find right actuator.
					yMotor = actuators.FirstOrDefault(x => x.Type == ObjectGuid.YAxis);
					//var actuator = actuators[0];
					// If default actuator not found then take default.
					if (yMotor == null)
						yMotor = actuators[0];
					actuators.Remove(yMotor);
				}
				swapMotor = TryParse(ps.ForceSwapMotor) == 1;
				// Allow to swap if both motors exist.
				if (swapMotor && xMotor != null && yMotor != null)
				{
					actuatorL = yMotor;
					actuatorR = xMotor;
				}
				else
				{
					actuatorL = xMotor;
					actuatorR = yMotor;
				}
				SpringAxisIndex = actuatorL == null ? -1 : actuatorL.DiIndex;
			}

			// Return if force feedback actuators not found.
			if (actuatorL == null)
                return false;

			// Cleared only when the update finishes. One that threw recorded its settings, the effect type and the motor
			// swap among them, before the device took them, so the next update makes every effect again with every
			// setting, as a new type or new motors do, not only what changed since.
			var resend = interrupted;
			interrupted = true;

			// A new state on a device still carrying effects from an earlier one starts clean, so
			// the effects it makes are the only ones the device holds. A state that remembers a
			// refusal is not new, even when it holds nothing.
			if (effectL == null && effectR == null && effectS == null && effectD == null && motorsRefused == MotorEffects.None)
				DisposeDeviceEffects(device);
			Counts.Updates++;
			// Only after the device was held again; each one resumed, so not short-circuited.
			var restarted = heldAgain && (Resume(effectL) | Resume(effectR) | Resume(effectS) | Resume(effectD));

			// Effect type changed.
			bool forceChanged =	Changed(ref old_ForceType, ps.ForceType);

			// Read on every poll, not only when the setting changes: effects that are made again after
			// the device lost them take their layout from it too, and a '2' type made as its plain type
			// drives one motor on the pads that need both axes in one effect.
			var forceType = (ForceEffectType)TryParse(ps.ForceType);
			if (motorsChanged || forceChanged)
            {
                // Update values.
				GUID_Force = ForceFeedbackDriver.EffectFor(forceType, ud.ForceFeedbackDriver);
                // A refusal is the answer for the effect that was asked for, so a new type or new
                // motors are asked for again.
                motorsRefused = MotorEffects.None;
            }
            // The directions are part of what was asked for too.
            var directionLChanged = Changed(ref old_LeftDirection, ps.LeftMotorDirection);
            var directionRChanged = Changed(ref old_RightDirection, ps.RightMotorDirection);
            if (directionLChanged)
                motorsRefused &= ~MotorEffects.Left;
            if (directionRChanged)
                motorsRefused &= ~MotorEffects.Right;

            // Which of the effects this state holds are stopped and disposed, and which are made.
            // On a single-actuator wheel with periodic effects, try two concurrent effects on actuatorL (Left impact + Right texture)
            // so both motor frequencies and strengths stay distinct; if the driver refuses the second effect, fall back transparently
            // to combining both motors into effectL.
            var singleActuatorDual = actuatorR == null && GUID_Force != EffectGuid.ConstantForce && (motorsRefused & MotorEffects.Right) == 0;
            var hasRightEffect = actuatorR != null || singleActuatorDual;
            var motors = hasRightEffect ? (MotorEffects.Left | MotorEffects.Right) : MotorEffects.Left;
            var held = (effectL == null ? MotorEffects.None : MotorEffects.Left)
                | (effectR == null ? MotorEffects.None : MotorEffects.Right);
            MotorEffects drop, make;
            PlanEffects(motors, held, motorsRefused, motorsChanged || forceChanged || resend, out drop, out make);
            if ((drop & MotorEffects.Left) != 0)
            {
                effectL.Stop();
                effectL.Dispose();
                effectL = null;
            }
            if ((drop & MotorEffects.Right) != 0)
            {
                effectR.Stop();
                effectR.Dispose();
                effectR = null;
            }
            // From here on, whether the effects are made again.
            forceChanged = make != MotorEffects.None;

            // Tells which effect parameters to modify.
            var flagsL = EffectParameterFlags.None;
            var flagsR = EffectParameterFlags.None;

            if (motorsChanged || forceChanged)
            {
                // If 2 actuators available
                if (actuatorR != null)
                {
					paramsL = GetParameters();
					paramsR = GetParameters();
					// Unfortunately SpeedLink GamePad needs both axis specified in order to operate motors separately.
					// Which is counter-intuitive.
					if (forceType.HasFlag(ForceEffectType._Type2))
					{
						// Note: Second axis direction will be set to zero i.e. motor will be not used by effect.
						// Directions must be set to 'Positive' on both first axis via force feedback settings interface.
						paramsL.Axes = new int[2] { actuatorL.ObjectId, actuatorR.ObjectId };
						paramsR.Axes = new int[2] { actuatorR.ObjectId, actuatorL.ObjectId };
						// There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
						//flagsR |= EffectParameterFlags.Axes;
					}
					// Used for normal devices like Logitech. Use one axis per Effect/Parameter.
					else
					{
						paramsL.Axes = new int[1] { actuatorL.ObjectId };
						paramsR.Axes = new int[1] { actuatorR.ObjectId };
						// There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
						//flagsR |= EffectParameterFlags.Axes;
					}
				}
                // If one actuator available.
                else if (actuatorL != null)
                {
                    paramsL = GetParameters();
                    paramsL.Axes = new int[1] { actuatorL.ObjectId };
                    if (singleActuatorDual)
                    {
                        paramsR = GetParameters();
                        paramsR.Axes = new int[1] { actuatorL.ObjectId };
                    }
                    else
                    {
                        paramsR = null;
                    }
                }
            }

            // Direction changed.
            // Right-handed Cartesian direction:
            // x: -1 = left,     1 = right,   0 - no direction
            // y: -1 = backward, 1 = forward, 0 - no direction
            // z: -1 = down,     1 = up,      0 - no direction

            // Direction needs to be updated when force or direction change.
            if (motorsChanged || forceChanged || directionLChanged)
            {
                var directionL = TryParse(old_LeftDirection);
                var dirL = new int[paramsL.Axes.Length];
                dirL[0] = directionL;
                paramsL.Directions = dirL;
                flagsL |= EffectParameterFlags.Direction;
            }

            // Direction needs to be updated when force or direction change.
            if (hasRightEffect && paramsR != null && (motorsChanged || forceChanged || directionRChanged))
            {
                var directionR = TryParse(old_RightDirection);
                var dirR = new int[paramsR.Axes.Length];
                dirR[0] = directionR;
                paramsR.Directions = dirR;
                flagsR |= EffectParameterFlags.Direction;
            }

            var strengthChanged = Changed(ref old_OveralStrength, ps.ForceOverall);
            var strengthLChanged = Changed(ref old_LeftStrength, ps.LeftMotorStrength);
            var strengthRChanged = Changed(ref old_RightStrength, ps.RightMotorStrength);

            if (motorsChanged || forceChanged || strengthChanged || strengthLChanged)
            {
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int leftGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetLeftMotorStrength());
                paramsL.Gain = leftGain;
                flagsL |= EffectParameterFlags.Gain;
            }

            if (hasRightEffect && paramsR != null && (motorsChanged || forceChanged || strengthChanged || strengthRChanged))
            {
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int rightGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetRightMotorStrength());
                paramsR.Gain = rightGain;
                flagsR |= EffectParameterFlags.Gain;
            }

            var periodLChanged = Changed(ref old_LeftPeriod, ps.LeftMotorPeriod);
            var periodRChanged = Changed(ref old_RightPeriod, ps.RightMotorPeriod);

            var speedLChanged = Changed(ref old_LeftMotorSpeed, v.LeftMotorSpeed);
            var speedRChanged = Changed(ref old_RightMotorSpeed, v.RightMotorSpeed);
            rumbleDrive = Math.Max(Drive(old_LeftMotorSpeed), Drive(old_RightMotorSpeed));

            // Convert speed into magnitude/amplitude.
            int leftMagnitudeAdjusted;
            int rightMagnitudeAdjusted = 0;

            int leftPeriod;
            int rightPeriod = 0;

            // Combine into effectL when the device has only one actuator and does not run a separate right effect.
            var combine = !hasRightEffect;

            // Get right values first for possible combine later.
            if (motorsChanged || forceChanged || periodRChanged || speedRChanged || combine)
            {
                rightMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, old_RightMotorSpeed);
                // The setting is the period at full drive; a real motor slows as the drive falls, and
                // so does the period played, along the measured line. Microseconds for the device.
                rightPeriod = MotorModel.PeriodMs(ps.GetRightMotorPeriod(), false, Drive(old_RightMotorSpeed)) * 1000;
                if (hasRightEffect && paramsR != null)
                {
                    // Update force values.
                    if (GUID_Force == EffectGuid.ConstantForce)
                    {
                        ConstantForceR.Magnitude = rightMagnitudeAdjusted;
                    }
                    else
                    {
                        PeriodicForceR.Magnitude = rightMagnitudeAdjusted;
                        PeriodicForceR.Period = rightPeriod;
                    }
                    // Update flags to indicate that specific force parameters changed.
                    flagsR |= EffectParameterFlags.TypeSpecificParameters;
                }
            }

            // Calculate left later for possible combine.
            if (motorsChanged || forceChanged || periodLChanged || speedLChanged || combine)
            {
                // Convert speed into magnitude/amplitude.
                leftMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, old_LeftMotorSpeed);
                leftPeriod = MotorModel.PeriodMs(ps.GetLeftMotorPeriod(), true, Drive(old_LeftMotorSpeed)) * 1000;
                // If device have only one force feedback actuator (probably wheel).
                if (combine)
                {
                    // Forces must be combined.
                    var combinedMagnitudeAdjusted = Math.Max(leftMagnitudeAdjusted, rightMagnitudeAdjusted);
                    var combinedPeriod = 0;
                    // If at least one speed is specified then...
                    if (leftMagnitudeAdjusted > 0 || rightMagnitudeAdjusted > 0)
                    {
                        // Get combined period depending on magnitudes.
                        combinedPeriod =
                            ((leftPeriod * leftMagnitudeAdjusted) + (rightPeriod * rightMagnitudeAdjusted))
                            / (leftMagnitudeAdjusted + rightMagnitudeAdjusted);
                    }
                    // Update force properties.
                    leftMagnitudeAdjusted = combinedMagnitudeAdjusted;
                    leftPeriod = combinedPeriod;
                }
                // Update force values.
                if (GUID_Force == EffectGuid.ConstantForce)
                {
                    ConstantForceL.Magnitude = leftMagnitudeAdjusted;
                }
                else
                {
                    PeriodicForceL.Magnitude = leftMagnitudeAdjusted;
                    PeriodicForceL.Period = leftPeriod;
                }
                // Update flags to indicate that specific force parameters changed.
                flagsL |= EffectParameterFlags.TypeSpecificParameters;
            }
            // Recreate effects if force changed.
            if (motorsChanged || forceChanged)
            {
                // Update Left force
                paramsL.Parameters = GUID_Force == EffectGuid.ConstantForce
                    ? ConstantForceL as TypeSpecificParameters : PeriodicForceL;
                // Note: Device must be acquired in exclusive mode before effect can be created.
                // A refused effect is remembered, so it is not asked for on every force update.
                if ((make & MotorEffects.Left) != 0)
                {
                    effectL = CreateEffect(device, GUID_Force, paramsL, out _);
                    if (effectL == null)
                        motorsRefused |= MotorEffects.Left;
                    else
                        Counts.Made++;
                }
                if (hasRightEffect && paramsR != null)
                {
                    // Update Right force
                    paramsR.Parameters = GUID_Force == EffectGuid.ConstantForce
                        ? ConstantForceR as TypeSpecificParameters : PeriodicForceR;
                    if ((make & MotorEffects.Right) != 0)
                    {
                        effectR = CreateEffect(device, GUID_Force, paramsR, out _);
                        if (effectR == null)
                            motorsRefused |= MotorEffects.Right;
                        else
                            Counts.Made++;
                    }
                }
            }
            if (flagsL != EffectParameterFlags.None)
            {
                SetParamaters(effectL, paramsL, flagsL);
            }
            if (flagsR != EffectParameterFlags.None)
                SetParamaters(effectR, paramsR, flagsR);

            // Each compared on its own, so every one is recorded as read.
            var springChanged = Changed(ref old_SpringEnable, ps.ForceSpringEnable)
                | Changed(ref old_SpringStrength, ps.ForceSpringStrength)
                | Changed(ref old_SpringCentre, ps.ForceSpringCentre)
                | Changed(ref old_SpringPeak, ps.ForceSpringPeak)
                | Changed(ref old_SpringLightening, ps.ForceSpringLightening)
                | Changed(ref old_SpringDamping, ps.ForceSpringDamping)
                | Changed(ref old_SpringSpeed, ps.ForceSpringSpeed);
            if (springChanged)
            {
                springStrength = ps.ForceSpringEnable == "1" ? Math.Max(0, Math.Min(100, ps.GetForceSpringStrength())) : 0;
                springCentre = ps.GetForceSpringCentre();
                springPeak = ps.GetForceSpringPeak();
                springLightening = ps.GetForceSpringLightening();
                springSpeed = ps.GetForceSpringSpeed();
                springDamper = springStrength == 0 ? 0 : SpringDamper(ps.GetForceSpringDamping());
            }
            // Off means nothing of ours on the wheel, so whatever else centres it is left in charge: the
            // spring's effects come off, and a wheel that takes the report gets its own centering back below.
            if (springStrength == 0)
                DropSpring();
            // The actuator the spring sits on may have changed, here or in an update that threw, so the effect is made
            // again on the next poll.
            if (motorsChanged || resend)
                DropSpring();
            UpdateRange(ud, device, ps, heldAgain, Environment.TickCount);
            // The wheel switches its own centering off when an effect is made, or played again after the device was
            // let go of, and keeps it off. It is switched on while this program's spring is off, so the wheel centres
            // itself under a game's rumble, and off while the spring holds the centre, so the two do not pull together.
            if (ud.DevVendorId != 0 && LogitechWheel.SupportsCentering(ud.DevVendorId, ud.DevProductId))
            {
                var centering = springStrength == 0;
                if (centering != centeringOn || make != MotorEffects.None || restarted)
                {
                    centeringPath = ud.HidDevicePath;
                    centeringOn = centering;
                    centeringDueAt = Environment.TickCount + CenteringSettleMs;
                    centeringDue = true;
                }
            }
            interrupted = false;
            return true;
        }

        /// <summary>The left and right motors' effects, as a set.</summary>
        [Flags]
        public enum MotorEffects
        {
            None = 0,
            Left = 1,
            Right = 2,
        }

        /// <summary>Which of its motor effects a force update stops and disposes, and which it makes.</summary>
        /// <remarks>
        /// Whenever an effect is made, every effect the state still holds is stopped and disposed
        /// first and made again with it, so the device never holds two for one motor. An effect the
        /// device refused with the current settings is not asked for again: the answer would be the
        /// same, and each ask would make the others again. A new type or new motors ask again. Runs
        /// on every force update, so it allocates nothing.
        /// </remarks>
        /// <param name="motors">The motors the state makes effects for: the left, and the right when the device has a second actuator.</param>
        /// <param name="held">The effects the state holds on the device.</param>
        /// <param name="refused">The effects the device refused to make with the current settings.</param>
        /// <param name="changed">The effect type changed, or the motors or which way round they are.</param>
        /// <param name="drop">The held effects to stop and dispose, before any is made.</param>
        /// <param name="make">The effects to make.</param>
        public static void PlanEffects(MotorEffects motors, MotorEffects held, MotorEffects refused, bool changed,
            out MotorEffects drop, out MotorEffects make)
        {
            // The refusal was the answer for the old type or motors.
            if (changed)
                refused = MotorEffects.None;
            // Wanted, not held and not refused: the device lost it, or making it failed.
            var missing = motors & ~held & ~refused;
            if (!changed && missing == MotorEffects.None)
            {
                drop = MotorEffects.None;
                make = MotorEffects.None;
                return;
            }
            drop = held;
            make = motors & ~refused;
        }

        /// <summary>The share of the travel to one side, in percent, over which the spring may rise by <see cref="SpringStiffnessLimit"/> percent at most.</summary>
        /// <remarks>
        /// With <see cref="SpringStiffnessLimit"/> it is the steepest the spring may be. The force answers the position a
        /// few milliseconds late: the poll, the bus, the device. A spring steep enough that the wheel crosses it in that
        /// time chases its own force: pumped into a swing at half strength over two percent of the travel, a G27 stayed in
        /// a nine hertz shake of ten degrees either side for as long as it was watched, damping and all, while at a quarter
        /// of its force over the same travel it came to rest in two crossings.
        /// </remarks>
        public const int SpringRampPercent = 2;

        /// <summary>The travel over which the spring may rise by <see cref="SpringStiffnessLimit"/> percent at most, in axis units.</summary>
        public const int SpringRamp = SpringCalibration.Center * SpringRampPercent / 100;

        /// <summary>The most the spring may rise over <see cref="SpringRamp"/>, in percent of the device's force.</summary>
        /// <remarks><see cref="SpringCurve"/> widens Centre so that the slope at the centre is never steeper.</remarks>
        public const int SpringStiffnessLimit = 30;

        /// <summary>How near the centre the spring asks nothing, as DirectInput counts the axis: a fifth of a percent of the travel.</summary>
        /// <remarks>
        /// Small: a wide band was where a wheel came to rest short of the centre. Outside it the force, the friction too,
        /// rises from the centre no steeper than <see cref="SpringStiffnessLimit"/> over <see cref="SpringRamp"/>, so the
        /// wheel slows as it comes in rather than being thrown across the centre.
        /// </remarks>
        public const int SpringDeadBand = SpringCalibration.AxisMax / 500;

        /// <summary>The damping that goes with the spring, in DirectInput's units: the damping setting's percent of the device's full scale, at every angle.</summary>
        /// <remarks>
        /// A spring alone can swing a wheel past the centre and back: it arrives with speed, and the force turns round.
        /// Resistance in proportion to the speed takes that energy out, and the device computes it itself between polls.
        /// A spring that grows with the angle, as a car's does, needs little of it. Measured on a G27, a thirty percent
        /// push crossed the wheel in 0.8 s free, in 1.2 s against a quarter of full damping, and not at all against half.
        /// </remarks>
        /// <param name="dampingPercent">The damping setting, 0 to 100.</param>
        public static int SpringDamper(int dampingPercent)
        {
            return Math.Max(0, Math.Min(100, dampingPercent)) * (DI_FFNOMINALMAX / 100);
        }

        /// <summary>The steering range, lock to lock, that degrees are counted against while the wheel range setting leaves a wheel that takes no range as it is.</summary>
        public const int DefaultRangeDegrees = 900;

        /// <summary>The steering range, lock to lock, the spring's degrees are counted against on this wheel.</summary>
        /// <remarks>
        /// No wheel tells what range it turns, so the range is the one chosen, which a wheel that takes it is sent. Left as it
        /// is, such a wheel turns the <see cref="LogitechWheel.PowerUpRange"/> it powers up at, unless the maker's software sent
        /// it a wider one, which only makes the spring softer than set. Counted against a range wider than the wheel turns,
        /// the spring is steeper than set, and the wheel swings at its centre. Any other wheel is counted against
        /// <see cref="DefaultRangeDegrees"/>.
        /// </remarks>
        /// <param name="wheelRange">The wheel range setting, or nought when it leaves the wheel as it is.</param>
        /// <param name="ud">The wheel, or null before one is known.</param>
        public static int SpringRange(int wheelRange, UserDevice ud)
        {
            if (wheelRange > 0)
                return wheelRange;
            return ud != null && LogitechWheel.SupportsRange(ud.DevVendorId, ud.DevProductId)
                ? LogitechWheel.PowerUpRange
                : DefaultRangeDegrees;
        }

        /// <summary>The steering range, lock to lock, the spring is counted against: <see cref="SpringRange"/>.</summary>
        int rangeDegrees = DefaultRangeDegrees;

        /// <summary>The steering range, lock to lock, currently active on this state.</summary>
        public int RangeDegrees { get { return rangeDegrees; } }

        /// <summary>
        /// A DirectInput direction names where a force comes from, so a positive magnitude along the
        /// positive axis pushes towards the low end. A force towards the high end is the negative one.
        /// </summary>
        const int TowardsHighEnd = -1;

        /// <summary>Holds a wheel at its centre with a spring the game never asked for. Called every poll.</summary>
        public void UpdateSpring(Joystick device, int position, SpringCalibration calibration, long nowMs, int? speedOverride, int frictionPercent)
        {
            UpdateSpring(device, position, calibration, nowMs, speedOverride, frictionPercent, WheelTelemetrySample.Inactive, 0, 0);
        }

        /// <summary>Holds a wheel at its centre with a spring shaped by the car's curve, live vehicle telemetry, optional rumble understeer and software soft-lock. Called every poll.</summary>
        public void UpdateSpring(Joystick device, int position, SpringCalibration calibration, long nowMs, int? speedOverride, int frictionPercent,
            WheelTelemetrySample telemetry, int rumbleUndersteerPercent, int softLockRangeDegrees)
        {
            UpdateSpring(device, position, calibration, nowMs, speedOverride, frictionPercent,
                telemetry, rumbleUndersteerPercent, softLockRangeDegrees, WheelTelemetryTuning.Default);
        }

        /// <summary>Holds a wheel at its centre with a spring shaped by the car's curve, live vehicle telemetry, user tuning scales, optional rumble understeer and software soft-lock. Called every poll.</summary>
        /// <param name="position">The steering axis, 0 to <see cref="SpringCalibration.AxisMax"/>.</param>
        /// <param name="calibration">The Auto button's run, or null.</param>
        /// <param name="nowMs">Monotonic clock in milliseconds.</param>
        /// <param name="speedOverride">The car's speed from a live source, which replaces the Speed setting while it is not null (<see cref="UserDevice.ForceSpringSpeedOverride"/>).</param>
        /// <param name="frictionPercent">The least force that turns the wheel, which the spring never falls below outside the centre (<see cref="UserDevice.ForceSpringFriction"/>).</param>
        /// <param name="telemetry">Live vehicle telemetry snapshot, or <see cref="WheelTelemetrySample.Inactive"/>.</param>
        /// <param name="rumbleUndersteerPercent">Zero-telemetry rumble-coupled understeer lightening (0 to 60 %).</param>
        /// <param name="softLockRangeDegrees">Optional virtual soft-lock range lock to lock (&gt; 0 and narrower than physical range), or 0 for off.</param>
        /// <param name="tuning">User-configured telemetry channel gain scales.</param>
        public void UpdateSpring(Joystick device, int position, SpringCalibration calibration, long nowMs, int? speedOverride, int frictionPercent,
            WheelTelemetrySample telemetry, int rumbleUndersteerPercent, int softLockRangeDegrees, WheelTelemetryTuning tuning)
        {
            int percent;
            int damper;
            if (calibration != null)
            {
                // The calibration's own asking: no damping while its pushes must move the wheel freely,
                // the spring's damping while it ramps and while it checks its answer.
                percent = calibration.Update(position, nowMs);
                damper = calibration.Damping;
            }
            else
            {
                var effectiveSpeed = WheelTelemetryMath.EffectiveSpeed(springSpeed, speedOverride, telemetry, nowMs);
                // When software soft-lock narrows a wheel without hardware range control, degrees are counted against its full physical range.
                var physicalRange = softLockRangeDegrees > 0 && softLockRangeDegrees < DefaultRangeDegrees
                    ? DefaultRangeDegrees
                    : rangeDegrees;
                // The friction is the spring's floor, so it plays only while the spring does.
                var activeFriction = springStrength == 0 ? 0 : frictionPercent;
                Curve.Set(springStrength, springCentre, springPeak, springLightening, physicalRange, effectiveSpeed, activeFriction);

                // Smooth rumble envelope for zero-telemetry tire-scrub lightening (attack ~60 ms, release ~180 ms).
                double rumbleAttenuation = 0.0;
                if (rumbleUndersteerPercent > 0 && springStrength > 0)
                {
                    var dtMs = lastSpringClockMs >= 0 ? Math.Max(0, Math.Min(100, nowMs - lastSpringClockMs)) : 0;
                    var tau = rumbleDrive > rumbleEnvelope ? 60.0 : 180.0;
                    var alpha = dtMs > 0 ? dtMs / (tau + dtMs) : 0.0;
                    rumbleEnvelope += alpha * (rumbleDrive - rumbleEnvelope);
                    var maxDrop = Math.Max(0, Math.Min(60, rumbleUndersteerPercent)) / 100.0;
                    rumbleAttenuation = rumbleEnvelope * maxDrop;
                }
                else
                {
                    rumbleEnvelope = 0.0;
                }
                lastSpringClockMs = nowMs;

                double dampingMultiplier;
                percent = WheelTelemetryMath.ComputeSpringForce(Curve, position, physicalRange, activeFriction, springStrength,
                    telemetry, nowMs, rumbleAttenuation, softLockRangeDegrees, tuning, ref LastSpringTerms, out dampingMultiplier);
                LastSpringTerms.SpeedKmh = effectiveSpeed;
                damper = springDamper == 0 ? 0 : Math.Max(0, Math.Min(DI_FFNOMINALMAX, (int)Math.Round(springDamper * dampingMultiplier)));
            }
            LastSpringPercent = percent;
            UpdateDamper(device, damper);
            var magnitude = TowardsHighEnd * percent * (DI_FFNOMINALMAX / 100);
            if (magnitude == springMagnitude && (magnitude == 0 || effectS != null))
                return;
            if (magnitude == 0 && effectS == null)
            {
                springMagnitude = 0;
                return;
            }
            if (springRefused || actuatorL == null)
                return;
            springForce.Magnitude = magnitude;
            if (effectS == null)
            {
                paramsS = GetParameters();
                paramsS.Axes = new int[1] { actuatorL.ObjectId };
                paramsS.Directions = new int[1] { 1 };
                paramsS.Gain = DI_FFNOMINALMAX;
                paramsS.Parameters = springForce;
                int error;
                effectS = CreateEffect(device, EffectGuid.ConstantForce, paramsS, out error);
                if (effectS == null)
                {
                    springRefused = true;
                    SpringRefusedError = error;
                    return;
                }
            }
            SetParamaters(effectS, paramsS, EffectParameterFlags.TypeSpecificParameters);
            springMagnitude = magnitude;
        }

        /// <summary>Sets the resistance to the wheel's speed, once per change. A device without a damper gets none, and is not asked again.</summary>
        void UpdateDamper(Joystick device, int coefficient)
        {
            if (coefficient == damperCoefficient && (coefficient == 0 || effectD != null))
                return;
            if (coefficient == 0 && effectD == null)
            {
                damperCoefficient = 0;
                return;
            }
            if (springRefused || actuatorL == null)
                return;
            var conditions = new ConditionSet();
            conditions.Conditions = new Condition[]
            {
                new Condition
                {
                    Offset = 0,
                    PositiveCoefficient = coefficient,
                    NegativeCoefficient = coefficient,
                    PositiveSaturation = DI_FFNOMINALMAX,
                    NegativeSaturation = DI_FFNOMINALMAX,
                    DeadBand = 0,
                }
            };
            if (effectD == null)
            {
                paramsD = GetParameters();
                paramsD.Axes = new int[1] { actuatorL.ObjectId };
                paramsD.Directions = new int[1] { 0 };
                paramsD.Gain = DI_FFNOMINALMAX;
                paramsD.Parameters = conditions;
                effectD = CreateEffect(device, EffectGuid.Damper, paramsD, out _);
                if (effectD == null)
                {
                    // Remembered as set, so a device with no damper is not asked on every poll.
                    damperCoefficient = coefficient;
                    return;
                }
            }
            else
            {
                paramsD.Parameters = conditions;
            }
            SetParamaters(effectD, paramsD, EffectParameterFlags.TypeSpecificParameters);
            damperCoefficient = coefficient;
        }

        /// <summary>Takes the spring and its damping off the device, to be made again when next needed.</summary>
        void DropSpring()
        {
            if (effectS != null)
            {
                effectS.Stop();
                effectS.Dispose();
                effectS = null;
                paramsS = null;
            }
            if (effectD != null)
            {
                effectD.Stop();
                effectD.Dispose();
                effectD = null;
                paramsD = null;
            }
            springMagnitude = 0;
            damperCoefficient = 0;
            springRefused = false;
            SpringRefusedError = 0;
        }

        /// <summary>Takes down every effect on the device, whichever state made it.</summary>
        /// <remarks>
        /// Disposed without asking the device first. An effect left by a state from before the device was let go
        /// of is no longer on the device, and DirectInput answers its status with DIERR_NOTDOWNLOADED, an answer
        /// the engine counts as a device condition and never reports; the new state would stop there on every
        /// poll and drive nothing. Disposing a playing effect takes its force off as a stop does.
        /// </remarks>
        static void DisposeDeviceEffects(Joystick device)
        {
            foreach (var effect in device.CreatedEffects.ToArray())
                effect.Dispose();
        }

        /// <summary>
        /// The two answers a device gives for an effect it will never take: not implemented, and
        /// refused settings. Neither is a fault of the program, and asking again gets the same answer.
        /// </summary>
        static bool IsUnsupported(SharpDX.SharpDXException ex)
        {
            var code = ex.ResultCode.Code;
            return code == unchecked((int)0x80004001) || code == unchecked((int)0x80070057);
        }

        /// <summary>An effect on the device, or null when the device will not take one of that kind.</summary>
        /// <param name="error">The device's answer when it would not take it, or 0.</param>
        static Effect CreateEffect(Joystick device, Guid guid, EffectParameters parameters, out int error)
        {
            error = 0;
            try
            {
                return new Effect(device, guid, parameters);
            }
            catch (SharpDX.SharpDXException ex) when (IsUnsupported(ex))
            {
                error = ex.ResultCode.Code;
                return null;
            }
        }

        /// <summary>Puts an effect back on the device and playing, if the device dropped it.</summary>
        /// <remarks>
        /// A device let go of and held again, which the program does whenever it changes how it holds
        /// the device, forgets what was playing. Downloaded again, an effect is only loaded; nothing
        /// sets it playing until a game changes the speed, so a game holding a steady rumble got none.
        /// Every effect this state makes plays from the moment it is made, at nought when it is quiet,
        /// so one that is not playing was dropped.
        /// </remarks>
        /// <returns>True when the effect had been dropped and was started again.</returns>
        bool Resume(Effect effect)
        {
            if (effect == null || unsupported.Contains(effect))
                return false;
            effect.Download();
            if (effect.Status == EffectStatus.Playing)
                return false;
            effect.Start(1);
            Counts.Resumed++;
            return true;
        }

        /// <summary>What the device refused to play, as a person would name it, or empty when it took everything asked of it.</summary>
        /// <remarks>
        /// A refused effect is not asked for again, since the device gives the same answer every time,
        /// so without this a motor simply stayed silent and nothing said why.
        /// </remarks>
        public string[] Refused
        {
            get
            {
                var list = new List<string>();
                if (paramsL != null && (effectL == null || unsupported.Contains(effectL)))
                    list.Add(actuatorR == null ? "the motor" : "the left motor");
                if (actuatorR != null && paramsR != null && (effectR == null || unsupported.Contains(effectR)))
                    list.Add("the right motor");
                if (springRefused)
                    list.Add("the centering spring");
                return list.ToArray();
            }
        }

        void SetParamaters(Effect effect, EffectParameters parameters, EffectParameterFlags flags)
        {
            if (parameters == null || effect == null || unsupported.Contains(effect))
                return;
            // Do not restart playing effect.
            var playing = effect.Status == EffectStatus.Playing;
            flags |= playing ? EffectParameterFlags.NoRestart : EffectParameterFlags.Start;
            if (effect == effectL || effect == effectR)
            {
                Counts.Changes++;
                if (!playing)
                    Counts.Started++;
            }
            try
            {
                effect.SetParameters(parameters, flags);
            }
            catch (SharpDX.SharpDXException ex) when (IsUnsupported(ex))
            {
                unsupported.Add(effect);
            }
        }

        int TryParse(string value)
        {
            int i;
            int.TryParse(value, out i);
            return i;
        }

        /// <summary>A motor speed as the game means it, 0 to 1. Speeds are carried as shorts, off at the bottom of the range.</summary>
        static double Drive(short speed)
        {
            return (speed - (double)short.MinValue) / (short.MaxValue - (double)short.MinValue);
        }

		bool Changed(ref ForceEffectType? oldValue, ForceEffectType newValue)
		{
			var changed = oldValue != newValue;
			oldValue = newValue;
			return changed;
		}

		bool Changed(ref string oldValue, string newValue)
        {
            var changed = oldValue != newValue;
            oldValue = newValue;
            return changed;
        }

        bool Changed(ref short oldValue, short newValue)
        {
            var changed = oldValue != newValue;
            oldValue = newValue;
            return changed;
        }

        public bool Changed(PadSetting ps)
        {
            return
            old_ForceEnable != ps.ForceEnable ||
            old_ForceType != ps.ForceType ||
            old_LeftPeriod != ps.LeftMotorPeriod ||
            old_RightPeriod != ps.RightMotorPeriod ||
            old_LeftStrength != ps.LeftMotorStrength ||
            old_RightStrength != ps.RightMotorStrength ||
            old_LeftDirection != ps.LeftMotorDirection ||
           old_RightDirection != ps.RightMotorDirection ||
           old_OveralStrength != ps.ForceOverall ||
           old_SpringEnable != ps.ForceSpringEnable ||
           old_SpringStrength != ps.ForceSpringStrength ||
           old_SpringCentre != ps.ForceSpringCentre ||
           old_SpringPeak != ps.ForceSpringPeak ||
           old_SpringLightening != ps.ForceSpringLightening ||
           old_SpringDamping != ps.ForceSpringDamping ||
           old_SpringSpeed != ps.ForceSpringSpeed ||
           old_WheelRange != ps.WheelRange;
        }


        #endregion


    }
}
