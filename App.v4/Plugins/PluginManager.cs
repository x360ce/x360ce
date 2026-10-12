using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
		{
			Folder = folder;
			OwnSigner = ownSigner;
			_allowThirdParty = allowThirdParty;
			_embedded = embedded;
		}

		public string Folder { get; }
		public string OwnSigner { get; }
		readonly Func<bool> _allowThirdParty;
		readonly Assembly _embedded;
		readonly object _lock = new object();
		FileSystemWatcher _watcher;
		Timer _scanDebounce;

		/// <summary>Raised on any thread when a plugin is found, loaded, refused or switched off.</summary>
		public event EventHandler Changed;

		/// <summary>Every plugin found, in the order found.</summary>
		public PluginFile[] Files { get { return _files; } }
		volatile PluginFile[] _files = new PluginFile[0];

		/// <summary>Every kind of device the loaded plugins add, faulted ones included.</summary>
		public PluginDeviceType[] DeviceTypes { get { return _deviceTypes; } }
		volatile PluginDeviceType[] _deviceTypes = new PluginDeviceType[0];

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
				byte[] bytes;
				using (var stream = _embedded.GetManifestResourceStream(name))
				{
					bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
				}
				Load(file, bytes, null);
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
				var file = known ?? new PluginFile(path, false) { Signer = GetSigner(path) };
				var trust = Trust(file.Signer, OwnSigner, _allowThirdParty());
				if (trust == PluginTrust.Refused)
				{
					if (known == null)
					{
						file.State = PluginFileState.WaitingForPermission;
						file.Reason = file.Signer == null
							? "Not signed. It loads once Allow third-party plugins is ticked on the Options tab's Plugins page."
							: "Signed by " + file.Signer + ", not by the publisher of x360ce. It loads once Allow third-party plugins is ticked on the Options tab's Plugins page.";
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

		/// <summary>The subject of the certificate that signed a file, or null when it has no valid Authenticode signature.</summary>
		/// <remarks>Checked by Windows, offline: no revocation lookup, so a plugin loads the same without a network.</remarks>
		public static string GetSigner(string path)
		{
			Exception error;
			CertificateHelper.VerifySignature(path, out error);
			if (error != null)
				return null;
			try
			{
				return X509Certificate.CreateFromSignedFile(path).Subject;
			}
			catch (System.Security.Cryptography.CryptographicException)
			{
				return null;
			}
		}

		#endregion

		#region Load

		void Load(PluginFile file, byte[] bytes, byte[] symbols, bool add = true)
		{
			try
			{
				file.Assembly = symbols == null ? Assembly.Load(bytes) : Assembly.Load(bytes, symbols);
				file.DeviceTypes = FindDeviceTypes(file.Assembly, file);
				file.State = PluginFileState.Loaded;
				file.Reason = file.DeviceTypes.Length == 0 ? "Loaded, but it has no public class that implements IPluginDevice." : null;
			}
			catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException || ex is ReflectionTypeLoadException || ex is TypeLoadException)
			{
				file.State = PluginFileState.Failed;
				file.Reason = "Could not be loaded: " + ex.Message;
				file.DeviceTypes = new PluginDeviceType[0];
			}
			lock (_lock)
			{
				if (add)
					_files = _files.Concat(new[] { file }).ToArray();
				else
					_files = _files.ToArray();
				_deviceTypes = _files.SelectMany(x => x.DeviceTypes ?? new PluginDeviceType[0]).ToArray();
			}
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
		/// One instance of each is made and asked for its name, product, controls and the values its settings start with,
		/// then disposed. The control a kind of device has is a public Windows Forms control in the same assembly whose
		/// constructor takes the device.
		/// </remarks>
		public static PluginDeviceType[] FindDeviceTypes(Assembly assembly, PluginFile file)
		{
			Type[] types;
			try
			{
				types = assembly.GetExportedTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(x => x != null && x.IsPublic).ToArray();
			}
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
			if (Trust(GetSigner(path), OwnSigner, _allowThirdParty()) == PluginTrust.Refused)
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
	}

	/// <summary>A kind of device a plugin adds: its class, and what one instance said about itself.</summary>
	public sealed class PluginDeviceType
	{
		public PluginDeviceType(Type type, PluginFile file)
		{
			Type = type;
			File = file;
			var attribute = type.GetCustomAttribute<DescriptionAttribute>();
			Description = attribute == null ? null : attribute.Description;
		}

		public Type Type { get; }
		public PluginFile File { get; }

		/// <summary>The plugin's help for the device, from the class's Description attribute; null when it has none.</summary>
		public string Description { get; }

		public string Name { get; private set; }
		public Guid ProductGuid { get; private set; }
		public int AxisCount { get; private set; }
		public int SliderCount { get; private set; }
		public int PovCount { get; private set; }
		public int ButtonCount { get; private set; }

		/// <summary>The settings the device's class has, shown on its tab of the controller page.</summary>
		public PluginSetting[] Settings { get; internal set; } = new PluginSetting[0];

		/// <summary>The control the plugin declares for the device, placed on its tab; null when it declares none.</summary>
		public Type ControlType { get; internal set; }

		/// <summary>True when the device has a tab of its own on the controller page: settings, or a control.</summary>
		public bool HasTab { get { return Settings.Length > 0 || ControlType != null; } }

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
