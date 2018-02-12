using System.Security.Permissions;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Win32
{
	public class UacHelper
	{
		/// <summary>Enables the UAC shield icon for the given button control</summary>
		/// <param name="ButtonToEnable">Button to display shield icon on.</param>
		public static void EnableShieldIcon(Button button)
		{
			// See http://msdn2.microsoft.com/en-us/library/aa361904.aspx
			int BCM_FIRST = 0x1600; // Normal button
			int BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
													// Input validation
			if (button == null) return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(button.Handle, BCM_SETSHIELD, new IntPtr(0), new IntPtr(1));
		}

		/// <summary>Disable the UAC shield icon for the given button control</summary>
		/// <param name="ButtonToEnable">Button to remove shield icon.</param>
		public static void DisableShieldIcon(Button button)
		{
			int BCM_FIRST = 0x1600; // Normal button
			int BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
													// Input validation
			if (button == null) return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(button.Handle, BCM_SETSHIELD, new IntPtr(0), new IntPtr(0));
		}

		/// <summary>Check if current process is elevated.</summary>
		public static bool IsElevated
		{
			get
			{
				//if (!IsVista) throw new ApplicationException("Function requires Vista or higher");
				// METHOD 1
				//AppDomain myDomain = Thread.GetDomain();
				//myDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
				//WindowsPrincipal myPrincipal = (WindowsPrincipal)Thread.CurrentPrincipal;
				//return (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator));
				// METHOD 2
				bool bRetVal = false;
				IntPtr hToken = IntPtr.Zero;
				IntPtr hProcess = NativeMethods.GetCurrentProcess();
				if (hProcess == IntPtr.Zero) throw new InvalidOperationException("Error getting current process handle");
				bRetVal = NativeMethods.OpenProcessToken(hProcess, WinNT.TOKEN_QUERY, out hToken);
				if (!bRetVal) throw new InvalidOperationException("Error opening process token");
				try
				{
					TOKEN_ELEVATION te;
					te.TokenIsElevated = 0;

					UInt32 dwReturnLength = 0;
					Int32 teSize = Marshal.SizeOf(te);
					IntPtr tePtr = Marshal.AllocHGlobal(teSize);
					try
					{
						Marshal.StructureToPtr(te, tePtr, true);
						bRetVal = NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, tePtr, (UInt32)teSize, out dwReturnLength);
						if ((!bRetVal) | (teSize != dwReturnLength))
						{
							throw new InvalidOperationException("Error getting token information");
						}
						te = (TOKEN_ELEVATION)Marshal.PtrToStructure(tePtr, typeof(TOKEN_ELEVATION));
					}
					finally
					{
						Marshal.FreeHGlobal(tePtr);
					}
					return (te.TokenIsElevated != 0);
				}
				finally
				{
					NativeMethods.CloseHandle(hToken);
				}
			}
		}

		/// <summary>
		/// Get elevation type.
		/// </summary>
		/// <returns>TokenElevationType</returns>
		public static TOKEN_ELEVATION_TYPE GetElevationType()
		{
			bool bRetVal = false;
			IntPtr hToken = IntPtr.Zero;
			IntPtr hProcess = NativeMethods.GetCurrentProcess();
			if (hProcess == IntPtr.Zero) throw new ApplicationException("Error getting current process handle");
			bRetVal = NativeMethods.OpenProcessToken(hProcess, WinNT.TOKEN_QUERY, out hToken);
			if (!bRetVal) throw new ApplicationException("Error opening process token");
			try
			{
				TOKEN_ELEVATION_TYPE tet = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
				UInt32 dwReturnLength = 0;
				UInt32 tetSize = (uint)Marshal.SizeOf((int)tet);
				IntPtr tetPtr = Marshal.AllocHGlobal((int)tetSize);
				try
				{
					bRetVal = NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevationType, tetPtr, tetSize, out dwReturnLength);
					if ((!bRetVal) | (tetSize != dwReturnLength))
					{
						throw new ApplicationException("Error getting token information");
					}
					tet = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(tetPtr);
				}
				finally
				{
					Marshal.FreeHGlobal(tetPtr);
				}
				return tet;
			}
			finally
			{
				NativeMethods.CloseHandle(hToken);
			}
		}

		public static bool IsInAdministratorRole
		{
			get
			{
				WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
				return pricipal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		public static bool AllowRead(params string[] files)
		{
			FileIOPermission f2 = new FileIOPermission(PermissionState.None);
			for (int i = 0; i < files.Length; i++)
			{
				f2.AddPathList(FileIOPermissionAccess.Read, files[i]);
			}
			try
			{
				f2.Demand();
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static Process CreateElevatedProcess(string fileName, string arguments = null, bool useFileWorkingFolder = false)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = true;
			psi.WorkingDirectory = useFileWorkingFolder
				? new System.IO.FileInfo(fileName).DirectoryName
				: Environment.CurrentDirectory;
			psi.FileName = fileName;
			if (arguments != null) psi.Arguments = arguments;
			psi.CreateNoWindow = true;
			psi.Verb = "runas";
			var process = new Process();
			// Must enable Exited event for both sync and async scenarios.
			process.EnableRaisingEvents = true;
			process.StartInfo = psi;
			return process;
		}

		/// <summary>
		/// Start program in elevated mode.
		/// </summary>
		/// <param name="fileName"></param>
		public static int RunElevated(string fileName, string arguments, ProcessWindowStyle style, bool useFileWorkingFolder = false)
		{
			int exitCode = -1;
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateElevatedProcess(fileName, arguments, useFileWorkingFolder))
			{
				try
				{
					process.StartInfo.WindowStyle = style;
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happend. Do nothing and return...
					return exitCode;
				}
				process.WaitForExit();
				exitCode = process.ExitCode;
			}
			return exitCode;
		}

		public static void RunElevatedAsync(string fileName, EventHandler exitedEventHandler)
		{
			if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateElevatedProcess(fileName))
			{
				if (exitedEventHandler != null) process.Exited += exitedEventHandler;
				try
				{
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happend. Do nothing and return...
				}
			}
		}

		// Restart curent app in elevated mode.
		public static void RunElevated()
		{
			if (IsElevated) throw new ApplicationException("Elevated already");
			RunElevatedAsync(Application.ExecutablePath, null);
			//Close this instance because we have an elevated instance
			Application.Exit();
		}

	}
}
