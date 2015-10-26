using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using x360ce.Engine;
using x360ce.App.Controls;
using System.Diagnostics;
using System.Data.Objects.DataClasses;

namespace x360ce.App
{
	[Serializable, XmlRoot("Data")]
	public class SettingsData<T>
	{

		public SettingsData()
		{
			Items = new SortableBindingList<T>();
		}

		public SortableBindingList<T> Items;

		[XmlAttribute]
		public int Version { get; set; }

		/// <summary>
		/// File Version.
		/// </summary>
		int _CurrentVersion = 4;

		public bool IsValidVersion()
		{
			return Version == _CurrentVersion;
		}

		[NonSerialized]
		FileInfo _XmlFile;
		object InitialFileLock = new object();
		[XmlIgnore]
		public FileInfo XmlFile
		{
			get
			{
				lock (InitialFileLock)
				{
					if (_XmlFile == null)
					{
						var folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\X360CE";
						var file = string.Format("x360ce.{0}.xml", typeof(T).Name);
						var path = folder + "\\" + file;
						_XmlFile = new FileInfo(path);
					}
					return _XmlFile;
				}
			}
		}

		object saveReadFileLock = new object();

		public void Save()
		{
			lock (saveReadFileLock)
			{
				Version = _CurrentVersion;
				for (int i = 0; i < Items.Count; i++)
				{
					var o = Items[i] as EntityObject;
					if (o != null) o.EntityKey = null;
				}
				Serializer.SerializeToXmlFile(this, XmlFile.FullName, Encoding.UTF8);
			}
		}

		public void Load()
		{
			bool settingsLoaded = false;
			var settingsFi = new FileInfo(XmlFile.FullName);
			// If configuration file exists then...
			if (settingsFi.Exists)
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
							data = Serializer.DeserializeFromXmlFile<SettingsData<T>>(XmlFile.FullName);
							if (data != null && data.IsValidVersion())
							{
								Items.Clear();
								if (data != null) for (int i = 0; i < data.Items.Count; i++) Items.Add(data.Items[i]);
								settingsLoaded = true;
							}
							break;
						}
						catch (Exception)
						{
							var form = new MessageBoxForm();
							var backupFile = XmlFile.FullName + ".bak";
							form.StartPosition = FormStartPosition.CenterParent;
							var result = form.ShowForm(
								"User " + typeof(T).Name + " file has become corrupted.\r\n" +
								"Program must reset your user " + typeof(T).Name + " in order to continue.\r\n\r\n" +
								"   Click [Yes] to reset your user " + typeof(T).Name + " and continue.\r\n" +
								"   Click [No] if you wish to attempt manual repair.\r\n\r\n" +
								typeof(T).Name + " File: " + XmlFile.FullName,
								"Corrupt user " + typeof(T).Name + " of " + Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
							if (result == DialogResult.Yes)
							{
								if (File.Exists(backupFile))
								{
									File.Copy(backupFile, XmlFile.FullName, true);
									settingsFi.Refresh();
								}
								else
								{
									File.Delete(XmlFile.FullName);
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
				// Get internal resources.
				var resource = EngineHelper.GetResource("x360ce_" + typeof(T).Name + ".xml.gz");
				// If internal preset was found.
				if (resource != null)
				{
					var sr = new StreamReader(resource);
					var compressedBytes = default(byte[]);
					using (var memstream = new MemoryStream())
					{
						sr.BaseStream.CopyTo(memstream);
						compressedBytes = memstream.ToArray();
					}
					var bytes = EngineHelper.Decompress(compressedBytes);
					var xml = Encoding.UTF8.GetString(bytes);
					var data = Serializer.DeserializeFromXmlString<SettingsData<T>>(xml);
					Items.Clear();
					for (int i = 0; i < data.Items.Count; i++) Items.Add(data.Items[i]);
				}
			}
			if (!settingsLoaded)
			{
				Save();
			}
		}

	}
}
