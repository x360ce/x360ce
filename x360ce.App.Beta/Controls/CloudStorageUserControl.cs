using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine.Data;
using x360ce.Engine;
using JocysCom.ClassLibrary.ComponentModel;

namespace x360ce.App.Controls
{
    public partial class CloudStorageUserControl : UserControl
    {

        public CloudStorageUserControl()
        {
            InitializeComponent();
            data = new SortableBindingList<CloudItem>();
            data.ListChanged += Data_ListChanged;
            TasksDataGridView.AutoGenerateColumns = false;
            TasksDataGridView.DataSource = data;
            // Force to create handle.
            var handle = this.Handle;
            queueTimer = new JocysCom.ClassLibrary.Threading.QueueTimer(500, 1000);
            queueTimer.SynchronizingObject = this;
            queueTimer.DoAction = DoAction;
            queueTimer.DoActionNow();
        }

        private void Data_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
            {
                var f = MainForm.Current;
                if (f == null) return;
                AppHelper.SetText(f.CloudMessagesLabel, "M: {0}", data.Count);
            }
        }

        JocysCom.ClassLibrary.Threading.QueueTimer queueTimer;
        SortableBindingList<CloudItem> data;

        public void Add<T>(CloudAction action, T[] items)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                var allow = MainForm.Current.OptionsPanel.InternetAutoSaveCheckBox.Checked;
                if (!allow)
                {
                    return;
                }
                for (int i = 0; i < items.Length; i++)
                {
                    var item = new CloudItem()
                    {
                        Action = action,
                        Date = DateTime.Now,
                        Item = items[i],
                        State = CloudState.None,
                    };
                    data.Add(item);
                }
            });
        }

        void DoAction(object state)
        {
            if (data.Count == 0) return;
            if (MainForm.Current.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    MainForm.Current.AddTask("Cloud...");
                }));
            }
            Exception error;
            try
            {
                error = Execute<Game>(CloudAction.Delete);
                if (error == null)
                    error = Execute<Game>(CloudAction.Insert);
                if (error == null)
                    error = Execute<UserController>(CloudAction.Delete);
                if (error == null)
                    error = Execute<UserController>(CloudAction.Insert);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Invoke(new Action(() =>
            {
                MainForm.Current.RemoveTask("Cloud...");
                if (error == null)
                {
                    MainForm.Current.SetHeaderBody(MessageBoxIcon.Information);
                }
                else
                {
                    var body = error.Message;
                    if (error.InnerException != null) body += "\r\n" + error.InnerException.Message;
                    MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, body);
                }
            }));
        }

        /// <summary>
        ///  Submit changed data to the cloud.
        /// </summary>
        Exception Execute<T>(CloudAction action)
        {
            var ws = new WebServiceClient();
            ws.Url = SettingsManager.Options.InternetDatabaseUrl;
            CloudResults result = null;
            try
            {
                var items = data.Where(x => x.Action == action).Select(x => x.Item).OfType<T>().ToList();
                if (items.Count > 0)
                {
                    var command = new CloudCommand();
                    command.Action = action;
                    if (typeof(T) == typeof(Game))
                    {
                        command.Games = items as List<Game>;
                    }
                    else if (typeof(T) == typeof(UserController))
                    {
                        command.UserControllers = items as List<UserController>;
                    }
                    // Add secure credentials.
                    var rsa = new JocysCom.ClassLibrary.Security.Encryption("Cloud");
                    if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
                    {
                        var username = rsa.RsaEncrypt("username");
                        var password = rsa.RsaEncrypt("password");
                        ws.SetCredentials(username, password);
                    }
                    result = ws.Execute(command);
                    if (result.ErrorCode > 0) return new Exception(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (queueTimer != null)
            {
                queueTimer.Dispose();
                queueTimer = null;
            }

            base.Dispose(disposing);
        }


    }
}
