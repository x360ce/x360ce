using JocysCom.ClassLibrary.Runtime;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using x360ce.Engine;

namespace x360ce.App.Service
{
	public class LocalService
	{
		public LocalService()
		{
			
		}

		public void Start()
		{
			InitializeLogging();
			InitializeAppDataPathPermissions();
		}

		public void Stop()
		{
		}

		public void InitializeLogging()
		{
			// Initialize exception handlers
			LogHelper.Current.LogExceptions = true;
			LogHelper.Current.LogToFile = true;
			LogHelper.Current.LogFirstChanceExceptions = false;
			LogHelper.Current.InitExceptionHandlers(EngineHelper.AppDataPath + "\\Errors");
			LogHelper.Current.WritingException += ErrorsHelper.LogHelper_Current_WritingException;
		}

		public void InitializeAppDataPathPermissions()
		{
			// Fix access rights to configuration folder.
			var di = new DirectoryInfo(EngineHelper.AppDataPath);
			// Create configuration folder if not exists.
			if (!di.Exists)
				di.Create();
			var rights = FileSystemRights.Modify;
			var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			// Check if users in non elevated mode have rights to modify the file.
			var hasRights = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(di.FullName, rights, users, false);
			if (!hasRights && JocysCom.ClassLibrary.Win32.WinAPI.IsElevated())
			{
				// Allow users to modify file when in non elevated mode.
				JocysCom.ClassLibrary.Security.PermissionHelper.SetRights(di.FullName, rights, users, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
				hasRights = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(di.FullName, rights, users, false);
			}
		}
		

	}

}
