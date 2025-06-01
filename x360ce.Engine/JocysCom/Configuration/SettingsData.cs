using JocysCom.ClassLibrary.Collections;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Represents a container for managing settings data T.
	/// Enables saving and loading of settings either as a single or multiple XML files.
	/// </summary>
	/// <typeparam name="T">The type of settings data this container will manage.</typeparam>
	[Serializable, XmlRoot("Data"), DataContract]
	public class SettingsData<T> : ISettingsData
	{
		/// <summary>
		/// Initializes a new instance of the SettingsData class with default settings.
		/// </summary>
		public SettingsData()
		{
			Initialize(null, false, null, null);
		}


		/// <summary>
		/// Initializes a new instance of the SettingsData class with specific settings.
		/// </summary>
		/// <param name="overrideFileName">Specifies a custom file name for the settings file. If null, a default name based on the type T is used.</param>
		/// <param name="userLevel">Determines the storage location of the XML settings file. True to use user-specific storage, False for common storage, Null for executable directory.</param>
		/// <param name="comment">A comment to include within the XML settings file.</param>
		/// <param name="assembly">The assembly to use for retrieving default company and product name for folder path generation.</param>
		/// <remarks>
		/// userLevel param defines where to store XML settings file:
		///   True  - Environment.SpecialFolder.ApplicationData
		///   False - Environment.SpecialFolder.CommonApplicationData
		///   Null  - Use ./{ExecutableBaseName}.xml settings file
		/// </remarks>
		public SettingsData(string overrideFileName = null, bool? userLevel = false, string comment = null, Assembly assembly = null)
		{
			Initialize(overrideFileName, userLevel, comment, assembly);
		}

		private List<Assembly> _Assemblies;
		private string _Company;
		private string _Product;

		private string GetAppDataPath(bool userLevel = false)
		{
			var mainAssembly = _Assemblies.First();
			_Company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(mainAssembly, typeof(AssemblyCompanyAttribute))).Company;
			_Product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(mainAssembly, typeof(AssemblyProductAttribute))).Product;
			// Get writable application folder.
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var path = string.Format("{0}\\{1}\\{2}", Environment.GetFolderPath(specialFolder), _Company, _Product);
			return path;
		}

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
			string folder;
			string fileBaseName;
			// Check if there is a folder with the same name as executable.
			folder = GetLocalSettingsDirectory();
			if (userLevel.HasValue)
			{
				if (string.IsNullOrEmpty(folder))
					folder = GetAppDataPath(userLevel.Value);
				fileBaseName = typeof(T).Name;
			}
			else
			{
				var fullName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
				if (string.IsNullOrEmpty(folder))
					folder = System.IO.Path.GetDirectoryName(fullName);
				fileBaseName = System.IO.Path.GetFileNameWithoutExtension(fullName);
			}
			string fileName = fileBaseName + ".xml";
			// If override file name is set then override the file name.
			if (!string.IsNullOrEmpty(overrideFileName))
				fileName = overrideFileName;
			var path = Path.Combine(folder, fileName);
			_XmlFile = new FileInfo(path);
		}

		/// <summary>
		/// Retrieves the directory path used for storing local settings, typically named after the executable.
		/// </summary>
		/// <returns>The directory path or null if the directory does not exist.</returns>
		public string GetLocalSettingsDirectory()
		{
			var moduleFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			var fi = new FileInfo(moduleFileName);
			var settingsFolderName = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
			// Parse command line settings.
			var args = Environment.GetCommandLineArgs();
			var ic = new JocysCom.ClassLibrary.Configuration.Arguments(args);
			// ------------------------------------------------
			if (ic.ContainsKey("SettingsPath"))
			{
				var settingsPath = ic["SettingsPath"];
				if (!string.IsNullOrEmpty(settingsPath))
				{
					var sdi = new DirectoryInfo(settingsPath);
					if (sdi.Exists)
						return sdi.FullName;
				}
			}
			// Check if folder with settings exists in the same folder as executable.
			var path = Path.Combine(fi.Directory.FullName, settingsFolderName);
			var di = new DirectoryInfo(path);
			if (di.Exists)
				return di.FullName;
			return null;
		}


		/// <summary>
		/// Indicates whether saving the settings is pending. This can be used to optimize write operations by delaying them until necessary.
		/// </summary>
		[XmlIgnore, JsonIgnore]
		public bool IsSavePending { get; set; }

		/// <summary>
		/// Indicates whether loading the settings is pending. Useful for deferring the loading operation until it's required.
		/// </summary>
		[XmlIgnore, JsonIgnore]
		public bool IsLoadPending { get; set; }

		/// <summary>
		/// Determines whether settings are stored in separate files.
		/// </summary>
		[XmlIgnore, JsonIgnore]
		public bool UseSeparateFiles { get; set; }

		/// <summary>
		/// Gets or sets the FileInfo object for the XML file that stores the settings data.
		/// </summary>
		[XmlIgnore, JsonIgnore]
		public FileInfo XmlFile { get { return _XmlFile; } set { _XmlFile = value; } }

		[NonSerialized]
		protected FileInfo _XmlFile;

		[NonSerialized]
		protected string _Comment;

		/// <summary>
		/// A list of settings items managed by this instance.
		/// </summary>
		[DataMember]
		public SortableBindingList<T> Items { get; set; }

		[NonSerialized]
		private object _SyncRoot;

		/// <summary>
		/// Synchronization root object for thread-safe operations.
		/// </summary>
		public virtual object SyncRoot
		{
			get
			{
				if (_SyncRoot is null)
					System.Threading.Interlocked.CompareExchange<object>(ref _SyncRoot, new object(), null);
				return _SyncRoot;
			}
		}

		/// <summary>
		/// Converts the items in the collection to an array and returns it.
		/// This operation is synchronized to prevent data inconsistency during the conversion.
		/// </summary>
		/// <returns>An array of items.</returns>
		public T[] ItemsToArraySynchronized()
		{
			lock (SyncRoot)
				return Items.ToArray();
		}

		[XmlIgnore, JsonIgnore]
		IBindingList ISettingsData.Items { get { return Items; } }

		public delegate void ApplyOrderDelegate(SettingsData<T> source);

		[XmlIgnore, JsonIgnore, NonSerialized]
		public ApplyOrderDelegate ApplyOrder;

		/// <summary>
		/// File Version.
		/// </summary>
		[XmlAttribute]
		public int Version { get; set; }

		[XmlIgnore, JsonIgnore, NonSerialized]
		object saveReadFileLock = new object();

		/// <summary>
		/// Occurs when the settings data is about to be saved to the XML file, allowing for pre-save operations or validation.
		/// </summary>
		public event EventHandler Saving;

		/// <summary>
		/// Saves the current settings into an XML file at the specified path. Compresses the file if the file extension is .gz.
		/// </summary>
		/// <param name="path">The file path where the settings will be saved.</param>
		/// <param name="items">Specific items to save. If null then save all items.</param>
		public void SaveAs(string path, object[] items = null)
		{
			SetFileMonitoring(false);
			var ev = Saving;
			if (ev != null)
				ev(this, new EventArgs());
			var tItems = items?.Cast<T>().ToArray() ?? ItemsToArraySynchronized();
			lock (saveReadFileLock)
			{
				// Remove unique primary keys.
				var type = tItems.FirstOrDefault()?.GetType();
				if (type != null && type.Name.EndsWith("EntityObject"))
				{
					var pi = type.GetProperty("EntityKey");
					for (int i = 0; i < tItems.Length; i++)
						pi.SetValue(tItems[i], null);
				}
				var fi = new FileInfo(path);
				var compress = fi.Name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
				// If each item will be saved to a separate file.
				if (UseSeparateFiles)
				{
					var di = GetRootDirectory(fi);
					// Create directory because it will be watched for changes.
					if (!di.Exists)
						di.Create();
					for (int i = 0; i < tItems.Length; i++)
					{
						var fileItem = (ISettingsFileItem)tItems[i];
						if (fileItem.IsReadOnlyFile)
							continue;
						var fileFullName = GetFileItemFullName(path, fileItem);
						var fiItem = new FileInfo(fileFullName);
						if (!fiItem.Directory.Exists)
							fiItem.Directory.Create();
						var bytes = Serialize(fileItem);
						if (compress)
							bytes = SettingsHelper.Compress(bytes);
						if (!AllowWriteFile(fiItem))
							continue;
						if (!SettingsHelper.WriteIfDifferent(fileFullName, bytes))
							continue;
						fi.Refresh();
						fileItem.WriteTime = new FileInfo(fileFullName).LastWriteTime;
						// Update last write time.
						fiItem.Refresh();
						SetLastWriteTime(fiItem);
					}
				}
				else
				{
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					var bytes = Serialize(this);
					if (compress)
						bytes = SettingsHelper.Compress(bytes);
					if (AllowWriteFile(fi))
					{
						if (SettingsHelper.WriteIfDifferent(fi.FullName, bytes))
						{
							// Update last write time.
							fi.Refresh();
							SetLastWriteTime(fi);
						}
					}
				}
			}
			IsSavePending = false;
			SetFileMonitoring(true);
		}

		public static string RemoveInvalidPathChars(string name)
		{
			var invalidChars = Path.GetInvalidPathChars();
			return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
		}

		public static string RemoveInvalidFileNameChars(string name)
		{
			var invalidChars = Path.GetInvalidFileNameChars();
			return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
		}

		/// <summary>
		/// Save items.
		/// </summary>
		/// <param name="items"> If items is null then save all items.</param>
		public void Save(object[] items = null)
		{
			SaveAs(_XmlFile.FullName, items);
		}

		/// <summary>
		/// Settings root directory.
		/// </summary>
		public DirectoryInfo RootDirectory =>
			UseSeparateFiles
				? GetRootDirectory(_XmlFile)
				: _XmlFile.Directory;

		/// <summary>
		/// Adds an array of items to the current collection of settings data.
		/// </summary>
		/// <param name="items">The array of items to add.</param>
		public void Add(params T[] items)
		{
			lock (SyncRoot)
				foreach (var item in items)
					Items.Add(item);
		}

		/// <summary>
		/// Removes an array of items from the current collection of settings data.
		/// </summary>
		/// <param name="items">The array of items to remove.</param>
		public void Remove(params T[] items)
		{
			lock (SyncRoot)
				foreach (var item in items)
					Items.Remove(item);
		}

		public class SettingsDataEventArgs : EventArgs
		{
			public SettingsDataEventArgs(IList<T> items)
			{
				Items = items;
			}
			public IList<T> Items { get; }
			public bool Handled { get; set; }
		}

		public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
		{
			/// <summary>The old value of the property.</summary>
			public object OldValue { get; }
			/// <summary>The new value of the property.</summary>
			public object NewValue { get; }
			public ItemPropertyChangedEventArgs(string propertyName, object oldValue, object newValue)
				: base(propertyName)
			{
				OldValue = oldValue;
				NewValue = newValue;
			}
		}

		public delegate IList<T> ValidateDataDelegate(IList<T> items);

		[XmlIgnore, JsonIgnore, NonSerialized]
		public ValidateDataDelegate ValidateData;

		/// <summary>
		/// Occurs when data validation is required, providing an opportunity to perform custom validation logic on settings data.
		/// </summary>
		public event EventHandler<SettingsDataEventArgs> OnValidateData;

		#region Last Write Time

		[XmlIgnore, JsonIgnore]
		public bool PreventWriteToNewerFiles { get; set; } = true;

		[XmlIgnore, JsonIgnore, NonSerialized]
		private Dictionary<string, DateTime> LastWriteTimes = new Dictionary<string, DateTime>();

		/// <summary>
		/// Record the LastWriteTime when loading for later comparison when saving.
		/// </summary>
		private void SetLastWriteTime(FileInfo fi)
		{
			// If file was deleted or don't exists.
			if (!fi.Exists)
				return;
			if (LastWriteTimes.ContainsKey(fi.FullName))
				LastWriteTimes[fi.FullName] = fi.LastWriteTime;
			else
				LastWriteTimes.Add(fi.FullName, fi.LastWriteTime);
		}

		private bool IsNewerOnDisk(FileInfo fi)
		{
			fi.Refresh();
			// If file was deleted or don't exists.
			if (!fi.Exists)
				return false;
			if (!LastWriteTimes.ContainsKey(fi.FullName))
				return false;
			return fi.Exists && fi.LastWriteTime > LastWriteTimes[fi.FullName];
		}

		private bool AllowWriteFile(FileInfo fi)
		{
			fi.Refresh();
			// If file was deleted or don't exists.
			if (!fi.Exists)
				return true;
			if (!PreventWriteToNewerFiles)
				return true;
			return !IsNewerOnDisk(fi);
		}

		#endregion

		public void Load()
		{
			LoadFrom(_XmlFile.FullName);
		}

		static DirectoryInfo GetRootDirectory(FileInfo fi)
		{
			var compress = fi.Name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
			var dirName = Path.GetFileNameWithoutExtension(fi.FullName);
			if (compress)
				dirName = Path.GetFileNameWithoutExtension(dirName);
			var dirPath = Path.Combine(fi.Directory.FullName, dirName);
			var di = new DirectoryInfo(dirPath);
			return di;
		}

		/// <summary>
		/// Loads settings data from an XML file specified by the fileName.
		/// </summary>
		/// <param name="fileName">The file name/path from which to load settings data.</param>
		public void LoadFrom(string fileName)
		{
			var settingsLoaded = false;
			var fi = new FileInfo(fileName);
			var di = GetRootDirectory(fi);
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
								var files = di.GetFiles("*" + fi.Extension, SearchOption.AllDirectories);
								var fileItems = new List<ISettingsFileItem>();
								for (int i = 0; i < files.Length; i++)
								{
									var file = files[i];
									// Record the LastWriteTime for later comparison.
									SetLastWriteTime(file);
									var bytes = System.IO.File.ReadAllBytes(file.FullName);
									try
									{
										var item = DeserializeItem(bytes, compress);
										var fileItem = (ISettingsFileItem)item;
										fileItem.WriteTime = file.LastWriteTime;
										// Set Name property value to the same as the file.
										var name = RemoveInvalidFileNameChars(file.Name);
										var fileBaseName = Path.GetFileNameWithoutExtension(file.Name);
										var path = IO.PathHelper.GetRelativePath(di.FullName + "\\", file.Directory.FullName + "\\");
										path = path.TrimEnd('.', '\\', '/');
										fileItem.Path = string.IsNullOrWhiteSpace(path) ? null : path;
										if (fileItem.BaseName != fileBaseName)
											fileItem.BaseName = fileBaseName;
										fileItems.Add(fileItem);
										var listItem = fileItem as ISettingsListFileItem;
										if (listItem != null)
										{
											// Created can be later than file creation.
											if (listItem.Created == DateTime.MinValue || listItem.Created > file.CreationTime)
												listItem.Created = file.CreationTime;
											// Modified can be earlier than Created.
											if (listItem.Modified < listItem.Created)
												listItem.Modified = listItem.Created;

										}
									}
									catch { }
								}
								SortList(fileItems);
								foreach (var fileItem1 in fileItems)
									data.Add((T)fileItem1);
							}
							else
							{
								// Record the LastWriteTime for later comparison.
								SetLastWriteTime(fi);
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
							var result = MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Error);
							reset = result == MessageBoxResult.Yes;
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

		/// <summary>Sort the list with the fewest UI changes.</summary>
		public void SortList<T1>(IList<T1> items) where T1 : ISettingsFileItem
		{
			// Move works with special characters to the end.
			var newItems = items
				.OrderBy(x => x.Path)
				.ThenBy(x => x.Name.StartsWith("®"))
				.ThenBy(x => x.Name)
				.ToList();
			CollectionsHelper.Synchronize(newItems, items);
		}

		#region Use Separate Files

		/// <summary>
		/// Get the full path for a file based on a filename with extension.
		/// </summary>
		public string GetFileItemFullName(ISettingsFileItem fileItem, string overrideBaseName = null)
		{
			var fileFullName = GetFileItemFullName(_XmlFile.FullName, fileItem, overrideBaseName);
			var fiItem = new FileInfo(fileFullName);
			if (!fiItem.Directory.Exists)
				fiItem.Directory.Create();
			return fileFullName;
		}

		public string GetFileItemFullBaseName(ISettingsFileItem fileItem)
		{
			var fi = new FileInfo(_XmlFile.FullName);
			var di = GetRootDirectory(fi);
			var fileName = RemoveInvalidFileNameChars(fileItem.BaseName);
			var itemPath = fileItem.Path;
			if (!string.IsNullOrEmpty(itemPath))
				itemPath = RemoveInvalidPathChars(itemPath);
			var fileFullName = string.IsNullOrEmpty(itemPath)
				? Path.Combine(di.FullName, fileName)
				: Path.Combine(di.FullName, itemPath, fileName);
			return fileFullName;
		}

		/// <summary>
		/// Get item path when using separte files.
		/// </summary>
		public static string GetFileItemFullName(string rootPath, ISettingsFileItem fileItem, string overrideBaseName = null)
		{
			var fi = new FileInfo(rootPath);
			var di = GetRootDirectory(fi);
			var fileName = RemoveInvalidFileNameChars(overrideBaseName ?? fileItem.BaseName) + fi.Extension;
			var itemPath = fileItem.Path;
			if (!string.IsNullOrEmpty(itemPath))
				itemPath = RemoveInvalidPathChars(itemPath);
			var fileFullName = string.IsNullOrEmpty(itemPath)
				? Path.Combine(di.FullName, fileName)
				: Path.Combine(di.FullName, itemPath, fileName);
			return fileFullName;
		}


		/// <summary>
		/// Renames the specified folder to a new name, managing potential case-sensitivity issues on certain file systems by temporarily renaming to a GUID-based name.
		/// </summary>
		/// <param name="currentPath">The current path of the folder to be renamed.</param>
		/// <param name="newFolderName">The new name for the folder.</param>
		/// <returns>A message indicating success, error, or null if the operation is successful.</returns>
		public string RenameFolder(string currentPath, string newFolderName)
		{
			try
			{
				// If directory don't exists
				if (!Directory.Exists(currentPath))
					return null;
				var directoryInfo = new DirectoryInfo(currentPath);
				var parentDirectory = directoryInfo.Parent.FullName;
				var newPath = Path.Combine(parentDirectory, newFolderName);
				// check if the new folder name is different from the current one (ignoring the case)
				if (string.Equals(directoryInfo.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
				{
					// rename to temp folder first if only the casing is changed
					var tempPath = Path.Combine(parentDirectory, Guid.NewGuid().ToString());
					directoryInfo.MoveTo(tempPath);
				}
				else if (Directory.Exists(newPath))
				{
					return "Folder with the same name already exists.";
				}
				directoryInfo.MoveTo(newPath);
			}
			catch (Exception ex)
			{
				return "An error occurred: " + ex.Message;
			}
			return null;
		}

		/// <summary>
		/// Occurs when item was renamed.
		/// </summary>
		public event EventHandler<ItemPropertyChangedEventArgs> ItemRenamed;

		/// <summary>
		/// Renames a settings item file to a new name, ensuring file system consistency and updating internal metadata accordingly.
		/// If folder with the same name exists. then rename the folder too.
		/// </summary>
		/// <param name="fileItem">The settings item file object to be renamed.</param>
		/// <param name="newName">The new name for the settings item file.</param>
		/// <returns>A message indicating the outcome of the operation or null if the operation is successful.</returns>
		public string RenameItem(ISettingsFileItem fileItem, string newName)
		{
			lock (saveReadFileLock)
			{
				var oldName = RemoveInvalidFileNameChars(fileItem.BaseName);
				// If the names are exactly the same (case sensitive comparison), no renaming is needed.
				if (string.Equals(oldName, newName, StringComparison.Ordinal))
					return null;
				if (string.IsNullOrEmpty(newName))
					return "The file name cannot be empty. Please provide a valid name.";
				//newName = RemoveInvalidFileNameChars(newName);
				var invalidChars = newName.Intersect(Path.GetInvalidFileNameChars());
				if (invalidChars.Any())
					return $"The file name contains invalid character(s): {string.Join("", invalidChars)}";
				var oldPath = GetFileItemFullName(fileItem, oldName);
				var file = new FileInfo(oldPath);
				var newPath = GetFileItemFullName(fileItem, newName);
				// Disable monitoring in order not to trigger reloading.
				SetFileMonitoring(false);
				try
				{
					// Rename folder if folder with the same name exists.
					var folderPath = Path.Combine(file.Directory.FullName, oldName);
					var error = RenameFolder(folderPath, newName);
					if (!string.IsNullOrEmpty(error))
						return error;
					// Rename file.
					// If only case changed then...
					if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
					{
						// rename to temp file first.
						var tempFilePath = Path.Combine(Path.GetDirectoryName(oldPath), Guid.NewGuid().ToString() + Path.GetExtension(oldPath));
						file.MoveTo(tempFilePath);
					}
					if (File.Exists(newPath))
					{
						var fi = new FileInfo(newPath);
						return $"A file named '{fi.Name}' ({fi.Length} bytes) already exists. Please remove or rename the existing file and try again.";
					}
					if (file.Exists)
					{
						var oldBaseName = fileItem.BaseName;
						file.MoveTo(newPath);
						fileItem.BaseName = newName;
						fileItem.WriteTime = file.LastWriteTime;
						var e = new ItemPropertyChangedEventArgs(nameof(fileItem.BaseName), oldBaseName, newName);
						ItemRenamed?.Invoke(fileItem, e);
					}
				}
				catch (Exception)
				{
					throw;
				}
				finally
				{
					SetFileMonitoring(true);
				}
				return null;
			}
		}

		/// <summary>
		/// Deletes the file associated with a settings item and removes the item from the internal collection, ensuring data integrity and freeing up resources.
		/// </summary>
		/// <param name="fileItem">The settings item file object to be deleted.</param>
		/// <returns>A message indicating the result of the delete operation, or null if successful.</returns>
		public string DeleteItem(ISettingsFileItem fileItem)
		{
			lock (saveReadFileLock)
			{
				var oldName = RemoveInvalidFileNameChars(fileItem.BaseName);
				var oldPath = GetFileItemFullName(fileItem, oldName);
				var fi = new FileInfo(oldPath);
				// Rename folder if folder with the same name exists.
				var folderPath = Path.Combine(fi.Directory.FullName, oldName);

				try
				{
					if (Directory.Exists(folderPath))
						Directory.Delete(folderPath, true);
				}
				catch (Exception ex)
				{
					return ex.Message;
				}
				try
				{
					if (fi.Exists)
						fi.Delete();
				}
				catch (Exception ex)
				{
					return ex.Message;
				}
				Items.Remove((T)fileItem);
				return null;
			}
		}

		#endregion

		/// <summary>
		/// Indicates whether the items collection should be cleared when loading new data.
		/// </summary>
		[XmlIgnore, JsonIgnore]
		public bool ClearWhenLoading = false;

		void LoadAndValidateData(IList<T> data)
		{
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
			if (ClearWhenLoading)
			{
				// Clear original data.
				Items.Clear();
				for (int i = 0; i < items.Count; i++)
					Items.Add(items[i]);
			}
			else if (!(e?.Handled == true))
			{
				var oldList = GetHashValues(Items);
				var newList = GetHashValues(data).ToArray();
				var newData = new List<T>();
				// Step 1: Update new list with the old items if they are exactly the same.
				for (int i = 0; i < newList.Length; i++)
				{
					var newItem = newList[i];
					// Find same item from the old list.
					var oldItem = oldList.FirstOrDefault(x => x.Value.SequenceEqual(newItem.Value));
					// If same item found then use it...
					if (oldItem.Key != null)
					{
						newData.Add(oldItem.Key);
						oldList.Remove(oldItem.Key);
					}
					else
					{
						newData.Add(newItem.Key);
					}
				}
				CollectionsHelper.Synchronize(newData, Items);
			}
		}

		#region Synchronize

		/// <summary>
		/// Synchronizes the content of the source collection with the target collection.
		/// </summary>
		/// <param name="source">The source collection to sync from.</param>
		/// <param name="target">The target collection to sync to.</param>
		/// <remarks>
		/// Same Code:
		/// JocysCom\Controls\SearchHelper.cs
		/// </remarks>
		public static void Synchronize(IList<T> source, IList<T> target)
		{
			// Convert to array to avoid modification of collection during processing.
			var sList = source.ToArray();
			var t = 0;
			for (var s = 0; s < sList.Length; s++)
			{
				var item = sList[s];
				// If item exists in destination and is in the correct position then continue
				if (t < target.Count && target[t].Equals(item))
				{
					t++;
					continue;
				}
				// If item is in destination but not at the correct position, remove it.
				var indexInDestination = target.IndexOf(item);
				if (indexInDestination != -1)
					target.RemoveAt(indexInDestination);
				// Insert item at the correct position.
				target.Insert(s, item);
				t = s + 1;
			}
			// Remove extra items.
			while (target.Count > sList.Length)
				target.RemoveAt(target.Count - 1);
		}


		/// <summary>
		/// Return list of items their SHA256 hash.
		/// </summary>
		Dictionary<T, byte[]> GetHashValues(IList<T> items)
		{
			var list = new Dictionary<T, byte[]>();
			var algorithm = System.Security.Cryptography.SHA256.Create();
			foreach (var item in items)
			{
				var bytes = Serialize(item);
				var byteHash = algorithm.ComputeHash(bytes);
				list.Add(item, byteHash);
			}
			return list;
		}

		#endregion

		/// <summary>
		/// Resets settings to default values defined within the resource files of the specified assemblies.
		/// </summary>
		/// <returns>True if default settings were successfully loaded; otherwise, False.</returns>
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

		#region Serialization

		byte[] Serialize(object fileItem)
		{
			return Serializer.SerializeToXmlBytes(fileItem, Encoding.UTF8, true, _Comment);
		}

		/// <summary>
		/// Deserializes settings data from a byte array, potentially decompressing it first if indicated.
		/// </summary>
		/// <param name="bytes">The byte array containing serialized settings data.</param>
		/// <param name="compressed">Indicates whether the byte array is compressed and requires decompression.</param>
		/// <returns>A <see cref="SettingsData{T}"/> instance deserialized from the byte array.</returns>
		public SettingsData<T> DeserializeData(byte[] bytes, bool compressed)
		{
			if (compressed)
				bytes = SettingsHelper.Decompress(bytes);
			var data = Serializer.DeserializeFromXmlBytes<SettingsData<T>>(bytes);
			return data;
		}

		/// <summary>
		/// Deserializes a single settings item from a byte array, optionally decompressing it. This method facilitates the reconstruction of individual settings from file storage.
		/// </summary>
		/// <param name="bytes">The byte array containing the serialized settings item.</param>
		/// <param name="compressed">Indicates whether the byte array is compressed.</param>
		/// <returns>The deserialized settings item.</returns>
		public T DeserializeItem(byte[] bytes, bool compressed)
		{
			if (compressed)
				bytes = SettingsHelper.Decompress(bytes);
			var item = Serializer.DeserializeFromXmlBytes<T>(bytes);
			return item;
		}

		#endregion

		#region Folder Monitoring

		/// <summary>
		/// Enables or disables monitoring on the settings file directory. When enabled, changes to the files are detected, allowing for the application to respond accordingly.
		/// </summary>
		/// <param name="enabled">true to enable monitoring; false to disable it.</param>
		/// <param name="folderPath">The path to the folder to monitor.</param>
		/// <param name="filePattern">The pattern of the file names to monitor within the folder.</param>
		public void SetFileMonitoring(bool enabled)
		{
			// Allow to monitor if items are in separate files.
			if (!UseSeparateFiles)
				return;
			var fi = new FileInfo(XmlFile.FullName);
			var di = GetRootDirectory(fi);
			SetFileMonitoring(enabled, di.FullName, "*.xml");
		}

		private FileSystemWatcher _folderWatcher;

		/// <summary>
		/// Raises an event when files in the monitored settings directory change, ensuring settings are reloaded or updated accordingly.
		/// </summary>
		public event EventHandler FilesChanged;

		[DefaultValue(false)]
		public bool IsFolderMonitored { get; set; }


		/// <summary>
		/// Enables or disables file monitoring for changes to settings files. When enabled, changes to the settings file on disk will trigger a reload of settings to reflect the new state.
		/// </summary>
		/// <param name="enabled">Indicates whether file monitoring should be enabled (true) or disabled (false).</param>
		/// <remarks>
		/// Monitoring settings files is crucial in scenarios where settings might be changed externally or by different instances, ensuring the application operates with the most up-to-date configuration.
		/// </remarks>
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
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
					IncludeSubdirectories = true,
				};

				_folderWatcher.Changed += OnChanged;
				_folderWatcher.Created += OnCreated;
				_folderWatcher.Deleted += OnDeleted;
				_folderWatcher.Renamed += OnRenamed;
				_folderWatcher.EnableRaisingEvents = true;
			}
			else
			{
				if (_folderWatcher != null)
				{
					_folderWatcher.EnableRaisingEvents = false;
					_folderWatcher.Dispose();
					_folderWatcher = null;
				}
			}
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
			 => _ = Helper.Debounce(FilesChangedDebounced);

		private void OnCreated(object sender, FileSystemEventArgs e)
			=> _ = Helper.Debounce(FilesChangedDebounced);

		private void OnDeleted(object sender, FileSystemEventArgs e)
			=> _ = Helper.Debounce(FilesChangedDebounced);

		private void OnRenamed(object sender, RenamedEventArgs e)
			=> _ = Helper.Debounce(FilesChangedDebounced);

		private void FilesChangedDebounced()
			=> FilesChanged?.Invoke(this, EventArgs.Empty);

		#endregion
	}
}
