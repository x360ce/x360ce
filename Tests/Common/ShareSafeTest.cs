// @under-test: App.v4/Common/Recording/ShareSafe.cs
// @area: recording   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.App.Recording;

namespace x360ce.Tests
{
	/// <summary>A recording is posted where anyone reads it, so every text from outside the program loses what names a person or a PC.</summary>
	[TestClass]
	public class ShareSafeTest
	{
		[TestMethod, TestCategory("recording")]
		[Description("A path keeps only its file name, whatever the folders, spaces and source line after it")]
		public void A_path_keeps_its_file_name()
		{
			Assert.AreEqual("Could not find x360ce.exe.", ShareSafe.Clean(@"Could not find C:\Users\Bob Smith\Desktop\x360ce - 4.26.38.0\x360ce.exe."));
			Assert.AreEqual("at X.Y() in File.cs:line 12", ShareSafe.Clean(@"at X.Y() in D:\Projects\x360ce\App.v4\File.cs:line 12"));
			Assert.AreEqual("share.txt", ShareSafe.Clean(@"\\server\users\bob\share.txt"));
		}

		[TestMethod, TestCategory("recording")]
		[Description("This PC's user and computer names are replaced wherever they appear")]
		public void The_user_and_the_PC_are_replaced()
		{
			var user = Environment.UserName;
			var pc = Environment.MachineName;
			if (user.Length >= 3)
				Assert.AreEqual("hello <user>!", ShareSafe.Clean("hello " + user + "!"));
			if (pc.Length >= 3)
				Assert.AreEqual("on <pc> now", ShareSafe.Clean("on " + pc.ToLowerInvariant() + " now"));
		}

		[TestMethod, TestCategory("recording")]
		[Description("A web address keeps its host; its path and query, which can hold a key, go")]
		public void A_web_address_keeps_its_host()
		{
			Assert.AreEqual("GET https://api.example.com failed", ShareSafe.Clean("GET https://api.example.com/v1/update?key=abc123&user=bob failed"));
		}

		[TestMethod, TestCategory("recording")]
		[Description("A device path or instance ID keeps its vendor and product ID and loses its serial and port")]
		public void A_device_path_keeps_its_vendor_and_product()
		{
			Assert.AreEqual("opened VID_046D&PID_C29B", ShareSafe.Clean(@"opened \\?\hid#vid_046d&pid_c29b&mi_00#7&2a8f1b&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"));
			Assert.AreEqual("VID_346E&PID_0006 refused", ShareSafe.Clean(@"HID\VID_346E&PID_0006\7&1A2B3C&0&0000 refused"));
			Assert.AreEqual("<device> gone", ShareSafe.Clean(@"ROOT\SYSTEM\0001 gone"));
		}

		[TestMethod, TestCategory("recording")]
		[Description("Mail addresses, access tokens and long keys are replaced")]
		public void Addresses_and_keys_are_replaced()
		{
			Assert.AreEqual("from <email>", ShareSafe.Clean("from someone@example.com"));
			Assert.AreEqual("token <key>", ShareSafe.Clean("token sk-x360ce-mcp-AbCdEf0123456789"));
			Assert.AreEqual("key <key>", ShareSafe.Clean("key " + new string('a', 20) + new string('7', 25)));
		}

		[TestMethod, TestCategory("recording")]
		[Description("A GUID becomes the device label it stands for, or <id>")]
		public void A_guid_becomes_its_label()
		{
			var known = Guid.NewGuid();
			Func<Guid, string> label = g => g == known ? "D1" : null;
			Assert.AreEqual("D1 and <id>", ShareSafe.Clean(known.ToString("B") + " and " + Guid.NewGuid(), label));
		}

		[TestMethod, TestCategory("recording")]
		[Description("What the program writes itself, versions, controls and numbers, is left as it is")]
		public void Program_words_are_left_alone()
		{
			const string text = "x360ce 4.28.14.0, Axis 1 32939, LT 255, ButtonA=Button 1; LeftThumbAxisX=IAxis 2";
			Assert.AreEqual(text, ShareSafe.Clean(text));
			Assert.IsNull(ShareSafe.Clean(null));
		}
	}
}
