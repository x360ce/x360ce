using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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
			var process = System.Diagnostics.Process.GetCurrentProcess();
			processFileName = process.MainModule.FileName;
		}

		public void UpdateSettingsMap()
		{
			SettingsManager.LoadAndMonitor(x => x.CheckForUpdates, CheckForUpdatesCheckBox);
		}

		readonly List<string> _lines = new List<string>();
		string processFileName;
		UpdateCheck _Check;

		/// <summary>Starts a new line of the log.</summary>
		void Log(string format, params object[] args)
		{
			_lines.Add(args.Length == 0 ? format : string.Format(format, args));
			Render();
		}

		/// <summary>Continues the last line of the log, the way a step reports its outcome.</summary>
		void Append(string text)
		{
			if (_lines.Count == 0)
				_lines.Add("");
			_lines[_lines.Count - 1] += text;
			Render();
		}

		void Render()
		{
			LogTextBox.Lines = _lines.ToArray();
			LogTextBox.SelectionStart = LogTextBox.TextLength;
			LogTextBox.ScrollToCaret();
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
			var o = SettingsManager.Options;
			var client = new UpdateClient(new Version(Application.ProductVersion));
			var etag = o.UpdateEtag;
			Task.Run(() => client.Check(etag)).ContinueWith(t =>
			{
				var check = t.Result;
				o.LastUpdateCheck = DateTime.Now;
				if (check.ETag != null)
					o.UpdateEtag = check.ETag;
				switch (check.Outcome)
				{
					case UpdateOutcome.Failed:
						Append(" Failed: " + check.Error);
						CheckButton.Enabled = true;
						return;
					case UpdateOutcome.Current:
						Append(" No new updates.");
						CheckButton.Enabled = true;
						return;
				}
				Append(string.Format(" Version {0} is available.", check.Version));
				if (check.Manifest != null && !string.IsNullOrEmpty(check.Manifest.Notes))
					Log("{0}", check.Manifest.Notes);
				_Check = check;
				Step2Download(client, check);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		void Step2Download(UpdateClient client, UpdateCheck check)
		{
			Log("Download... {0}", UpdateClient.ZipUrl);
			Task.Run(() => client.Download(check)).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					Append(" Failed: " + t.Exception.GetBaseException().Message);
					CheckButton.Enabled = true;
					return;
				}
				var data = t.Result;
				Append(string.Format(" Done, {0:N1} MB", data.Length / 1024m / 1024m));
				if (check.Manifest != null)
					Log("Size and SHA-256 match the release manifest.");
				Log("Saving File...");
				var zipFileName = processFileName + ".zip";
				File.WriteAllBytes(zipFileName, data);
				Append(" Done");
				Step3ExtractAndCheck(zipFileName);
				CheckButton.Enabled = true;
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		void Step3ExtractAndCheck(string zipFileName)
		{
			var updateFileName = processFileName + ".tmp";
			JocysCom.ClassLibrary.Files.Zip.UnZipFile(zipFileName, "x360ce.exe", updateFileName);
			if (CheckDigitalSignatureCheckBox.Checked)
			{
				Log("Check Digital Signature...");
				X509Certificate2 certificate;
				Exception error;
				if (!CertificateHelper.IsSignedAndTrusted(updateFileName, out certificate, out error))
				{
					Append(error == null ? " Failed" : string.Format(" Failed: {0}", error.Message));
					return;
				}
				Append(" Done");
			}
			if (CheckVersionCheckBox.Checked)
			{
				var processVersion = new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(processFileName).FileVersion);
				var updatedVersion = new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(updateFileName).FileVersion);
				Log("Current version: {0}", processVersion);
				Log("Updated version: {0}", updatedVersion);
				if (processVersion == updatedVersion)
				{
					Log("Versions are the same. Skip Update.");
					return;
				}
				if (processVersion > updatedVersion)
				{
					Log("Remote version is older. Skip Update.");
					return;
				}
				// A release announcing one version and shipping another installs nowhere.
				if (!UpdateClient.Announced(_Check, updatedVersion))
				{
					Log("The release announces {0} but the file is {1}. Skip Update.", _Check.Version, updatedVersion);
					return;
				}
			}
			Step5ReplaceFiles(updateFileName);
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

		void Step5ReplaceFiles(string updateFileName)
		{
			if (GameIsActive())
			{
				var answer = MessageBoxForm.Show(
					"A game is using the emulated controllers. Installing the update restarts this program and the game loses them.\r\n\r\nInstall now?",
					"X360CE - Update", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (answer != DialogResult.Yes)
				{
					Log("Update postponed while the game runs. Press Check now again when it has closed.");
					return;
				}
			}
			// Change the currently running executable so it can be overwritten.
			var bak = processFileName + ".bak";
			Log("Renaming running process...");
			try
			{
				if (File.Exists(bak))
					File.Delete(bak);
				File.Move(processFileName, bak);
				File.Copy(updateFileName, processFileName);
			}
			catch (Exception ex)
			{
				Append(" Failed: " + ex.Message);
				return;
			}
			Append(" Done");
			Log("Restarting...");
			Application.Restart();
		}
	}
}
