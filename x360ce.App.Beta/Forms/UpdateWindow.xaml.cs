using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Threading;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using x360ce.Engine;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for UpdateWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class UpdateWindow : Window
	{
		public UpdateWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			LogPanel.LogGridScrollUp = false;
			var process = System.Diagnostics.Process.GetCurrentProcess();
			processFileName = process.MainModule.FileName;
		}

		public void OpenDialog()
		{
			Global.CloudClient.TasksTimer.BeforeRemove += TasksTimer_BeforeRemove;
		}


		public void CloseDialog()
		{
			Global.CloudClient.TasksTimer.BeforeRemove -= TasksTimer_BeforeRemove;
		}

		bool CancelUpdate;

		private void CloseButton_Click(object sender, EventArgs e)
		{
			CancelUpdate = true;
			var item = CheckUpateItem;
			item.Retries = 0;
		}

		private void CheckButton_Click(object sender, EventArgs e)
		{
			Step1ChekOnline();
		}

		CloudItem CheckUpateItem;

		void Step1ChekOnline()
		{
			CurrentLogItem = LogPanel.Add("Check Online...");
			var message = new CloudMessage(Engine.CloudAction.CheckUpdates);
			var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			message.Values.Add(CloudKey.ClientVersion, ai.Version);
			var item = new CloudItem()
			{
				Date = DateTime.Now,
				Message = message,
				State = CloudState.None,
				Retries = 4,
			};
			CheckUpateItem = item;
			Global.CloudClient.TasksTimer.DoActionNow(item);
		}

		private void TasksTimer_BeforeRemove(object sender, QueueTimerEventArgs e)
		{
			var item = e.Item as CloudItem;
			// If check online task failed then...
			if (Equals(CheckUpateItem, item) && !e.Keep)
			{
				CurrentLogItem.Message += " Failed";
				if (item.Error != null)
					CurrentLogItem.Message += ": " + item.Error.Message;
			}
		}

		Downloader _downloader;
		string processFileName;

		public void Step2ProcessUpdateResults(CloudMessage results)
		{
			if (CancelUpdate)
				return;
			var url = results.Values.GetValue<string>(CloudKey.UpdateUrl);
			if (string.IsNullOrEmpty(url))
			{
				CurrentLogItem.Message += " No new updates.";
				return;
			}
			CurrentLogItem.Message += " Update URL retrieved.";
			LogPanel.Add("{0}", url);
			CurrentLogItem = LogPanel.Add("Download...");
			_downloader = new Downloader();
			_downloader.Progress += _downloader_Progress;
			_downloader.LoadAsync(url);
		}

		LogItem CurrentLogItem;
		decimal oldProgress;
		object progressLock = new object();

		private void _downloader_Progress(object sender, DownloaderEventArgs e)
		{
			lock (progressLock)
			{
				var progress = Math.Round(100m * e.BytesReceived / e.TotalBytesToReceive, 1);
				if (oldProgress != progress || _downloader.Params.ResponseData != null)
				{
					oldProgress = progress;
					ControlsHelper.Invoke(() =>
					{
						var mb = Math.Round(e.BytesReceived / 1024m / 1024m, 1);
						CurrentLogItem.Message = string.Format("Download... {0}% - {1} MB", progress, mb);
						if (_downloader.Params.ResponseData != null)
						{
							CurrentLogItem.Message = "Saving File...";
							var zipFileName = processFileName + ".zip";
							System.IO.File.WriteAllBytes(zipFileName, _downloader.Params.ResponseData);
							CurrentLogItem.Message += " Done";
							Step3AExtractFiles(zipFileName);
						}
					});
				}
			}
		}

		void Step3AExtractFiles(string zipFileName)
		{
			if (CancelUpdate)
				return;
			var name = System.IO.Path.GetFileName(processFileName);
			string updateFileName = processFileName + ".tmp";
			JocysCom.ClassLibrary.Files.Zip.UnZipFile(zipFileName, "x360ce.exe", updateFileName);
			Step3CheckSignature(updateFileName);
		}

		void Step3CheckSignature(string updateFileName)
		{
			if (CancelUpdate)
				return;
			if (CheckDigitalSignatureCheckBox.IsChecked == true)
			{
				CurrentLogItem = LogPanel.Add("Check Digital Signature...");
				X509Certificate2 certificate;
				Exception error;
				if (!CertificateHelper.IsSignedAndTrusted(updateFileName, out certificate, out error))
				{
					var errMessage = error == null
						? " Failed" : string.Format(" Failed: {0}", error.Message);
					CurrentLogItem.Message += errMessage;
					return;
				}
			}
			Step4CheckVersion(updateFileName);
		}

		void Step4CheckVersion(string updatedFileName)
		{
			if (CancelUpdate)
				return;
			if (CheckVersionCheckBox.IsChecked == true)
			{
				var processFi = System.Diagnostics.FileVersionInfo.GetVersionInfo(processFileName);
				var updatedFi = System.Diagnostics.FileVersionInfo.GetVersionInfo(updatedFileName);
				var processVersion = new Version(processFi.FileVersion);
				var updatedVersion = new Version(updatedFi.FileVersion);
				LogPanel.Add("Current version: {0}", processVersion);
				LogPanel.Add("Updated version: {0}", updatedVersion);
				if (processVersion == updatedVersion)
				{
					LogPanel.Add("Versions are the same. Skip Update");
					return;
				}
				if (processVersion > updatedVersion)
				{
					LogPanel.Add("Remote version is older. Skip Update.");
					return;
				}
			}
			Step5ReplaceFiles(updatedFileName);
		}

		void Step5ReplaceFiles(string updateFileName)
		{
			if (CancelUpdate)
				return;
			// Change the currently running executable so it can be overwritten.
			string bak = processFileName + ".bak";
			CurrentLogItem = LogPanel.Add("Renaming running process...");
			try
			{
				if (System.IO.File.Exists(bak))
					System.IO.File.Delete(bak);
			}
			catch (Exception ex)
			{
				CurrentLogItem.Message += " Failed: " + ex.Message;
				return;
			}
			System.IO.File.Move(processFileName, bak);
			System.IO.File.Copy(updateFileName, processFileName);
			CurrentLogItem.Message += " Done";
			Step6Restart();
		}

		void Step6Restart()
		{
			if (CancelUpdate)
				return;
			var process = System.Diagnostics.Process.GetCurrentProcess();
			CurrentLogItem = LogPanel.Add("Restarting...");
			System.Windows.Forms.Application.Restart();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			CancelUpdate = false;
			LogPanel.Items.Clear();
			// Center message box window in application.
			if (Owner == null)
				ControlsHelper.CenterWindowOnApplication(this);
		}
	}
}
