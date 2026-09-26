// @under-test: Engine/Common/XInputMaskScanner.cs, App.v4/Controls/PadTabPages/GamesControl.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A game folder is scanned to the end whatever else is in it.
	/// </summary>
	/// <remarks>
	/// Two things ended the scan of a whole folder in 4.22.21.0: a file whose name ends in a space,
	/// which the folder lists but nothing can open (reported from a Resident Evil 2 folder), and a
	/// subfolder the person may not enter (the "My Music" junction under Documents). One bad entry
	/// is left out; the rest of the folder is read.
	/// </remarks>
	[TestClass]
	public class MaskScanSkipsUnreadableTest
	{
		string folder;
		string barred;

		[TestInitialize]
		public void Before()
		{
			folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			barred = Path.Combine(folder, "barred");
			Directory.CreateDirectory(barred);
		}

		[TestCleanup]
		public void After()
		{
			Allow(barred);
			// The name ending in a space is deleted the way it was made, past the path rules.
			foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
				File.Delete(@"\\?\" + file);
			Directory.Delete(folder, true);
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A file name Windows cannot open is left out; the rest of the folder is scanned")]
		public void A_name_ending_in_a_space_does_not_end_the_scan()
		{
			var source = typeof(XInputMaskScanner).Assembly.Location;
			// Made past the path rules, so the name really ends in a space, as the reported one did.
			File.Copy(source, @"\\?\" + Path.Combine(folder, "trailing.exe "));
			File.Copy(source, Path.Combine(folder, "readable.dll"));
			Assert.IsTrue(Directory.GetFiles(folder, "*.exe").Any(x => x.EndsWith(" ")), "The folder does not list the bad name, so this test proves nothing.");

			// The Games page writes the current file and its size on every step; that is where it died.
			var scanner = new XInputMaskScanner();
			scanner.Progress += (sender, e) => GamesGridUserControl.ProgressText(e);
			var masks = scanner.GetMasks(folder, SearchOption.AllDirectories, Environment.Is64BitProcess);

			Assert.IsNotNull(masks, "The scan did not come back.");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A subfolder that may not be entered is left out; the rest of the folder is scanned")]
		public void A_barred_subfolder_does_not_end_the_scan()
		{
			var source = typeof(XInputMaskScanner).Assembly.Location;
			File.Copy(source, Path.Combine(folder, "readable.dll"));
			Deny(barred);

			var masks = new XInputMaskScanner().GetMasks(folder, SearchOption.AllDirectories, Environment.Is64BitProcess);

			Assert.IsNotNull(masks, "The scan did not come back.");
		}

		static void Deny(string path)
		{
			var info = new DirectoryInfo(path);
			var security = info.GetAccessControl();
			security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,
				FileSystemRights.ListDirectory, AccessControlType.Deny));
			info.SetAccessControl(security);
		}

		static void Allow(string path)
		{
			if (!Directory.Exists(path))
				return;
			var info = new DirectoryInfo(path);
			var security = info.GetAccessControl();
			security.RemoveAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,
				FileSystemRights.ListDirectory, AccessControlType.Deny));
			info.SetAccessControl(security);
		}
	}
}
