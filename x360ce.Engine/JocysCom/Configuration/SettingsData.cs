using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.Objects.DataClasses;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.ComponentModel;
using System.Reflection;
using System.Linq;
using System.IO.Compression;

namespace JocysCom.ClassLibrary.Configuration
{
	[Serializable, XmlRoot("Data")]
	public class SettingsData<T> : ISettingsData
	{

		public SettingsData() { }

		public SettingsData(string fileName, bool userLevel = false, string comment = null)
		{
			Items = new SortableBindingList<T>();
			_Comment = comment;
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var file = string.IsNullOrEmpty(fileName)
				? string.Format("{0}.xml", typeof(T).Name)
				: fileName;
			var folder = string.Format("{0}\\{1}\\{2}",
				Environment.GetFolderPath(specialFolder),
				Application.CompanyName,
				Application.ProductName);
			var path = Path.Combine(folder, file);
			_XmlFile = new FileInfo(path);
		}

		[XmlIgnore]
		public FileInfo XmlFile { get { return _XmlFile; } }

		[NonSerialized]
		protected FileInfo _XmlFile;

		[NonSerialized]
		protected string _Comment;

		public SortableBindingList<T> Items { get; set; }

		[XmlIgnore]
		System.Collections.IList ISettingsData.Items { get { return Items; } }

		public delegate void ApplyOrderDelegate(SettingsData<T> source);

		[XmlIgnore]
		public ApplyOrderDelegate ApplyOrder;

		/// <summary>
		/// File Version.
		/// </summary>
		[XmlAttribute]
		public int Version { get; set; }

		object initialFileLock = new object();
		object saveReadFileLock = new object();

		public void SaveAs(string fileName)
		{
			lock (saveReadFileLock)
			{
				for (int i = 0; i < Items.Count; i++)
				{
					var o = Items[i] as EntityObject;
					if (o != null) o.EntityKey = null;
				}
				var fi = new FileInfo(fileName);
				if (!fi.Directory.Exists)
				{
					fi.Directory.Create();
				}
				byte[] bytes;
				bytes = Serializer.SerializeToXmlBytes(this, Encoding.UTF8, true, _Comment);
				if (fi.Name.EndsWith(".gz"))
				{
					bytes = SettingsHelper.Compress(bytes);
				}
				SettingsHelper.WriteIfDifferent(fi.FullName, bytes);
			}
		}

		public void Save()
		{
			SaveAs(_XmlFile.FullName);
		}

		public void Remove(params T[] items)
		{
			foreach (var item in items)
			{
				Items.Remove(item);
			}
		}

		public void Add(params T[] items)
		{
			foreach (var item in items)
			{
				Items.Add(item);
			}
		}

		public delegate IList<T> FilterListDelegate(IList<T> items);

		[NonSerialized, XmlIgnore]
		public FilterListDelegate FilterList;

		public void Load()
		{
			LoadFrom(_XmlFile.FullName);
		}

		public void LoadFrom(string fileName)
		{
			bool settingsLoaded = false;
			var fi = new FileInfo(fileName);
			// If configuration file exists then...
			if (fi.Exists)
			{
				// Try to read file until success.
				while (true)
				{
					SettingsData<T> data;
					// Deserialize and load data.
					lock (saveReadFileLock)
					{
						try
						{
							SettingsData<T> xmlItems;
							if (fi.FullName.EndsWith(".gz"))
							{
								var compressedBytes = File.ReadAllBytes(fi.FullName);
								var bytes = SettingsHelper.Decompress(compressedBytes);
								var xml = Encoding.UTF8.GetString(bytes);
								xmlItems = Serializer.DeserializeFromXmlString<SettingsData<T>>(xml, Encoding.UTF8);
							}
							else
							{
								xmlItems = Serializer.DeserializeFromXmlFile<SettingsData<T>>(fi.FullName);
							}
							data = xmlItems;
							//foreach (T item in items.Items)
							//{
							//	var oldItem = data.FirstOrDefault(x => x.Group == item.Group);
							//	// If old item was not found then...
							//	if (oldItem == null)
							//	{
							//		// Add as new.
							//		SettingsManager.Current.Settings.Items.Add(item);
							//	}
							//	else
							//	{
							//		// Udate old item.
							//		oldItem.Group = item.Group;
							//	}
							//}
							if (data != null)
							{
								if (ApplyOrder != null)
								{
									ApplyOrder(data);
								}
								Items.Clear();
								if (data != null)
								{
									var m = FilterList;
									var items = (m == null)
										? data.Items
										: m(data.Items);
									if (items != null)
									{
										for (int i = 0; i < items.Count; i++) Items.Add(items[i]);
									}
								}
								settingsLoaded = true;
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
							var caption = string.Format("Corrupt {0} of {1}", fi.Name, Application.ProductName);
							//var form = new MessageBox();
							//form.StartPosition = FormStartPosition.CenterParent;
							var text = sb.ToString();
							var result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
							if (result == DialogResult.Yes)
							{
								if (File.Exists(backupFile))
								{
									File.Copy(backupFile, fi.FullName, true);
									fi.Refresh();
								}
								else
								{
									File.Delete(fi.FullName);
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
			}
			// If settings failed to load then...
			if (!settingsLoaded)
			{
				ResetToDefault();
			}
			if (!settingsLoaded)
			{
				Save();
			}
		}

		public bool ResetToDefault()
		{
			var assemblies = new List<Assembly>();
			var exasm = Assembly.GetExecutingAssembly();
			var enasm = Assembly.GetEntryAssembly();
			assemblies.Add(exasm);
			if (exasm != enasm)
				assemblies.Add(enasm);
			var success = false;
			for (int a = 0; a < assemblies.Count; a++)
			{
				var assembly = assemblies[a];
				var names = assembly.GetManifestResourceNames();
				// Get compressed resource name.
				var name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name + ".gz"));
				if (string.IsNullOrEmpty(name))
				{
					// Get uncompressed resource name.
					name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name));
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
					if (name.EndsWith(".gz"))
					{

						bytes = SettingsHelper.Decompress(bytes);
					}
					var xml = Encoding.UTF8.GetString(bytes);
					var data = Serializer.DeserializeFromXmlString<SettingsData<T>>(xml);
					Items.Clear();
					for (int i = 0; i < data.Items.Count; i++)
						Items.Add(data.Items[i]);
					success = true;
					break;
				}
			}
			return success;
		}

	}
}
