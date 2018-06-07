using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Linq;
using System.IO;

namespace x360ce.App
{
	public class ProcessMonitor
	{

		public ProcessMonitor()
		{
			StartQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
			StartWatcher = new ManagementEventWatcher(StartQuery);
			StartWatcher.EventArrived += startWatcher_EventArrived;
			StopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
			StopWatcher = new ManagementEventWatcher(StopQuery);
			StopWatcher.EventArrived += stopWatcher_EventArrived;
		}

		WqlEventQuery StartQuery;
		ManagementEventWatcher StartWatcher;

		WqlEventQuery StopQuery;
		ManagementEventWatcher StopWatcher;

		object ActionLock = new object();

		public void Start()
		{
			lock (ActionLock)
			{
				StartWatcher.Start();
				StopWatcher.Start();
			}
		}

		public void Stop()
		{
			lock (ActionLock)
			{
				StartWatcher.Start();
				StopWatcher.Start();
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
				var setting = SettingsManager.UserGames.Items.Where(x => string.Compare(x.FullPath, fi.FullName, true) == 0).ToArray();
				if (setting.Length > 0)
				{
					ProcessList.Add(processId, fi);
					// Trigger switch of the profile here.
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
				ProcessList.Remove(processId);
		}

		public static Process IsProcessOpen(string name)
		{
			foreach (var process in Process.GetProcesses())
				if (process.ProcessName.Contains(name))
					return process;
			return null;
		}

		public static FileInfo GetProcessFileInfo(int processId)
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
			}
			item.Dispose();
			searcher.Dispose();
			return fi;
		}
	}

}
