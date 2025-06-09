using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.App
{
	public class ErrorsHelper
	{

		private static Label _StatusLabel;
		private static ContentControl _StatusIcon;
		private static FrameworkElement _TopControl;

		private static FileSystemWatcher errorsWatcher;
		private static readonly object errorsWatcherLock = new object();
		public static int ErrorFilesCount;

		static bool enabled;

		public static void InitErrorsHelper(bool clearErrors, Label statusLabel, ContentControl statusIcon, FrameworkElement topControl)
		{
			if (clearErrors)
				ClearErrors(true);
			MonitorErrors(true);
			_StatusLabel = statusLabel;
			_StatusIcon = statusIcon;
			_TopControl = topControl;
			enabled = true;
		}

		public static void DisposeErrorsHelper()
		{
			enabled = false;
			MonitorErrors(false);
			_StatusLabel = null;
			_StatusIcon = null;
			_TopControl = null;
		}

		public static void MonitorErrors(bool enable)
		{
			lock (errorsWatcherLock)
			{
				if (enable && errorsWatcher == null)
				{
					var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
					if (!dir.Exists)
						dir.Create();
					errorsWatcher = new FileSystemWatcher(dir.FullName, LogHelper.Current.FilePattern);
					errorsWatcher.Deleted += ErrorsWatcher_Changed;
					errorsWatcher.Created += ErrorsWatcher_Changed;
					errorsWatcher.EnableRaisingEvents = true;
					ErrorsWatcher_Changed(null, null);
				}
				else if (!enable && errorsWatcher != null)
				{
					errorsWatcher.Deleted -= ErrorsWatcher_Changed;
					errorsWatcher.Created -= ErrorsWatcher_Changed;
					errorsWatcher.Dispose();
					errorsWatcher = null;
				}
			}
		}

		private static void ErrorsWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			ControlsHelper.BeginInvoke(new Action(() =>
			{
				var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
				ErrorFilesCount = dir.GetFiles(LogHelper.Current.FilePattern).Count();
				UpdateStatusErrorsLabel();
			}));
		}

		public static void ClearErrors(bool silent = false)
		{
			var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
			if (!dir.Exists)
				return;
			// Disable monitor while deleting files.
			MonitorErrors(false);
			var fis = dir
				.GetFiles(LogHelper.Current.FilePattern)
				.OrderByDescending(x => x.CreationTime).ToArray();
			if (fis.Count() > 0)
			{
				if (!silent)
				{
					var form = new MessageBoxWindow();
					var result = form.ShowDialog("Do you want to clear all errors?", "Clear Errors?",
						System.Windows.MessageBoxButton.YesNo,
						System.Windows.MessageBoxImage.Error,
						 System.Windows.MessageBoxResult.No
					);
					if (result != System.Windows.MessageBoxResult.Yes)
						return;
				}
				foreach (var fi in fis)
				{
					try
					{
						fi.Delete();
					}
					catch (Exception ex)
					{
						_ = ex.Message;
					}
				}
			}
			// Enable monitor and show stats.
			MonitorErrors(true);
		}

		public static void UpdateStatusErrorsLabel()
		{
			var label = _StatusLabel;
			var icon = _StatusIcon;
			if (label == null)
				return;
			if (icon == null)
				return;
			label.Content = string.Format("Errors: {0} | {1}", ErrorFilesCount, LogHelper.Current.ExceptionsCount);
			label.Foreground = ErrorFilesCount > 0
				? System.Windows.Media.Brushes.DarkRed
				: System.Windows.SystemColors.ControlDarkBrush;
			icon.Opacity = ErrorFilesCount > 0
				? 1.000
				: 0.125;
		}

		public static void LogHelper_Current_WritingException(object sender, LogHelperEventArgs e)
		{
			if (!enabled)
				e.Cancel = true;
			var ex = e.Exception as SharpDX.SharpDXException;
			var d = ex?.Descriptor;
			if (d != null)
			{
				// If exception when getting Joystick properties in
				// CustomDiState.cs class: var o = device.GetObjectInfoByOffset((int)list[i]);
				if (d.ApiCode == "NotFound" && d.Code == -2147024894 &&
					d.Module == "SharpDX.DirectInput" &&
					d.NativeApiCode == "DIERR_NOTFOUND"
				)
				{
					// Cancel reporting error.
					e.Cancel = true;
				}
				// If another DInput errors
			}

			// C:\WINDOWS\system32\xinput1_3.dll C:\WINDOWS\system32\xinput1_4.dll
			var fex = e.Exception as FileNotFoundException;
			// If serializer warning then...
			if (fex != null && fex.HResult == unchecked((int)0x80070002) && fex.FileName?.Contains(".XmlSerializers") == true)
				// Cancel reporting error.
				e.Cancel = true;
			ControlsHelper.GetActiveControl(_TopControl, out var activeControl, out var activePath);
			// Add path to current control to help with error fixing.
			e.Exception.Data.Add("ActiveControlPath", activePath);
		}


	}
}
