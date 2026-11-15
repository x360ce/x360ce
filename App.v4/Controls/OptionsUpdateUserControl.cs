using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.App.Forms;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The Options page that finds a newer release, fetches it, checks it and swaps it in. A page
	/// rather than a window, so nothing pops up. Each step writes one line and stops the moment a
	/// check fails, so the log reads as the reason nothing was installed.
	/// </summary>
	public partial class OptionsUpdateUserControl : UserControl
	{
		public OptionsUpdateUserControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			FormsTheme.IndentUnder(PrivacyLabel, CheckForUpdatesCheckBox);
			var process = System.Diagnostics.Process.GetCurrentProcess();
			processFileName = process.MainModule.FileName;
		}

		public void UpdateSettingsMap()
		{
			SettingsManager.LoadAndMonitor(x => x.CheckForUpdates, CheckForUpdatesCheckBox);
			SettingsManager.LoadAndMonitor(x => x.CheckUpdateSignature, CheckDigitalSignatureCheckBox);
			SettingsManager.LoadAndMonitor(x => x.CheckUpdateVersion, CheckVersionCheckBox);
		}

		readonly List<string> _lines = new List<string>();
		internal string processFileName;

		/// <summary>Starts a new line of the log, shown by the next <see cref="Render"/>.</summary>
		/// <remarks>The steps after the download write here on a worker while the window's thread leaves the log alone.</remarks>
		void Log(string format, params object[] args)
		{
			_lines.Add(args.Length == 0 ? format : string.Format(format, args));
		}

		/// <summary>Continues the last line of the log, the way a step reports its outcome.</summary>
		void Append(string text)
		{
			if (_lines.Count == 0)
				_lines.Add("");
			_lines[_lines.Count - 1] += text;
		}

		/// <summary>Shows the log. On the window's thread only.</summary>
		void Render()
		{
			LogTextBox.Lines = _lines.ToArray();
			LogTextBox.SelectionStart = LogTextBox.TextLength;
			LogTextBox.ScrollToCaret();
		}

		/// <summary>Shows the log and lets the person check again.</summary>
		void Finish()
		{
			Render();
			CheckButton.Enabled = true;
		}

		private void CheckButton_Click(object sender, EventArgs e)
		{
			_lines.Clear();
			CheckButton.Enabled = false;
			Step1CheckOnline();
		}

		void Step1CheckOnline()
		{
			Log("Check Online...");
			Render();
			var o = SettingsManager.Options;
			var client = new UpdateClient(typeof(OptionsUpdateUserControl).Assembly.GetName().Version);
			// Asked now, so asked afresh: a stored tag could have been stored by another version of the program.
			Task.Run(() => client.Check(null)).ContinueWith(t =>
			{
				var check = t.Result;
				o.LastUpdateCheck = DateTime.Now;
				if (check.ETag != null)
					o.UpdateEtag = check.ETag;
				switch (check.Outcome)
				{
					case UpdateOutcome.Failed:
						Append(" Failed: " + check.Error);
						Finish();
						return;
					case UpdateOutcome.Current:
						Append(" No new updates.");
						Finish();
						return;
				}
				Append(string.Format(" Version {0} is available.", check.Version));
				if (!string.IsNullOrEmpty(check.Release.Page))
					Log("{0}", check.Release.Page);
				Step2Download(client, check);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		/// <summary>
		/// Downloads the release and checks what it holds on a worker, so the window goes on drawing while the file arrives
		/// and Windows reads its signature, then shows what each step found at once and installs the program it passed.
		/// </summary>
		internal void Step2Download(UpdateClient client, UpdateCheck check)
		{
			Log("Download... {0}", check.Release.Url);
			Render();
			// The boxes are read here, on the window's thread, for the worker.
			var checkSignature = CheckDigitalSignatureCheckBox.Checked;
			var checkVersion = CheckVersionCheckBox.Checked;
			Task.Run(() => Step3ExtractAndCheck(client, check, checkSignature, checkVersion)).ContinueWith(t =>
			{
				string updateFileName = null;
				if (t.IsFaulted)
					Append(" Failed: " + t.Exception.GetBaseException().Message);
				else
					updateFileName = t.Result;
				Render();
				// Restarting closes the window: nothing is left to show.
				if (updateFileName != null && Step5ReplaceFiles(updateFileName))
					return;
				Finish();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		/// <summary>
		/// Downloads the release, unpacks its program beside the running one from the download in memory, and checks the
		/// program's signature and version. On a worker: touches no control.
		/// </summary>
		/// <returns>The unpacked program when it is the one to install; otherwise null, and the program is deleted.</returns>
		string Step3ExtractAndCheck(UpdateClient client, UpdateCheck check, bool checkSignature, bool checkVersion)
		{
			byte[] data;
			try
			{
				data = client.Download(check);
			}
			catch (Exception ex)
			{
				Append(" Failed: " + ex.GetBaseException().Message);
				return null;
			}
			Append(string.Format(" Done, {0:N1} MB", data.Length / 1024m / 1024m));
			Log("Size and SHA-256 match what GitHub publishes for the release.");
			var updateFileName = processFileName + ".tmp";
			var install = false;
			try
			{
				Log("Unpacking...");
				if (!Unpack(data, updateFileName))
				{
					Append(" Failed: the download holds no x360ce.exe.");
					return null;
				}
				Append(" Done");
				install = Passes(check, updateFileName, checkSignature, checkVersion);
				return install ? updateFileName : null;
			}
			finally
			{
				// A program that is not installed is not left beside the running one.
				if (!install && File.Exists(updateFileName))
					File.Delete(updateFileName);
			}
		}

		/// <summary>Writes the program a release's zip holds to a file, read from the download in memory.</summary>
		static bool Unpack(byte[] zip, string fileName)
		{
			using (var storer = ZipStorer.Open(new MemoryStream(zip, false), FileAccess.Read))
			{
				var entry = storer.ReadCentralDir().FirstOrDefault(x => string.Equals(x.FilenameInZip, "x360ce.exe", StringComparison.OrdinalIgnoreCase));
				return entry != null && storer.ExtractFile(entry, fileName);
			}
		}

		/// <summary>Whether the unpacked program passes the checks the page has ticked: its signature, and a version newer than this one.</summary>
		bool Passes(UpdateCheck check, string updateFileName, bool checkSignature, bool checkVersion)
		{
			if (checkSignature)
			{
				Log("Check Digital Signature...");
				Exception error;
				if (!CertificateHelper.IsSignedAndTrusted(updateFileName, out error))
				{
					Append(error == null ? " Failed" : string.Format(" Failed: {0}", error.Message));
					return false;
				}
				Append(" Done");
			}
			if (checkVersion)
			{
				Version processVersion, updatedVersion;
				if (!JocysCom.ClassLibrary.Helper.TryParseVersion(System.Diagnostics.FileVersionInfo.GetVersionInfo(processFileName).FileVersion, out processVersion)
					|| !JocysCom.ClassLibrary.Helper.TryParseVersion(System.Diagnostics.FileVersionInfo.GetVersionInfo(updateFileName).FileVersion, out updatedVersion))
				{
					Log("The version of the program or of the update cannot be read. Skip Update.");
					return false;
				}
				Log("Current version: {0}", processVersion);
				Log("Updated version: {0}", updatedVersion);
				if (processVersion == updatedVersion)
				{
					Log("Versions are the same. Skip Update.");
					return false;
				}
				if (processVersion > updatedVersion)
				{
					Log("Remote version is older. Skip Update.");
					return false;
				}
				// A release announcing one version and shipping another installs nowhere.
				if (!UpdateClient.Announced(check, updatedVersion))
				{
					Log("The release announces {0} but the file is {1}. Skip Update.", check.Version, updatedVersion);
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Whether emulated controllers are feeding a game right now. Replacing the program
		/// restarts it, and the game loses them; that is the player's call to make.
		/// </summary>
		static bool GameIsActive()
		{
			var game = SettingsManager.CurrentGame;
			if (game == null || !SettingsManager.Options.XInputEnabled)
				return false;
			var name = Path.GetFileName(game.FileName ?? "");
			return !string.Equals(name, "x360ce.exe", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Puts the new program in the running one's place and restarts, unless the person waits for the game to close.</summary>
		/// <returns>True when the program restarts.</returns>
		bool Step5ReplaceFiles(string updateFileName)
		{
			try
			{
				if (GameIsActive())
				{
					var answer = MessageBoxForm.Show(
						"A game is using the emulated controllers. Installing the update restarts this program and the game loses them.\r\n\r\nInstall now?",
						"X360CE - Update", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
					if (answer != DialogResult.Yes)
					{
						Log("Update postponed while the game runs. Press Check now again when it has closed.");
						return false;
					}
				}
				// The running program cannot be deleted, but it can be renamed; the new start deletes it (DeleteLeftovers).
				var bak = processFileName + ".bak";
				Log("Renaming running process...");
				try
				{
					if (File.Exists(bak))
						File.Delete(bak);
					File.Move(processFileName, bak);
					try
					{
						File.Move(updateFileName, processFileName);
					}
					catch
					{
						// Put back, so there is still a program to start.
						File.Move(bak, processFileName);
						throw;
					}
				}
				catch (Exception ex)
				{
					Append(" Failed: " + ex.Message);
					return false;
				}
				Append(" Done");
				Log("Restarting...");
				Render();
				Application.Restart();
				return true;
			}
			finally
			{
				// Postponed or failed: the unpacked program is not left beside the running one.
				if (File.Exists(updateFileName))
					File.Delete(updateFileName);
			}
		}

		/// <summary>
		/// Deletes what an update leaves beside the program: the program it replaced, kept as ".bak" because a running
		/// program cannot be deleted, and the download and unpacked copy older versions left as ".zip" and ".tmp". Called
		/// as the program starts, on a worker. The copy that installed the update may still be closing and holding its
		/// file, so that one is tried again for a while.
		/// </summary>
		/// <param name="programFile">The running program.</param>
		public static Task DeleteLeftovers(string programFile)
		{
			return Task.Run(async () =>
			{
				TryDelete(programFile + ".zip");
				TryDelete(programFile + ".tmp");
				var replaced = programFile + ".bak";
				for (var i = 0; i < 30 && File.Exists(replaced) && !TryDelete(replaced); i++)
					await Task.Delay(1000);
			});
		}

		/// <summary>Deletes a file, if it is there, and says whether it is gone: false while another program holds it.</summary>
		static bool TryDelete(string path)
		{
			try
			{
				File.Delete(path);
				return true;
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}
	}
}
