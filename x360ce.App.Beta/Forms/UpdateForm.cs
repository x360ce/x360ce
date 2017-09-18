using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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

        public void ProcessUpdateResults(CloudMessage results)
        {
            LogPanel.Add("Process results....");
            var url = results.Values.GetValue<string>(CloudKey.UpdateUrl);
            if (string.IsNullOrEmpty(url))
            {
                LogPanel.Add("No new updates.");
                return;
            }
            LogPanel.Add("Update URL: {0}", url);
            LogPanel.Add("Begin update...");
            _downloader = new Downloader();
            _downloader.SynchronizingObject = this;
            _downloader.Progress += _downloader_Progress;
            _downloader.LoadAsync(url);
        }

        private void _downloader_Progress(object sender, DownloaderEventArgs e)
        {
            var percent = Math.Round(100m * (decimal)e.BytesReceived / (decimal)e.TotalBytesToReceive, 0);
            AppHelper.SetText(ProgressLabel, "{0}", percent);
        }

        Downloader _downloader;

        private void OkButton_Click(object sender, EventArgs e)
        {

        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {

        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            LogPanel.Add("Begin check...");
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
    }
}
