using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace x360ce.Engine
{

	/// <summary>
	/// A folder settings can live in, and whether this user can actually write there.
	/// </summary>
	/// <remarks>
	/// There is more than one sensible place, and which one works depends on the
	/// machine rather than on the program. C:\ProgramData looks writable to everyone
	/// and mostly is: any user may create a file there. What they may not do is
	/// change a file somebody else created. The permission that grants write is
	/// inherited by folders and not by files, so an existing settings file belongs to
	/// whoever wrote it first, through CREATOR OWNER. On a machine where an installer
	/// or another account wrote the settings, every later user can read them and none
	/// can save.
	///
	/// So the question is never "is this folder writable" but "can this user write
	/// the files that are already in it", which is what is tested here.
	/// </remarks>
	public class SettingsLocation
	{

		/// <summary>Shown to a person choosing where settings should live.</summary>
		public string Name { get; private set; }

		/// <summary>How the choice is written down. Never shown, never translated.</summary>
		public string Key { get; private set; }

		/// <summary>The folder itself.</summary>
		public string Path { get; private set; }

		/// <summary>True when this folder already holds settings.</summary>
		public bool HasSettings
		{
			get { return SettingsFiles().Any(); }
		}

		/// <summary>Why this folder cannot be used, or null when it can.</summary>
		/// <remarks>
		/// Tested by doing rather than by reading permissions. Rights come from the
		/// folder, the files, the account, group membership and any policy on top, and
		/// the only answer that matters is what happens when the program writes.
		/// </remarks>
		public string WriteProblem
		{
			get
			{
				try
				{
					var folder = new DirectoryInfo(SettingsFolder);
					// A folder that is not there yet is usable when it can be made. Asking
					// must leave the disk as it was: a location a person only looked at
					// should not appear on it.
					var created = false;
					if (!folder.Exists)
					{
						folder.Create();
						created = true;
					}
					var probe = System.IO.Path.Combine(folder.FullName, "write.test.tmp");
					File.WriteAllText(probe, "");
					File.Delete(probe);
					if (created)
					{
						folder.Delete(true);
						return null;
					}
					// The folder allowing new files says nothing about the files in it.
					// This is the case that fails on a shared machine.
					foreach (var file in SettingsFiles())
					{
						using (File.Open(file.FullName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
						{
						}
					}
					return null;
				}
				catch (Exception ex)
				{
					return ex.Message;
				}
			}
		}

		/// <summary>True when settings can be saved here.</summary>
		public bool CanWrite
		{
			get { return WriteProblem == null; }
		}

		/// <summary>The folder the settings files themselves sit in.</summary>
		private string SettingsFolder
		{
			get { return System.IO.Path.Combine(Path, "Settings"); }
		}

		private IEnumerable<FileInfo> SettingsFiles()
		{
			var folder = new DirectoryInfo(SettingsFolder);
			if (!folder.Exists)
				return new FileInfo[0];
			return folder.GetFiles("*.xml");
		}

		#region The places settings can be

		/// <summary>Company and product as the assembly declares them.</summary>
		private static string CompanyProduct()
		{
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
			var product = assembly.GetCustomAttribute<AssemblyProductAttribute>();
			var parts = new[]
			{
				company == null ? "" : company.Company,
				product == null ? "" : product.Product,
			};
			return System.IO.Path.Combine(parts.Where(x => !string.IsNullOrEmpty(x)).ToArray());
		}

		/// <summary>Where settings have always been kept, and still are by default.</summary>
		public static SettingsLocation Machine()
		{
			return new SettingsLocation
			{
				Key = "Machine",
				Name = "All users on this computer",
				Path = System.IO.Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "X360CE"),
			};
		}

		/// <summary>This user's own folder, which nobody else's permissions can close.</summary>
		public static SettingsLocation User()
		{
			return new SettingsLocation
			{
				Key = "User",
				Name = "This user only",
				Path = System.IO.Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyProduct()),
			};
		}

		/// <summary>A folder beside the program, which is how it is carried on a stick.</summary>
		public static SettingsLocation Portable(string programFolder)
		{
			return new SettingsLocation
			{
				Key = "Portable",
				Name = "Beside the program",
				Path = System.IO.Path.Combine(programFolder, "x360ce"),
			};
		}

		/// <summary>
		/// Every place settings may be kept, in the order they are preferred.
		/// </summary>
		/// <remarks>
		/// The machine folder comes before the user folder on purpose, and stays there
		/// until versions that know about both have replaced the ones that do not. A
		/// release that started saving somewhere new would look, to an older copy of
		/// the program on the same machine, exactly like a user who had lost every
		/// setting they had.
		/// </remarks>
		public static SettingsLocation[] All(string programFolder)
		{
			var all = new List<SettingsLocation>();
			if (!string.IsNullOrEmpty(programFolder))
				all.Add(Portable(programFolder));
			all.Add(Machine());
			all.Add(User());
			return all.ToArray();
		}

		#endregion

		#region The choice a person made

		/// <summary>
		/// Where the choice itself is kept: in this user's own folder, always.
		/// </summary>
		/// <remarks>
		/// It cannot live with the settings, because it is what decides where those
		/// are. The user folder is the one place that is always readable and always
		/// writable by whoever is running the program, which is exactly what a pointer
		/// read before anything else needs to be.
		/// </remarks>
		private static string PreferenceFile
		{
			get { return System.IO.Path.Combine(User().Path, "settings-location.txt"); }
		}

		/// <summary>The location a person chose, or null when they have not chosen.</summary>
		public static string Preference
		{
			get
			{
				try
				{
					return File.Exists(PreferenceFile)
						? File.ReadAllText(PreferenceFile).Trim()
						: null;
				}
				catch
				{
					// An unreadable preference is the same as none: fall back to the
					// order below rather than refusing to start.
					return null;
				}
			}
			set
			{
				var folder = System.IO.Path.GetDirectoryName(PreferenceFile);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				File.WriteAllText(PreferenceFile, value ?? "");
			}
		}

		/// <summary>
		/// Copies the settings that are in use into this folder, so choosing a location
		/// carries the configuration with it.
		/// </summary>
		/// <remarks>
		/// Without this, choosing a folder that happens to be empty would look exactly
		/// like every setting having been forgotten. Files already in the target are
		/// left alone: they belong to whoever put them there.
		/// </remarks>
		public int CopyFrom(SettingsLocation source)
		{
			if (source == null || string.Equals(source.Path, Path, StringComparison.OrdinalIgnoreCase))
				return 0;
			var target = new DirectoryInfo(SettingsFolder);
			if (!target.Exists)
				target.Create();
			var copied = 0;
			foreach (var file in source.SettingsFiles())
			{
				var to = System.IO.Path.Combine(target.FullName, file.Name);
				if (File.Exists(to))
					continue;
				file.CopyTo(to);
				copied++;
			}
			return copied;
		}

		#endregion

		/// <summary>
		/// The folder to use: what was chosen if it can be used, then the first that
		/// already holds settings and can be written, then the first that holds them at
		/// all, then the first that can be written.
		/// </summary>
		/// <remarks>
		/// Settings that exist win over settings that could exist, because a folder
		/// chosen for being writable while somebody's configuration sits unread in
		/// another one is indistinguishable, to them, from having lost it.
		///
		/// A folder that holds settings but cannot be written is still chosen. Reading
		/// what is there and saying why saving fails is worth more than starting again
		/// somewhere else without saying so.
		/// </remarks>
		public static SettingsLocation Resolve(string programFolder)
		{
			var all = All(programFolder);
			// A choice outranks the order below, which is the point of making one. It
			// is ignored only when the folder it names cannot be written, because
			// honouring it then would mean failing every save from now on.
			var chosen = Preference;
			if (!string.IsNullOrEmpty(chosen))
			{
				var wanted = all.FirstOrDefault(x =>
					string.Equals(x.Key, chosen, StringComparison.OrdinalIgnoreCase) && x.CanWrite);
				if (wanted != null)
					return wanted;
			}
			return all.FirstOrDefault(x => x.HasSettings && x.CanWrite)
				?? all.FirstOrDefault(x => x.HasSettings)
				?? all.FirstOrDefault(x => x.CanWrite)
				?? Machine();
		}

		public override string ToString()
		{
			return Name + ": " + Path;
		}

	}
}
