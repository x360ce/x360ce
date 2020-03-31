using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Configuration
{
	[Serializable, XmlRoot("Data"), DataContract]
	public class SettingsData<T> : ISettingsData
	{

		public SettingsData()
		{
			Initialize();
		}

		public SettingsData(string fileName, bool userLevel = false, string comment = null)
		{
			Initialize(fileName, userLevel, comment);
		}

		void Initialize(string fileName = null, bool userLevel = false, string comment = null)
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

		[DataMember]
		public SortableBindingList<T> Items { get; set; }

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
		object initialFileLock = new object();

		[XmlIgnore, NonSerialized]
		object saveReadFileLock = new object();

		public event EventHandler Saving;

		public void SaveAs(string fileName)
		{
			var ev = Saving;
			if (ev != null)
				ev(this, new EventArgs());
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

		public delegate IList<T> ValidateDataDelegate(IList<T> items);

		[XmlIgnore, NonSerialized]
		public ValidateDataDelegate ValidateData;

		public void Load()
		{
			LoadFrom(_XmlFile.FullName);
		}

		public void LoadFrom(string fileName)
		{
			var settingsLoaded = false;
			var fi = new FileInfo(fileName);
			// If configuration file exists then...
			if (fi.Exists)
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
							var bytes = File.ReadAllBytes(fi.FullName);
							data = DeserializeData(bytes, fi.Name.EndsWith(".gz"));
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
				// If data read was successful then...
				if (data != null)
				{
					// Reorder data of order method exists.
					var ao = ApplyOrder;
					if (ao != null)
						ao(data);
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

		void LoadAndValidateData(IList<T> data)
		{
			// Clear original data.
			Items.Clear();
			if (data == null)
				data = new SortableBindingList<T>();
			// Filter data if filter method exists.
			var fl = ValidateData;
			var items = (fl == null)
				? data
				: fl(data);
			for (int i = 0; i < items.Count; i++)
				Items.Add(items[i]);
		}

		public bool ResetToDefault()
		{
			// Clear original data.
			Items.Clear();
			SettingsData<T> data = null;
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
				var name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name + ".gz", StringComparison.InvariantCulture));
				if (string.IsNullOrEmpty(name))
				{
					// Get uncompressed resource name.
					name = names.FirstOrDefault(x => x.EndsWith(_XmlFile.Name, StringComparison.InvariantCulture));
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
					data = DeserializeData(bytes, name.EndsWith(".gz", StringComparison.InvariantCulture));
					success = true;
					break;
				}
			}
			LoadAndValidateData(data == null ? null : data.Items);
			return success;
		}

		SettingsData<T> DeserializeData(byte[] bytes, bool compressed)
		{
			if (compressed)
			{
				bytes = SettingsHelper.Decompress(bytes);
			}
			var data = Serializer.DeserializeFromXmlBytes<SettingsData<T>>(bytes);
			return data;
		}
	}
}
