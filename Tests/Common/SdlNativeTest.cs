// @under-test: Native/x360ce/Sdl.cpp, Native/x360ce/x360ce.rc, Engine/Common/Sdl/SdlConverter.cs, Engine/Resources/gamecontrollerdb.txt.gz, Engine/Resources/gamecontrollerdb_x360ce.windows.txt.gz
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The xinput library of x360ce 3 maps a controller from SDL's database as x360ce 4 does: both converters pass the
	/// cases in SdlConverterCases.txt, the library carries the gzip compressed database the engine carries and reads its
	/// Windows rows, and finds a device's row by its vendor and product, a gamecontrollerdb.txt beside it first. It
	/// carries x360ce's controller database the engine carries too, and finds its rows the same way.
	/// </summary>
	/// <remarks>
	/// The library is the one built for this process's bitness, loaded from a copy under a name of its own through its
	/// test hooks SdlTranslate, SdlFind, X360ceFind and IniMapping. Loaded, it reads no controller and starts nothing.
	/// </remarks>
	[TestClass]
	public class SdlNativeTest
	{
		#region The library

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr LoadLibrary(string path);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr BeginUpdateResource(string fileName, bool deleteExistingResources);

		[DllImport("kernel32", SetLastError = true)]
		static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);

		[DllImport("kernel32", SetLastError = true)]
		static extern bool EndUpdateResource(IntPtr update, bool discard);

		/// <summary>The library's resources: RCDATA, and the databases' ids and language in Native\x360ce\resource.h and x360ce.rc.</summary>
		const int RcData = 10;
		const int SdlResource = 101;
		const int X360ceResource = 102;
		const ushort EnglishUnitedStates = 0x0409;

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		static extern IntPtr GetProcAddress(IntPtr module, string name);

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate uint SdlTranslateFunction(string row, uint axisMask, uint sliderMask, uint buttonCount, uint povCount, StringBuilder buffer, uint size);

		/// <remarks>Names and rows are UTF-8, as the databases are written: x360ce's database holds names in many scripts.</remarks>
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate uint SdlFindFunction(uint vendorId, uint productId, byte[] name, [In, Out] byte[] buffer, uint size);

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate uint IniMappingFunction(string iniPath, string section, StringBuilder buffer, uint size);

		/// <summary>The library's test hooks, from a copy in a folder of its own, beside the files given.</summary>
		sealed class Library
		{
			public string Folder;
			public SdlTranslateFunction Translate;
			public SdlFindFunction Find;
			public SdlFindFunction X360ceFind;
			public IniMappingFunction Ini;

			/// <summary>The mapping a section of a file in the library's folder gives, as the library describes it.</summary>
			public string Section(string file, string section)
			{
				var buffer = new StringBuilder(8192);
				Ini(Path.Combine(Folder, file), section, buffer, (uint)buffer.Capacity);
				return buffer.ToString();
			}

			public string Map(string row, SdlDevice device)
			{
				var buffer = new StringBuilder(8192);
				Translate(row, (uint)device.AxisMask, (uint)device.SliderMask, (uint)device.ButtonCount, (uint)device.PovCount, buffer, (uint)buffer.Capacity);
				return buffer.ToString();
			}

			public string Row(int vendorId, int productId, string name)
			{
				return Found(Find, vendorId, productId, name);
			}

			/// <summary>The row of x360ce's controller database the library gives a device, empty for none.</summary>
			public string X360ceRow(int vendorId, int productId, string name)
			{
				return Found(X360ceFind, vendorId, productId, name);
			}

			static string Found(SdlFindFunction find, int vendorId, int productId, string name)
			{
				var buffer = new byte[8192];
				find((uint)vendorId, (uint)productId, Encoding.UTF8.GetBytes((name ?? "") + "\0"), buffer, (uint)buffer.Length);
				var end = Array.IndexOf(buffer, (byte)0);
				return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end);
			}
		}

		/// <summary>The library built last for this process's bitness, or null when none is built.</summary>
		static FileInfo Built()
		{
			var bin = Path.Combine(Ui.RepoRoot.FullName, "Native", "x360ce", Environment.Is64BitProcess ? "bin64" : "bin");
			if (!Directory.Exists(bin))
				return null;
			return new DirectoryInfo(bin).GetFiles("xinput1_3.dll", SearchOption.AllDirectories)
				.OrderByDescending(x => x.Directory.Name == "Release")
				.ThenByDescending(x => x.LastWriteTimeUtc)
				.FirstOrDefault();
		}

		/// <summary>Loads a copy of the library from a new folder holding the files given, so it reads its own database.</summary>
		static Library Load(params KeyValuePair<string, string>[] files)
		{
			return Load(null, files);
		}

		/// <summary>Loads a copy of the library, changed first by <paramref name="change"/>, which is given the copy's path.</summary>
		static Library Load(Action<string> change, params KeyValuePair<string, string>[] files)
		{
			var built = Built();
			if (built == null)
				Assert.Inconclusive("The xinput library is not built for " + (Environment.Is64BitProcess ? "x64" : "x86") + ". Build it first.");
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.SdlNativeTest", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			foreach (var file in files)
				File.WriteAllText(Path.Combine(folder, file.Key), file.Value);
			var path = Path.Combine(folder, "x360ce_sdl_test.dll");
			built.CopyTo(path);
			change?.Invoke(path);
			Console.WriteLine("Library: " + built.FullName + " (built " + built.LastWriteTime + ")");
			var module = LoadLibrary(path);
			Assert.AreNotEqual(IntPtr.Zero, module, "The library did not load: error " + Marshal.GetLastWin32Error() + ".");
			return new Library
			{
				Folder = folder,
				Translate = Hook<SdlTranslateFunction>(module, "SdlTranslate"),
				Find = Hook<SdlFindFunction>(module, "SdlFind"),
				X360ceFind = Hook<SdlFindFunction>(module, "X360ceFind"),
				Ini = Hook<IniMappingFunction>(module, "IniMapping"),
			};
		}

		static T Hook<T>(IntPtr module, string name) where T : class
		{
			var address = GetProcAddress(module, name);
			Assert.AreNotEqual(IntPtr.Zero, address, "The library does not export " + name + ".");
			return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
		}

		#endregion

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("GuideButton counts from 1, as every other button key and as x360ce 3 writes it; 0 and -1 are no guide button")]
		public void Guide_button_counts_from_one()
		{
			// Not named x360ce.ini, which the library would read as its own when it loads.
			const string file = "mapping.ini";
			var library = Load(new KeyValuePair<string, string>(file, string.Join("\r\n",
				"[Eleven]", "GuideButton=11", "A=11", "[One]", "GuideButton=1", "[Zero]", "GuideButton=0", "[None]", "GuideButton=-1", "[Absent]", "B=2")));
			Assert.AreEqual("ButtonA=11 ButtonGuide=11", library.Section(file, "Eleven"), "GuideButton=11 is not the button A=11 reads.");
			Assert.AreEqual("ButtonGuide=1", library.Section(file, "One"));
			Assert.AreEqual("", library.Section(file, "Zero"), "GuideButton=0 is a guide button.");
			Assert.AreEqual("", library.Section(file, "None"), "GuideButton=-1 is a guide button.");
			Assert.AreEqual("ButtonB=2", library.Section(file, "Absent"));
		}

		#region The shared cases

		sealed class Case
		{
			public int Line;
			public string Description;
			public SdlDevice Device;
			public string Row;
			public string[] Expected;

			public override string ToString()
			{
				return "Case at line " + Line + (string.IsNullOrEmpty(Description) ? "" : " (" + Description + ")");
			}
		}

		static List<Case> Cases()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, @"Tests\Common\SdlConverterCases.txt");
			var cases = new List<Case>();
			Case current = null;
			var note = "";
			var lines = File.ReadAllLines(path);
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				if (line.Length == 0)
				{
					note = "";
					continue;
				}
				if (line.StartsWith("#", StringComparison.Ordinal))
				{
					note = (note + " " + line.TrimStart('#').Trim()).Trim();
					continue;
				}
				if (line.StartsWith("device:", StringComparison.Ordinal))
				{
					current = new Case { Line = i + 1, Description = note, Device = Device(line.Substring(7)) };
					continue;
				}
				Assert.IsNotNull(current, "Line " + (i + 1) + " is not inside a case.");
				if (line.StartsWith("row:", StringComparison.Ordinal))
					current.Row = line.Substring(4).Trim();
				else if (line.StartsWith("expect:", StringComparison.Ordinal))
				{
					current.Expected = Sorted(line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
					Assert.IsNotNull(current.Row, current + " has no row.");
					cases.Add(current);
					current = null;
				}
				else
					Assert.Fail("Line " + (i + 1) + " is not a device, row or expect line: " + line);
			}
			Assert.IsTrue(cases.Count >= 15, "The cases file holds only " + cases.Count + " cases.");
			return cases;
		}

		static SdlDevice Device(string text)
		{
			var device = new SdlDevice();
			foreach (var part in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var pair = part.Split('=');
				var value = pair[1].StartsWith("0x", StringComparison.Ordinal)
					? int.Parse(pair[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
					: int.Parse(pair[1], CultureInfo.InvariantCulture);
				switch (pair[0])
				{
					case "axes": device.AxisMask = value; break;
					case "sliders": device.SliderMask = value; break;
					case "buttons": device.ButtonCount = value; break;
					case "povs": device.PovCount = value; break;
					default: Assert.Fail("Unknown device part: " + part); break;
				}
			}
			return device;
		}

		static string[] Sorted(IEnumerable<string> boxes)
		{
			return boxes.OrderBy(x => x, StringComparer.Ordinal).ToArray();
		}

		/// <summary>The boxes x360ce 4's converter fills from a row, as "Box=value".</summary>
		static string[] EngineBoxes(Case c)
		{
			SdlMapping mapping;
			Assert.IsTrue(SdlMapping.TryParse(c.Row, out mapping), c + ": the row is not read.");
			var setting = SdlConverter.ToPadSetting(mapping, c.Device).Setting;
			return Sorted(SdlConverter.MappingBoxes
				.Select(x => new { Code = x, Value = SdlConverter.GetValue(setting, x) })
				.Where(x => x.Value.Length > 0)
				.Select(x => x.Code + "=" + x.Value));
		}

		static void AssertBoxes(Case c, string[] actual, string converter)
		{
			Assert.AreEqual(string.Join(" ", c.Expected), string.Join(" ", actual), c + ", " + converter + ": " + c.Row);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("x360ce 4's SDL converter passes the cases the xinput library's converter is held to")]
		public void Engine_converter_passes_the_shared_cases()
		{
			foreach (var c in Cases())
				AssertBoxes(c, EngineBoxes(c), "x360ce 4");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The xinput library's SDL converter passes the cases x360ce 4's converter is held to")]
		public void Native_converter_passes_the_shared_cases()
		{
			var library = Load();
			foreach (var c in Cases())
				AssertBoxes(c, Sorted(library.Map(c.Row, c.Device).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)), "xinput library");
		}

		#endregion

		#region The database

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The xinput library finds every Windows row of SDL's database the engine carries by the vendor, product and name the row is for")]
		public void Native_library_finds_every_row_by_its_device()
		{
			var library = Load();
			// The rows as the engine reads them, one line at a time, as the library gives them back: trimmed. Only rows that
			// name a vendor and product can be found by a device's; the "xinput" row is not one.
			var rows = SdlDatabase.ReadBuiltIn().Split('\n').Select(x => x.Trim(' ', '\t', '\r'))
				.Where(x => SdlSource.Parse(x, SdlSourceKind.BuiltIn).Mappings.Any(m => m.Guid.HasIds)).ToArray();
			Assert.IsTrue(rows.Length > 800, "The engine carries only " + rows.Length + " Windows rows.");
			var missed = new List<string>();
			foreach (var text in rows)
			{
				SdlMapping row;
				Assert.IsTrue(SdlMapping.TryParse(text, out row), "Not a row: " + text);
				if (library.Row(row.Guid.VendorId, row.Guid.ProductId, row.Name) != text)
					missed.Add(text);
			}
			Assert.AreEqual(0, missed.Count, "Rows not found by their own device:" + Environment.NewLine + string.Join(Environment.NewLine, missed.Take(10)));
			Assert.AreEqual("", library.Row(0xFFFF, 0xFFFE, "No such controller"), "A device SDL does not know was given a row.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The xinput library carries x360ce's controller database the engine carries and finds every row by its device")]
		public void Native_library_finds_every_row_of_x360ces_database()
		{
			var library = Load();
			var rows = X360ceDatabase.ReadBuiltIn().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Length > 0 && x[0] != '#').ToArray();
			Assert.IsTrue(rows.Length > 500, "The engine carries only " + rows.Length + " rows.");
			var missed = rows.Where(x =>
			{
				SdlMapping row;
				Assert.IsTrue(SdlMapping.TryParse(x, out row), "Not a row: " + x);
				return library.X360ceRow(row.Guid.VendorId, row.Guid.ProductId, row.Name) != x;
			}).ToArray();
			Assert.AreEqual(0, missed.Length, "Rows not found by their own device, or the library was built with another copy; build it again:"
				+ Environment.NewLine + string.Join(Environment.NewLine, missed.Take(10)));
			StringAssert.Contains(library.X360ceRow(0x046D, 0xC29B, "Logitech G27 Racing Wheel USB"), ",Logitech G27 Racing Wheel USB,users:", "The G27 has no row.");
			Assert.AreEqual("", library.X360ceRow(0xFFFF, 0xFFFE, "No such controller"), "A device x360ce's database does not know was given a row.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A built-in database whose gzip check fails reads as no rows, and the library goes on")]
		public void A_damaged_database_reads_as_empty()
		{
			var library = Load(path =>
			{
				// Both databases as the engine carries them, each with a CRC-32 that does not match.
				var update = BeginUpdateResource(path, false);
				Assert.AreNotEqual(IntPtr.Zero, update, "The library's copy cannot be changed: error " + Marshal.GetLastWin32Error() + ".");
				foreach (var resource in new[] { new { Id = SdlResource, File = "gamecontrollerdb.txt.gz" }, new { Id = X360ceResource, File = "gamecontrollerdb_x360ce.windows.txt.gz" } })
				{
					var bytes = File.ReadAllBytes(Path.Combine(Ui.RepoRoot.FullName, "Engine", "Resources", resource.File));
					bytes[bytes.Length - 8] ^= 0xFF;
					Assert.IsTrue(UpdateResource(update, (IntPtr)RcData, (IntPtr)resource.Id, EnglishUnitedStates, bytes, (uint)bytes.Length),
						"The library's copy cannot be changed: error " + Marshal.GetLastWin32Error() + ".");
				}
				Assert.IsTrue(EndUpdateResource(update, false), "The library's copy cannot be changed: error " + Marshal.GetLastWin32Error() + ".");
			});
			Assert.AreEqual("", library.Row(0x054C, 0x05C4, "Wireless Controller"), "A damaged SDL database gave a row.");
			Assert.AreEqual("", library.X360ceRow(0x046D, 0xC29B, "Logitech G27 Racing Wheel USB"), "A damaged x360ce database gave a row.");
			// The library still works: a row the converter is given is still read.
			StringAssert.Contains(library.Map("030000004c050000c405000000000000,PS4 Controller,a:b1,platform:Windows,", new SdlDevice { ButtonCount = 12 }), "ButtonA=2");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A gamecontrollerdb.txt beside the library wins for the devices it has rows for, and the name chooses among rows of one vendor and product")]
		public void A_file_beside_the_library_wins_for_its_devices()
		{
			const string xbox = "030000005e0400008e02000000000000,My Xbox Pad,a:b1,b:b0,platform:Windows,";
			const string linux = "030000005e0400008e02000010010000,Linux Pad,a:b2,platform:Linux,";
			const string first = "03000000ad1b00000100000000000000,Arcade Stick,a:b0,platform:Windows,";
			const string second = "03000000ad1b00000100000000000000,Racing Wheel Pro,a:b3,platform:Windows,";
			var library = Load(new KeyValuePair<string, string>("gamecontrollerdb.txt", string.Join("\r\n", "# My rows", linux, xbox, first, second)));
			Assert.AreEqual(xbox, library.Row(0x045E, 0x028E, "Controller (XBOX 360 For Windows)"), "The file's row did not win.");
			Assert.AreEqual(second, library.Row(0x1BAD, 0x0001, "Racing Wheel"), "The nearest name did not choose the row.");
			Assert.AreEqual(first, library.Row(0x1BAD, 0x0001, "Something Else"), "With no name nearer, the first row written was not taken.");
			StringAssert.StartsWith(library.Row(0x054C, 0x05C4, "Wireless Controller"), "03000000", "A device the file does not have lost the library's own row.");
		}

		#endregion
	}
}
