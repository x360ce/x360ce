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

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			form = new MainForm();
			Application.Run(form);
		}

		//static string cLogFile = "x360ce.log";

        public static object DeviceLock = new object();

        public static int TimerCount = 0;
        public static int ReloadCount = 0;
        public static int ErrorCount = 0;

       public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
			form.timer.Stop();
            ErrorCount++;
            form.UpdateStatus("- " + e.Exception.Message);
			form.timer.Start();
        }
	}
}
