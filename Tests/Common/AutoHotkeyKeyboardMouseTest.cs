// @under-test: AutoHotkey/x360ce.ahk, Engine/Input/Script/ScriptChannel.cs
// @area: engine   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Plain AutoHotkey code, run by x360ce.ahk's hotkeys, turns a controller's buttons, hat and sticks into keyboard keys,
	/// mouse buttons and mouse moves: AutoHotkey's own Send, ControlSend, SetTimer and MouseMove do the work, x360ce only
	/// says what the controller does.
	/// </summary>
	[TestClass]
	public class AutoHotkeyKeyboardMouseTest
	{
		/// <summary>Writes to a file what a keyboard and mouse script would send, so the desktop is never touched.</summary>
		const string RecordingScript =
			"global Out := A_ScriptDir \"\\output.txt\"\r\n" +
			"Log(text) {\r\n" +
			"\tloop 100 {\r\n" +
			"\t\ttry {\r\n" +
			"\t\t\tFileAppend(text \"`n\", Out)\r\n" +
			"\t\t\treturn\r\n" +
			"\t\t}\r\n" +
			"\t\tSleep(5)\r\n" +
			"\t}\r\n" +
			"}\r\n" +
			"dev := X360ce.c1.d1\r\n" +
			"dev.Hotkey(\"b1\", (*) => Log(\"{a down}\"))\r\n" +
			"dev.Hotkey(\"b1 up\", (*) => Log(\"{a up}\"))\r\n" +
			"dev.Hotkey(\"d1\", (*) => Log(\"{Up down}\"))\r\n" +
			"dev.Hotkey(\"d1 up\", (*) => Log(\"{Up up}\"))\r\n" +
			"dev.Hotkey(\"a1 > 50%\", (*) => Log(\"{Right down}\"))\r\n" +
			"dev.Hotkey(\"a1 > 50% up\", (*) => Log(\"{Right up}\"))\r\n" +
			"X360ce.c2.d1.Hotkey(\"b2\", (*) => Log(\"{LButton down}\"))\r\n" +
			"X360ce.c2.d1.Hotkey(\"b2 up\", (*) => Log(\"{LButton up}\"))\r\n" +
			"X360ce.c1.d2.Hotkey(\"b4\", (*) => Log(\"{c down}\"))\r\n" +
			"X360ce.c1.d2.Hotkey(\"b4 up\", (*) => Log(\"{c up}\"))\r\n" +
			"try dev.Hotkey(\"q1\", (*) => 0)\r\n" +
			"catch ValueError as e\r\n" +
			"\tLog(\"refused \" e.Message)\r\n" +
			"try dev.x1 := 1\r\n" +
			"catch ValueError as e\r\n" +
			"\tLog(\"refused \" e.Message)\r\n" +
			"SetTimer(MovePointer, 10)\r\n" +
			"MovePointer() {\r\n" +
			"\tx := dev.a4, y := dev.a5\r\n" +
			"\tif x != \"\" && Abs(x) > 0.15\r\n" +
			"\t\tLog(\"move \" Round(x * 20) \" \" (Abs(y) > 0.15 ? Round(y * 20) : 0))\r\n" +
			"}\r\n";

		/// <summary>What the script has written so far, read once its FileAppend lets go of the file, which it holds alone.</summary>
		static string[] Lines(AutoHotkeyRun run)
		{
			var path = Path.Combine(run.Folder, "output.txt");
			for (var attempt = 0; ; attempt++)
			{
				if (!File.Exists(path))
					return new string[0];
				try
				{
					using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					using (var reader = new StreamReader(stream, Encoding.UTF8))
						return reader.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
				}
				catch (IOException) when (attempt < 100)
				{
					System.Threading.Thread.Sleep(5);
				}
			}
		}

		static void Expect(AutoHotkeyRun run, string line, string why)
		{
			Assert.IsTrue(run.PassUntil(() => Lines(run).Contains(line), 1000), why + " Output: " + string.Join(" | ", Lines(run)) + ". " + run.Counters());
		}

		[TestMethod, TestCategory("engine")]
		[Description("A button, a hat direction, an axis past a limit, buttons of other devices and a stick reach plain AutoHotkey code")]
		public void Controls_become_keys_mouse_buttons_and_mouse_moves()
		{
			using (var run = new AutoHotkeyRun(RecordingScript))
			{
				run.Start(() => run.SlotInt(run.Slot, ScriptChannel.SlotOwnButtons) == 1
					&& run.SlotInt(run.Slot2, ScriptChannel.SlotOwnButtons) == (1 << 1 | 1 << 3));
				run.PassUntil(() => Lines(run).Length >= 2, 2000);
				run.Passes(100);
				CollectionAssert.AreEqual(new[] { "refused Not a control: q1", "refused Half an axis or slider cannot be set: set a1 instead." },
					Lines(run), "A wrong name was not refused, or the script got a press nobody made: " + string.Join(" | ", Lines(run)));

				run.Device.Buttons[0] = true;
				Expect(run, "{a down}", "Button 1 did not run its hotkey.");
				Assert.IsFalse(run.Mapped.Buttons[0], "A button the script took still reached the controller.");
				run.Device.Buttons[0] = false;
				Expect(run, "{a up}", "Letting go of button 1 did not run its hotkey.");

				run.Device.Povs[0] = 0;
				Expect(run, "{Up down}", "The hat pushed up did not run d1.");
				run.Device.Povs[0] = -1;
				Expect(run, "{Up up}", "The hat at rest did not run d1 up.");

				run.Device.Axis[0] = 65535;
				Expect(run, "{Right down}", "Axis 1 past 50 % did not run its hotkey.");
				run.Device.Axis[0] = 32767;
				Expect(run, "{Right up}", "Axis 1 back did not run its up hotkey.");

				run.Device2.Buttons[1] = true;
				Expect(run, "{LButton down}", "c2.d1.b2 did not run its hotkey.");
				run.Device2.Buttons[1] = false;
				Expect(run, "{LButton up}", "Letting go of c2.d1.b2 did not run its hotkey.");

				run.Device2.Buttons[3] = true;
				Expect(run, "{c down}", "c1.d2.b4, the same device as device 2 on Controller 1, did not run its hotkey.");
				run.Device2.Buttons[3] = false;
				Expect(run, "{c up}", "Letting go of c1.d2.b4 did not run its hotkey.");

				var before = Lines(run).Count(x => x.StartsWith("move "));
				run.Device.Axis[3] = 65535;
				run.Passes(300);
				run.Device.Axis[3] = 32767;
				run.Passes(100);
				var moves = Lines(run).Where(x => x.StartsWith("move ")).Skip(before).Select(x => x.Split(' ')).ToArray();
				Assert.IsTrue(moves.Length >= 5, "The stick tilted for 300 ms moved the mouse " + moves.Length + " times.");
				Assert.IsTrue(moves.All(m => m[1] == "20" && m[2] == "0"), "The stick tilted right did not move the mouse right only: "
					+ string.Join(" | ", moves.Select(m => m[1] + "," + m[2])));
				var count = Lines(run).Count(x => x.StartsWith("move "));
				run.Passes(200);
				Assert.AreEqual(count, Lines(run).Count(x => x.StartsWith("move ")), "The mouse kept moving with the stick at rest.");
			}
		}

		#region Typing into a window

		/// <summary>A hidden window with a text box, which the buttons type into with ControlSend, without the window being active.</summary>
		const string TypingScript =
			"DetectHiddenWindows(true)\r\n" +
			"box := Gui(, \"x360ce typing test \" A_Args[1])\r\n" +
			"box.Add(\"Edit\", \"r5 w300\")\r\n" +
			"box.Show(\"Hide\")\r\n" +
			"global target := \"ahk_id \" box.Hwnd\r\n" +
			"global dev := X360ce.c1.d1\r\n" +
			"Hold(button, key) {\r\n" +
			"\tdev.Hotkey(button, (*) => ControlSend(\"{\" key \" down}\", \"Edit1\", target))\r\n" +
			"\tdev.Hotkey(button \" up\", (*) => ControlSend(\"{\" key \" up}\", \"Edit1\", target))\r\n" +
			"}\r\n" +
			"Hold(\"b1\", \"h\")\r\n" +
			"Hold(\"b2\", \"i\")\r\n" +
			"Hold(\"b3\", \"Space\")\r\n" +
			"Hold(\"b4\", \"Enter\")\r\n" +
			"dev.Hotkey(\"b5\", (*) => ControlSendText(\"Hello from x360ce!\", \"Edit1\", target))\r\n";

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr FindWindow(string className, string windowName);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string windowName);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, StringBuilder lParam);

		/// <summary>The text of a window, read with WM_GETTEXT, which Windows carries across processes.</summary>
		static string GetText(IntPtr hWnd)
		{
			var text = new StringBuilder(4096);
			SendMessage(hWnd, 0x000D, (IntPtr)text.Capacity, text);
			return text.ToString();
		}

		/// <summary>Presses and lets go of a button, waiting a few passes after each.</summary>
		static void Tap(AutoHotkeyRun run, int button)
		{
			run.Device.Buttons[button] = true;
			run.Passes(40);
			run.Device.Buttons[button] = false;
			run.Passes(40);
		}

		[TestMethod, TestCategory("engine")]
		[Description("Controller buttons type into a text box of a window that is not active, without touching the real keyboard")]
		public void Buttons_type_into_a_window_that_is_not_active()
		{
			var id = Guid.NewGuid().ToString("N");
			using (var run = new AutoHotkeyRun(TypingScript))
			{
				run.Start(() => run.SlotInt(run.Slot, ScriptChannel.SlotOwnButtons) == 0x1F, id);
				var window = IntPtr.Zero;
				Assert.IsTrue(run.PassUntil(() => (window = FindWindow(null, "x360ce typing test " + id)) != IntPtr.Zero, 5000), "The script's window was not found.");
				var box = FindWindowEx(window, IntPtr.Zero, "Edit", null);
				Assert.AreNotEqual(IntPtr.Zero, box, "The script's text box was not found.");

				Tap(run, 4);
				Tap(run, 3);
				foreach (var button in new[] { 0, 1, 2, 0, 1 })
					Tap(run, button);
				var expected = "Hello from x360ce!\r\nhi hi";
				Assert.IsTrue(run.PassUntil(() => GetText(box) == expected, 2000),
					"The text box does not hold what the controller typed: \"" + GetText(box).Replace("\r", "\\r").Replace("\n", "\\n") + "\"");
			}
		}

		#endregion

		#region Notepad

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		static extern bool GetCursorPos(out POINT point);

		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

		[DllImport("user32.dll")]
		static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		struct POINT { public int X, Y; }

		struct RECT { public int Left, Top, Right, Bottom; }

		/// <summary>Types with the real keyboard, but only while the test's Notepad is in front.</summary>
		const string NotepadScript =
			"global np := \"ahk_id \" A_Args[1]\r\n" +
			"WinActivate(np)\r\n" +
			"global dev := X360ce.c1.d1\r\n" +
			"Hold(button, key) {\r\n" +
			"\tdev.Hotkey(button, (*) => WinActive(np) && Send(\"{\" key \" down}\"))\r\n" +
			"\tdev.Hotkey(button \" up\", (*) => Send(\"{\" key \" up}\"))\r\n" +
			"}\r\n" +
			"Hold(\"b1\", \"h\")\r\n" +
			"Hold(\"b2\", \"i\")\r\n" +
			"Hold(\"b3\", \"Space\")\r\n" +
			"Hold(\"b4\", \"Enter\")\r\n" +
			"dev.Hotkey(\"b5\", (*) => WinActive(np) && SendText(\"Hello from x360ce!\"))\r\n" +
			"dev.Hotkey(\"b6\", (*) => WinActive(np) && Send(\"^s\"))\r\n" +
			"SetTimer(MovePointer, 10)\r\n" +
			"MovePointer() {\r\n" +
			"\tx := dev.a4\r\n" +
			"\tif WinActive(np) && x != \"\" && Abs(x) > 0.15\r\n" +
			"\t\tMouseMove(x * 20, 0, 0, \"R\")\r\n" +
			"}\r\n";

		/// <summary>Presses and lets go of a button while Notepad stays in front; a person who uses the keyboard or mouse meanwhile stops the test.</summary>
		static void Tap(AutoHotkeyRun run, int button, IntPtr notepad)
		{
			if (GetForegroundWindow() != notepad)
				Assert.Inconclusive("Notepad lost the focus while the test typed: the keyboard or mouse was used. Nothing was typed anywhere else.");
			Tap(run, button);
		}

		[TestMethod, TestCategory("ui-interactive")]
		[Description("Controller buttons type into Notepad and save the file; the stick moves the mouse over it")]
		public void A_controller_types_into_Notepad_and_moves_the_mouse()
		{
			if (Process.GetProcessesByName("notepad").Length > 0)
				Assert.Inconclusive("Notepad is open: close it, so this test types only into a Notepad it opened.");
			var file = Path.Combine(Path.GetTempPath(), "x360ce-notepad-" + Guid.NewGuid().ToString("N") + ".txt");
			File.WriteAllText(file, "");
			IntPtr notepad = IntPtr.Zero;
			POINT cursor;
			GetCursorPos(out cursor);
			try
			{
				Process.Start("notepad.exe", "\"" + file + "\"");
				// The window is found by its title, which names the file: never a Notepad window of anyone else.
				var title = Path.GetFileNameWithoutExtension(file);
				var watch = Stopwatch.StartNew();
				while (notepad == IntPtr.Zero && watch.ElapsedMilliseconds < 15000)
				{
					var found = Process.GetProcessesByName("notepad").FirstOrDefault(p => p.MainWindowTitle.Contains(title));
					if (found != null)
						notepad = found.MainWindowHandle;
					else
						System.Threading.Thread.Sleep(100);
				}
				Assert.AreNotEqual(IntPtr.Zero, notepad, "Notepad did not open the test's file within 15 seconds.");
				using (var run = new AutoHotkeyRun(NotepadScript))
				{
					run.Start(() => run.SlotInt(run.Slot, ScriptChannel.SlotOwnButtons) == 0x3F, notepad.ToInt64().ToString());
					Assert.IsTrue(run.PassUntil(() => GetForegroundWindow() == notepad, 3000), "Notepad did not come to the front.");

					Tap(run, 4, notepad);
					Tap(run, 3, notepad);
					foreach (var button in new[] { 0, 1, 2, 0, 1 })
						Tap(run, button, notepad);
					Tap(run, 5, notepad);
					var expected = "Hello from x360ce!\r\nhi hi";
					Assert.IsTrue(run.PassUntil(() => Saved(file) == expected, 5000),
						"Notepad did not save what the controller typed. Saved: \"" + (Saved(file) ?? "").Replace("\r", "\\r").Replace("\n", "\\n") + "\"");

					RECT rect;
					GetWindowRect(notepad, out rect);
					SetCursorPos((rect.Left * 3 + rect.Right) / 4, (rect.Top + rect.Bottom) / 2);
					POINT start;
					GetCursorPos(out start);
					run.Device.Axis[3] = 65535;
					run.Passes(400);
					run.Device.Axis[3] = 32767;
					run.Passes(50);
					POINT end;
					GetCursorPos(out end);
					Console.WriteLine("The stick tilted right for 400 ms moved the mouse " + (end.X - start.X) + " pixels right, " + (end.Y - start.Y) + " down.");
					Assert.IsTrue(end.X - start.X >= 100, "The stick tilted right did not move the mouse right.");
				}
			}
			finally
			{
				SetCursorPos(cursor.X, cursor.Y);
				if (notepad != IntPtr.Zero)
				{
					uint id;
					GetWindowThreadProcessId(notepad, out id);
					// WM_CLOSE: the test's file is saved, so Notepad closes without asking.
					PostMessage(notepad, 0x0010, IntPtr.Zero, IntPtr.Zero);
					try
					{
						using (var process = Process.GetProcessById((int)id))
							process.WaitForExit(5000);
					}
					catch (ArgumentException)
					{
						// Already closed.
					}
				}
				File.Delete(file);
			}
		}

		static string Saved(string file)
		{
			try
			{
				using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (var reader = new StreamReader(stream, Encoding.UTF8))
					return reader.ReadToEnd();
			}
			catch (IOException)
			{
				return null;
			}
		}

		#endregion
	}
}
