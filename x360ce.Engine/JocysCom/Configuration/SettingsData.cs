using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
using System.Windows;
#else // .NET Framework
using System.Windows.Forms;
#endif

namespace JocysCom.ClassLibrary.Configuration
{
	[Serializable, XmlRoot("Data"), DataContract]
	public class SettingsData<T> : ISettingsData
	{
		/// <summary>
		/// Initialize class. Use 'Environment.SpecialFolder.CommonApplicationData' folder, same for all users, to store settings.
		/// </summary>
		public SettingsData()
		{
			Initialize(null, false, null, null);
		}

		/// <summary>
		/// Initialize class
		/// </summary>
		/// <param name="overrideFileName"></param>
		/// <param name="userLevel">
		/// Defines where to store XML settings file:
		///   True  - Environment.SpecialFolder.ApplicationData
		///   False - Environment.SpecialFolder.CommonApplicationData
		///   Null  - Use ./{ExecutableBaseName}.xml settings file
		/// </param>
		/// <param name="comment"></param>
		/// <param name="assembly">Used to get company and product name.</param>
		public SettingsData(string overrideFileName = null, bool? userLevel = false, string comment = null, Assembly assembly = null)
		{
			Initialize(overrideFileName, userLevel, comment, assembly);
		}

		private List<Assembly> _Assemblies;
		private string _Company;
		private string _Product;

