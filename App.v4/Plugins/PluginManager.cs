using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>Finds plugins, decides which may load, loads them, and keeps the kinds of device they add.</summary>
	/// <remarks>
	/// Plugins come from two places. Those embedded in this program are part of it, signed with it, and always load.
	/// Those in the plugins folder, each in a folder of its own named like it (Plugins\MyDevice\MyDevice.dll), load when
	/// their signature is valid and signed by the same publisher as this program, or when the person allows third-party
	/// plugins. Any other DLL in a plugin's folder is one of its dependencies, looked for only on behalf of that plugin.
	/// The folder is watched, so a plugin that appears there loads without a restart.
	///
	/// A DLL is loaded from its bytes, so the file stays free to replace; .NET Framework cannot unload an assembly, so a
	/// new version of a loaded DLL takes effect on the next start. Lists are replaced whole, never changed, so a reader
	/// on another thread sees the old list or the new one and never half of each.
	/// </remarks>
	public sealed class PluginManager : IDisposable
	{
		/// <summary>The folder name under the settings folder that plugins are loaded from.</summary>
		public const string FolderName = "Plugins";

		/// <summary>The start of the name of every plugin embedded in this program.</summary>
		public const string EmbeddedPrefix = "x360ce.App.Resources.Plugins.";

		/// <summary>The program's manager. Set once at start.</summary>
		public static PluginManager Current { get; set; }

		/// <param name="folder">The folder holding a folder for each plugin.</param>
		/// <param name="ownSigner">The subject of this program's signing certificate, or null when it is not signed.</param>
		/// <param name="allowThirdParty">Read whenever a DLL not signed like this program is found.</param>
		/// <param name="embedded">The assembly whose embedded plugins load, or null for none.</param>
		public PluginManager(string folder, string ownSigner, Func<bool> allowThirdParty, Assembly embedded)
			: this(folder, ownSigner, allowThirdParty, embedded, null)
		{
		}

		/// <param name="folder">The folder holding a folder for each plugin.</param>
		/// <param name="ownSigner">The subject of this program's signing certificate, or null when it is not signed.</param>
		/// <param name="allowThirdParty">Read whenever a DLL not signed like this program is found.</param>
		/// <param name="embedded">The assembly whose embedded plugins load, or null for none.</param>
		/// <param name="isEnabled">Returns whether a plugin name is enabled; null enables every plugin by default (including built-in plugins).</param>
		public PluginManager(string folder, string ownSigner, Func<bool> allowThirdParty, Assembly embedded, Func<string, bool> isEnabled)
		{
			Folder = folder;
			OwnSigner = ownSigner;
			_allowThirdParty = allowThirdParty;
			_embedded = embedded;
			_isEnabled = isEnabled;
		}

		public string Folder { get; }
		public string OwnSigner { get; }
		readonly Func<bool> _allowThirdParty;
		readonly Assembly _embedded;
		readonly Func<string, bool> _isEnabled;
		readonly object _lock = new object();
		FileSystemWatcher _watcher;
		Timer _scanDebounce;

		bool IsPluginEnabled(string name)
		{
			return _isEnabled == null || _isEnabled(name);
		}

		/// <summary>The names of the plugins found to stay off until the person ticks them (<see cref="PluginOffByDefaultAttribute"/>).</summary>
		static readonly ConcurrentDictionary<string, bool> OffByDefaultNames = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		/// <summary>True for a plugin found to stay off until the person ticks it on the Options tab's Plugins page.</summary>
		/// <remarks>A manager knows it once it has read the plugin, which it does before it asks whether the plugin is enabled.</remarks>
		public static bool IsOffByDefault(string name)
		{
			return !string.IsNullOrEmpty(name) && OffByDefaultNames.ContainsKey(name);
		}

		/// <summary>True when a plugin just read is off by default and the person has not ticked it; remembers which plugins are off by default.</summary>
		bool StaysOff(PluginFile file, bool ticked)
		{
			if (!file.Assembly.IsDefined(typeof(PluginOffByDefaultAttribute), false))
				return false;
			OffByDefaultNames[file.Name] = true;
			return !ticked && !IsPluginEnabled(file.Name);
		}

		/// <summary>Raised on any thread when a plugin is found, loaded, refused or switched off.</summary>
		public event EventHandler Changed;

		/// <summary>Every plugin found, in the order found.</summary>
		public PluginFile[] Files { get { return _files; } }
		volatile PluginFile[] _files = new PluginFile[0];

		/// <summary>Every kind of device the loaded plugins add, faulted ones included.</summary>
		public PluginDeviceType[] DeviceTypes { get { return _deviceTypes; } }
		volatile PluginDeviceType[] _deviceTypes = new PluginDeviceType[0];

		/// <summary>Every wheel telemetry provider the loaded plugins add, faulted ones included.</summary>
		public PluginTelemetryType[] TelemetryTypes { get { return _telemetryTypes; } }
		volatile PluginTelemetryType[] _telemetryTypes = new PluginTelemetryType[0];

		/// <summary>Every game plugin the loaded plugins add, faulted ones included.</summary>
		public PluginGameType[] GameTypes { get { return _gameTypes; } }
		volatile PluginGameType[] _gameTypes = new PluginGameType[0];

		/// <summary>The kind of device with this product GUID, or null when no loaded plugin adds it.</summary>
		public PluginDeviceType FindDeviceType(Guid productGuid)
		{
			var types = _deviceTypes;
			for (var i = 0; i < types.Length; i++)
				if (types[i].ProductGuid == productGuid)
					return types[i];
			return null;
		}

		#region Start, scan and watch

		/// <summary>Loads the embedded plugins and those in the folder, and starts watching the folder.</summary>
		public void Start()
		{
			LoadEmbedded();
			Directory.CreateDirectory(Folder);
			Scan();
			_scanDebounce = new Timer(_ => Scan(), null, Timeout.Infinite, Timeout.Infinite);
			_watcher = new FileSystemWatcher(Folder, "*.dll")
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
			};
			// A copy fires several events while the file is written: the scan runs once, a second after the last.
			FileSystemEventHandler arm = (s, e) => _scanDebounce.Change(1000, Timeout.Infinite);
			_watcher.Created += arm;
			_watcher.Changed += arm;
			_watcher.Renamed += (s, e) => _scanDebounce.Change(1000, Timeout.Infinite);
			_watcher.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			if (_watcher != null)
				_watcher.Dispose();
			if (_scanDebounce != null)
				_scanDebounce.Dispose();
		}

		/// <summary>Loads the plugins embedded in this program. Called by <see cref="Start"/>; on its own, it loads no folder.</summary>
		public void LoadEmbedded()
		{
			if (_embedded == null)
				return;
			foreach (var name in _embedded.GetManifestResourceNames())
			{
				if (!name.StartsWith(EmbeddedPrefix, StringComparison.Ordinal) || !name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
					continue;
				var file = new PluginFile(name, true) { Signer = OwnSigner };
				if (!IsPluginEnabled(file.Name))
				{
					file.Enabled = false;
					file.State = PluginFileState.Disabled;
					file.Reason = "Disabled on the Options tab’s Plugins page.";
					Add(file);
					continue;
				}
				var bytes = ReadEmbeddedBytes(name);
				if (bytes != null)
					Load(file, bytes, null);
			}
		}

		byte[] ReadEmbeddedBytes(string resourceName)
		{
			if (_embedded == null)
				return null;
			using (var stream = _embedded.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
					return null;
				var bytes = new byte[stream.Length];
				stream.Read(bytes, 0, bytes.Length);
				return bytes;
			}
		}

		/// <summary>Looks for plugin DLLs not seen before, and loads those waiting for permission once it is given.</summary>
		/// <remarks>Called by the watcher, and by the interface when the person allows third-party plugins.</remarks>
		public void Scan()
		{
			if (!Directory.Exists(Folder))
				return;
			string[] paths;
			try
			{
				paths = Directory.GetFiles(Folder, "*.dll", SearchOption.AllDirectories);
			}
			catch (IOException) { return; }
			catch (UnauthorizedAccessException) { return; }
			foreach (var path in paths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
			{
				var known = _files.FirstOrDefault(x => !x.Embedded && string.Equals(x.Source, path, StringComparison.OrdinalIgnoreCase));
				if (known != null && known.State != PluginFileState.WaitingForPermission)
					continue;
				var folder = Path.GetDirectoryName(path);
				if (string.Equals(folder.TrimEnd('\\'), Folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
				{
					var loose = new PluginFile(path, false) { State = PluginFileState.Failed };
					loose.Reason = "Not loaded: a plugin goes in a folder of its own, named like it: " + FolderName + "\\" + loose.Name + "\\" + loose.Name + ".dll.";
					Add(loose);
					continue;
				}
				// A DLL not named like its folder is a dependency of the plugin beside it.
				if (!string.Equals(Path.GetFileName(folder), Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase))
					continue;
				var file = known ?? new PluginFile(path, false) { Signer = CertificateHelper.GetSigner(path) };
				if (!IsPluginEnabled(file.Name))
				{
					file.Enabled = false;
					file.State = PluginFileState.Disabled;
					file.Reason = "Disabled on the Options tab’s Plugins page.";
					if (known == null)
						Add(file);
					else
						RebuildAndNotify();
					continue;
				}
				var trust = Trust(file.Signer, OwnSigner, _allowThirdParty());
				if (trust == PluginTrust.Refused)
				{
					if (known == null)
					{
						file.State = PluginFileState.WaitingForPermission;
						file.Reason = PermissionReason(file.Signer);
						Add(file);
					}
					continue;
				}
				byte[] bytes;
				try
				{
					bytes = File.ReadAllBytes(path);
				}
				catch (IOException) { continue; }
				catch (UnauthorizedAccessException) { continue; }
				var symbols = Path.ChangeExtension(path, ".pdb");
				Load(file, bytes, File.Exists(symbols) ? File.ReadAllBytes(symbols) : null, known == null);
			}
		}

		/// <summary>Enables or disables a discovered plugin by name, updating active device/telemetry types immediately.</summary>
		public void SetEnabled(string pluginName, bool enabled)
		{
			if (string.IsNullOrEmpty(pluginName))
				return;
			var file = _files.FirstOrDefault(x => string.Equals(x.Name, pluginName, StringComparison.OrdinalIgnoreCase));
			if (file == null)
				return;
			if (!enabled)
			{
				if (!file.Enabled && file.State == PluginFileState.Disabled)
					return;
				lock (_lock)
				{
					file.Enabled = false;
					if (file.State != PluginFileState.Failed)
					{
						file.State = PluginFileState.Disabled;
						file.Reason = "Disabled on the Options tab’s Plugins page.";
					}
					RebuildTypesLocked();
				}
				OnChanged();
				return;
			}
			if (file.Enabled && file.State == PluginFileState.Loaded)
				return;
			if (file.Assembly != null)
			{
				// A plugin off by default was read only for that: its kinds are found when it is first ticked.
				if (file.DeviceTypes.Length == 0 && file.TelemetryTypes.Length == 0 && file.GameTypes.Length == 0)
					FindTypes(file);
				lock (_lock)
				{
					file.Enabled = true;
					file.State = PluginFileState.Loaded;
					file.Reason = LoadedReason(file);
					RebuildTypesLocked();
				}
				OnChanged();
				return;
			}
			file.Enabled = true;
			if (file.Embedded)
			{
				var bytes = ReadEmbeddedBytes(file.Source);
				if (bytes != null)
					Load(file, bytes, null, false, true);
				return;
			}
			if (Trust(file.Signer, OwnSigner, _allowThirdParty()) == PluginTrust.Refused)
			{
				lock (_lock)
				{
					file.State = PluginFileState.WaitingForPermission;
					file.Reason = PermissionReason(file.Signer);
					RebuildTypesLocked();
				}
				OnChanged();
				return;
			}
			byte[] diskBytes;
			try
			{
				diskBytes = File.ReadAllBytes(file.Source);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				lock (_lock)
				{
					file.State = PluginFileState.Failed;
					file.Reason = "Could not be loaded: " + ex.Message;
					RebuildTypesLocked();
				}
				OnChanged();
				return;
			}
			var diskSymbols = Path.ChangeExtension(file.Source, ".pdb");
			Load(file, diskBytes, File.Exists(diskSymbols) ? File.ReadAllBytes(diskSymbols) : null, false, true);
		}

		static string PermissionReason(string signer)
		{
			return signer == null
				? "Not signed. It loads once Allow third-party plugins is ticked on the Options tab’s Plugins page."
				: "Signed by " + signer + ", not by the publisher of x360ce. It loads once Allow third-party plugins is ticked on the Options tab’s Plugins page.";
		}

		#endregion

		#region Trust

		/// <summary>Whether a DLL may load, by who signed it.</summary>
		/// <param name="signer">The subject of the DLL's signing certificate, or null when it has no valid signature.</param>
		/// <param name="ownSigner">The subject of this program's signing certificate, or null when it is not signed.</param>
		/// <param name="allowThirdParty">Whether the person allows plugins from anyone else.</param>
		public static PluginTrust Trust(string signer, string ownSigner, bool allowThirdParty)
		{
			if (!string.IsNullOrEmpty(signer) && !string.IsNullOrEmpty(ownSigner) && string.Equals(signer, ownSigner, StringComparison.Ordinal))
				return PluginTrust.SameSigner;
			return allowThirdParty ? PluginTrust.ThirdParty : PluginTrust.Refused;
		}

		#endregion

		#region Load

		/// <param name="ticked">True when the person ticked the plugin just now, so it loads even when it is off by default.</param>
		void Load(PluginFile file, byte[] bytes, byte[] symbols, bool add = true, bool ticked = false)
		{
			try
			{
				file.Assembly = symbols == null ? Assembly.Load(bytes) : Assembly.Load(bytes, symbols);
				if (StaysOff(file, ticked))
				{
					file.Enabled = false;
					file.State = PluginFileState.Disabled;
					file.Reason = "Off by default. Tick Enabled to use it.";
				}
				else
				{
					FindTypes(file);
					file.State = PluginFileState.Loaded;
					file.Reason = LoadedReason(file);
				}
			}
			catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException || ex is ReflectionTypeLoadException || ex is TypeLoadException)
			{
				file.State = PluginFileState.Failed;
				file.Reason = "Could not be loaded: " + ex.Message;
				file.DeviceTypes = new PluginDeviceType[0];
				file.TelemetryTypes = new PluginTelemetryType[0];
				file.GameTypes = new PluginGameType[0];
			}
			lock (_lock)
			{
				if (add)
					_files = _files.Concat(new[] { file }).ToArray();
				RebuildTypesLocked();
			}
			OnChanged();
		}

		void RebuildTypesLocked()
		{
			_files = _files.ToArray();
			_deviceTypes = _files
				.Where(x => x.Enabled && x.State == PluginFileState.Loaded)
				.SelectMany(x => x.DeviceTypes ?? new PluginDeviceType[0])
				.ToArray();
			_telemetryTypes = _files
				.Where(x => x.Enabled && x.State == PluginFileState.Loaded)
				.SelectMany(x => x.TelemetryTypes ?? new PluginTelemetryType[0])
				.ToArray();
			_gameTypes = _files
				.Where(x => x.Enabled && x.State == PluginFileState.Loaded)
				.SelectMany(x => x.GameTypes ?? new PluginGameType[0])
				.ToArray();
		}

		/// <summary>Finds every kind of device, telemetry provider and game plugin a plugin just loaded adds.</summary>
		static void FindTypes(PluginFile file)
		{
			file.DeviceTypes = FindDeviceTypes(file.Assembly, file);
			file.TelemetryTypes = FindTelemetryTypes(file.Assembly, file);
			file.GameTypes = FindGameTypes(file.Assembly, file);
		}

		/// <summary>What is wrong with a plugin that loaded: null, or that it offers nothing x360ce can use.</summary>
		static string LoadedReason(PluginFile file)
		{
			return file.DeviceTypes.Length == 0 && file.TelemetryTypes.Length == 0 && file.GameTypes.Length == 0
				? "Loaded, but it has no public class that implements IPluginDevice, IPluginWheelTelemetry or IPluginGame."
				: null;
		}

		void RebuildAndNotify()
		{
			lock (_lock)
				RebuildTypesLocked();
			OnChanged();
		}

		void Add(PluginFile file)
		{
			lock (_lock)
				_files = _files.Concat(new[] { file }).ToArray();
			OnChanged();
		}

		/// <summary>Every kind of device an assembly adds: each public class that implements <see cref="IPluginDevice"/> and can be made without arguments.</summary>
		/// <remarks>
		/// One instance of each is made and asked for its name, product, controls, recommended mapping and the values its
		/// settings start with, then disposed. The control a kind of device has is a public Windows Forms control in the
		/// same assembly whose constructor takes the device.
		/// </remarks>
		public static PluginDeviceType[] FindDeviceTypes(Assembly assembly, PluginFile file)
		{
			var types = ExportedTypes(assembly);
			var list = new List<PluginDeviceType>();
			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsInterface || !typeof(IPluginDevice).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
					continue;
				var deviceType = new PluginDeviceType(type, file);
				try
				{
					var probe = (IPluginDevice)Activator.CreateInstance(type);
					deviceType.Describe(probe);
					deviceType.Settings = PluginSetting.Find(type, probe);
					deviceType.ControlType = FindControlType(types, type);
					deviceType.McpServer = FindMcpServer(type, probe);
					var disposable = probe as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				catch (Exception ex)
				{
					deviceType.Fault(ex);
				}
				list.Add(deviceType);
			}
			return list.ToArray();
		}

		/// <summary>Every wheel telemetry provider an assembly adds: each public class that implements <see cref="IPluginWheelTelemetry"/> and can be made without arguments.</summary>
		public static PluginTelemetryType[] FindTelemetryTypes(Assembly assembly, PluginFile file)
		{
			var types = ExportedTypes(assembly);
			var list = new List<PluginTelemetryType>();
			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsInterface || !typeof(IPluginWheelTelemetry).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
					continue;
				var telemetryType = new PluginTelemetryType(type, file);
				try
				{
					var probe = (IPluginWheelTelemetry)Activator.CreateInstance(type);
					telemetryType.Describe(probe);
					telemetryType.ControlType = FindTelemetryControlType(types);
					var disposable = probe as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				catch (Exception ex)
				{
					telemetryType.Fault(ex);
				}
				list.Add(telemetryType);
			}
			return list.ToArray();
		}

		/// <summary>Every game plugin an assembly adds: each public class that implements <see cref="IPluginGame"/> and can be made without arguments.</summary>
		/// <remarks>One instance of each is made and asked for its name, then disposed; the games it is for are asked when a game is selected. Its page is found as a device's control is.</remarks>
		public static PluginGameType[] FindGameTypes(Assembly assembly, PluginFile file)
		{
			var types = ExportedTypes(assembly);
			var list = new List<PluginGameType>();
			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsInterface || !typeof(IPluginGame).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
					continue;
				var gameType = new PluginGameType(type, file);
				try
				{
					var probe = gameType.NewInstance();
					gameType.Describe(probe);
					gameType.ControlType = FindControlType(types, type);
					var disposable = probe as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				catch (Exception ex)
				{
					gameType.Fault(ex);
				}
				list.Add(gameType);
			}
			return list.ToArray();
		}

		/// <summary>The public types of an assembly, those that load when some do not.</summary>
		static Type[] ExportedTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetExportedTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null && x.IsPublic).ToArray();
			}
		}

		/// <summary>The control a plugin declares for a kind of device: a public Windows Forms control whose constructor takes the device; null when there is none.</summary>
		static Type FindControlType(Type[] types, Type deviceType)
		{
			foreach (var type in types)
			{
				if (type.IsAbstract || !typeof(System.Windows.Forms.Control).IsAssignableFrom(type))
					continue;
				foreach (var constructor in type.GetConstructors())
				{
					var parameters = constructor.GetParameters();
					if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(deviceType)
						&& parameters[0].ParameterType != typeof(object))
						return type;
				}
			}
			return null;
		}

		/// <summary>The dedicated tab control a plugin declares for a wheel telemetry provider; null when none.</summary>
		static Type FindTelemetryControlType(Type[] types)
		{
			foreach (var type in types)
			{
				if (type.IsAbstract || !typeof(System.Windows.Forms.Control).IsAssignableFrom(type))
					continue;
				foreach (var constructor in type.GetConstructors())
				{
					var parameters = constructor.GetParameters();
					if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IPluginWheelTelemetryHost))
						return type;
				}
			}
			foreach (var type in types)
			{
				if (type.IsAbstract || !typeof(System.Windows.Forms.Control).IsAssignableFrom(type))
					continue;
				if (type.GetConstructor(Type.EmptyTypes) != null)
					return type;
			}
			return null;
		}

		/// <summary>The MCP server a kind of device declares: the value of its public <see cref="PluginMcpServer"/> property; null when it has none.</summary>
		static PluginMcpServer FindMcpServer(Type type, IPluginDevice probe)
		{
			var property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(x => x.PropertyType == typeof(PluginMcpServer) && x.GetIndexParameters().Length == 0 && x.GetGetMethod() != null);
			return property == null ? null : (PluginMcpServer)property.GetValue(probe);
		}

		/// <summary>Finds a DLL a plugin depends on in the plugin's own folder, under the same trust as a plugin.</summary>
		/// <remarks>Hooked to the program's assembly resolver, after its own embedded libraries.</remarks>
		public Assembly ResolveDependency(ResolveEventArgs e)
		{
			var requesting = e.RequestingAssembly;
			var file = requesting == null ? null : _files.FirstOrDefault(x => x.Assembly == requesting && !x.Embedded);
			if (file == null)
				return null;
			var name = new AssemblyName(e.Name).Name;
			var path = Path.Combine(Path.GetDirectoryName(file.Source), name + ".dll");
			if (!File.Exists(path))
				return null;
			if (Trust(CertificateHelper.GetSigner(path), OwnSigner, _allowThirdParty()) == PluginTrust.Refused)
				return null;
			return Assembly.Load(File.ReadAllBytes(path));
		}

		#endregion

		void OnChanged()
		{
			var ev = Changed;
			if (ev != null)
				ev(this, EventArgs.Empty);
		}

		/// <summary>Switches a kind of device off after it threw, and says so.</summary>
		public void Fault(PluginDeviceType type, Exception ex)
		{
			if (type.Faulted)
				return;
			type.Fault(ex);
			OnChanged();
		}

		/// <summary>Switches a telemetry provider off after it threw, and says so.</summary>
		public void Fault(PluginTelemetryType type, Exception ex)
		{
			if (type.Faulted)
				return;
			type.Fault(ex);
			OnChanged();
		}
	}

	/// <summary>Whether a plugin DLL may load.</summary>
	public enum PluginTrust
	{
		/// <summary>Signed by the publisher of this program.</summary>
		SameSigner,
		/// <summary>From anyone else, and the person allows third-party plugins.</summary>
		ThirdParty,
		/// <summary>From anyone else, and the person has not allowed third-party plugins.</summary>
		Refused,
	}

	public enum PluginFileState
	{
		/// <summary>Loaded; its kinds of device are listed.</summary>
		Loaded,
		/// <summary>Not loaded or unloaded: switched off on the Options tab's Plugins page.</summary>
		Disabled,
		/// <summary>Not loaded: not signed by the publisher of this program, and third-party plugins are not allowed.</summary>
		WaitingForPermission,
		/// <summary>Not loaded: not a .NET library this program can load, or not in a folder of its own.</summary>
		Failed,
	}

	/// <summary>One plugin DLL: where it came from, who signed it, and what became of it.</summary>
	public sealed class PluginFile
	{
		public PluginFile(string source, bool embedded)
		{
			Source = source;
			Embedded = embedded;
		}

		/// <summary>The DLL's full path, or the resource name of one embedded in this program.</summary>
		public string Source { get; }

		/// <summary>True for a plugin embedded in this program.</summary>
		public bool Embedded { get; }

		/// <summary>True while this plugin is enabled on the Options tab's Plugins page. Built-in plugins are enabled by default.</summary>
		public bool Enabled { get; set; } = true;

		/// <summary>The name shown for the plugin: its file name without the extension.</summary>
		public string Name
		{
			get
			{
				var name = Embedded ? Source.Substring(PluginManager.EmbeddedPrefix.Length) : Path.GetFileName(Source);
				return name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
			}
		}

		/// <summary>The subject of the certificate that signed it, or null when it has no valid signature.</summary>
		public string Signer { get; set; }

		public PluginFileState State { get; set; }

		/// <summary>Why it is not loaded, or what is wrong with it; null when nothing is.</summary>
		public string Reason { get; set; }

		public Assembly Assembly { get; set; }

		public PluginDeviceType[] DeviceTypes { get; set; } = new PluginDeviceType[0];

		public PluginTelemetryType[] TelemetryTypes { get; set; } = new PluginTelemetryType[0];

		public PluginGameType[] GameTypes { get; set; } = new PluginGameType[0];
	}

	/// <summary>A game plugin a plugin adds: its class, its page, and the games it is for.</summary>
	public sealed class PluginGameType
	{
		public PluginGameType(Type type, PluginFile file)
		{
			Type = type;
			File = file;
			var attribute = type.GetCustomAttribute<DescriptionAttribute>();
			Description = attribute == null ? null : attribute.Description;
		}

		public Type Type { get; }
		public PluginFile File { get; }

		/// <summary>The plugin's help for the game, from the class's Description attribute; null when it has none.</summary>
		public string Description { get; }

		/// <summary>The name of its tab on the Games tab.</summary>
		public string Name { get; private set; }

		/// <summary>The file names of the games it is for now: asked of a new instance each time, as they can come from x360ce's data; empty once it failed.</summary>
		public string[] FileNames
		{
			get
			{
				if (Faulted)
					return new string[0];
				try
				{
					var game = NewInstance();
					var names = (game.FileNames ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
					var disposable = game as IDisposable;
					if (disposable != null)
						disposable.Dispose();
					return names;
				}
				catch (Exception ex)
				{
					Fault(ex);
					return new string[0];
				}
			}
		}

		/// <summary>The page the plugin declares: a public Windows Forms control whose constructor takes the class; null when none.</summary>
		public Type ControlType { get; internal set; }

		public bool Faulted { get { return FaultReason != null; } }
		public string FaultReason { get; private set; }

		/// <summary>True when the game whose file has this name is one the plugin is for.</summary>
		public bool IsFor(string fileName)
		{
			return !string.IsNullOrEmpty(fileName) && FileNames.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
		}

		internal void Describe(IPluginGame game)
		{
			Name = string.IsNullOrWhiteSpace(game.Name) ? Type.Name : game.Name;
		}

		internal void Fault(Exception ex)
		{
			if (Name == null)
				Name = Type.Name;
			var inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
			FaultReason = inner.GetType().Name + ": " + inner.Message;
		}

		/// <summary>A new instance of the plugin, given x360ce's telemetry data before anything else.</summary>
		public IPluginGame NewInstance()
		{
			var game = (IPluginGame)Activator.CreateInstance(Type);
			game.TelemetryData = PluginTelemetryData.Current;
			return game;
		}
	}

	/// <summary>A wheel telemetry provider a plugin adds: its class, and what one instance said about itself.</summary>
	public sealed class PluginTelemetryType
	{
		public PluginTelemetryType(Type type, PluginFile file)
		{
			Type = type;
			File = file;
			var attribute = type.GetCustomAttribute<DescriptionAttribute>();
			Description = attribute == null ? null : attribute.Description;
		}

		public Type Type { get; }
		public PluginFile File { get; }
		public string Description { get; }
		public string Name { get; private set; }
		/// <summary>The dedicated tab control the plugin declares for the "Wheel Telemetry" controller tab; null when none.</summary>
		public Type ControlType { get; internal set; }
		public bool Faulted { get { return FaultReason != null; } }
		public string FaultReason { get; private set; }

		internal void Describe(IPluginWheelTelemetry provider)
		{
			Name = string.IsNullOrWhiteSpace(provider.Name) ? Type.Name : provider.Name;
		}

		internal void Fault(Exception ex)
		{
			if (Name == null)
				Name = Type.Name;
			var inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
			FaultReason = inner.GetType().Name + ": " + inner.Message;
		}

		public IPluginWheelTelemetry NewInstance()
		{
			return (IPluginWheelTelemetry)Activator.CreateInstance(Type);
		}
	}

	/// <summary>A kind of device a plugin adds: its class, and what one instance said about itself.</summary>
	public sealed class PluginDeviceType
	{
		public PluginDeviceType(Type type, PluginFile file)
		{
			Type = type;
			File = file;
			RequiresHardwareIdentity = typeof(IPluginDeviceIdentity).IsAssignableFrom(type);
			var attribute = type.GetCustomAttribute<DescriptionAttribute>();
			Description = attribute == null ? null : attribute.Description;
		}

		public Type Type { get; }
		public PluginFile File { get; }

		/// <summary>True when this plugin wraps real external hardware (implements <see cref="IPluginDeviceIdentity"/>) and must report real hardware identity before cloud upload.</summary>
		public bool RequiresHardwareIdentity { get; }

		/// <summary>The plugin's help for the device, from the class's Description attribute; null when it has none.</summary>
		public string Description { get; }

		public string Name { get; private set; }
		public Guid ProductGuid { get; private set; }
		public int AxisCount { get; private set; }
		public int SliderCount { get; private set; }
		public int PovCount { get; private set; }
		public int ButtonCount { get; private set; }

		/// <summary>The mapping the plugin recommends for the device, as the fields of an SDL row (<see cref="IPluginMapping"/>); null when it recommends none.</summary>
		public string Mapping { get; private set; }

		/// <summary>The settings the device's class has, shown on its tab of the controller page.</summary>
		public PluginSetting[] Settings { get; internal set; } = new PluginSetting[0];

		/// <summary>The control the plugin declares for the device, placed on its tab; null when it declares none.</summary>
		public Type ControlType { get; internal set; }

		/// <summary>The MCP server the kind of device declares, whose settings its tab shows; null when it declares none.</summary>
		public PluginMcpServer McpServer { get; internal set; }

		/// <summary>True when the device has a tab of its own on the controller page: settings, a control, or an MCP server.</summary>
		public bool HasTab { get { return Settings.Length > 0 || ControlType != null || McpServer != null; } }

		/// <summary>True once the plugin threw: its devices are offline until the program starts again.</summary>
		public bool Faulted { get { return FaultReason != null; } }

		/// <summary>What the plugin threw, or null while it has not.</summary>
		public string FaultReason { get; private set; }

		internal void Describe(IPluginDevice device)
		{
			Name = string.IsNullOrWhiteSpace(device.Name) ? Type.Name : device.Name;
			ProductGuid = device.ProductGuid;
			AxisCount = Clamp(device.AxisCount, 6);
			SliderCount = Clamp(device.SliderCount, 8);
			PovCount = Clamp(device.PovCount, 4);
			ButtonCount = Clamp(device.ButtonCount, 128);
			var mapping = device as IPluginMapping;
			Mapping = mapping == null || string.IsNullOrWhiteSpace(mapping.Mapping) ? null : mapping.Mapping.Trim();
		}

		internal void Fault(Exception ex)
		{
			if (Name == null)
				Name = Type.Name;
			var inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
			FaultReason = inner.GetType().Name + ": " + inner.Message;
		}

		static int Clamp(int value, int max)
		{
			return value < 0 ? 0 : value > max ? max : value;
		}

		/// <summary>A new instance of the device, for one device the person added.</summary>
		public IPluginDevice NewInstance()
		{
			return (IPluginDevice)Activator.CreateInstance(Type);
		}
	}
}
