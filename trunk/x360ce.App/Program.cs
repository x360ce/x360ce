using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace x360ce.App
{
	static class Program
	{
		
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				//Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
				MainForm.Current = new MainForm();
				Application.Run(MainForm.Current);
			}
			catch (Exception ex)
			{
				var message = ex.ToString();
				AddLoaderException(ex, ref message);
				if (ex.InnerException != null) AddLoaderException(ex, ref message);
				var box = new Controls.MessageBoxForm();
				var result = box.ShowForm(message, "Exception!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
				if (result == DialogResult.Cancel) Application.Exit();
				throw ex;
			}
		}

		/// <summary>Add information about missing libraries and DLLs</summary>
		private static void AddLoaderException(Exception ex, ref string message)
		{
			if (!ex.GetType().Equals(typeof(ReflectionTypeLoadException))) return;
			message += "\r\n===============================================================\r\n";
			var exceptions = ((ReflectionTypeLoadException)ex).LoaderExceptions;
			foreach (Exception ex3 in exceptions) message += ex3.Message + "\r\n";
		}

		//static string cLogFile = "x360ce.log";

		public static object DeviceLock = new object();

		public static int TimerCount = 0;
		public static int ReloadCount = 0;
		public static int ErrorCount = 0;

		public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			ErrorCount++;
			MainForm.Current.UpdateTimer.Stop();
			MainForm.Current.UpdateStatus("- " + e.Exception.Message);
			MainForm.Current.UpdateTimer.Start();
		}
	}
}
