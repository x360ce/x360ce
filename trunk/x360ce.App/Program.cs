using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace x360ce.App
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static MainForm form;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			//try
			//{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				//Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
				form = new MainForm();
				MainForm.Current = form;
				Application.Run(form);
			//}
			//catch (Exception ex)
			//{
			//    var box = new Controls.MessageBoxForm();
			//    var result = box.ShowForm(ex.ToString(), "Exception!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
			//    if (result == DialogResult.Cancel) Application.Exit();
			//    throw ex;
			//}
		}

		//static string cLogFile = "x360ce.log";

        public static object DeviceLock = new object();

        public static int TimerCount = 0;
        public static int ReloadCount = 0;
        public static int ErrorCount = 0;

       public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
			form.UpdateTimer.Stop();
            ErrorCount++;
            form.UpdateStatus("- " + e.Exception.Message);
			form.UpdateTimer.Start();
        }
	}
}
