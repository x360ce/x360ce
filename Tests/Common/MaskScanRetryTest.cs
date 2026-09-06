// @under-test: Engine/Common/XInputMaskScanner.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>A game file the antivirus is holding: asked again, then left out, never a crash.</summary>
	[TestClass]
	public class MaskScanRetryTest
	{
		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A file that cannot be read is retried, then skipped with no mask and nothing cached, and read normally once it is free")]
		public void A_refused_file_is_skipped_and_asked_again_next_time()
		{
			// A real program file, because the scan reads the header before the bytes.
			var source = typeof(XInputMaskScanner).Assembly.Location;
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var held = Path.Combine(folder, "held.dll");
			var free = Path.Combine(folder, "free.dll");
			File.Copy(source, held);
			File.Copy(source, free);
			try
			{
				var scanner = new XInputMaskScanner();
				var expected = scanner.GetMask(free);
				XInputMask whileHeld;
				var started = DateTime.UtcNow;
				// Held the way an antivirus holds a file it is inspecting: nobody else may read it.
				using (new FileStream(held, FileMode.Open, FileAccess.Read, FileShare.None))
					whileHeld = scanner.GetMask(held);
				Assert.AreEqual(XInputMask.None, whileHeld, "A file that cannot be read has no mask.");
				Assert.IsTrue(DateTime.UtcNow - started >= TimeSpan.FromSeconds(2), "The read must be tried again before the file is given up on.");
				Assert.IsFalse(XInputMaskScanner.FileInfoCache.Items.Any(x => string.Equals(x.FullName, held, StringComparison.OrdinalIgnoreCase)),
					"A skipped file must not be remembered as having no mask, or the next scan would not look again.");
				Assert.AreEqual(expected, scanner.GetMask(held), "Once free, the file reads as any other.");
			}
			finally
			{
				XInputMaskScanner.FileInfoCache.Items.Remove(XInputMaskScanner.FileInfoCache.Items.FirstOrDefault(x => string.Equals(x.FullName, held, StringComparison.OrdinalIgnoreCase)));
				XInputMaskScanner.FileInfoCache.Items.Remove(XInputMaskScanner.FileInfoCache.Items.FirstOrDefault(x => string.Equals(x.FullName, free, StringComparison.OrdinalIgnoreCase)));
				Directory.Delete(folder, true);
			}
		}
	}
}
