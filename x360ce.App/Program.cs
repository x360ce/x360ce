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
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			form = new MainForm();
			Application.Run(form);
		}

		static string cLogFile = "x360ce.log";

        public static object DeviceLock = new object();

        public static int ErrorCount = 0;

		static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
            form.timer.Stop();
            ErrorCount++;
			string message = e.Exception.Message;
			message += string.Format("\r\n\r\nDo you want to write error details into {0} log file?", cLogFile);
			//DialogResult dr = MessageBox.Show(message, "Application Error", MessageBoxButtons.OKCancel);
			//if (dr == DialogResult.OK)
			//{
			//	System.IO.File.AppendAllText(cLogFile, e.Exception.ToString());
			//
			//}

            form.timer.Start();
		}
	}
}
