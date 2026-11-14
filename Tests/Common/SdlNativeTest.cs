// @under-test: Native/x360ce/Sdl.cpp, Native/x360ce/gamecontrollerdb.windows.txt, Engine/Common/Sdl/SdlConverter.cs
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
	/// cases in SdlConverterCases.txt, the library carries the Windows rows of the database the engine carries, and finds
	/// a device's row by its vendor and product, a gamecontrollerdb.txt beside it first.
	/// </summary>
	/// <remarks>
	/// The library is the one built for this process's bitness, loaded from a copy under a name of its own through its
	/// test hooks SdlTranslate and SdlFind. Loaded, it reads no controller and starts nothing.
	/// </remarks>
	[TestClass]
	public class SdlNativeTest
	{
		/// <summary>Set to 1, the database test writes the library's copy instead of failing.</summary>
		const string WriteVariable = "X360CE_WRITE_SDL_COPY";

		const string CopyPath = @"Native\x360ce\gamecontrollerdb.windows.txt";

		#region The library

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr LoadLibrary(string path);

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		static extern IntPtr GetProcAddress(IntPtr module, string name);

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate uint SdlTranslateFunction(string row, uint axisMask, uint sliderMask, uint buttonCount, uint povCount, StringBuilder buffer, uint size);

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate uint SdlFindFunction(uint vendorId, uint productId, string name, StringBuilder buffer, uint size);

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate uint IniMappingFunction(string iniPath, string section, StringBuilder buffer, uint size);

		/// <summary>The library's test hooks, from a copy in a folder of its own, beside the files given.</summary>
		sealed class Library
		{
			public string Folder;
			public SdlTranslateFunction Translate;
			public SdlFindFunction Find;
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
				var buffer = new StringBuilder(8192);
				Find((uint)vendorId, (uint)productId, name, buffer, (uint)buffer.Capacity);
				return buffer.ToString();
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
			var built = Built();
			if (built == null)
				Assert.Inconclusive("The xinput library is not built for " + (Environment.Is64BitProcess ? "x64" : "x86") + ". Build it first.");
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.SdlNativeTest", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			foreach (var file in files)
				File.WriteAllText(Path.Combine(folder, file.Key), file.Value);
			var path = Path.Combine(folder, "x360ce_sdl_test.dll");
			built.CopyTo(path);
			Console.WriteLine("Library: " + built.FullName + " (built " + built.LastWriteTime + ")");
			var module = LoadLibrary(path);
			Assert.AreNotEqual(IntPtr.Zero, module, "The library did not load: error " + Marshal.GetLastWin32Error() + ".");
			return new Library
			{
				Folder = folder,
				Translate = Hook<SdlTranslateFunction>(module, "SdlTranslate"),
				Find = Hook<SdlFindFunction>(module, "SdlFind"),
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

		/// <summary>The library's copy of the database: the Windows rows of the one the engine carries, sorted by vendor and product, with its commit and licence.</summary>
		static string NativeCopy()
		{
			var source = SdlSource.Parse(SdlDatabase.ReadBuiltIn(), SdlSourceKind.BuiltIn);
			Assert.IsNotNull(source.Sha, "The engine's database has no commit line.");
			var sb = new StringBuilder();
			sb.Append("# SDL_GameControllerDB's Windows rows, sorted by vendor and product, for x360ce's xinput library (Sdl.cpp).\n");
			sb.Append("# Written from Engine\\Resources\\gamecontrollerdb.txt.gz by SdlNativeTest; do not edit by hand.\n");
			sb.Append(SdlSource.HeaderPrefix).Append(source.Sha).Append(' ')
				.Append(source.Date.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)).Append('\n');
			sb.Append("#\n");
			foreach (var line in SdlDatabase.ReadLicence().Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
				sb.Append(("# " + line).TrimEnd()).Append('\n');
			// Only rows that name a vendor and product can be found by a device's; the "xinput" row is not one.
			var rows = source.Mappings.Where(x => x.Guid.HasIds)
				.OrderBy(x => x.Guid.VendorId).ThenBy(x => x.Guid.ProductId);
			foreach (var row in rows)
				sb.Append(row.ToString()).Append('\n');
			return sb.ToString();
		}

		[TestMethod, TestCategory("mapping"), TestCategory("build"), TestCategory("critical")]
		[Description("The xinput library carries the Windows rows of the SDL database the engine carries, sorted by vendor and product")]
		public void Native_library_carries_the_engines_database()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, CopyPath);
			var expected = NativeCopy();
			var actual = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
			if (actual == expected)
				return;
			if (Environment.GetEnvironmentVariable(WriteVariable) == "1")
			{
				File.WriteAllText(path, expected, new UTF8Encoding(false));
				Console.WriteLine("Wrote " + path + "; build the xinput library again.");
				return;
			}
			Assert.Fail(CopyPath + " differs from Engine\\Resources\\gamecontrollerdb.txt.gz. Write it again, then build the xinput library:"
				+ Environment.NewLine + "$env:" + WriteVariable + " = '1'; vstest.console.exe Tests\\bin\\Release\\net462\\x360ce.Tests.dll /Tests:"
				+ nameof(SdlNativeTest) + "." + nameof(Native_library_carries_the_engines_database) + "; Remove-Item env:" + WriteVariable);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The xinput library finds every row it carries by the vendor, product and name the row is for")]
		public void Native_library_finds_every_row_by_its_device()
		{
			var library = Load();
			var rows = File.ReadAllLines(Path.Combine(Ui.RepoRoot.FullName, CopyPath))
				.Where(x => x.Length > 0 && x[0] != '#').ToArray();
			Assert.IsTrue(rows.Length > 800, "The library carries only " + rows.Length + " rows.");
			var missed = new List<string>();
			long key = -1;
			foreach (var text in rows)
			{
				SdlMapping row;
				Assert.IsTrue(SdlMapping.TryParse(text, out row), "Not a row: " + text);
				var rowKey = (long)row.Guid.VendorId << 16 | (long)row.Guid.ProductId;
				Assert.IsTrue(rowKey >= key, "The rows are not sorted by vendor and product at: " + text);
				key = rowKey;
				if (library.Row(row.Guid.VendorId, row.Guid.ProductId, row.Name) != text)
					missed.Add(text);
			}
			Assert.AreEqual(0, missed.Count, "Rows not found by their own device:" + Environment.NewLine + string.Join(Environment.NewLine, missed.Take(10)));
			Assert.AreEqual("", library.Row(0xFFFF, 0xFFFE, "No such controller"), "A device SDL does not know was given a row.");
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
