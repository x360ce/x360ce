using System;
using System.Diagnostics;
using System.Threading;

namespace x360ce.Engine
{
	/// <summary>One game controller a <see cref="RawInputHub"/> reads: its reader, which the hub thread uses, and the states it hands the engine.</summary>
	/// <remarks>
	/// The reader writes only the controls a report carries, so every report goes into one state that only the hub
	/// thread touches. After each report that state is copied into the <see cref="TripleBuffer{T}"/> and published,
	/// and the engine copies the newest one out with <see cref="CopyState"/>.
	/// </remarks>
	public sealed class RawInputHubDevice
	{
		/// <summary>A device whose state is at rest and whose controls go where <paramref name="layout"/> puts them, or where their usages put them, <see cref="RawInputLayout.Standard"/>, when it is null.</summary>
		/// <param name="layout">Made for controls the same as the device's: see <see cref="RawInputLayout.Fits"/>.</param>
		public RawInputHubDevice(RawInputDevice device, RawInputLayout layout)
		{
			Device = device;
			readerLayout = layout ?? RawInputLayout.Standard(device.Controls);
			this.layout = readerLayout;
			reader = new RawInputReader(device, readerLayout);
			current = AtRest();
			states = new TripleBuffer<SourceState>(AtRest(), AtRest(), AtRest());
		}

		/// <summary>A state as DirectInput reads the device before its first report.</summary>
		SourceState AtRest()
		{
			var state = new SourceState();
			reader.Rest(state);
			return state;
		}

		/// <summary>The device: who it is and its controls. Its handle and preparsed data belong to the hub thread, which disposes it.</summary>
		public RawInputDevice Device { get; }

		/// <summary>Used on the hub thread only.</summary>
		RawInputReader reader;

		/// <summary>The layout <see cref="reader"/> was made with. Hub thread only.</summary>
		RawInputLayout readerLayout;

		/// <summary>The state every report is read into. Hub thread only.</summary>
		readonly SourceState current;

		readonly TripleBuffer<SourceState> states;

		/// <summary>A layout asked for by <see cref="SetLayout"/> that the hub thread has not taken yet.</summary>
		RawInputLayout pendingLayout;

		RawInputLayout layout;

		/// <summary>Where the device's controls go: the last layout <see cref="SetLayout"/> asked for, or the one the device arrived with. Any thread.</summary>
		/// <remarks>The hub thread reads by it from the next report on at the latest, so it is what the device list describes the device by.</remarks>
		public RawInputLayout Layout { get { return Volatile.Read(ref layout); } }

		long reportCount;
		long lastReportTimestamp;

		/// <summary>The reports read since the device arrived. Any thread.</summary>
		public long ReportCount { get { return Interlocked.Read(ref reportCount); } }

		/// <summary>The <see cref="Stopwatch.GetTimestamp"/> of the last report read; 0 before the first. Any thread.</summary>
		public long LastReportTimestamp { get { return Interlocked.Read(ref lastReportTimestamp); } }

		/// <summary>Asks for the device's controls to go where <paramref name="layout"/> puts them, from the next report on. Any thread.</summary>
		/// <remarks>The hub thread takes it with <see cref="ApplyLayout"/>, and before reading the next report at the latest. The last layout asked for wins.</remarks>
		public void SetLayout(RawInputLayout layout)
		{
			Volatile.Write(ref this.layout, layout);
			Volatile.Write(ref pendingLayout, layout);
		}

		/// <summary>Takes the layout <see cref="SetLayout"/> asked for, if any, and starts the state again at rest, since its values were read into the old slots. Hub thread only.</summary>
		/// <remarks>A layout that puts every control where the one in use does changes nothing, so the state goes on as it is.</remarks>
		/// <returns>True when the device reads by another layout now.</returns>
		public bool ApplyLayout()
		{
			var layout = Interlocked.Exchange(ref pendingLayout, null);
			if (layout == null || layout.SameSlots(readerLayout))
				return false;
			readerLayout = layout;
			reader = new RawInputReader(Device, layout);
			reader.Rest(current);
			Publish();
			return true;
		}

		/// <summary>Reads every report one RAWINPUT carries and hands the engine the state they leave. Hub thread only.</summary>
		/// <remarks>Makes nothing, takes no lock and throws nothing.</remarks>
		/// <returns>The number of reports read.</returns>
		public int Read(byte[] rawInput)
		{
			if (Volatile.Read(ref pendingLayout) != null)
				ApplyLayout();
			var read = reader.ReadRawInput(rawInput, current);
			if (read == 0)
				return 0;
			Publish();
			Interlocked.Add(ref reportCount, read);
			Interlocked.Exchange(ref lastReportTimestamp, Stopwatch.GetTimestamp());
			return read;
		}

		void Publish()
		{
			current.CopyTo(states.Back);
			states.Publish();
		}

		/// <summary>Copies the newest state the hub thread has published into <paramref name="into"/>. One thread only, the engine's.</summary>
		/// <remarks>Makes nothing, takes no lock and never waits: it swaps one index, when the hub has published since, and copies the arrays.</remarks>
		/// <returns>True when the hub has published a state since the last copy.</returns>
		public bool CopyState(SourceState into)
		{
			var fresh = states.Take();
			states.Front.CopyTo(into);
			return fresh;
		}
	}
}
