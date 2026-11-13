// @under-test: App.v4/Forms/ErrorReportForm.Designer.cs, App.v4/Forms/WebBrowserForm.Designer.cs, App.v4/Forms/HelpForm.cs, App.v3/Controls/MessageBoxForm.Designer.cs, Engine/JocysCom/Controls/InfoForm.cs, Engine/JocysCom/Controls/MessageBoxForm.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Every window the program opens shows the program's icon in its title bar, on its taskbar button and in Alt+Tab,
	/// never the icon Windows Forms gives a window that sets none.
	/// </summary>
	/// <remarks>
	/// x360ce's own windows carry x360ce's icon from its resources. The windows of the class library, which other programs
	/// show too, carry the icon of the program that shows them, read from its file.
	/// </remarks>
	[TestClass]
	public class WindowIconTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The program's windows that are not made here, and why.</summary>
		static readonly Dictionary<string, string> NotMade = new Dictionary<string, string>
		{
			{ "x360ce.App.MainForm", "it starts the engine and reads the settings as it is made; its designer is read instead" },
			{ "x360ce.App.Controls.BaseFormWithHeader", "a base for windows, never shown on its own: each window made from it sets its icon" },
			{ "JocysCom.ClassLibrary.IO.DeviceDetectorForm", "never shown: it only receives Windows' device messages" },
		};

		/// <summary>The line every x360ce window's designer gives it its icon with.</summary>
		const string IconLine = "this.Icon = global::x360ce.App.Properties.Resources.app;";

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Every window of x360ce 4 and of the class library it uses, made as the program makes it, shows the program's icon in its title bar")]
		public void Every_window_shows_the_programs_icon()
		{
			var faults = new List<string>();
			var made = new List<string>();
			Ui.OnUiThread(() =>
			{
				using (var plain = new Form())
				using (var program = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
				using (var app = App.Properties.Resources.app)
				using (var hardware = App.Properties.Resources.hardware)
				{
					Assert.IsFalse(Same(plain.Icon, program), "The test host's own icon is the one Windows Forms gives a window that sets none, so the test cannot tell them apart.");
					var library = typeof(JocysCom.ClassLibrary.Controls.MessageBoxForm).Assembly;
					foreach (var type in WindowTypes())
					{
						var name = type.FullName;
						// The window shows x360ce's icon, but the Hardware window, which shows the computer's hardware, its own.
						var expected = type.Assembly == library ? program : type == typeof(App.Forms.HardwareForm) ? hardware : app;
						using (var form = (Form)Activator.CreateInstance(type, true))
						{
							made.Add(name);
							if (!form.ShowIcon)
								faults.Add(name + " shows no icon in its title bar.");
							if (Same(form.Icon, plain.Icon))
								faults.Add(name + " has the icon Windows Forms gives a window that sets none.");
							else if (!Same(form.Icon, expected))
								faults.Add(name + " has an icon that is not the program's.");
						}
					}
				}
			});
			foreach (var name in made)
				TestContext.WriteLine("Made: " + name);
			foreach (var pair in NotMade)
				TestContext.WriteLine("Not made: " + pair.Key + ": " + pair.Value);
			Assert.IsTrue(made.Contains(typeof(App.Forms.ErrorReportForm).FullName), "The error report window was not made.");
			Assert.IsTrue(made.Contains(typeof(JocysCom.ClassLibrary.Controls.MessageBoxForm).FullName), "The message box was not made.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The windows that cannot be made in the test, x360ce 4's main window and every window of x360ce 3, are given x360ce's icon by their designers")]
		public void Windows_not_made_here_are_given_the_programs_icon_by_their_designers()
		{
			var root = Ui.RepoRoot.FullName;
			// x360ce 3 builds x360ce.exe in the same namespace as x360ce 4, so the test cannot load both: its windows are read.
			var designers = new List<string> { Path.Combine(root, "App.v4", "MainForm.Designer.cs") };
			foreach (var file in Directory.GetFiles(Path.Combine(root, "App.v3"), "*.cs", SearchOption.AllDirectories))
			{
				if (file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) || file.Contains(@"\obj\") || file.Contains(@"\bin\"))
					continue;
				if (Regex.IsMatch(File.ReadAllText(file), @"\bclass\s+\w+\s*:\s*(System\.Windows\.Forms\.)?Form\b"))
					designers.Add(Path.ChangeExtension(file, ".Designer.cs"));
			}
			Assert.IsTrue(designers.Count >= 5, "Only " + designers.Count + " windows were found.");
			var missing = designers.Where(x => !File.Exists(x) || !File.ReadAllText(x).Contains(IconLine)).ToList();
			foreach (var file in designers)
				TestContext.WriteLine("Read: " + file.Substring(root.Length + 1));
			Assert.AreEqual(0, missing.Count, "These windows are not given x360ce's icon:" + Environment.NewLine
				+ string.Join(Environment.NewLine, missing.Select(x => x.Substring(root.Length + 1))));
		}

		/// <summary>The windows of x360ce 4 and of the class library it uses, but those <see cref="NotMade"/> and the see-through ones.</summary>
		/// <remarks>A see-through window has no title bar, no taskbar button and no place in Alt+Tab, so it shows no icon anywhere.</remarks>
		internal static IEnumerable<Type> WindowTypes()
		{
			var assemblies = new[] { typeof(App.MainForm).Assembly, typeof(JocysCom.ClassLibrary.Controls.MessageBoxForm).Assembly };
			return assemblies
				.SelectMany(x => x.GetTypes())
				.Where(x => typeof(Form).IsAssignableFrom(x) && !x.IsAbstract && !x.ContainsGenericParameters)
				.Where(x => !typeof(OverlayForm).IsAssignableFrom(x) && !NotMade.ContainsKey(x.FullName))
				.OrderBy(x => x.FullName);
		}

		/// <summary>Whether two icons draw the same picture at the size Windows shows a window's icon in Alt+Tab.</summary>
		static bool Same(Icon a, Icon b)
		{
			using (var x = Picture(a))
			using (var y = Picture(b))
			{
				if (x.Size != y.Size)
					return false;
				for (var i = 0; i < x.Width; i++)
					for (var j = 0; j < x.Height; j++)
						if (x.GetPixel(i, j).ToArgb() != y.GetPixel(i, j).ToArgb())
							return false;
				return true;
			}
		}

		static Bitmap Picture(Icon icon)
		{
			using (var sized = new Icon(icon, 32, 32))
				return sized.ToBitmap();
		}
	}
}
