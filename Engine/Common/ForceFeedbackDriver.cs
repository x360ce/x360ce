using Microsoft.Win32;
using SharpDX.DirectInput;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace x360ce.Engine
{
	/// <summary>The force feedback driver a device uses, and what the program must not ask of it.</summary>
	/// <remarks>
	/// DirectInput names each device's force feedback driver by the class identifier of a COM
	/// object, and Windows names the file behind that identifier in the registry. The file is what
	/// tells drivers apart: two pads from different makers can share one, and a fault in it is a
	/// fault in every pad that uses it.
	/// </remarks>
	public static class ForceFeedbackDriver
	{
		static readonly ConcurrentDictionary<Guid, string> files = new ConcurrentDictionary<Guid, string>();

		/// <summary>The file of the driver with this identifier, or empty when the device has none or Windows does not say.</summary>
		public static string FileOf(Guid driver)
		{
			if (driver == Guid.Empty)
				return "";
			return files.GetOrAdd(driver, Find);
		}

		static string Find(Guid driver)
		{
			// Registered in the view of its own bitness, which is not necessarily this program's.
			foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
			{
				try
				{
					using (var root = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view))
					using (var key = root.OpenSubKey(@"CLSID\{" + driver + @"}\InProcServer32"))
					{
						var file = key?.GetValue("") as string;
						if (!string.IsNullOrEmpty(file))
							return Environment.ExpandEnvironmentVariables(file);
					}
				}
				catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException || ex is IOException)
				{
				}
			}
			return "";
		}

		/// <summary>Whether the driver takes down the whole program when asked for a sawtooth.</summary>
		/// <remarks>
		/// The generic "USB Vibration" gamepad driver, EZFRD, found under several makers' names, plays
		/// sine and constant forces but faults inside itself on a sawtooth: the pad vibrates without
		/// stopping and the program dies with an access violation no code can catch. Reported on a
		/// Havit HV-G69, also sold as the SPEEDLINK STRIKE.
		/// </remarks>
		public static bool FailsOnSawtooth(string driverFile)
		{
			var name = Path.GetFileName(driverFile ?? "");
			return string.Equals(name, "EZFRD64.DLL", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "EZFRD32.DLL", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>The DirectInput effect to play for an effect type on a device with this driver.</summary>
		/// <remarks>A driver that faults on a sawtooth gets a sine instead, which it plays, rather than a setting that ends the program.</remarks>
		public static Guid EffectFor(ForceEffectType type, string driverFile)
		{
			if (type.HasFlag(ForceEffectType.PeriodicSine))
				return EffectGuid.Sine;
			if (type.HasFlag(ForceEffectType.PeriodicSawtooth))
				return FailsOnSawtooth(driverFile) ? EffectGuid.Sine : EffectGuid.SawtoothDown;
			return EffectGuid.ConstantForce;
		}
	}
}
