// @under-test: Plugins/WheelTelemetry.Games/WheelTelemetry.Games.cs, Plugins/WheelTelemetry.Games/WheelTelemetry.Games.Data.cs, Plugins/WheelTelemetry.Games/WheelTelemetry.Games.Page.cs, Plugins/WheelTelemetry.Games/WheelTelemetry.Games.Help.cs, Plugins/WheelTelemetry.Games/Resources/x360ce.Telemetry.3.cs, PluginApi/IPluginGame.cs, PluginApi/IPluginTelemetryData.cs, App.v4/Plugins/PluginManager.cs, App.v4/Plugins/PluginTelemetryData.cs, App.v4/Common/SettingsManager.cs, App.v4/Controls/PadTabPages/GamesControl.cs, App.v4/Resources/x360ce.Telemetry*.xml, Data/Change Scripts/20261122-01-Telemetry.sql, docs/Help.Plugins.md
// @area: plugins   @layer: unit
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// The Wheel Telemetry Games plugin, embedded in x360ce and off until the person ticks it: a game plugin, whose tab on
	/// the Games tab shows, after x360ce's own settings of the game, for each game x360ce's telemetry data names, and whose
	/// page is made from that game's rows alone; the data itself, x360ce's TelemetryFormats and TelemetryGames settings,
	/// made from the [x360ce] tables; and GTA V's game script, which ScriptHookVDotNet compiles in the game, and which sends
	/// Forza Motorsport 7 "Data Out" packets in Turn 10's "Dash" layout, only in story mode, to the Wheel Telemetry plugin.
	/// </summary>
	/// <remarks>
	/// The script is compiled here as ScriptHookVDotNet 3.6.0 and Enhanced compile it, with CodeDom's C# compiler, against
	/// <see cref="ScriptHookVDotNetStub"/>, the part of the scripting API both flavours have. The stub's bodies are a small
	/// game the tests set, and the script's packets are read from a UDP socket of the test's, so no game is needed.
	/// </remarks>
	[TestClass]
	public class WheelTelemetryGamesTest
	{
		const string PluginName = "WheelTelemetry.Games";
		const string ScriptFileName = "x360ce.Telemetry.3.cs";
		const string SettingsFileName = "x360ce.Telemetry.ini";
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		#region x360ce's telemetry data

		/// <summary>The six settings lists, filled from the data x360ce carries, as a first start fills them.</summary>
		/// <remarks>The program's own lists, so the plugin reads them through the host as it does in the program; the person's files are not read.</remarks>
		static void LoadDefaults()
		{
			Fill(SettingsManager.TelemetryFormats, "TelemetryFormats.xml");
			Fill(SettingsManager.TelemetryFormatSpecs, "TelemetryFormatSpecs.xml");
			Fill(SettingsManager.TelemetryGames, "TelemetryGames.xml");
			Fill(SettingsManager.TelemetryGameSteps, "TelemetryGameSteps.xml");
			Fill(SettingsManager.TelemetryGameMods, "TelemetryGameMods.xml");
			Fill(SettingsManager.TelemetryGameFiles, "TelemetryGameFiles.xml");
		}

		static void Fill<T>(XSettingsData<T> data, string file)
		{
			var items = Defaults(data, file);
			data.Items.Clear();
			foreach (var item in items)
				data.Items.Add(item);
		}

		/// <summary>The rows x360ce carries for a settings file, read as the settings read them.</summary>
		static IList<T> Defaults<T>(XSettingsData<T> data, string file)
		{
			var assembly = typeof(Global).Assembly;
			var name = assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(".x360ce." + file, StringComparison.Ordinal));
			Assert.IsNotNull(name, "x360ce does not carry x360ce." + file + ".");
			byte[] bytes;
			using (var stream = assembly.GetManifestResourceStream(name))
			using (var memory = new MemoryStream())
			{
				stream.CopyTo(memory);
				bytes = memory.ToArray();
			}
			var items = data.DeserializeData(bytes, false).Items;
			Assert.IsTrue(items.Count > 0, file + " holds no rows.");
			return items;
		}

		/// <summary>The receiver's ids for the formats it reads: its UDP protocols and its decoders, by their names.</summary>
		static string[] ReceiverIds()
		{
			var telemetryType = TestControllerDevice.Manager.TelemetryTypes.SingleOrDefault(x => x.Name == "Wheel Telemetry");
			Assert.IsNotNull(telemetryType, "The Wheel Telemetry plugin is not embedded in x360ce.");
			var type = telemetryType.Type;
			var protocols = type.GetNestedType("UdpProtocol", Any);
			Assert.IsNotNull(protocols, "The Wheel Telemetry plugin has no UdpProtocol any more: its ids are read from it.");
			var decoders = type.GetMethods(Any).Select(x => x.Name)
				.Where(x => x.StartsWith("TryDecode", StringComparison.Ordinal) && x != "TryDecodeLocked")
				.Select(x => x.Substring("TryDecode".Length));
			return Enum.GetNames(protocols).Where(x => x != "None").Concat(decoders).Distinct().OrderBy(x => x).ToArray();
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Every telemetry settings file x360ce carries reads and writes back through XSettingsData to the same rows")]
		public void The_telemetry_data_round_trips_through_the_settings_files()
		{
			RoundTrip(SettingsManager.TelemetryFormats, "TelemetryFormats.xml", x => x.TelemetryFormatId + "|" + x.Name + "|" + x.MapName + "|" + x.DefaultPort);
			RoundTrip(SettingsManager.TelemetryFormatSpecs, "TelemetryFormatSpecs.xml", x => x.TelemetryFormatSpecId + "|" + x.Url + "|" + x.SpecDate);
			RoundTrip(SettingsManager.TelemetryGames, "TelemetryGames.xml", x => x.TelemetryGameId + "|" + x.FileName + "|" + x.FileProductName + "|" + x.TelemetryFormatId);
			RoundTrip(SettingsManager.TelemetryGameSteps, "TelemetryGameSteps.xml", x => x.TelemetryGameStepId + "|" + x.TelemetryGameId + "|" + x.IsWarning + "|" + x.StepOrder + "|" + x.Text);
			RoundTrip(SettingsManager.TelemetryGameMods, "TelemetryGameMods.xml", x => x.TelemetryGameModId + "|" + x.DetectFile + "|" + x.DetectProduct + "|" + x.Url);
			RoundTrip(SettingsManager.TelemetryGameFiles, "TelemetryGameFiles.xml", x => x.TelemetryGameFileId + "|" + x.ResourceName + "|" + x.TargetPath);
		}

		static void RoundTrip<T>(XSettingsData<T> data, string file, Func<T, string> key)
		{
			var rows = Defaults(data, file);
			var written = new XSettingsData<T> { Items = new SortableBindingList<T>() };
			foreach (var row in rows)
				written.Items.Add(row);
			var read = written.DeserializeData(Serializer.SerializeToXmlBytes(written, Encoding.UTF8, true), false).Items;
			CollectionAssert.AreEqual(rows.Select(key).ToArray(), read.Select(key).ToArray(), file + " does not read back as written.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Every format the Wheel Telemetry plugin reads has a TelemetryFormats row and no row names a format it does not read; every game names one of them")]
		public void Every_format_the_receiver_reads_has_a_row_and_every_game_names_one()
		{
			var ids = ReceiverIds();
			var formats = Defaults(SettingsManager.TelemetryFormats, "TelemetryFormats.xml").Select(x => x.TelemetryFormatId).OrderBy(x => x).ToArray();
			CollectionAssert.AreEqual(ids, formats, "The formats x360ce lists are not the ones its Wheel Telemetry plugin reads.");
			foreach (var game in Defaults(SettingsManager.TelemetryGames, "TelemetryGames.xml"))
				CollectionAssert.Contains(formats, game.TelemetryFormatId, game.FileName + " names a format x360ce does not list.");
			foreach (var spec in Defaults(SettingsManager.TelemetryFormatSpecs, "TelemetryFormatSpecs.xml"))
			{
				CollectionAssert.Contains(formats, spec.TelemetryFormatId, "A specification of a format x360ce does not list.");
				if (spec.Url.Length == 0)
					Assert.IsTrue(spec.Note.Length > 0, spec.TelemetryFormatId + ": a specification with no page says why.");
				else
					Assert.IsTrue(spec.Url.StartsWith("https://") || spec.Url.StartsWith("http://"), spec.Url);
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Every game row has a program file and a unique key, every child row points to a game, every mod links to its page, and every file the plugin installs is one it carries and stays in the game's folder")]
		public void Every_game_row_is_valid()
		{
			var games = Defaults(SettingsManager.TelemetryGames, "TelemetryGames.xml");
			var ids = games.Select(x => x.TelemetryGameId).ToArray();
			Assert.AreEqual(ids.Length, ids.Distinct().Count(), "Two games share a key.");
			Assert.AreEqual(games.Count, games.Select(x => x.FileName.ToLowerInvariant() + "|" + x.FileProductName).Distinct().Count(),
				"Two games share a program file and product name, the unique index of the table.");
			foreach (var game in games)
			{
				StringAssert.EndsWith(game.FileName.ToLowerInvariant(), ".exe", game.FileName);
				Assert.IsTrue(game.GameName.Length > 0, game.FileName + " has no name.");
			}
			var steps = Defaults(SettingsManager.TelemetryGameSteps, "TelemetryGameSteps.xml");
			foreach (var step in steps)
				CollectionAssert.Contains(ids, step.TelemetryGameId, "A step of no game: " + step.Text);
			Assert.AreEqual(steps.Count, steps.Select(x => x.TelemetryGameId + "|" + x.IsWarning + "|" + x.StepOrder).Distinct().Count(), "Two steps share a place.");
			foreach (var game in games)
				Assert.IsTrue(steps.Any(x => x.TelemetryGameId == game.TelemetryGameId && !x.IsWarning), game.FileName + " has no steps.");
			foreach (var mod in Defaults(SettingsManager.TelemetryGameMods, "TelemetryGameMods.xml"))
			{
				CollectionAssert.Contains(ids, mod.TelemetryGameId, "A mod of no game: " + mod.Name);
				Assert.IsTrue(mod.Url.StartsWith("https://") || mod.Url.StartsWith("http://"), mod.Name + ": " + mod.Url);
				Assert.IsFalse(Path.IsPathRooted(mod.DetectFile), mod.DetectFile);
			}
			var resources = Type().Type.Assembly.GetManifestResourceNames();
			foreach (var file in Defaults(SettingsManager.TelemetryGameFiles, "TelemetryGameFiles.xml"))
			{
				CollectionAssert.Contains(ids, file.TelemetryGameId, "A file of no game: " + file.TargetPath);
				Assert.IsTrue(resources.Any(x => x.EndsWith(".Resources." + file.ResourceName, StringComparison.Ordinal)), "The plugin does not carry " + file.ResourceName + ".");
				Assert.IsFalse(Path.IsPathRooted(file.TargetPath) || file.TargetPath.Contains(".."), file.TargetPath + " leaves the game's folder.");
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The change script that makes the [x360ce] tables runs twice against the local x360ce database without an error")]
		public void The_change_script_runs_twice()
		{
			var script = Path.Combine(Ui.RepoRoot.FullName, "Data", "Change Scripts", "20261122-01-Telemetry.sql");
			Assert.IsTrue(File.Exists(script), script);
			for (var i = 1; i <= 2; i++)
			{
				var start = new ProcessStartInfo("sqlcmd", "-S localhost -E -C -f 65001 -d x360ce -b -l 5 -i \"" + script + "\"")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				};
				Process process;
				try
				{
					process = Process.Start(start);
				}
				catch (System.ComponentModel.Win32Exception)
				{
					Assert.Inconclusive("sqlcmd is not installed here.");
					return;
				}
				using (process)
				{
					var error = process.StandardError.ReadToEndAsync();
					var text = process.StandardOutput.ReadToEnd() + error.Result;
					process.WaitForExit();
					if (process.ExitCode != 0 && text.Contains("Login timeout expired"))
						Assert.Inconclusive("No SQL Server with an x360ce database on this computer: " + text);
					Assert.AreEqual(0, process.ExitCode, "Run " + i + ": " + text);
				}
			}
		}

		#endregion

		#region The plugin and its script

		static PluginGameType Type()
		{
			LoadDefaults();
			var manager = TestControllerDevice.Manager;
			// The plugin is off by default, and the manager a test before this one left may keep it so.
			if (!manager.GameTypes.Any(x => x.File.Name == PluginName))
				manager.SetEnabled(PluginName, true);
			var type = manager.GameTypes.SingleOrDefault(x => x.File.Name == PluginName);
			Assert.IsNotNull(type, "The Wheel Telemetry Games plugin is not embedded in x360ce.");
			Assert.IsFalse(type.Faulted, type.FaultReason);
			return type;
		}

		/// <summary>A type of the plugin that x360ce does not see, by its name.</summary>
		static System.Type Internal(string name)
		{
			return Type().Type.Assembly.GetType("x360ce.Plugins.Samples." + name, true);
		}

		/// <summary>The game script the plugin installs into GTA V, as it carries it.</summary>
		static string ScriptSource()
		{
			var assembly = Type().Type.Assembly;
			var name = assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(".Resources." + ScriptFileName, StringComparison.Ordinal));
			Assert.IsNotNull(name, "The plugin does not carry " + ScriptFileName + ".");
			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}

		static readonly object CompileLock = new object();
		static Assembly _stub;
		static Assembly _script;

		/// <summary>The stub API, compiled once.</summary>
		static Assembly Stub()
		{
			Script();
			return _stub;
		}

		/// <summary>The game script compiled against the stub API, as ScriptHookVDotNet compiles it in the game, once.</summary>
		static Assembly Script()
		{
			lock (CompileLock)
			{
				if (_script != null)
					return _script;
				var folder = Path.Combine(Path.GetTempPath(), "x360ce.WheelTelemetryGamesTest." + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(folder);
				var stubPath = Path.Combine(folder, ScriptHookVDotNetStub.AssemblyName + ".dll");
				var stub = Compile(ScriptHookVDotNetStub.Source, stubPath, false);
				Assert.IsFalse(stub.Errors.HasErrors, "The stub API does not compile: " + Messages(stub));
				_stub = Assembly.Load(File.ReadAllBytes(stubPath));
				AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
					new AssemblyName(e.Name).Name == ScriptHookVDotNetStub.AssemblyName ? _stub : null;
				var script = Compile(ScriptSource(), Path.Combine(folder, "x360ce.Telemetry.dll"), true, stubPath);
				Assert.IsFalse(script.Errors.HasErrors, ScriptFileName + " does not compile against the API both ScriptHookVDotNet flavours share: " + Messages(script));
				Assert.AreEqual(0, script.Errors.Count, ScriptFileName + " compiles with warnings: " + Messages(script));
				_script = script.CompiledAssembly;
				return _script;
			}
		}

		/// <summary>Compiles C# with CodeDom, with the options and references ScriptHookVDotNet 3.6.0 and Enhanced give a script.</summary>
		static CompilerResults Compile(string source, string output, bool inMemory, params string[] references)
		{
			using (var compiler = new Microsoft.CSharp.CSharpCodeProvider())
			{
				var options = new CompilerParameters
				{
					CompilerOptions = "/optimize /unsafe",
					GenerateInMemory = inMemory,
					OutputAssembly = output,
				};
				options.ReferencedAssemblies.AddRange(new[] { "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.XML.dll", "System.XML.Linq.dll" });
				options.ReferencedAssemblies.AddRange(references);
				return compiler.CompileAssemblyFromSource(options, source);
			}
		}

		static string Messages(CompilerResults results)
		{
			return string.Join(Environment.NewLine, results.Errors.Cast<CompilerError>().Select(x => "line " + x.Line + ": " + x.ErrorNumber + " " + x.ErrorText));
		}

		static System.Type StubType(string name) { return Stub().GetType(name, true); }
		static System.Type ScriptType(string name) { return Script().GetType("X360ce.GtaV." + name, true); }
		static System.Type Harness { get { return StubType("ShvdnStub.Harness"); } }

		static object Vector(float x, float y, float z)
		{
			return Activator.CreateInstance(StubType("GTA.Math.Vector3"), x, y, z);
		}

		/// <summary>The body of the player's ped or of the vehicle it is in, which the stub's entities read.</summary>
		static object Body(string entity)
		{
			var instance = Harness.GetField(entity).GetValue(null);
			return instance.GetType().GetField("Body", Any).GetValue(instance);
		}

		static void Set(object target, string field, object value)
		{
			target.GetType().GetField(field).SetValue(target, value);
		}

		static void SetHarness(string field, object value)
		{
			Harness.GetField(field).SetValue(null, value);
		}

		static void SetFlag(string native, bool value)
		{
			var flags = (IDictionary)Harness.GetField("Flags").GetValue(null);
			flags[Enum.Parse(StubType("GTA.Native.Hash"), native)] = value;
		}

		static void SetControl(int control, float value)
		{
			var controls = (IDictionary)Harness.GetField("Controls").GetValue(null);
			controls[control] = value;
		}

		/// <summary>The script running in the stub's game, sending to a socket of its own.</summary>
		sealed class ScriptRun : IDisposable
		{
			public ScriptRun()
			{
				Folder = Path.Combine(Path.GetTempPath(), "x360ce.WheelTelemetryGamesRun." + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(Folder);
				Client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
				var port = ((IPEndPoint)Client.Client.LocalEndPoint).Port;
				File.WriteAllText(Path.Combine(Folder, SettingsFileName), "Port=" + port + "\r\nRate=60\r\n");
				Harness.GetMethod("Reset").Invoke(null, null);
				SetHarness("ScriptFile", Path.Combine(Folder, ScriptFileName));
			}

			public readonly string Folder;
			public readonly UdpClient Client;
			public object Instance;

			public void Tick()
			{
				if (Instance == null)
					Instance = Activator.CreateInstance(ScriptType("TelemetryScript"));
				Harness.GetMethod("Tick").Invoke(null, new[] { Instance });
			}

			/// <summary>The next packet the script sent, or null when none comes within the time given.</summary>
			public byte[] Packet(int milliseconds = 2000)
			{
				Client.Client.ReceiveTimeout = milliseconds;
				var from = new IPEndPoint(IPAddress.Any, 0);
				try
				{
					return Client.Receive(ref from);
				}
				catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
				{
					return null;
				}
			}

			public void Dispose()
			{
				if (Instance != null)
					Harness.GetMethod("Abort").Invoke(null, new[] { Instance });
				Client.Close();
				Directory.Delete(Folder, true);
			}
		}

		/// <summary>The byte a field of Turn 10's Dash layout starts at.</summary>
		static readonly Dictionary<string, int> Dash = new Dictionary<string, int>
		{
			{ "IsRaceOn", 0 }, { "EngineMaxRpm", 8 }, { "EngineIdleRpm", 12 }, { "CurrentEngineRpm", 16 }, { "AccelerationX", 20 },
			{ "VelocityZ", 40 }, { "AngularVelocityY", 48 }, { "Yaw", 56 }, { "Pitch", 60 }, { "Roll", 64 }, { "CarOrdinal", 212 },
			{ "PositionX", 232 }, { "PositionY", 236 }, { "PositionZ", 240 }, { "Speed", 244 }, { "Accel", 303 }, { "Brake", 304 },
			{ "Gear", 307 }, { "Steer", 308 },
		};

		static float Float(byte[] packet, string name) { return BitConverter.ToSingle(packet, Dash[name]); }
		static int Int32(byte[] packet, string name) { return BitConverter.ToInt32(packet, Dash[name]); }

		/// <summary>The packets given, in turn, to the Wheel Telemetry plugin's decoder, and what it reads from them; null when it takes none.</summary>
		static WheelTelemetry? Decode(params byte[][] packets)
		{
			var telemetryType = TestControllerDevice.Manager.TelemetryTypes.Single(x => x.Name == "Wheel Telemetry");
			// Without its worker, as WheelTelemetryTest makes it: packets are given to it, and it opens no socket.
			var provider = (IPluginWheelTelemetry)Activator.CreateInstance(telemetryType.Type, Any, null, new object[] { false }, null);
			try
			{
				var take = provider.GetType().GetMethod("TakeUdp");
				var taken = false;
				for (var i = 0; i < packets.Length; i++)
					taken = (bool)take.Invoke(provider, new object[] { 37150, packets[i], packets[i].Length, 5000L + i * 16 });
				var sample = WheelTelemetry.Neutral;
				if (!taken || !provider.TryReadTelemetry(ref sample))
					return null;
				return sample;
			}
			finally
			{
				((IDisposable)provider).Dispose();
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("GTA V's game script compiles with ScriptHookVDotNet's C# compiler against the part of the version 3 API that ScriptHookVDotNet 3 and ScriptHookVDotNet Enhanced share, with no warnings, and its packets are Turn 10's 311-byte Dash")]
		public void The_game_script_compiles_against_the_api_both_scripthookvdotnet_flavours_share()
		{
			var script = Script();
			var scriptType = ScriptType("TelemetryScript");
			Assert.IsTrue(StubType("GTA.Script").IsAssignableFrom(scriptType), "The script's class is not a ScriptHookVDotNet script.");
			Assert.IsNotNull(scriptType.GetConstructor(System.Type.EmptyTypes), "ScriptHookVDotNet makes a script with a public constructor without parameters.");
			Assert.AreEqual(1, script.GetExportedTypes().Count(x => StubType("GTA.Script").IsAssignableFrom(x)), "ScriptHookVDotNet runs every script class it finds: one is wanted.");
			Assert.AreEqual(311, (int)ScriptType("ForzaDash").GetField("Length").GetValue(null), "Turn 10's Dash layout is 311 bytes.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("On foot the script sends the race on, the player's speed and position, as pitch the ground's slope along the way the player moves, and the red line, so x360ce's decoder takes the packet; the rest stays neutral")]
		public void On_foot_the_pitch_is_the_ground_slope_and_x360ces_decoder_takes_the_packet()
		{
			using (var run = new ScriptRun())
			{
				// Walking north at 5 m/s up a 10 % slope; GTA's world has X east, Y north and Z up.
				var ped = Body("Ped");
				Set(ped, "Position", Vector(100f, 200f, 30f));
				Set(ped, "Velocity", Vector(0f, 5f, 0f));
				Set(ped, "Speed", 5f);
				SetHarness("Ground", new Func<float, float, float>((x, y) => 30f + (y - 200f) * 0.1f));
				var packets = new List<byte[]>();
				// A frame apart, as the game runs the script: the decoder takes Forza packets once their timestamps advance.
				for (var i = 0; i < 4; i++)
				{
					if (i > 0)
						System.Threading.Thread.Sleep(40);
					run.Tick();
					var next = run.Packet();
					Assert.IsNotNull(next, "The script sent nothing.");
					packets.Add(next);
				}
				var packet = packets[0];
				Assert.AreEqual(311, packet.Length);
				Assert.AreEqual(1, Int32(packet, "IsRaceOn"));
				Assert.AreEqual(5f, Float(packet, "Speed"), 0.0001f);
				Assert.AreEqual(5f, Float(packet, "VelocityZ"), 0.0001f, "Velocity is in the player's own axes: Z forward.");
				Assert.AreEqual((float)Math.Atan(0.1), Float(packet, "Pitch"), 0.0001f, "Pitch is the slope of the ground ahead of and behind the player.");
				Assert.AreEqual(100f, Float(packet, "PositionX"), 0.0001f);
				Assert.AreEqual(30f, Float(packet, "PositionY"), 0.0001f, "Position has Y up, as Forza's axes do.");
				Assert.AreEqual(200f, Float(packet, "PositionZ"), 0.0001f);
				Assert.AreEqual(7000f, Float(packet, "EngineMaxRpm"), 0.001f, "On foot the packet carries the red line a vehicle's does.");
				Assert.AreEqual(1400f, Float(packet, "EngineIdleRpm"), 0.001f);
				foreach (var neutral in new[] { "CurrentEngineRpm", "AccelerationX", "AngularVelocityY", "Yaw", "Roll" })
					Assert.AreEqual(0f, Float(packet, neutral), neutral + " is neutral on foot.");
				Assert.AreEqual(0, packet[Dash["Accel"]]);
				Assert.AreEqual(0, packet[Dash["Gear"]]);

				var sample = Decode(packets.ToArray());
				Assert.IsTrue(sample.HasValue, "x360ce's Forza decoder drops the packets sent on foot.");
				Assert.AreEqual(18f, sample.Value.SpeedKmh, 0.001f, "5 m/s is 18 km/h.");
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("In a vehicle the pitch is the road's slope rather than the body's, which dives under braking; engine, gear and pedals come from the vehicle, and x360ce's decoder reads its speed")]
		public void In_a_vehicle_the_pitch_is_the_road_slope_rather_than_the_body_pitch()
		{
			using (var run = new ScriptRun())
			{
				SetHarness("InVehicle", true);
				SetHarness("Rpm", 0.5f);
				SetHarness("Gear", 3);
				SetControl(71, 1f);
				SetControl(72, 0.25f);
				SetControl(59, -0.5f);
				var car = Body("Vehicle");
				Set(car, "Position", Vector(10f, 20f, 5f));
				Set(car, "Velocity", Vector(0f, 20f, 0f));
				Set(car, "Speed", 20f);
				// Nose down 5 degrees under braking, on a flat road.
				Set(car, "Rotation", Vector(-5f, 0f, 0f));
				SetHarness("Ground", new Func<float, float, float>((x, y) => 4f));
				var packets = new List<byte[]>();
				// A frame apart, as the game runs the script: the decoder takes Forza packets once their timestamps advance.
				for (var i = 0; i < 4; i++)
				{
					if (i > 0)
						System.Threading.Thread.Sleep(40);
					run.Tick();
					var next = run.Packet();
					Assert.IsNotNull(next, "The script sent nothing.");
					packets.Add(next);
				}
				var packet = packets[0];
				Assert.AreEqual(1, Int32(packet, "IsRaceOn"));
				Assert.AreEqual(0f, Float(packet, "Pitch"), 0.0001f, "The road is flat, whatever the body does.");
				Assert.AreEqual(20f, Float(packet, "Speed"), 0.0001f);
				Assert.AreEqual(7000f, Float(packet, "EngineMaxRpm"), 0.001f);
				Assert.AreEqual(1400f, Float(packet, "EngineIdleRpm"), 0.001f, "GTA idles at a fifth of the red line.");
				Assert.AreEqual(3500f, Float(packet, "CurrentEngineRpm"), 0.001f);
				Assert.AreEqual(3, packet[Dash["Gear"]]);
				Assert.AreEqual(255, packet[Dash["Accel"]]);
				Assert.AreEqual(64, packet[Dash["Brake"]]);
				Assert.AreEqual(-64, (sbyte)packet[Dash["Steer"]]);
				Assert.AreEqual(0, Int32(packet, "CarOrdinal"));
				var sample = Decode(packets.ToArray());
				Assert.IsTrue(sample.HasValue, "x360ce's Forza decoder does not take the packets sent in a vehicle.");
				Assert.AreEqual("Forza Data Out", sample.Value.Source);
				Assert.AreEqual(72f, sample.Value.SpeedKmh, 0.001f, "20 m/s is 72 km/h.");
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Paused, loading, in a cutscene, dead or online, the script sends only race off packets, nothing else of the game, one a second")]
		public void Paused_or_online_sends_only_race_off_once_a_second()
		{
			foreach (var native in new[] { "IS_PAUSE_MENU_ACTIVE", "GET_IS_LOADING_SCREEN_ACTIVE", "IS_CUTSCENE_ACTIVE", "IS_PLAYER_SWITCH_IN_PROGRESS", "NETWORK_IS_SESSION_STARTED", "NETWORK_IS_IN_SESSION", "dead" })
			{
				using (var run = new ScriptRun())
				{
					SetHarness("InVehicle", true);
					SetHarness("Rpm", 0.5f);
					var car = Body("Vehicle");
					Set(car, "Velocity", Vector(0f, 20f, 0f));
					Set(car, "Speed", 20f);
					if (native == "dead")
						SetHarness("PlayerDead", true);
					else
						SetFlag(native, true);
					run.Tick();
					var packet = run.Packet();
					Assert.IsNotNull(packet, native + ": the script sent nothing.");
					Assert.AreEqual(0, Int32(packet, "IsRaceOn"), native + ": the race is not off.");
					Assert.IsTrue(packet.Skip(Dash["EngineMaxRpm"]).All(x => x == 0), native + ": a race off packet carries something of the game.");
					run.Tick();
					Assert.IsNull(run.Packet(300), native + ": a second race off packet within a second.");
				}
			}
		}

		#endregion

		#region The installer

		/// <summary>A folder of the test's own, holding a pretend file of each name given.</summary>
		static string NewGameFolder(params string[] files)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.WheelTelemetryGame." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			foreach (var file in files)
				File.WriteAllBytes(Path.Combine(folder, file), new byte[0]);
			return folder;
		}

		static string[] Files(string folder)
		{
			return Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
		}

		/// <summary>A game's row, as the plugin finds it in the data x360ce gives it.</summary>
		static ITelemetryGame Find(string gamePath)
		{
			return (ITelemetryGame)Internal("GameData").GetMethod("Find", Any).Invoke(null, new object[] { PluginTelemetryData.Current, gamePath });
		}

		static string Installer(string method, string gamePath, params object[] more)
		{
			var arguments = new object[] { PluginTelemetryData.Current, Find(gamePath), gamePath }.Concat(more).ToArray();
			return (string)Internal("GameInstaller").GetMethod(method, Any).Invoke(null, arguments);
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Install writes only the files the game's rows name, with the port put into its settings, and Remove deletes only those; a game without such rows has none written")]
		public void Install_and_remove_touch_only_the_games_files()
		{
			Type();
			var game = NewGameFolder("GTA5_Enhanced.exe", "ForzaHorizon6.exe", "ScriptHookV.dll");
			try
			{
				Directory.CreateDirectory(Path.Combine(game, "scripts"));
				File.WriteAllText(Path.Combine(game, "scripts", "Other.cs"), "// another mod");
				var exe = Path.Combine(game, "GTA5_Enhanced.exe");
				var before = Files(game);
				Assert.IsNull(Installer("Install", exe, 5300), "The files were not installed.");
				var script = Path.Combine(game, "scripts", ScriptFileName);
				var settings = Path.Combine(game, "scripts", SettingsFileName);
				CollectionAssert.AreEquivalent(before.Concat(new[] { script, settings }).ToArray(), Files(game), "Only the script and its settings are written.");
				Assert.AreEqual(ScriptSource(), File.ReadAllText(script), "The script installed is the one the plugin carries.");
				CollectionAssert.AreEqual(new[] { "Port=5300", "Rate=60" }, File.ReadAllLines(settings).Where(x => !x.StartsWith(";")).ToArray(),
					"The settings are the port and the rate, the keys the script reads.");
				Assert.AreEqual(5300, (int)Internal("GameInstaller").GetMethod("ReadPort", Any).Invoke(null, new object[] { PluginTelemetryData.Current, Find(exe), game }),
					"The page reads the port back from the installed settings.");
				Assert.IsNull(Installer("Remove", exe), "The files were not removed.");
				CollectionAssert.AreEquivalent(before, Files(game), "Remove takes away the game's files, and nothing else.");

				// Forza Horizon 6 is set in its own menus: nothing to write.
				Assert.IsNull(Installer("Install", Path.Combine(game, "ForzaHorizon6.exe"), 37150));
				CollectionAssert.AreEquivalent(before, Files(game), "Something was written for a game whose rows name no file.");
				// A file the Games tab says is there, but is not, is refused.
				Assert.IsNotNull(Installer("Install", Path.Combine(game, "GTA5.exe"), 37150), "A game file that is not there was taken.");
				CollectionAssert.AreEquivalent(before, Files(game));
			}
			finally
			{
				Directory.Delete(game, true);
			}
		}

		#endregion

		#region The plugin in x360ce

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A fresh options file loads the Wheel Telemetry Games plugin disabled, the other built-in plugins enabled; ticking it loads it, its games come from x360ce's data, and the choice is kept")]
		public void A_fresh_options_file_loads_the_plugin_disabled()
		{
			LoadDefaults();
			var options = new Options();
			var app = typeof(PluginManager).Assembly;
			using (var manager = new PluginManager(null, null, () => false, app, options.IsPluginEnabled))
			{
				manager.LoadEmbedded();
				var file = manager.Files.Single(x => x.Name == PluginName);
				Assert.IsFalse(file.Enabled, "The plugin starts ticked.");
				Assert.AreEqual(PluginFileState.Disabled, file.State, file.Reason);
				Assert.IsFalse(manager.GameTypes.Any(x => x.File == file), "A plugin off by default offers its Games tab page.");
				Assert.IsTrue(manager.Files.Where(x => x.Name != PluginName).All(x => x.Enabled && x.State == PluginFileState.Loaded),
					"The other built-in plugins load by default.");
				Assert.IsTrue(PluginManager.IsOffByDefault(PluginName));

				options.SetPluginEnabled(PluginName, true);
				manager.SetEnabled(PluginName, true);
				Assert.AreEqual(PluginFileState.Loaded, file.State, file.Reason);
				Assert.IsNull(file.Reason, "A game plugin is something the plugin offers.");
				var type = manager.GameTypes.Single(x => x.File == file);
				Assert.AreEqual("Wheel Telemetry Games", type.Name);
				CollectionAssert.AreEquivalent(SettingsManager.TelemetryGames.Items.Select(x => x.FileName).ToArray(), type.FileNames,
					"The games are x360ce's, from its TelemetryGames data.");
				Assert.IsNotNull(type.ControlType, "The plugin has no page.");
			}
			var serializer = new XmlSerializer(typeof(Options));
			var text = new StringWriter();
			serializer.Serialize(text, options);
			var saved = (Options)serializer.Deserialize(new StringReader(text.ToString()));
			using (var restarted = new PluginManager(null, null, () => false, app, saved.IsPluginEnabled))
			{
				restarted.LoadEmbedded();
				Assert.AreEqual(PluginFileState.Loaded, restarted.Files.Single(x => x.Name == PluginName).State, "The choice to tick it was not kept.");
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("games"), TestCategory("ui")]
		[Description("The Games tab shows x360ce's own settings on its first page for every game, and the Wheel Telemetry Games tab, with the game's page and Help, only for a game x360ce's telemetry data names; the settings page still binds to the game selected")]
		public void The_games_tab_shows_the_plugin_tab_only_for_its_games()
		{
			Type();
			var names = new[] { "GTA5.exe", "GTA5_Enhanced.exe", "ForzaHorizon6.exe", "F1_25.exe", "other.exe" };
			var folder = NewGameFolder(names);
			try
			{
				Ui.OnUiThread(() =>
				{
					using (var form = new Form())
					using (var panel = new GamesGridUserControl { Dock = DockStyle.Fill })
					{
						form.Controls.Add(panel);
						form.Show();
						var grid = panel.GamesDataGridView;
						var games = new SortableBindingList<UserGame>();
						foreach (var name in names)
							games.Add(new UserGame { FileName = name, FullPath = Path.Combine(folder, name), IsEnabled = true });
						// The list's own columns only: nothing under it may make the list's window, and bind its games, before
						// the list is told to make no columns of its own.
						Assert.AreEqual(8, grid.Columns.Count, "The games list made columns of its own.");
						grid.DataSource = games;
						Application.DoEvents();
						var tabs = panel.GameTabControl;
						Func<string[]> shown = () => tabs.TabPages.Cast<TabPage>().Select(x => x.Text).ToArray();
						Action<string> select = name =>
						{
							var row = grid.Rows.Cast<DataGridViewRow>().Single(x => ((UserGame)x.DataBoundItem).FileName == name);
							grid.ClearSelection();
							row.Selected = true;
							Application.DoEvents();
						};
						foreach (var name in names.Take(4))
						{
							select(name);
							CollectionAssert.AreEqual(new[] { "x360ce", "Wheel Telemetry Games" }, shown(), name + ": the tabs.");
							Assert.AreSame(panel.GameSettingsTabPage, tabs.TabPages[0], "x360ce's own settings come first.");
							var inner = tabs.TabPages[1].Controls.OfType<TabControl>().Single();
							var gameName = SettingsManager.TelemetryGames.Items.Single(x => x.FileName == name).GameName;
							CollectionAssert.AreEqual(new[] { gameName, "Help" }, inner.TabPages.Cast<TabPage>().Select(x => x.Text).ToArray(),
								name + ": the plugin's tab holds the game's page, then its help.");
							Assert.AreEqual(Type().ControlType, inner.TabPages[0].Controls.Cast<Control>().Single().GetType(), name + ": the plugin's page.");
						}
						select("other.exe");
						CollectionAssert.AreEqual(new[] { "x360ce" }, shown(), "A game x360ce's data does not name has no plugin tab.");

						// The settings page shows and changes the game selected, as before.
						var other = games.Single(x => x.FileName == "other.exe");
						Assert.AreSame(other, panel.GameDetailsControl.CurrentItem);
						Assert.AreSame(panel.GameSettingsTabPage, panel.GameDetailsControl.Parent, "The game's settings are on the x360ce page.");
						var box = (CheckBox)panel.GameDetailsControl.Controls.Find(nameof(GameDetailsUserControl.HookCOMCheckBox), true).Single();
						var mask = other.HookMask;
						box.Checked = !box.Checked;
						Assert.AreEqual((int)HookMask.HookCOM, (other.HookMask ^ mask) & (int)HookMask.HookCOM, "Ticking a hook on the x360ce page does not reach the game.");
					}
				});
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("One page made from a game's rows: GTA V puts its warnings first and shows its mods, with the loader and Script Hook V .NET each edition needs, and Install; Forza Horizon 6 shows its steps with the port and no Install; every control is named and none hidden")]
		public void The_page_is_made_from_the_games_rows()
		{
			Type();
			var folder = NewGameFolder("GTA5.exe", "GTA5_Enhanced.exe", "ForzaHorizon6.exe", "ScriptHookV.dll", "ScriptHookVDotNet.asi", "ScriptHookVDotNet3.dll");
			try
			{
				Ui.OnUiThread(() =>
				{
					foreach (var exe in new[] { "GTA5.exe", "GTA5_Enhanced.exe", "ForzaHorizon6.exe" })
					{
						var game = Type().NewInstance();
						game.GamePath = Path.Combine(folder, exe);
						using (var form = new Form())
						using (var page = (Control)Activator.CreateInstance(Type().ControlType, game))
						{
							page.Dock = DockStyle.Fill;
							form.Controls.Add(page);
							form.Show();
							Application.DoEvents();
							var controls = Descendants(page).ToArray();
							foreach (var control in controls)
							{
								Assert.IsFalse(string.IsNullOrEmpty(control.Name), exe + ": " + control.GetType().Name + " \"" + control.Text + "\" has no name.");
								Assert.IsTrue(control.Visible, exe + ": " + control.Name + " is hidden.");
							}
							Func<string, Control> named = name => controls.SingleOrDefault(x => x.Name == name);
							Func<string, string> link = name => (string)((LinkLabel)named(name)).Links[0].LinkData;
							var table = controls.OfType<TableLayoutPanel>().First();
							var portBox = (NumericUpDown)named("PortUpDown");
							Assert.AreEqual(37150, (int)portBox.Value, exe + ": the game sends to the Wheel Telemetry plugin's port.");
							Assert.IsNotNull(named("OpenFolderButton"), exe + ": no Open game folder.");
							Assert.AreEqual("Forza Data Out, from Turn 10 Studios and Playground Games.", named("FormatValueLabel").Text, exe + ": the format, from x360ce's list.");
							StringAssert.StartsWith(link("FormatLinkLabel"), "https://support.forza.net/", exe + ": the format's specification.");
							if (exe == "ForzaHorizon6.exe")
							{
								Assert.AreEqual("Forza Horizon 6", page.Text);
								Assert.IsNull(named("Warning1Label"), "Forza Horizon 6 has no warnings.");
								Assert.IsNull(named("InstallButton"), "Install shows only for a game whose rows name files.");
								Assert.IsNull(named("Mod1ValueLabel"), "Forza Horizon 6 needs no mods.");
								Assert.AreEqual("Set Data Out IP Port to 37150.", named("Step4Label").Text, "A step shows the port on the page.");
								portBox.Value = 5301;
								Application.DoEvents();
								Assert.AreEqual("Set Data Out IP Port to 5301.", named("Step4Label").Text, "A step follows the port.");
								continue;
							}
							Assert.AreEqual("GTA V", page.Text);
							Assert.AreEqual(0, table.GetRow(named("Warning1Label")), exe + ": the warnings come first.");
							var warnings = string.Join(" ", controls.Where(x => x.Name.StartsWith("Warning")).Select(x => x.Text));
							foreach (var words in new[] { "Story mode only", "BattlEye", "GTA Online", "ban" })
								StringAssert.Contains(warnings, words);
							Assert.AreEqual("http://www.dev-c.com/gtav/scripthookv/", link("Mod1LinkLabel"));
							Assert.IsNotNull(named("InstallButton"));
							Assert.IsFalse(named("RemoveButton").Enabled, "Remove with nothing installed.");
							var loader = exe == "GTA5.exe" ? "dinput8.dll" : "xinput1_4.dll";
							Assert.AreEqual("Not found: " + loader + ". Unzip it into the game’s folder from its official page.", named("Mod2ValueLabel").Text,
								exe + " loads Script Hook V with " + loader + ".");
							StringAssert.StartsWith(named("Mod1ValueLabel").Text, "Found ScriptHookV.dll");
							if (exe == "GTA5.exe")
							{
								Assert.AreEqual("https://github.com/scripthookvdotnet/scripthookvdotnet/releases", link("Mod3LinkLabel"));
								StringAssert.StartsWith(named("Mod3ValueLabel").Text, "Found ScriptHookVDotNet3.dll");
							}
							else
							{
								Assert.AreEqual("https://www.gta5-mods.com/tools/script-hook-v-net-enhanced", link("Mod3LinkLabel"));
								StringAssert.Contains(named("Mod3ValueLabel").Text, "which is not Script Hook V .NET Enhanced",
									"A Script Hook V .NET that does not name itself Enhanced is the wrong one for GTA V Enhanced.");
							}
						}
					}
				});
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The plugin's help names every game of x360ce's telemetry data, and the plugins help carries the same text")]
		public void The_help_names_every_game_and_the_plugins_help_carries_it()
		{
			var help = Type().Description;
			Assert.IsNotNull(help, "The plugin has no help.");
			foreach (var name in SettingsManager.TelemetryGames.Items.Select(x => x.GameName).Distinct())
				StringAssert.Contains(help, name, "The help does not name " + name + ".");
			var plugins = AppHelper.ReadHelp(AppHelper.HelpPluginsResource).Replace("\r\n", "\n");
			StringAssert.Contains(plugins, help.Replace("\r\n", "\n"), "docs/Help.Plugins.md does not carry the plugin's help as the plugin gives it.");
		}

		/// <summary>The page's controls at any depth, but for the parts Windows Forms makes inside a number box.</summary>
		static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				if (child is UpDownBase)
					continue;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}

		#endregion
	}
}
