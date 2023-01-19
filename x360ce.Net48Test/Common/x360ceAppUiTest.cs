using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;


namespace x360ce.Tests
{
	[TestClass]
	public class x360ceAppUiTest
	{
		string exePath = "..\\..\\..\\x360ce.App.Beta\\bin\\Debug\\x360ce.exe";
		string winName = "Jocys.com X360 Controller Emulator";

		[TestMethod]
		public void Test_Start()
		{
			var di = new DirectoryInfo(".");
			Console.WriteLine($"Current Path: {di.FullName}");
			var p = Process.Start(exePath);
			var window = AutomationHelper.FindWindow(p, new Regex("^" + winName, RegexOptions.IgnoreCase));
			var info = window.Current;
			var windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
			Task.Delay(1000).Wait();
			windowPattern.Close();
			Task.Delay(1000).Wait();
			p.Close();
			Assert.IsTrue(true);

		}

	}
}
