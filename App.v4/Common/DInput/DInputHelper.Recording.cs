using System.Diagnostics;
using System.Threading;
using x360ce.App.Recording;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The recording this engine hands what it reads and sends to, or null. Set and cleared by <see cref="ReportRecorder"/>.</summary>
		/// <remarks>Read once a pass. While it is null the engine pays one field read a pass and one per device call for it.</remarks>
		public ReportRecorder Recording
		{
			get { return Volatile.Read(ref _recording); }
			set
			{
				Volatile.Write(ref _recording, value);
				_timeSteps = value != null && value.Has(RecordParts.Engine);
			}
		}
		ReportRecorder _recording;

		/// <summary>True while a recording times the engine's steps, as the engine log does.</summary>
		static volatile bool _timeSteps;

		/// <summary>Names the device call about to be made for the recording, which times each one, and gives the name back.</summary>
		static string Calling(ReportRecorder recording, UserDevice ud, string call)
		{
			if (recording != null)
				recording.Calling(ud, call);
			return call;
		}

		/// <summary>Hands the recording what this pass read from each device and sent to each controller. Input thread only.</summary>
		/// <param name="xinputRead">Whether XInput was read back this pass, so what it read is news.</param>
		void RecordPass(ReportRecorder recording, DeviceRouting routing, bool xinputRead)
		{
			var now = Stopwatch.GetTimestamp();
			if (recording.Has(RecordParts.Input) || recording.Has(RecordParts.Forces))
			{
				var devices = routing.MappedDevices;
				for (var i = 0; i < devices.Length; i++)
					recording.Device(devices[i], now);
			}
			for (var i = 0; i < 4; i++)
			{
				if (recording.Has(RecordParts.Output))
				{
					recording.PadOutput(i, CombinedXiConencted[i], CombinedXiStates[i].Gamepad, now);
					var feeding = FeedingState[i];
					recording.PadFeed(i, feeding.HasValue ? (feeding.Value ? 1 : 0) : -1, VirtualErrors[i], XiPlaceForPad[i], now);
					if (xinputRead)
						recording.PadLive(i, LiveXiConnected[i], LiveXiStates[i].Gamepad, now);
				}
				if (recording.Has(RecordParts.Forces))
					recording.GameForce(i, _lastLargeMotor[i], _lastSmallMotor[i], now);
			}
		}
	}
}
