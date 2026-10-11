// @under-test: Engine/x360ce.Engine.csproj, Engine/JocysCom/Mcp/McpUiTools.cs, Engine/JocysCom/Mcp/McpServer.cs, Engine/JocysCom/Controls/UiTree/UiTreeWalker.cs, Engine/JocysCom/Controls/UiTree/UiCallout.cs, App.v4/Mcp/McpTools.cs
// @area: mcp   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// The door and the interface tree are shared with programs written in WPF, whose half this program leaves out.
	/// Loading WPF commits about 90 MB for the life of the process and makes the window DPI-aware mid-run, so the
	/// door's whole path is run where nothing else can have loaded WPF first: an application domain of its own.
	/// </summary>
	[TestClass]
	public class McpNoWpfTest
	{
		static readonly string[] Wpf = { "PresentationCore", "PresentationFramework", "WindowsBase", "System.Xaml" };

		const string DomainName = "McpNoWpfTest";

		[TestMethod, TestCategory("mcp"), TestCategory("critical"), Timeout(120000)]
		[Description("Registering the program's tools, reading the tree, and ui_read, ui_find, ui_show and ui_hide through the door load no WPF assembly")]
		public void The_door_and_the_tree_load_no_wpf()
		{
			var setup = new AppDomainSetup
			{
				ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
				ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
			};
			var domain = AppDomain.CreateDomain(DomainName, null, setup);
			DoorRun run;
			try
			{
				var door = (Door)domain.CreateInstanceAndUnwrap(typeof(Door).Assembly.FullName, typeof(Door).FullName);
				run = door.Run();
			}
			finally
			{
				AppDomain.Unload(domain);
			}
			Assert.IsNull(run.Failure, run.Failure);
			Assert.AreEqual(DomainName, run.Domain, "The door ran in the test's own domain, where other tests may have loaded WPF.");
			Console.WriteLine("Loaded in the door's domain: " + string.Join(", ", run.Loaded));
			// The program's own code ran there, so an empty list below means something.
			CollectionAssert.Contains(run.Loaded, "x360ce", "The program's tools were not registered in the new domain.");
			CollectionAssert.Contains(run.Loaded, "x360ce.Engine", "The door did not run in the new domain.");
			var wpf = run.Loaded.Intersect(Wpf, StringComparer.OrdinalIgnoreCase).ToArray();
			Assert.AreEqual(0, wpf.Length, "The door loaded " + string.Join(", ", wpf) + ". Loaded: " + string.Join(", ", run.Loaded));
			StringAssert.Contains(run.Answers["tools/list"], "devices_list", "The program's own tools are not listed.");
			StringAssert.Contains(Text(run.Answers["ui_tree"]), "Overall strength", "The tree does not describe the window.");
			StringAssert.Contains(Text(run.Answers["ui_read"]), "Tabs/Page/Strength", "ui_read does not give the path.");
			StringAssert.Contains(Text(run.Answers["ui_read branch"]), "\"Value\":\"40\"", "ui_read of a branch does not give the value.");
			StringAssert.Contains(Text(run.Answers["ui_find"]), "Tabs/Page/Strength", "ui_find does not find by purpose.");
			Assert.AreEqual("Done.", Text(run.Answers["ui_show"]), "ui_show did not point.");
			Assert.AreEqual("Done.", Text(run.Answers["ui_hide"]), "ui_hide found nothing to take away after ui_show.");
		}

		/// <summary>The text of a tool's answer, failing on an error or a refusal.</summary>
		static string Text(string response)
		{
			var answer = (Dictionary<string, object>)new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(response);
			Assert.IsFalse(answer.ContainsKey("error"), "The door answered with an error: " + response);
			var result = (Dictionary<string, object>)answer["result"];
			Assert.IsFalse(result.ContainsKey("isError"), "The tool refused: " + response);
			return (string)((Dictionary<string, object>)((object[])result["content"])[0])["text"];
		}

		/// <summary>What happened in the other domain: every answer by name, the assemblies loaded, and a failure if any.</summary>
		[Serializable]
		public sealed class DoorRun
		{
			public string Failure;
			public string Domain;
			public string[] Loaded;
			public Dictionary<string, string> Answers = new Dictionary<string, string>();
		}

		/// <summary>
		/// Runs in the other domain as the program does: a window on an interface thread with its message loop, and the
		/// requests from another thread, carried over by the door's own marshalling.
		/// </summary>
		public sealed class Door : MarshalByRefObject
		{
			public DoorRun Run()
			{
				var run = new DoorRun();
				var ui = new Thread(() =>
				{
					try { Drive(run); }
					catch (Exception ex) { run.Failure = ex.ToString(); }
				});
				ui.SetApartmentState(ApartmentState.STA);
				ui.Start();
				if (!ui.Join(TimeSpan.FromSeconds(90)))
					run.Failure = "The window did not close within 90 seconds.";
				run.Domain = AppDomain.CurrentDomain.FriendlyName;
				run.Loaded = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
				return run;
			}

			static void Drive(DoorRun run)
			{
				App.Mcp.McpTools.Register();
				McpCatalog.Level = () => AiAccess.Read;
				McpLog.Folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "x360ce-tests")).FullName;
				SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
				ControlsHelper.InitInvokeContext();
				// Off the screen, so nothing appears in front of the person running the tests.
				using (var form = new Form { Name = "Main", Text = "No WPF", ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-32000, -32000) })
				{
					var tabs = new TabControl { Name = "Tabs", Dock = DockStyle.Fill };
					var page = new TabPage { Name = "Page", Text = "Page" };
					var strength = new TrackBar { Name = "Strength", Maximum = 100, Value = 40, AccessibleName = "Overall strength", AccessibleDescription = "Scales all vibration." };
					var go = new Button { Name = "Go", Text = "Go", Top = 60, AccessibleDescription = "Starts it." };
					page.Controls.AddRange(new Control[] { strength, go });
					tabs.TabPages.Add(page);
					form.Controls.Add(tabs);
					McpUiTools.Root = form;
					McpUiTools.MainWindow = () => form;
					McpUiTools.TrayMenu = () => null;
					form.Shown += (s, e) =>
					{
						var caller = new Thread(() =>
						{
							try
							{
								run.Answers["tools/list"] = McpServer.Handle("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}");
								run.Answers["ui_tree"] = Call(2, "ui_tree", "{}");
								run.Answers["ui_read"] = Call(3, "ui_read", "{}");
								run.Answers["ui_read branch"] = Call(4, "ui_read", "{\"path\":\"Tabs/Page/Strength\"}");
								run.Answers["ui_find"] = Call(5, "ui_find", "{\"query\":\"vibration\"}");
								run.Answers["ui_show"] = Call(6, "ui_show", "{\"path\":\"Tabs/Page/Go\",\"text\":\"Here\",\"seconds\":0}");
								run.Answers["ui_hide"] = Call(7, "ui_hide", "{}");
							}
							catch (Exception ex)
							{
								run.Failure = ex.ToString();
							}
							finally
							{
								form.BeginInvoke((Action)form.Close);
							}
						}) { IsBackground = true };
						caller.Start();
					};
					Application.Run(form);
				}
			}

			static string Call(int id, string tool, string arguments)
			{
				return McpServer.Handle("{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"method\":\"tools/call\",\"params\":{\"name\":\"" + tool + "\",\"arguments\":" + arguments + "}}");
			}
		}
	}
}
