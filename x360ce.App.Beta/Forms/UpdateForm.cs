using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Forms
{
    public partial class UpdateForm : Form
    {
        public UpdateForm()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {

        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            Step1ChekOnline();
        }

        void Step1ChekOnline()
        {
            CurrentLogItem = LogPanel.Add("Check Online...");
            var message = new CloudMessage(Engine.CloudAction.CheckUpdates);
            message.Values.Add(CloudKey.ClientVersion, Application.ProductVersion);
            var item = new CloudItem()
            {
                Date = DateTime.Now,
                Message = message,
                State = CloudState.None,
            };
            MainForm.Current.CloudPanel.Add(item);
        }

        Downloader _downloader;

        public void Step2ProcessUpdateResults(CloudMessage results)
        {
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
                var progress = Math.Round(100m * (decimal)e.BytesReceived / (decimal)e.TotalBytesToReceive, 1);
                if (oldProgress != progress || _downloader.Params.ResponseData != null)
                {
                    oldProgress = progress;
                    Invoke((Action)delegate ()
                    {
                        CurrentLogItem.Message = string.Format("Download...{0}%", progress);
                        if (_downloader.Params.ResponseData != null)
                        {
                            CurrentLogItem.Message = "Saving File...";
                            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                            string fileName = thisProcess.MainModule.FileName;
                            var zipFileName = fileName + ".zip";
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
            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            string fileName = thisProcess.MainModule.FileName;
            var name = System.IO.Path.GetFileName(fileName);
            string updateFileName = fileName + ".tmp";
            // Open an existing zip file for reading.
            var zip = ZipStorer.Open(zipFileName, System.IO.FileAccess.Read);
            // Read the central directory collection
            List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
            // Look for the file.
            foreach (ZipStorer.ZipFileEntry entry in dir)
            {
                if (System.IO.Path.GetFileName(entry.FilenameInZip) == name)
                {
                    // File found, extract it
                    zip.ExtractFile(entry, updateFileName);
                    break;
                }
            }
            zip.Close();
            Step3CheckSignature(updateFileName);
        }

        void Step3CheckSignature(string updateFileName)
        {
            // Begin new check.
            CurrentLogItem = LogPanel.Add("Check Signature...");
            X509Certificate2 certificate;
            Exception error;
            if (!CertificateHelper.IsSignedAndTrusted(updateFileName, out certificate, out error))
            {
                var errMessage = error == null
                    ? "Failed" : string.Format("Failed: {0}", error.Message);
                CurrentLogItem.Message += errMessage;
                return;
            }
            Step4CheckVersion(updateFileName);
        }

        void Step4CheckVersion(string updatedFileName)
        {
            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            string fileName = thisProcess.MainModule.FileName;
            var currentFi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fileName);
            var updatedFi = System.Diagnostics.FileVersionInfo.GetVersionInfo(updatedFileName);

            var currentVersion = new Version(currentFi.FileVersion);
            var updatedVersion = new Version(updatedFi.FileVersion);

            LogPanel.Add("Current version: {0}", currentVersion);
            LogPanel.Add("Updated version: {0}", updatedVersion);
            if (currentVersion == updatedVersion)
            {
                LogPanel.Add("Versions are the same. Skip Update");
                return;
            }
            if (currentVersion > updatedVersion)
            {
                LogPanel.Add("Remote version is older. Skip Update.");
                return;
            }
            Step5ReplaceFiles(updatedFileName);
        }

        void Step5ReplaceFiles(string updateFileName)
        {
            // Change the currently running executable so it can be overwritten.
            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            string fileName = thisProcess.MainModule.FileName;
            string bak = fileName + ".bak";
            CurrentLogItem = LogPanel.Add("Renaming running process...");
            if (System.IO.File.Exists(bak))
                System.IO.File.Delete(bak);
            System.IO.File.Move(fileName, bak);
            System.IO.File.Copy(updateFileName, fileName);
            CurrentLogItem.Message += " Done";
            Step6Restart(fileName);
        }

        void Step6Restart(string fileName)
        {
            // Restart.
            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            CurrentLogItem = LogPanel.Add("Restarting...");
            var spawn = System.Diagnostics.Process.Start(fileName);
            LogPanel.Add("New process ID is {0}", spawn.Id);
            LogPanel.Add("Closing old running process {0}.", thisProcess.Id);
            thisProcess.CloseMainWindow();
            thisProcess.Close();
            thisProcess.Dispose();
       }

        private void OkButton_Click(object sender, EventArgs e)
        {

        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {

        }

    }
}
