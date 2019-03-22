using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

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
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			WriteAppHeader(assembly);
		}

		public static void WriteAppHeader(System.Reflection.Assembly assembly)
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
			Console.Write(GetTextResource("Documents/Help.txt"));
		}

		public static string GetTextResource(string name)
		{
			return GetResource<string>(name);
		}

		public static string GetTextResource(Assembly assembly, string name)
		{
			return GetResource<string>(assembly, name);
		}

		/// <summary>
		/// Find resource in all loaded assemblies.
		/// </summary>
		public static T FindResource<T>(string name)
		{
			object results = default(T);
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				T o = FindResource<T>(assembly, name);
				if (o != null) return o;
			}
			return default(T);
		}

		/// <summary>
		/// Get resource from class *.resx file.
		/// </summary>
		public static T FindResource<T>(string name, object o)
		{
			var resources = new System.ComponentModel.ComponentResourceManager(o.GetType());
			return (T)(resources.GetObject(name));
		}

		/// <summary>
		/// Find resource in all loaded assemblies.
		/// </summary>
		public static T FindResource<T>(Assembly assembly, string name)
		{
			T results = default(T);
			if (assembly.IsDynamic)
				return results;
			var resourceNames = assembly.GetManifestResourceNames();
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			foreach (var resourceName in resourceNames)
			{
				if (resourceName.EndsWith(name))
				{
					results = GetResource<T>(assembly, resourceName);
					return (T)results;
				}
			}
			return default(T);
		}

		public static T GetResource<T>(string name)
		{
			object results;
			System.Reflection.Assembly assembly;
			// Look inside calling assembly.
			assembly = System.Reflection.Assembly.GetCallingAssembly();
			results = GetResource<T>(assembly, name);
			if (results != null) return (T)results;
			// Look inside executing assembly (class library of this method).
			assembly = System.Reflection.Assembly.GetExecutingAssembly();
			results = GetResource<T>(assembly, name);
			return (results == null) ? default(T) : (T)results;
		}

		public static T GetResource<T>(Assembly assembly, string name)
		{
			object results = default(T);
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			var assemblyPrefix = assembly.GetName().Name;
			System.IO.Stream stream = assembly.GetManifestResourceStream(name);
			if (stream != null)
			{
				if (typeof(T) == typeof(System.Drawing.Image)
					|| typeof(T) == typeof(System.Drawing.Bitmap)
				)
				{
					return (T)(object)System.Drawing.Image.FromStream(stream);
				}
				else if (typeof(T) == typeof(string))
				{
					System.IO.StreamReader streamReader = new System.IO.StreamReader(stream);
					return (T)(object)streamReader.ReadToEnd();
				}
				else
				{
					byte[] bytes = new byte[stream.Length];
					stream.Read(bytes, 0, (int)stream.Length);
					results = bytes;
				}
			}
			return (T)(object)results;
		}

		#endregion

		#region Disk Activity

		// Sometimes it is good to pause if there is too much disk activity.
		// By letting windows/SQL to commit all changes to disk we can improve speed.

		PerformanceCounter _diskReadCounter = new PerformanceCounter();
		PerformanceCounter _diskWriteCounter = new PerformanceCounter();

		double GetCounterValue(PerformanceCounter pc, string categoryName, string counterName, string instanceName)
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
			}
		}

		#endregion

	}

}
