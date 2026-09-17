// @under-test: Engine/Common/Ini.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The game's x360ce.ini is written through Windows' profile functions, which keep a file's
	/// existing encoding and create a new one as ANSI. The program used to rewrite the file to
	/// UTF-16 at every start, and from a protected game folder that rewrite closed the program
	/// before its window appeared. The encoding is now settled where the file is written, and a
	/// file that cannot be written is reported as not written rather than thrown.
	/// </summary>
	[TestClass]
	public class IniTest
	{
		static string NewFolder()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", "ini-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			return folder;
		}

		static bool IsUtf16(string path)
		{
			var bytes = File.ReadAllBytes(path);
			return bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A new file is created as UTF-16 and keeps a name outside the code page")]
		public void New_file_is_utf16_and_keeps_any_name()
		{
			var folder = NewFolder();
			try
			{
				var ini = new Ini(Path.Combine(folder, "x360ce.ini"));
				var name = "Контроллер ▶ Ψ";
				Assert.AreNotEqual(0, ini.SetValue("PAD1", "ProductName", name), "The value was not written.");
				Assert.IsTrue(IsUtf16(ini.File.FullName), "The new file is not UTF-16.");
				Assert.AreEqual(name, ini.GetValue("PAD1", "ProductName"));
			}
			finally { Directory.Delete(folder, true); }
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A file in another encoding is converted on the first write and loses nothing")]
		public void Existing_ansi_file_is_converted_on_write()
		{
			var folder = NewFolder();
			try
			{
				var path = Path.Combine(folder, "x360ce.ini");
				File.WriteAllText(path, "[Options]\r\nUseInitBeep=1\r\n", Encoding.Default);
				Assert.IsFalse(IsUtf16(path));
				var ini = new Ini(path);
				Assert.AreNotEqual(0, ini.SetValue("PAD1", "ProductName", "Pad"));
				Assert.IsTrue(IsUtf16(path), "The file was not converted.");
				Assert.AreEqual("1", ini.GetValue("Options", "UseInitBeep"), "The old content was lost.");
				Assert.AreEqual("Pad", ini.GetValue("PAD1", "ProductName"));
			}
			finally { Directory.Delete(folder, true); }
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A file that cannot be written is reported as not written, and nothing is thrown")]
		public void Unwritable_file_is_reported_not_thrown()
		{
			var folder = NewFolder();
			var path = Path.Combine(folder, "x360ce.ini");
			try
			{
				File.WriteAllText(path, "[Options]\r\n", Encoding.Default);
				File.SetAttributes(path, FileAttributes.ReadOnly);
				var ini = new Ini(path);
				Assert.AreEqual(0, ini.SetValue("PAD1", "ProductName", "Pad"), "A read-only file must not report success.");
				Assert.IsFalse(IsUtf16(path), "A read-only file must be left as it was.");
			}
			finally
			{
				File.SetAttributes(path, FileAttributes.Normal);
				Directory.Delete(folder, true);
			}
		}
	}
}
