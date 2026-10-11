// @under-test: Engine/JocysCom/Security/MD5Helper.cs, Engine/Common/BoardInfo.cs, Engine/Common/EngineHelper.cs, Engine/Input/Devices/RawInputDevice.cs
// @area: settings   @layer: unit
using JocysCom.ClassLibrary.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// x360ce makes its identifiers and checksums with an MD5 of its own, which the Windows policy "System cryptography:
	/// Use FIPS compliant algorithms" does not block (Issue #1648).
	/// </summary>
	/// <remarks>
	/// With that policy on, .NET Framework refuses its own MD5 classes, and the program stopped at start-up making the
	/// computer's identifier. The values are stored - in settings files, in shared presets and in the database behind the
	/// cloud - so they must come out exactly as the framework's MD5 made them. Each is measured here against that MD5, on
	/// the runtime the program runs on. The settings checksums are measured by PadSettingChecksumTest and UserDeviceTest.
	/// </remarks>
	[TestClass]
	public class MD5HelperTest
	{
		/// <summary>What the program made before: the framework's MD5 of the bytes, as a GUID.</summary>
		static Guid FrameworkMd5(byte[] bytes)
		{
			using (var md5 = new MD5CryptoServiceProvider())
				return new Guid(md5.ComputeHash(bytes));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The managed MD5 gives the framework's bytes at every length around two blocks and on a large input")]
		public void Managed_matches_the_framework_MD5()
		{
			var random = new Random(1648);
			for (var length = 0; length <= 300; length++)
			{
				var bytes = new byte[length];
				random.NextBytes(bytes);
				Assert.AreEqual(FrameworkMd5(bytes), MD5Helper.GetGuid(bytes), "Length " + length + ".");
			}
			var large = new byte[1 << 20];
			random.NextBytes(large);
			Assert.AreEqual(FrameworkMd5(large), MD5Helper.GetGuid(large), "One megabyte.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The computer's identifier, a Raw Input device's identifier and a file's checksum come out as before")]
		public void Stored_identifiers_are_unchanged()
		{
			const string disk = "WD-WCC4N0123456";
			Assert.AreEqual(FrameworkMd5(Encoding.ASCII.GetBytes(disk)), BoardInfo.GetHashedDiskId(disk), "Computer identifier.");

			const string path = @"\\?\hid#vid_046d&pid_c21c&mi_00#7&1a2b3c4d&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
			Assert.AreEqual(FrameworkMd5(Encoding.UTF8.GetBytes(path.ToUpperInvariant())), RawInputDevice.GetInstanceGuid(path),
				"Raw Input device identifier.");

			var bytes = new byte[10000];
			new Random(1649).NextBytes(bytes);
			var file = Path.GetTempFileName();
			try
			{
				File.WriteAllBytes(file, bytes);
				Assert.AreEqual(FrameworkMd5(bytes), EngineHelper.GetFileChecksum(file), "File checksum.");
			}
			finally
			{
				File.Delete(file);
			}
		}
	}
}
