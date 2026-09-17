// @under-test: App.v4
// @area: devices   @layer: source
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// Nothing in the program asks the machine for every device it has.
	/// </summary>
	/// <remarks>
	/// Reading every device is a millisecond a node and a machine has hundreds; describing every HID
	/// interface opens each one for its strings at ten milliseconds more. Two to five seconds, and it
	/// was asked for on every device arrival and removal - each virtual controller this program plugs
	/// in among them - so starting or toggling emulation cost ten seconds or more of it, with no
	/// controller polled and force feedback left at whatever it last was.
	///
	/// Every question the program actually asks is about a few named devices, so every read it makes
	/// names them: ids first, which are cheap, then the descriptions of the few that matter. This
	/// fails on the calls that read the whole machine instead, because the cost of one does not show
	/// up in a rate measured on a machine where nothing is plugged in.
	/// </remarks>
	[TestClass]
	public class DeviceReadsAreTargetedTest
	{
		/// <summary>The calls that read the whole machine, by the shape they are written in.</summary>
		/// <remarks>
		/// A device id or a filter narrows the read, so a call carrying one is not listed. These three
		/// are the forms that carry nothing: every device, every device again, every HID interface.
		/// </remarks>
		static readonly string[] WholeMachineReads =
		{
			"DeviceDetector.GetDevices()",
			"DeviceDetector.GetDevices(null",
			"DeviceDetector.GetInterfaces()",
		};

		/// <summary>
		/// The whole-machine reads that are accounted for, and why each one is. Nothing may be added
		/// here to make a new call pass: a device read on any path a user waits on belongs in the list
		/// above, not here.
		/// </summary>
		/// <remarks>
		/// Each entry has to still be there. A site that goes has to lose its line here in the same
		/// change, so the list cannot quietly grow into an allow-everything.
		/// </remarks>
		static readonly Dictionary<string, string> Accounted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{
				@"App.v4\Controls\DebugUserControl.cs",
				"The debug page's device clean-up looks for offline, problem and unknown devices, which is "
				+ "a question about the whole machine and cannot be asked of a few named devices. It is a "
				+ "button a person presses, it runs on a thread of its own, and the same file is excluded "
				+ "from InterfaceNeverReadsDevicesTest for the same reason."
			},
		};

		[TestMethod, TestCategory("engine")]
		[Description("No code in the program reads every device or every interface on the machine")]
		public void The_program_never_reads_the_whole_machine()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var offences = new List<string>();
			var accountedFor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var file in Sources(app))
			{
				var relative = file.Substring(Ui.RepoRoot.FullName.Length + 1);
				var lines = File.ReadAllLines(file);
				for (var i = 0; i < lines.Length; i++)
				{
					var code = lines[i];
					// A commented-out call is not a call.
					var comment = code.IndexOf("//", StringComparison.Ordinal);
					if (comment >= 0)
						code = code.Substring(0, comment);
					foreach (var call in WholeMachineReads)
					{
						if (code.IndexOf(call, StringComparison.Ordinal) < 0)
							continue;
						if (Accounted.ContainsKey(relative))
							accountedFor.Add(relative);
						else
							offences.Add(relative + ":" + (i + 1) + ": " + lines[i].Trim());
					}
				}
			}
			Assert.AreEqual(0, offences.Count,
				"These read every device or every interface on the machine, which costs seconds and stops every "
				+ "controller being polled while it runs. Ask for the ids and then for the few devices that matter:"
				+ Environment.NewLine + string.Join(Environment.NewLine, offences));
			var gone = Accounted.Keys.Where(x => !accountedFor.Contains(x)).ToArray();
			Assert.AreEqual(0, gone.Length,
				"These no longer read the whole machine, so they no longer need to be accounted for. Remove them "
				+ "from Accounted, or the next one added there is never questioned: "
				+ string.Join(", ", gone));
		}

		/// <summary>Every source file of the program, leaving out what the build wrote.</summary>
		static IEnumerable<string> Sources(string app)
		{
			return Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
				.Where(x => x.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0)
				.Where(x => x.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0);
		}
	}
}
