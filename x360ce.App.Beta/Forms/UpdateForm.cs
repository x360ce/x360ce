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
            var url = results.Values.GetValue<string>(CloudKey.UpdateUrl);
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {

        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {

        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
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
