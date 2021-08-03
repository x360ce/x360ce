using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary
{
	public partial class Helper : IDisposable
	{
		#region Control Resources

		/// <summary>
		/// Write application header title to CLI interface.
		/// </summary>
		public static void WriteAppHeader()
		{
			var assembly = Assembly.GetExecutingAssembly();
			WriteAppHeader(assembly);
		}

		public static void WriteAppHeader(Assembly assembly)
		{
			// Write title.
			// Microsoft (R) SQL Server Database Publishing Wizard 1.1.1.0
			// Copyright (C) Microsoft Corporation. All rights reserved.
			var a = new Configuration.AssemblyInfo(assembly);
			Console.WriteLine(a.Title + " " + a.Version.ToString());
			Console.WriteLine(a.Copyright);
			Console.WriteLine(a.Description);
		}

		/// <summary>
		/// Write help text from help.txt file.
		/// </summary>
		public static void WriteAppHelp()
		{
			Console.Write(GetResource<string>("Documents/Help.txt"));
		}

		/// <summary>
		/// Get resource from class *.resx file by full name.
		/// </summary>
		public static T FindResource<T>(string name, object o)
		{
			if (o == null)
				throw new ArgumentNullException(nameof(o));
			var resources = new System.ComponentModel.ComponentResourceManager(o.GetType());
			return (T)(resources.GetObject(name));
		}

		/// <summary>
		/// Find resource in all loaded assemblies if not specified by full or partial (EndsWith) name.
		/// Look inside "Build Action: Embedded Resource".
		/// </summary>
		public static T FindResource<T>(string name, params Assembly[] assemblies)
		{
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			if (assemblies.Length == 0)
				assemblies = GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var resourceNames = assembly.GetManifestResourceNames();
				foreach (var resourceName in resourceNames)
				{
					if (!resourceName.EndsWith(name))
						continue;
					var stream = assembly.GetManifestResourceStream(resourceName);
					return ConvertResource<T>(stream);
				}
			}
			return default(T);
		}

		/// <summary>
		/// Project Build Action: "Resource".
		/// </summary>
		public static string[] GetResourceKeys(Assembly assembly)
		{
			string resName = assembly.GetName().Name + ".g.resources";
			using (var stream = assembly.GetManifestResourceStream(resName))
			using (var reader = new System.Resources.ResourceReader(stream))
				return reader.Cast<System.Collections.DictionaryEntry>().Select(x => (string)x.Key).ToArray();
		}

		/// <summary>
		/// Project Build Action: "Resource".
		/// </summary>
		public static Stream GetResourceValue(string name, Assembly assembly)
		{
			string resName = assembly.GetName().Name + ".g.resources";
			using (var stream = assembly.GetManifestResourceStream(resName))
			using (var reader = new System.Resources.ResourceReader(stream))
			{
				var value = reader.Cast<System.Collections.DictionaryEntry>()
					.Where(x => (string)x.Key == name)
					.Select(x => x.Value).FirstOrDefault();
				return (Stream)value;
				//var path = string.Format("{0};component/{1}", assembly.GetName().Name, name);
				//var s = System.Windows.Application.GetResourceStream(new Uri(path, UriKind.Relative));
				//return s.Stream;
			}
		}

		/// <summary>
		/// Get embedded resource from type (*.resx file).
		/// </summary>
		public static T GetResource<TSource, T>(string name)
		{
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(T));
			return (T)resources.GetObject(name);
		}

		/// <summary>
		/// Get embedded resource by its full name.
		/// </summary>
		public static T GetResource<T>(string name, params Assembly[] assemblies)
		{
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			if (assemblies.Length == 0)
				assemblies = GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var resourceNames = assembly.GetManifestResourceNames();
				foreach (var resourceName in resourceNames)
				{
					if (resourceName != name)
						continue;
					var stream = assembly.GetManifestResourceStream(resourceName);
					return ConvertResource<T>(stream);
				}
			}
			throw new Exception("Resource not found");
		}

		static T ConvertResource<T>(Stream stream)
		{
			if (typeof(T) == typeof(Stream))
				return (T)(object)stream;
			var results = default(T);
			if (typeof(T) == typeof(string))
			{
				// File must contain Byte Order Mark (BOM) header in order for bytes correctly encoded to string.
				// If header is missing then get resource as byte[] type and encode manually.
				var streamReader = new StreamReader(stream, true);
				return (T)(object)streamReader.ReadToEnd();
			}
#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
			else if (typeof(T) == typeof(System.Drawing.Image) || typeof(T) == typeof(System.Drawing.Bitmap))
			{
				return (T)(object)System.Drawing.Image.FromStream(stream);
			}
#endif
			else
			{
				var bytes = new byte[stream.Length];
				stream.Read(bytes, 0, (int)stream.Length);
				results = (T)(object)bytes;
			}
			return results;
		}

		static Assembly[] GetAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
			var orderDesc = new Assembly[]
			{
				Assembly.GetExecutingAssembly(),
				Assembly.GetCallingAssembly(),
				Assembly.GetEntryAssembly(),
			};
			// Move assemblies to top.
			foreach (var item in orderDesc)
			{
				if (assemblies.Contains(item))
				{
					assemblies.Remove(item);
					assemblies.Insert(0, item);
				}
			}
			return assemblies.ToArray();
		}

		#endregion

#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework

		#region Disk Activity

		// Sometimes it is good to pause if there is too much disk activity.
		// By letting windows/SQL to commit all changes to disk we can improve speed.

		private PerformanceCounter _diskReadCounter = new PerformanceCounter();
		private PerformanceCounter _diskWriteCounter = new PerformanceCounter();

		private static double GetCounterValue(PerformanceCounter pc, string categoryName, string counterName, string instanceName)
		{
			pc.CategoryName = categoryName;
			pc.CounterName = counterName;
			pc.InstanceName = instanceName;
			return pc.NextValue();
		}

		public enum DiskData { ReadAndWrite, Read, Write };

		public double GetDiskData(DiskData dd)
		{
			return dd == DiskData.Read ?
						GetCounterValue(_diskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") :
						dd == DiskData.Write ?
						GetCounterValue(_diskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") :
						dd == DiskData.ReadAndWrite ?
						GetCounterValue(_diskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") +
						GetCounterValue(_diskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") :
					0;
		}

		#endregion

#endif

		#region Comparisons

		private static Regex _GuidRegex;
		public static Regex GuidRegex
		{
			get
			{
				if (_GuidRegex == null)
				{
					_GuidRegex = new Regex(
				"^[A-Fa-f0-9]{32}$|" +
				"^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
				"^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$");
				}
				return _GuidRegex;
			}

		}

		public static bool IsGuid(string s)
		{
			return string.IsNullOrEmpty(s)
				? false
				: GuidRegex.IsMatch(s);
		}

		#endregion

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{

#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework

				// Free managed resources.
				if (_diskReadCounter != null)
				{
					_diskReadCounter.Dispose();
					_diskReadCounter = null;
				}
				if (_diskWriteCounter != null)
				{
					_diskWriteCounter.Dispose();
					_diskWriteCounter = null;
				}
#endif
			}
		}

		#endregion

	}

}
