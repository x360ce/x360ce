using SharpDX.DirectInput;
using SharpDX.XInput;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The device test under way, or null. While one is set the engine reads only its device, with no wait between passes.</summary>
		/// <remarks>
		/// Set and cleared by the Test Device window, which clears it whenever it closes. Read by the input thread once a pass.
		/// Clearing it resumes the normal passes at once: the next one takes the routing in again and reads every device.
		/// </remarks>
		public DeviceTest Testing
		{
			get { return _testing; }
			set { _testing = value; }
		}
		volatile DeviceTest _testing;

		/// <summary>A pass while a device is tested: the device list kept up to date, and only the tested device read and recorded.</summary>
		/// <remarks>
		/// The first pass of a test sends each virtual controller a neutral state, so nothing stays pressed in a game; after that
		/// nothing is converted or sent, and the controllers keep the neutral state until the test ends.
		/// </remarks>
		void TestPass(DirectInput manager, System.Windows.Forms.NativeWindow window, DeviceTest test)
		{
			if (!test.VirtualNeutral)
			{
				for (var i = 0; i < CombinedXiStates.Length; i++)
					CombinedXiStates[i].Gamepad = default(Gamepad);
				var game = SettingsManager.CurrentGame;
				if (game != null)
					UpdateVirtualDevices(game);
				test.NoteVirtualNeutral();
			}
			UpdateDiDevices(manager);
			var ud = test.Device;
			if (!ud.IsOnline)
			{
				test.NoteOffline();
				return;
			}
			UpdateDiStates(manager, test.Game, window, test.Routing);
			var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
			test.Record(ud.SourceState, ticks);
			// What DirectInput kept since the last read, which this pass's read took in. Taken once: cleared once recorded.
			var changes = ud.Device?.Changes;
			if (changes != null && changes.EntryCount > 0)
			{
				test.Record(changes, ticks);
				changes.Clear();
			}
		}
	}
}
