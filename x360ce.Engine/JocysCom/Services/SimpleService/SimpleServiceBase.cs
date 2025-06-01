#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace JocysCom.ClassLibrary.Services.SimpleService
{
	/// <summary>
	/// Base class for hosting an ISimpleService implementation as either a Windows Service or console application.
	/// Manages installation, event logging, lifecycle (start, stop, pause, continue), and execution loop with configurable sleep.
	/// </summary>
	public partial class SimpleServiceBase<T> : ServiceBase where T : ISimpleService, new()
	{
		/// <summary>
		/// Configures service and event log installer components based on assembly metadata (company, product, run mode) and command-line parameters (UserName, Password).
		/// </summary>
		public SimpleServiceBase()
		{
			InitializeComponent();
			_installer = new SimpleServiceInstaller();
			var assembly = Assembly.GetEntryAssembly();
			// Get information from assembly.
			var company = GetAttribute<AssemblyCompanyAttribute>(assembly, a => a.Company);
			var product = GetAttribute<AssemblyProductAttribute>(assembly, a => a.Product);
			var rm = LogHelper.RunMode;
			var rx = new Regex("[^a-zA-Z0-9]");
			var cp = string.Format("{0} {1} ({2})", company, product, rm);
			var sn = rx.Replace(cp, "");
			// Set properties.
			_installer.AppEventLogInstaller.Log = company;
			_installer.AppEventLogInstaller.Source = sn + "Source";
			_installer.AppServiceInstaller.Description = cp;
			_installer.AppServiceInstaller.DisplayName = cp;
			_installer.AppServiceInstaller.ServiceName = sn;
			ServiceName = sn;
			var ic = new InstallContext(null, Environment.GetCommandLineArgs());
			if (ic.Parameters.ContainsKey("UserName"))
			{
				AppServiceProcessInstaller.Account = ServiceAccount.User;
				AppServiceProcessInstaller.Username = ic.Parameters["UserName"];
				if (ic.Parameters.ContainsKey("Password"))
				{
					AppServiceProcessInstaller.Password = ic.Parameters["Password"];
				}
			}
		}

		SimpleServiceInstaller _installer;

		string GetAttribute<A>(Assembly assembly, Func<A, string> value) where A : Attribute
		{
			var attribute = (A)Attribute.GetCustomAttribute(assembly, typeof(A));
			return value.Invoke(attribute);
		}

		#region "Service"

		/// <summary>
		/// Invoked by Service Control Manager to start the service: initializes environment and launches the worker thread asynchronously.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			InitOnStart();
			StartServiceAsync(args);
			// Detect system shutdown and log off.
			Microsoft.Win32.SystemEvents.SessionEnded += SystemEvents_SessionEnded;
		}

		private void SystemEvents_SessionEnded(object sender, Microsoft.Win32.SessionEndedEventArgs e)
		{
			LogHelper.WriteInfo("Session Ended ({0})...", e.Reason);
			IsSessionEnded = true;
		}

		/// <summary>
		/// True if a Windows session end event (shutdown or logoff) has been detected.
		/// </summary>
		public bool IsSessionEnded { get; private set; } = false;

		/// <summary>
		/// Maximum wait time in seconds to allow graceful worker thread shutdown before aborting (default 5 minutes).
		/// </summary>
		public int TerminateTimeout = 5 * 60;

		/// <summary>
		/// Requests service stop, signals worker thread to terminate, waits up to TerminateTimeout seconds for a graceful shutdown,
		/// aborts the thread on timeout, and removes session end event subscription.
		/// </summary>
		protected override void OnStop()
		{
			Service.IsStopping = true;
			// Wait 5 minutes for thread to terminate itself.
			for (int i = 1; i <= TerminateTimeout; i++)
			{
				if (!_thread.IsAlive) break;
				if (i % 10 == 0)
				{
					LogHelper.WriteInfo("Service is busy. Waiting...");
				}
				// Logical delay without blocking the current hardware thread.
				System.Threading.Tasks.Task.Delay(1000).Wait();
			}
			if (_thread.IsAlive)
			{
				LogHelper.WriteWarning("Thread won't stop itself. Aborting Thread!");
				_thread.Abort();
				ExitCode = 1;
			}
			// Detach event
			Microsoft.Win32.SystemEvents.SessionEnded -= SystemEvents_SessionEnded;
		}


		object serviceLock = new object();

		ISimpleService _service;
		public ISimpleService Service
		{
			get
			{
				lock (serviceLock)
				{
					if (_service is null)
						_service = new T();
				}
				return _service;
			}
			set { _service = value; }
		}

		protected override void OnPause()
		{
			Service.IsPaused = true;
		}

		protected override void OnContinue()
		{
			Service.IsPaused = false;
		}

		Thread _thread;

		/// <summary>
		/// Starts the service execution asynchronously on a new thread, aborting any existing worker thread first.
		/// </summary>
		public void StartServiceAsync(object parameter)
		{
			// Clean up previous
			if (_thread != null)
				_thread.Abort();
			var starter = new ParameterizedThreadStart(StartService);
			_thread = new Thread(starter);
			_thread.Start(parameter);
		}

		#endregion

		/// <summary>
		/// Core execution loop: initializes service, optionally hosts it via WCF if T derives from MarshalByRefObject, then
		/// repeatedly calls Service.DoAction until stopping, respecting pause and session-end flags, and enforces configured sleep interval.
		/// </summary>
		void StartService(object parameter)
		{
			Service.InitStart();
			// Run as SVC host if necessary.
			if (typeof(T).BaseType.Equals(typeof(MarshalByRefObject)))
			{
				// Create service
				var svcHost = new ServiceHost(Service);
				string info = GetInfo(Service.GetType().Name, svcHost);
				LogHelper.WriteInfo(info, EventLogEntryType.Information);
				// If you are getting "There is already a listener on IP endpoint 0.0.0.0:NNNN" error
				// but all processes who could use NNNN port is closed then maybe this ports is kept
				// open by Visual Studio debugger which tries to debug previously crashed executable.
				// Close Visual Studio debugger window in order to release resources and port.
				svcHost.Open();
			}
			// Start actions.
			string st = ConfigurationManager.AppSettings["SimpleService_SleepTime"];
			// Sleep 5 seconds if not specified.
			int sleepTime = string.IsNullOrEmpty(st) ? 5 : (int)TimeSpan.Parse(st).TotalSeconds;
			// Make sure that service initialized (for some reason locks doesn't work inside Service properly.);
			// Logical delay without blocking the current hardware thread.
			System.Threading.Tasks.Task.Delay(100).Wait();
			while (!Service.IsStopping)
			{
				var watch = Stopwatch.StartNew();
				bool skipSleep = false;
				if (!Service.IsPaused && !IsSessionEnded)
				{
					Service.DoAction((string[])parameter, ref skipSleep);
				}
				// If service finished sooner than expected.
				while (!skipSleep && watch.Elapsed.TotalSeconds < sleepTime)
				{
					if (Service.IsStopping || Service.IsPaused || IsSessionEnded) break;
					// Logical delay without blocking the current hardware thread.
					System.Threading.Tasks.Task.Delay(1000).Wait();
				}
			}
			Service.InitEnd();
		}

		#region "Helper Functions"

		public ServiceProcessInstaller AppServiceProcessInstaller;

		object controllerLock = new object();

		ServiceController _controller;
		public ServiceController Controller
		{
			get
			{
				lock (controllerLock)
				{
					if (_controller is null)
						_controller = new ServiceController(_installer.AppServiceInstaller.ServiceName);
				}
				return _controller;
			}
		}

		public bool IsInstalled
		{
			get
			{
				var services = ServiceController.GetServices();
				foreach (var item in services)
				{
					if (item.ServiceName == _installer.AppServiceInstaller.ServiceName)
						return true;
				}
				return false;
			}
		}

		public string AssemblyLocation
		{
			get { return Assembly.GetEntryAssembly().Location; }
		}

		public Dictionary<object, object> InstallService()
		{
			return InstallService(AssemblyLocation);
		}

		public Dictionary<object, object> InstallService(string assemblyLocation)
		{
			if (IsInstalled)
			{
				if (Environment.UserInteractive)
				{
					LogHelper.WriteInfo("The specified service already exists!");
				}
				return null;
			}
			if (EventLog.SourceExists(_installer.AppEventLogInstaller.Source))
			{
				EventLog.DeleteEventSource(_installer.AppEventLogInstaller.Source);
			}
			var savedState = new Dictionary<object, object>();
			_installer.Context = new InstallContext(null, null);
			_installer.Context.Parameters["AssemblyPath"] = assemblyLocation;
			try
			{
				_installer.Install(savedState);
			}
			catch (Exception ex)
			{
				LogHelper.WriteInfo("Error: {0}", ex.Message);
			}
			return savedState;
		}

		public Dictionary<object, object> UninstallService()
		{
			if (!IsInstalled)
			{
				if (Environment.UserInteractive)
					LogHelper.WriteInfo("The specified service doesn't exists!");
				return null;
			}
			Dictionary<object, object> savedState = null;
			if (Controller.Status == ServiceControllerStatus.Running | Controller.Status == ServiceControllerStatus.Paused)
			{
				Controller.Stop();
				Controller.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 15));
				Controller.Close();
			}
			_installer.Context = new InstallContext(null, null);
			_installer.Uninstall(savedState);
			return savedState;
		}

		/// <summary>
		/// Write application header title to CLI interface.
		/// </summary>
		public void WriteAppHeader()
		{
			var assembly = Assembly.GetExecutingAssembly();
			WriteAppHeader(assembly);
		}

		public void WriteAppHeader(Assembly assembly)
		{
			// Write title.
			// Microsoft (R) SQL Server Database Publishing Wizard 1.1.1.0
			// Copyright (C) Microsoft Corporation. All rights reserved.
			var title = GetAttribute<AssemblyTitleAttribute>(assembly, x => x.Title);
			var version = GetAttribute<AssemblyFileVersionAttribute>(assembly, a => a.Version);
			var copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly, a => a.Copyright);
			var description = GetAttribute<AssemblyDescriptionAttribute>(assembly, a => a.Description);
			var header = string.Format("{0} {1} ({2})", title, version, LogHelper.RunMode);
			LogHelper.WriteInfo(header);
			LogHelper.WriteInfo(copyright);
			LogHelper.WriteInfo(description);
			if (IsConsole)
				Console.Title = header;
		}

		public bool InitEnvironment()
		{
			var assembly = Assembly.GetEntryAssembly();
			WriteAppHeader(assembly);
			Console.WriteLine();
			var ic = new InstallContext(null, Environment.GetCommandLineArgs());
			if (ic.Parameters.ContainsKey("?"))
			{
			}
			else if (ic.Parameters.ContainsKey("InstallService"))
			{
				InstallService();
				return false;
			}
			else if (ic.Parameters.ContainsKey("UninstallService"))
			{
				UninstallService();
				return false;
			}
			return true;
		}

		public bool IsConsole = true;

		public void PauseEnvironment()
		{
			LogHelper.WriteInfo("Service is running...");
			if (IsConsole) LogHelper.WriteInfo("Press CTRL-C to exit...");
			Console.WriteLine();
			while ((!Service.IsStopping))
			{
				// Logical delay without blocking the current hardware thread.
				System.Threading.Tasks.Task.Delay(500).Wait();
			}
			LogHelper.WriteInfo("Service is stopping...");
		}

		public void InitOnStart()
		{
			// If this is running as a service then...
			if (!IsConsole)
			{
				// Initialize and configure event log.
				string m = ".";
				string l = _installer.AppEventLogInstaller.Log;
				string s = _installer.AppEventLogInstaller.Source;
				// Remove outdated location.
				// Important!: SourceExists(...) will fail with SecurityException if you are not running this as an administrator.
				// This is a permissions problem - you should give the running user permission to read the following registry key:
				// HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EventLog
				if (EventLog.SourceExists(s, m) && EventLog.LogNameFromSourceName(s, m) != l)
				{
					EventLog.DeleteEventSource(s, m);
				}
				// Create log if not exists.
				if (!EventLog.SourceExists(s, m))
				{
					var cd = new EventSourceCreationData(s, l);
					EventLog.CreateEventSource(cd);
				}
			}
		}

		public string GetInfo(string prefix, ServiceHost host)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine(prefix + ": " + host.State.ToString());
			sb.AppendLine();
			for (int i = 0; i <= host.BaseAddresses.Count - 1; i++)
			{
				if (i == 0)
					sb.AppendLine("    BaseAddresses:");
				Uri v = host.BaseAddresses[i];
				sb.AppendLine("  " + v.AbsoluteUri);
			}
			for (int i = 0; i <= host.Description.Endpoints.Count - 1; i++)
			{
				var v = host.Description.Endpoints[i];
				sb.AppendLine("    Endpoint address: " + v.Address.ToString());
				sb.AppendLine("       Binding  name: " + v.Binding.Name);
				sb.AppendLine("       Contract name: " + v.Contract.Name);
				sb.AppendLine();
			}
			return sb.ToString();
		}

		#endregion

		#region "Console - Unmanaged"


		SimpleServiceBase<T> srv;
		public void RunServer()
		{
			RunServer(this);
		}

		public void RunServer(SimpleServiceBase<T> service)
		{
			if (Environment.UserInteractive)
			{
				if (InitEnvironment())
					RunServerAsConsole(service, Environment.GetCommandLineArgs());
			}
			else
			{
				RunServerAsService(service);
			}
		}

		public void RunServerAsService(SimpleServiceBase<T> service)
		{
			// Run server as windows service.
			var ServicesToRun = new ServiceBase[] { service };
			Run(ServicesToRun);
		}

		public void RunServerAsConsole(SimpleServiceBase<T> service, string[] args)
		{
			srv = service;
			hr = new SimpleServiceHandler.PHANDLER_ROUTINE(ConsoleCtrlCheck);
			// Handle CTRL-C, Break, Close, Shut-down, Log-off events so console can be closed properly.
			SimpleServiceHandler.SetConsoleCtrlHandler(hr, true);
			//Dim mi As MethodInfo = srv.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
			//mi.Invoke(srv, New Object() {args})
			srv.OnStart(args);
			PauseEnvironment();
			// Hold till service is closed properly.
			while (IsConsole && !IsClosing)
			{
				// Logical delay without blocking the current hardware thread.
				System.Threading.Tasks.Task.Delay(100).Wait();
			}
		}


		bool IsClosing = false;

		SimpleServiceHandler.PHANDLER_ROUTINE hr;
		public bool ConsoleCtrlCheck(CtrlTypes ctrlType)
		{
			//Dim mi As MethodInfo = srv.GetType().GetMethod("OnStop", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
			//mi.Invoke(srv, Nothing)
			srv.OnStop();
			IsClosing = true;
			return true;
		}

		#endregion

	}
}
#endif
