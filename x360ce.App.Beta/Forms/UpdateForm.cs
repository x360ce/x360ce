using JocysCom.ClassLibrary.Processes;
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
            LogPanel.LogGridScrollUp = false;
            var process = System.Diagnostics.Process.GetCurrentProcess();
            processFileName = process.MainModule.FileName;
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
        string processFileName;

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
                        var mb = Math.Round((decimal)e.BytesReceived / 1024m / 1024m, 1);
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
            var name = System.IO.Path.GetFileName(processFileName);
            string updateFileName = processFileName + ".tmp";
            JocysCom.ClassLibrary.Files.Zip.UnZipFile(zipFileName, "x360ce.exe", updateFileName);
            Step3CheckSignature(updateFileName);
        }

        void Step3CheckSignature(string updateFileName)
        {
            if (CheckDigitalSignatureCheckBox.Checked)
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
            if (CheckVersionCheckBox.Checked)
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
            Step6Restart(processFileName);
        }

        void Step6Restart(string fileName)
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            CurrentLogItem = LogPanel.Add("Restarting...");
            Application.Restart();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {

        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {

        }

    }
}