		/// <summary>
		/// Initialize class.
		/// </summary>
		private void Initialize(string overrideFileName, bool? userLevel, string comment, Assembly assembly)
		{
			// Wraps all methods into lock.
			//var items = System.Collections.ArrayList.Synchronized(Items);
			Items = new SortableBindingList<T>();
			_Comment = comment;
			// Get assemblies which will be used to select default (fists) and search for resources.
			_Assemblies = new List<Assembly>{
				assembly,
				Assembly.GetEntryAssembly(),
				Assembly.GetExecutingAssembly(),
			}.Where(x => x != null)
			.Distinct()
			.ToList();
			var mainAssembly = _Assemblies.First();
			_Company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(mainAssembly, typeof(AssemblyCompanyAttribute))).Company;
			_Product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(mainAssembly, typeof(AssemblyProductAttribute))).Product;
			string folder;
			string fileName;
			if (userLevel.HasValue)
			{
				// Get writable application folder.
				var specialFolder = userLevel.Value
					? Environment.SpecialFolder.ApplicationData
					: Environment.SpecialFolder.CommonApplicationData;
				folder = string.Format("{0}\\{1}\\{2}", Environment.GetFolderPath(specialFolder), _Company, _Product);
				fileName = typeof(T).Name + ".xml";
			}
			else
			{
				var fullName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
				folder = System.IO.Path.GetDirectoryName(fullName);
				fileName = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".xml";
			}
			// If override file name is set then override the file name.
			if (!string.IsNullOrEmpty(overrideFileName))
				fileName = overrideFileName;
			var path = Path.Combine(folder, fileName);
			_XmlFile = new FileInfo(path);
		}

		[XmlIgnore]
		public bool UseSeparateFiles { get; set; }

		[XmlIgnore]
		public string FileNamePropertyName
		{
			get => _FileNamePropertyName;
			set { _FileNamePropertyName = value; FileNameProperty = typeof(T).GetProperty(value); }
		}
		string _FileNamePropertyName;


		[XmlIgnore]
		public PropertyInfo FileNameProperty { get; set; }

		[XmlIgnore]
		public FileInfo XmlFile { get { return _XmlFile; } set { _XmlFile = value; } }

		[NonSerialized]
		protected FileInfo _XmlFile;

		[NonSerialized]
		protected string _Comment;

		[DataMember]
		public SortableBindingList<T> Items { get; set; }

		[NonSerialized]
		private object _SyncRoot;

		// Synchronization root for this object.
		public virtual object SyncRoot
		{
			get
			{
				if (_SyncRoot is null)
					System.Threading.Interlocked.CompareExchange<object>(ref _SyncRoot, new object(), null);
				return _SyncRoot;
			}
		}

		public T[] ItemsToArraySynchronized()
		{
			lock (SyncRoot)
				return Items.ToArray();
		}

		[XmlIgnore]
		System.Collections.IList ISettingsData.Items { get { return Items; } }

		public delegate void ApplyOrderDelegate(SettingsData<T> source);

		[XmlIgnore, NonSerialized]
		public ApplyOrderDelegate ApplyOrder;

		/// <summary>
		/// File Version.
		/// </summary>
		[XmlAttribute]
		public int Version { get; set; }

		[XmlIgnore, NonSerialized]
		object saveReadFileLock = new object();

		public event EventHandler Saving;

		public void SaveAs(string path)
		{
			SetFileMonitoring(false);
			var ev = Saving;
			if (ev != null)
				ev(this, new EventArgs());
			var items = ItemsToArraySynchronized();
			lock (saveReadFileLock)
			{
				// Remove unique primary keys.
				var type = items.FirstOrDefault()?.GetType();
				if (type != null && type.Name.EndsWith("EntityObject"))
				{
					var pi = type.GetProperty("EntityKey");
					for (int i = 0; i < items.Length; i++)
						pi.SetValue(items[i], null);
				}
				var fi = new FileInfo(path);
				var compress = fi.Name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
				// If each item will be saved to a separate file.
				if (UseSeparateFiles)
				{
					var di = GetCreateDirectory(fi);
					if (!di.Exists)
						di.Create();
					for (int i = 0; i < items.Length; i++)
					{
						var item = items[i];
						var bytes = Serializer.SerializeToXmlBytes(item, Encoding.UTF8, true, _Comment);
						var fileName = GetFileNameWithoutExtension(item) + fi.Extension;
						var fileFullName = Path.Combine(di.FullName, fileName);
						if (compress)
							bytes = SettingsHelper.Compress(bytes);
						SettingsHelper.WriteIfDifferent(fileFullName, bytes);
					}
				}
				else
				{
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					var bytes = Serializer.SerializeToXmlBytes(this, Encoding.UTF8, true, _Comment);
					if (compress)
						bytes = SettingsHelper.Compress(bytes);
					SettingsHelper.WriteIfDifferent(fi.FullName, bytes);
				}
			}
			SetFileMonitoring(true);
		}

		static string RemoveInvalidFileNameChars(string name)
		{
			var invalidChars = Path.GetInvalidFileNameChars();
			return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
		}

		public void Save()
		{
			SaveAs(_XmlFile.FullName);
		}

		/// <summary>Remove with SyncRoot lock.</summary>
		public void Remove(params T[] items)
		{
			lock (SyncRoot)
				foreach (var item in items)
					Items.Remove(item);
		}

		/// <summary>Add with SyncRoot lock.</summary>
		public void Add(params T[] items)
		{
			lock (SyncRoot)
				foreach (var item in items)
					Items.Add(item);
		}

		public class SettingsDataEventArgs : EventArgs
		{
			public SettingsDataEventArgs(IList<T> items)
			{
				Items = items;
			}
			public IList<T> Items { get; }
		}

		public delegate IList<T> ValidateDataDelegate(IList<T> items);

		[XmlIgnore, NonSerialized]
		public ValidateDataDelegate ValidateData;

		public event EventHandler<SettingsDataEventArgs> OnValidateData;

		public void Load()
		{
			LoadFrom(_XmlFile.FullName);
		}

		static DirectoryInfo GetCreateDirectory(FileInfo fi)
		{
			var compress = fi.Name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
			var dirName = Path.GetFileNameWithoutExtension(fi.FullName);
			if (compress)
				dirName = Path.GetFileNameWithoutExtension(dirName);
			var dirPath = Path.Combine(fi.Directory.FullName, dirName);
			var di = new DirectoryInfo(dirPath);
			return di;
		}

		public void LoadFrom(string fileName)
		{
			var settingsLoaded = false;
			var fi = new FileInfo(fileName);
			var di = GetCreateDirectory(fi);
			var compress = fi.Name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
			// If configuration file exists then...
			if (fi.Exists || di.Exists)
			{
				SettingsData<T> data = null;
				// Try to read file until success.
				while (true)
				{
					// Deserialize and load data.
					lock (saveReadFileLock)
					{
						try
						{

							// If each item will be saved to a separate file.
							if (UseSeparateFiles)
							{
								data = new SettingsData<T>();
								var files = di.GetFiles();
								for (int i = 0; i < files.Length; i++)
								{
									var file = files[i];
									var bytes = System.IO.File.ReadAllBytes(file.FullName);
									try
									{
										var item = DeserializeItem(bytes, compress);
										// Set Name property value to the same as the file.
										var name = GetFileNameWithoutExtension(item);
										var fileNamePropertyValue = (string)FileNameProperty.GetValue(item);
										var fileBaseName = Path.GetFileNameWithoutExtension(file.Name);
										if (fileNamePropertyValue != fileBaseName)
											FileNameProperty.SetValue(item, fileBaseName);
										data.Add(item);
									}
									catch { }
								}
							}
							else
							{
								var bytes = System.IO.File.ReadAllBytes(fi.FullName);
								data = DeserializeData(bytes, compress);
							}
							break;
						}
						catch (Exception ex)
						{
							var backupFile = fi.FullName + ".bak";
							var sb = new StringBuilder();
							sb.AppendFormat("{0} file has become corrupted.\r\n\r\n" +
								"Reason: " + ex.Message + "\r\n\r\n" +
								"Program must reset {0} file in order to continue.\r\n\r\n" +
								"   Click [Yes] to reset and continue.\r\n" +
								"   Click [No] if you wish to attempt manual repair.\r\n\r\n" +
								" File: {1}", fi.Name, fi.FullName);
							sb.AppendLine();
							sb.Append('-', 64);
							sb.AppendLine();
							sb.AppendLine(ex.ToString());
							var caption = string.Format("Corrupt {0} of {1}", fi.Name, _Product);
							//var form = new MessageBox();
							//form.StartPosition = FormStartPosition.CenterParent;
							var text = sb.ToString();
							bool reset;
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
							var result = MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Error);
							reset = result == MessageBoxResult.Yes;
#else // .NET Framework
							var result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
							reset = result == DialogResult.Yes;
#endif
							if (reset)
							{
								if (System.IO.File.Exists(backupFile))
								{
									System.IO.File.Copy(backupFile, fi.FullName, true);
									fi.Refresh();
								}
								else
								{
									System.IO.File.Delete(fi.FullName);
									break;
								}
							}
							else
							{
								// Avoid the inevitable crash by killing application first.
								Process.GetCurrentProcess().Kill();
								return;
							}
						}
					}
				}
				// If data read was successful then...
				if (data != null)
				{
					// Reorder data of order method exists.
					var ao = ApplyOrder;
					if (ao != null)
						ao(data);
					Version = data.Version;
					LoadAndValidateData(data.Items);
					settingsLoaded = true;
				}
			}
			// If settings failed to load then...
			if (!settingsLoaded)
			{
				ResetToDefault();
				Save();
			}
		}

		#region Use Separate Files

		public string GetItemFileFullName(string fileNameWithoutExtension)
		{
			var fi = new FileInfo(_XmlFile.FullName);
			var di = GetCreateDirectory(fi);
			var path = Path.Combine(di.FullName, fileNameWithoutExtension + ".xml");
			return path;
		}

		public string GetFileNameWithoutExtension(T item)
		{
			var name = (string)FileNameProperty.GetValue(item);
			name = RemoveInvalidFileNameChars(name);
			return name;
		}

		/// <summary>
		/// Returns error.
		/// </summary>
		public string RenameItem(T item, string newName)
		{
			lock (saveReadFileLock)
			{
				var oldName = GetFileNameWithoutExtension(item);
				// Case sensitive comparison.
				if (string.Equals(oldName, newName, StringComparison.Ordinal))
					return null;
				if (string.IsNullOrEmpty(newName))
					return "File name cannot be empty.";
				//newName = RemoveInvalidFileNameChars(newName);
				var invalidChars = newName.Intersect(Path.GetInvalidFileNameChars());
				if (invalidChars.Any())
					return $"File name cannot contain invalid characters: {string.Join("", invalidChars)}";
				var oldPath = GetItemFileFullName(oldName);
				var oldFile = new FileInfo(oldPath);
				var newPath = GetItemFileFullName(newName);
				// If only case changed then rename to temp file first.
				if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
				{
					var tempFilePath = Path.Combine(Path.GetDirectoryName(oldPath), Guid.NewGuid().ToString() + Path.GetExtension(oldPath));
					oldFile.MoveTo(tempFilePath);
				}
				else if (File.Exists(newPath))
				{
					return "File with the same name already exists.";
				}
				if (oldFile.Exists)
				{
					oldFile.MoveTo(newPath);
					FileNameProperty.SetValue(item, newName);
				}
				return null;
			}
		}

		/// <summary>
		/// Returns new name.
		/// </summary>
		public void DeleteItem(T item)
		{
			lock (saveReadFileLock)
			{
				var oldName = GetFileNameWithoutExtension(item);
				var oldPath = GetItemFileFullName(oldName);
				var fi = new FileInfo(oldPath);
				if (fi.Exists)
					fi.Delete();
				Items.Remove(item);
			}
		}

		#endregion

		void LoadAndValidateData(IList<T> data)
		{
			// Clear original data.
			Items.Clear();
			if (data is null)
				data = new SortableBindingList<T>();
			// Filter data if filter method exists.
			var fl = ValidateData;
			var items = (fl is null)
				? data
				: fl(data);
			// Filter data if filter method exists.
			var e = new SettingsDataEventArgs(items);
			OnValidateData?.Invoke(this, e);
			for (int i = 0; i < items.Count; i++)
				Items.Add(items[i]);
		}

		public bool ResetToDefault()
		{
			// Clear original data.
			Items.Clear();
			SettingsData<T> data = null;
			var success = false;
			for (int a = 0; a < _Assemblies.Count; a++)
			{
				var assembly = _Assemblies[a];
				var names = assembly.GetManifestResourceNames();
				// Get compressed resource name.
				var name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name + ".gz", StringComparison.OrdinalIgnoreCase));
				if (string.IsNullOrEmpty(name))
				{
					// Get uncompressed resource name.
					name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name, StringComparison.OrdinalIgnoreCase));
				}
				// If internal preset was found.
				if (!string.IsNullOrEmpty(name))
				{
					var resource = assembly.GetManifestResourceStream(name);
					var sr = new StreamReader(resource);
					byte[] bytes;
					using (var memstream = new MemoryStream())
					{
						sr.BaseStream.CopyTo(memstream);
						bytes = memstream.ToArray();
					}
					sr.Dispose();
					data = DeserializeData(bytes, name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase));
					success = true;
					break;
				}
			}
			LoadAndValidateData(data is null ? null : data.Items);
			return success;
		}

		SettingsData<T> DeserializeData(byte[] bytes, bool compressed)
		{
			if (compressed)
				bytes = SettingsHelper.Decompress(bytes);
			var data = Serializer.DeserializeFromXmlBytes<SettingsData<T>>(bytes);
			return data;
		}

		public T DeserializeItem(byte[] bytes, bool compressed)
		{
			if (compressed)
				bytes = SettingsHelper.Decompress(bytes);
			var item = Serializer.DeserializeFromXmlBytes<T>(bytes);
			return item;
		}

		#region Folder Monitoring

		public void SetFileMonitoring(bool enabled)
		{
			// Allow to monitor if items are in separate files.
			if (!UseSeparateFiles)
				return;
			var fi = new FileInfo(XmlFile.FullName);
			var di = GetCreateDirectory(fi);
			SetFileMonitoring(enabled, di.FullName, "*.xml");
		}

		private FileSystemWatcher _folderWatcher;
		private System.Timers.Timer _debounceTimer;
		public event EventHandler FilesChanged;

		public bool IsFolderMonitored { get; set; }

		public void SetFileMonitoring(bool enabled, string folderPath, string filePattern)
		{
			IsFolderMonitored = enabled;

			if (enabled)
			{
				if (_folderWatcher != null)
				{
					_folderWatcher.EnableRaisingEvents = false;
					_folderWatcher.Dispose();
				}

				_folderWatcher = new FileSystemWatcher(folderPath, filePattern)
				{
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
				};

				_folderWatcher.Changed += OnChanged;
				_folderWatcher.Created += OnChanged;
				_folderWatcher.Deleted += OnChanged;
				_folderWatcher.Renamed += OnRenamed;

				_folderWatcher.EnableRaisingEvents = true;

				// Initialize the debounce timer with an interval of 500ms
				_debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
				_debounceTimer.Elapsed += _debounceTimer_Elapsed;
			}
			else
			{
				if (_folderWatcher != null)
				{
					_folderWatcher.EnableRaisingEvents = false;
					_folderWatcher.Dispose();
					_folderWatcher = null;
				}

				if (_debounceTimer != null)
				{
					_debounceTimer.Stop();
					_debounceTimer.Dispose();
					_debounceTimer = null;
				}
			}
		}

		private void _debounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			FilesChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			DebounceEvent();
		}

		private void OnRenamed(object sender, RenamedEventArgs e)
		{
			DebounceEvent();
		}

		private void DebounceEvent()
		{
			// Reset and start the debounce timer each time a file system event occurs
			_debounceTimer.Stop();
			_debounceTimer.Start();
		}

		#endregion
	}
}
