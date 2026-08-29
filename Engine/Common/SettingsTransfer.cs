using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace x360ce.Engine
{

	/// <summary>How settings should end up in the folder a person chose.</summary>
	public enum SettingsTransferMode
	{
		/// <summary>Both folders keep a copy, and they drift apart from now on.</summary>
		Copy,
		/// <summary>One copy, in the chosen folder. Nothing is deleted until every file is proven to have arrived.</summary>
		Move,
		/// <summary>One copy, in the chosen folder, with the old path pointing at it so both still work.</summary>
		Link,
	}

	/// <summary>What a transfer did, and what it refused to do.</summary>
	public class SettingsTransferResult
	{
		public bool Success { get; set; }
		public int Copied { get; set; }
		public int Verified { get; set; }
		public int Removed { get; set; }
		public string Problem { get; set; }
		public List<string> Mismatched { get; private set; }

		public SettingsTransferResult()
		{
			Mismatched = new List<string>();
		}
	}

	/// <summary>
	/// Moves settings between the folders they may live in, without ever being the
	/// reason somebody loses them.
	/// </summary>
	/// <remarks>
	/// Deleting is the only step that cannot be undone, so it is the last one and it
	/// happens only against proof: every file is copied, then every copy is compared
	/// with its original by checksum, and the originals are removed only when all of
	/// them match. A copy that reports success because the file system did not
	/// complain is not proof - a half-written file has a size and a date like any
	/// other.
	/// </remarks>
	public static class SettingsTransfer
	{

		private static string Checksum(string path)
		{
			using (var sha = SHA256.Create())
			using (var stream = File.OpenRead(path))
				return BitConverter.ToString(sha.ComputeHash(stream));
		}

		/// <summary>
		/// Carries the settings from one folder to another in the way asked for.
		/// </summary>
		public static SettingsTransferResult Run(string sourceFolder, string targetFolder, SettingsTransferMode mode)
		{
			var result = new SettingsTransferResult();
			try
			{
				var source = new DirectoryInfo(sourceFolder);
				var target = new DirectoryInfo(targetFolder);
				if (!source.Exists)
				{
					// Nothing to carry is not a failure: the chosen folder simply starts
					// out as the only one.
					result.Success = true;
					return result;
				}
				if (string.Equals(source.FullName.TrimEnd('\\'), target.FullName.TrimEnd('\\'),
					StringComparison.OrdinalIgnoreCase))
				{
					result.Success = true;
					return result;
				}
				if (!target.Exists)
					target.Create();

				var files = source.GetFiles("*.xml");

				// Step one: copy. A file already in the target is only left alone when it
				// is the same file; one that differs is somebody else's and stops the
				// transfer rather than being overwritten.
				foreach (var file in files)
				{
					var to = Path.Combine(target.FullName, file.Name);
					if (File.Exists(to))
					{
						if (Checksum(file.FullName) != Checksum(to))
						{
							result.Problem = "The chosen folder already holds a different " +
								file.Name + ". Nothing was changed.";
							return result;
						}
						continue;
					}
					file.CopyTo(to);
					result.Copied++;
				}

				// Step two: prove it. Every original must have a copy that matches it,
				// byte for byte, before anything is removed.
				foreach (var file in files)
				{
					var to = Path.Combine(target.FullName, file.Name);
					if (!File.Exists(to) || Checksum(file.FullName) != Checksum(to))
					{
						result.Mismatched.Add(file.Name);
						continue;
					}
					result.Verified++;
				}
				if (result.Mismatched.Count > 0)
				{
					result.Problem = result.Mismatched.Count +
						" file(s) did not arrive intact, so nothing was removed: " +
						string.Join(", ", result.Mismatched.ToArray());
					return result;
				}

				if (mode == SettingsTransferMode.Copy)
				{
					result.Success = true;
					return result;
				}

				// Step three, and only now: remove the originals.
				foreach (var file in files)
				{
					file.Delete();
					result.Removed++;
				}

				if (mode == SettingsTransferMode.Move)
				{
					result.Success = true;
					return result;
				}

				// A link leaves the old path working, so a version that only knows that
				// path keeps reading and writing the same settings.
				result.Problem = CreateJunction(source.FullName, target.FullName);
				result.Success = result.Problem == null;
				return result;
			}
			catch (Exception ex)
			{
				result.Problem = ex.Message;
				return result;
			}
		}

		/// <summary>
		/// Points one folder at another. Returns null when done, or why it could not be.
		/// </summary>
		/// <remarks>
		/// A junction rather than a symbolic link: both do the job for a folder on the
		/// same machine, and a junction is the one Windows allows without administrator
		/// rights or developer mode. Asking a person to elevate to tidy their own
		/// settings would defeat the point.
		/// </remarks>
		private static string CreateJunction(string linkPath, string targetPath)
		{
			try
			{
				var folder = new DirectoryInfo(linkPath);
				if (folder.Exists)
				{
					if (folder.GetFileSystemInfos().Any())
						return "The old folder still holds files, so it was left as it is.";
					folder.Delete();
				}
				var start = new ProcessStartInfo("cmd.exe",
					"/c mklink /J \"" + linkPath.TrimEnd('\\') + "\" \"" + targetPath.TrimEnd('\\') + "\"")
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};
				using (var process = Process.Start(start))
				{
					var error = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
					process.WaitForExit();
					if (process.ExitCode != 0 || !Directory.Exists(linkPath))
						return "The link could not be made: " + error.Trim();
				}
				return null;
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}

	}
}
