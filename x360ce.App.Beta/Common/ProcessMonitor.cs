using System.Collections.Generic;
using System.Management;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Win32;
using System;

namespace x360ce.App
{
	public class ProcessMonitor: IDisposable
	{

		public ProcessMonitor()
		{
			if (WinAPI.IsElevated())
			{
				StartQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
				StartWatcher = new ManagementEventWatcher(StartQuery);
				StartWatcher.EventArrived += startWatcher_EventArrived;
				StopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
				StopWatcher = new ManagementEventWatcher(StopQuery);
				StopWatcher.EventArrived += stopWatcher_EventArrived;
			}
		}

		WqlEventQuery StartQuery;
		ManagementEventWatcher StartWatcher;

		WqlEventQuery StopQuery;
		ManagementEventWatcher StopWatcher;

		object ActionLock = new object();

		public void Start()
		{
			// Supported only in elevated mode.
			if (!WinAPI.IsElevated())
				return;
			lock (ActionLock)
			{
				StartWatcher.Start();
				StopWatcher.Start();
			}
		}

		public void Stop()
		{
			// Supported only in elevated mode.
			if (!WinAPI.IsElevated())
				return;
			lock (ActionLock)
			{
				StartWatcher.Stop();
				StopWatcher.Stop();
			}
		}

		Dictionary<int, FileInfo> ProcessList = new Dictionary<int, FileInfo>();

		void startWatcher_EventArrived(object sender, EventArrivedEventArgs e)
		{
			// Win32_ProcessStartTrace Properties:
			//   byte[] SECURITY_DESCRIPTOR;
			//   ulong TIME_CREATED;
			//   uint ProcessID;
			//   uint ParentProcessID;
			//   byte[] Sid;
			//   string ProcessName;
			//   uint SessionID;
			var processId = (int)(uint)e.NewEvent.Properties["ProcessID"].Value;
			var fi = GetProcessFileInfo(processId);
			if (fi != null)
			{
				var game = SettingsManager.UserGames.Items.FirstOrDefault(x => string.Compare(x.FullPath, fi.FullName, true) == 0);
				if (game != null && !game.IsCurrentApp())
				{
					ProcessList.Add(processId, fi);
					// Switch to user game profile.
					MainForm.Current.SelectCurrentOrDefaultGame(game);
				}
			}
		}

		void stopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
		{
			// Win32_ProcessStartTrace Properties:
			//   byte[] SECURITY_DESCRIPTOR;
			//   ulong TIME_CREATED;
			//   uint ProcessID;
			//   uint ParentProcessID;
			//   byte[] Sid;
			//   string ProcessName;
			//   uint SessionID;
			//   uint ExitStatus;
			var processId = (int)(uint)e.NewEvent.Properties["ProcessID"].Value;
			if (ProcessList.ContainsKey(processId))
			{
				ProcessList.Remove(processId);
				// Switch to user game profile.
				MainForm.Current.SelectCurrentOrDefaultGame();
			}
		}

		public static bool SelectOpenGame()
		{
			string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
			var searcher = new ManagementObjectSearcher(query);
			var processes = searcher.Get().Cast<ManagementObject>().ToArray();
			var defaultName = Path.GetFileName(Application.ExecutablePath);
			foreach (var process in processes)
			{
				var path = (string)process.Properties["ExecutablePath"].Value;
				if (!string.IsNullOrEmpty(path))
				{
					var game = SettingsManager.UserGames.Items.FirstOrDefault(x => string.Compare(x.FullPath, path, true) == 0);
					if (game != null && !game.IsCurrentApp())
					{
						// Switch to user game profile.
						MainForm.Current.SelectCurrentOrDefaultGame(game);
						return true;
					}
				}
			}
			return false;
		}

		static FileInfo GetProcessFileInfo(int processId)
		{
			FileInfo fi = null;
			string query = string.Format("SELECT ExecutablePath FROM Win32_Process WHERE ProcessID = {0}", processId);
			var searcher = new ManagementObjectSearcher(query);
			var item = searcher.Get().Cast<ManagementObject>().ToArray().FirstOrDefault();
			if (item != null)
			{
				var path = (string)item.Properties["ExecutablePath"].Value;
				if (!string.IsNullOrEmpty(path))
					fi = new FileInfo(path);
				item.Dispose();
			}
			searcher.Dispose();
			return fi;
		}

		#region IDisposable

		bool IsDisposing;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Do not dispose twice.
				if (IsDisposing)
					return;
				IsDisposing = true;
				Stop();
				if (StartWatcher != null)
					StartWatcher.Dispose();
				if (StopWatcher != null)
					StopWatcher.Dispose();
			}
		}

		#endregion

	}
}
