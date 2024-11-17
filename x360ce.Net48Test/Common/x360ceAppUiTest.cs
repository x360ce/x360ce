using JocysCom.ClassLibrary.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using x360ce.App.Service;

namespace x360ce.Tests
{
	[TestClass]
	public class x360ceAppUiTest
	{

		string TrayManagerName = TrayManager.TrayNotifyIconText;
		string exePath = "..\\..\\..\\x360ce.App.Beta\\bin\\Debug\\x360ce.exe";
		string mainWindow = "x360ceAppWin";
		string configWindow = "Jocys.com X360 Controller Emulator";

		[TestMethod]
		public void Test_Start()
		{
			var di = new DirectoryInfo(".");
			Console.WriteLine($"Current Path: {di.FullName}");
			var p = Process.Start(exePath);
			var appWindow = AutomationHelper.WaitForWindow(p, new Regex("^" + mainWindow, RegexOptions.IgnoreCase));
			var window = AutomationHelper.WaitForWindow(p, new Regex("^" + configWindow, RegexOptions.IgnoreCase));
			var info = window.Current;
			var appWindowPattern = appWindow.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
			var windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
			Task.Delay(1000).Wait();
			appWindowPattern.Close();
			//windowPattern.Close();
			//p.WaitForExit();
			Task.Delay(1000).Wait();
			//p.Close();
			Assert.IsTrue(true);
		}


		[TestMethod]
		public void Test_StartAndExitViaToolBar()
		{
			var p = Process.GetProcessesByName("x360ce").FirstOrDefault() ?? Process.Start(exePath);
			var appWindow = AutomationHelper.WaitForWindow(p, new Regex("^" + mainWindow, RegexOptions.IgnoreCase));
			var trayButtons = AutomationHelper.FindToolbarButtons();
			var trayButton = trayButtons.FirstOrDefault(x => x.Current.Name == TrayManagerName);
			AutomationHelper.ClickButton(trayButton);
			Task.Delay(500).Wait();
			var desktop = AutomationElement.RootElement;
			// Find menu
			var menu = AutomationHelper.FindFirstChild(desktop, ControlType.ToolBar, processId: p.Id);
			var menuButtons = AutomationHelper.FindAllChildren(menu, ControlType.MenuItem);
			var exitButton = menuButtons.First(x => x.Current.Name == "Exit");
			AutomationHelper.ClickButton(exitButton);
			p.WaitForExit();
		}

		[TestMethod]
		public void Test_NotifyIcon2()
		{
			var di = new DirectoryInfo(".");
			Console.WriteLine($"Current Path: {di.FullName}");
			var p = Process.Start(exePath);
			// Wait Application.Current.MainWindow.
			var appWindow = AutomationHelper.WaitForWindow(p, new Regex("^" + mainWindow, RegexOptions.IgnoreCase));
			var windows = AutomationHelper.FindWindowsByProcessId(p.Id);
			var list = new List<string>();
			foreach (var window in windows)
			{
				var all = AutomationHelper.GetAll(window, window.Current.ControlType.ProgrammaticName.Split('.').Last(), false);
				foreach (var child in all)
					list.Add(ToString(child.Key, child.Value));
			}
			//var toolBar = AutomationHelper.FindToolBarByProcessId(p.Id);
			var contents = string.Join("\r\n", list.ToArray());
			System.IO.File.WriteAllText("C:\\temp\\controls.automation.txt", contents);
			//MessageBox.Show(contents);
			Assert.IsTrue(true);
		}

		private static string ToString(AutomationElement e, string path)
		{
			/// Get all child controls with path.
			/// Use regex to make shorter tabbed path:
			var rx = new Regex("[^.]+[.]+");
			path = rx.Replace(path, "\t");
			var s = "";
			s += $"{path}: ";
			//s += $"{e.Current.ControlType.ProgrammaticName}";
			if (!string.IsNullOrEmpty(e.Current.ItemType))
				s += $", ItemType: {e.Current.ItemType}";
			if (!string.IsNullOrEmpty(e.Current.Name))
				s += $", Name: {e.Current.Name}";
			if (!string.IsNullOrEmpty(e.Current.AccessKey))
				s += $", AccessKey: {e.Current.AccessKey}";
			if (!string.IsNullOrEmpty(e.Current.AutomationId))
				s += $", AutomationId: {e.Current.AutomationId}";
			if (!string.IsNullOrEmpty(e.Current.ClassName))
				s += $", ClassName: {e.Current.ClassName}";
			return s;
		}

	}
}
