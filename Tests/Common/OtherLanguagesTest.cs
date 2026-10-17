// @under-test: Engine/Common/ForegroundWindowHook.cs, Engine/Common/Ini.cs, Engine/JocysCom/Win32/NativeMethods.user32.cs, Engine/JocysCom/Win32/NativeMethods.advapi32.cs
// @area: languages   @layer: unit
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Text in other languages, through every way the program hands text to Windows: the path of a running
	/// game, window titles, the registry, and x360ce.ini.
	/// </summary>
	/// <remarks>
	/// A Windows function imported without a character set is called in its ANSI form, which writes each
	/// character the PC's language for non-Unicode programs cannot hold as a question mark. A game in a
	/// folder named in another script then never matched its entry in the list. Chinese, Greek and Cyrillic
	/// together are held by no one such language, so the text below fails through any ANSI call on any PC.
	/// </remarks>
	[TestClass]
	public class OtherLanguagesTest
	{
		/// <summary>Chinese, Greek and Cyrillic, which no one language for non-Unicode programs holds.</summary>
		internal const string Text = "x360ce 测试 Ελληνικά Тест";

		/// <summary>The program's own assemblies, which hold every Windows function it imports.</summary>
		static readonly Assembly[] Programs = { typeof(EngineHelper).Assembly, typeof(App.MainForm).Assembly };

		/// <summary>Windows functions that have no Unicode form and take ANSI text by design.</summary>
		static readonly string[] AnsiOnly = { "GetProcAddress" };

		[TestMethod, TestCategory("languages")]
		[Description("Every Windows function the program imports that takes text calls its Unicode form")]
		public void Every_imported_function_that_takes_text_calls_the_Unicode_form()
		{
			var ansi = new List<string>();
			foreach (var type in Programs.SelectMany(LoadableTypes))
			{
				var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (var method in methods)
				{
					var import = (DllImportAttribute)method.GetCustomAttributes(typeof(DllImportAttribute), false).FirstOrDefault();
					if (import == null || import.CharSet == CharSet.Unicode || import.CharSet == CharSet.Auto)
						continue;
					if (AnsiOnly.Contains(import.EntryPoint ?? method.Name))
						continue;
					var text = method.GetParameters().Where(x => CarriesText(x.ParameterType) && !IsWide(x)).Select(x => x.Name).ToArray();
					if (text.Length > 0)
						ansi.Add(type.FullName + "." + method.Name + "(" + string.Join(", ", text) + ")");
				}
			}
			Assert.AreEqual(0, ansi.Count, "These take text in the ANSI form, which turns other languages into question marks: "
				+ string.Join("; ", ansi));
		}

		[TestMethod, TestCategory("languages"), TestCategory("games")]
		[Description("A program running from a folder named in other languages reads back with its own path")]
		public void A_program_in_a_folder_named_in_other_languages_reads_back_its_path()
		{
			var folder = Path.Combine(Path.GetTempPath(), Text + " " + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var exe = Path.Combine(folder, "game.exe");
			File.Copy(Path.Combine(Environment.SystemDirectory, "PING.EXE"), exe);
			var start = new ProcessStartInfo(exe, "-n 30 127.0.0.1")
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
			};
			using (var game = Process.Start(start))
			{
				try
				{
					Assert.AreEqual(exe, ForegroundWindowHook.GetProcessFileName(game), true,
						"The running program's path came back changed, so a game listed with it is never found running.");
				}
				finally
				{
					game.Kill();
					game.WaitForExit(5000);
					Directory.Delete(folder, true);
				}
			}
		}

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("A window titled in other languages reads back its title and is found by it")]
		public void A_window_titled_in_other_languages_reads_back_and_is_found_by_its_title()
		{
			var title = Text + " " + Guid.NewGuid().ToString("N");
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { Text = title, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000) })
				using (var button = new Button { Text = title })
				{
					form.Controls.Add(button);
					form.Show();
					var read = new StringBuilder(256);
					NativeMethods.GetWindowText(form.Handle, read, read.Capacity);
					Assert.AreEqual(title, read.ToString(), "The window's title read back changed.");
					Assert.AreEqual(form.Handle, NativeMethods.FindWindow(null, title), "The window was not found by its title.");
					Assert.AreEqual(button.Handle, NativeMethods.FindWindowEx(form.Handle, IntPtr.Zero, null, title),
						"The button was not found in its window by its text.");
				}
			});
		}

		[TestMethod, TestCategory("languages"), TestCategory("settings")]
		[Description("A registry value written in other languages reads back unchanged")]
		public void A_registry_value_in_other_languages_reads_back()
		{
			var root = @"Software\x360ce.Tests";
			try
			{
				using (var key = Registry.CurrentUser.CreateSubKey(root + @"\" + Guid.NewGuid().ToString("N")))
				{
					key.SetValue("PortName", Text);
					var value = new StringBuilder(64);
					// In bytes, as the Unicode form counts them.
					var size = value.Capacity * sizeof(char);
					var type = default(REG);
					var result = NativeMethods.RegQueryValueEx(key.Handle.DangerousGetHandle(), "PortName", 0, ref type, value, ref size);
					Assert.AreEqual(0, result, "The value could not be read.");
					Assert.AreEqual(Text, value.ToString(), "The value read back changed.");
				}
			}
			finally
			{
				Registry.CurrentUser.DeleteSubKeyTree(root, false);
			}
		}

		[TestMethod, TestCategory("languages"), TestCategory("settings")]
		[Description("A section and value of x360ce.ini written in other languages read back unchanged")]
		public void An_ini_value_in_other_languages_reads_back()
		{
			var file = Path.Combine(Path.GetTempPath(), "x360ce-" + Guid.NewGuid().ToString("N") + ".ini");
			try
			{
				// As the program keeps x360ce.ini: in UTF-16, the one encoding the profile functions keep every language in.
				var ini = new Ini(file);
				ini.EnsureUnicode();
				ini.SetValue(Text, "ProductName", Text);
				Assert.AreEqual(Text, ini.GetValue(Text, "ProductName"), "The value read back changed.");
			}
			finally
			{
				File.Delete(file);
			}
		}

		/// <summary>The types of an assembly that load here; one referring to a library absent from the tests is left out.</summary>
		static IEnumerable<Type> LoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null);
			}
		}

		/// <summary>True for a parameter Windows reads or writes as characters.</summary>
		static bool CarriesText(Type type)
		{
			if (type.IsByRef || type.IsArray)
				type = type.GetElementType();
			return type == typeof(string) || type == typeof(StringBuilder) || type == typeof(char);
		}

		/// <summary>True for a parameter marshalled as UTF-16 whatever the function's character set.</summary>
		static bool IsWide(ParameterInfo parameter)
		{
			var marshal = (MarshalAsAttribute)parameter.GetCustomAttributes(typeof(MarshalAsAttribute), false).FirstOrDefault();
			return marshal != null && (marshal.Value == UnmanagedType.LPWStr || marshal.Value == UnmanagedType.BStr);
		}
	}
}
