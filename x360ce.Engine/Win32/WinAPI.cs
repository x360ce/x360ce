using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Principal;
using System.Security.Permissions;

namespace x360ce.Engine.Win32
{
	public class WinAPI
	{
		public static bool IsVista
		{
			get { return System.Environment.OSVersion.Version.Major >= 6; }
		}

		/// <summary>
		/// </summary>
		/// <returns>Bool indicating whether the current process is elevated</returns>
		public static bool IsElevated()
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
				if (hProcess == IntPtr.Zero) throw new Exception("Error getting current process handle");
				bRetVal = NativeMethods.OpenProcessToken(hProcess, WinNT.TOKEN_QUERY, out hToken);
				if (!bRetVal) throw new Win32Exception();
				try
				{
					TOKEN_ELEVATION te;
					te.TokenIsElevated = 0;

					UInt32 dwReturnLength = 0;
					Int32 teSize = System.Runtime.InteropServices.Marshal.SizeOf(te);
					IntPtr tePtr = Marshal.AllocHGlobal(teSize);
					try
					{
						System.Runtime.InteropServices.Marshal.StructureToPtr(te, tePtr, true);
						bRetVal = NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, tePtr, (UInt32)teSize, out dwReturnLength);
						if (!bRetVal) throw new Win32Exception();
						if (teSize != dwReturnLength) throw new Exception("Error getting token information");
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
				UInt32 tetSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf((int)tet);
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

		/// <summary>
		/// Enables the UAC shield icon for the given button control
		/// </summary>
		/// <param name="ButtonToEnable">
		/// Button to display shield icon on.
		/// </param>
		///////////////////////////////////////////////////////////////////////
		public static void EnableShieldIcon(Button button)
		{
			// See http://msdn2.microsoft.com/en-us/library/aa361904.aspx
			uint BCM_FIRST = 0x1600; // Normal button
			uint BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
			// Input validation
			if (button == null)return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(new HandleRef(button, button.Handle), BCM_SETSHIELD, new IntPtr(0), new IntPtr(1));
		}

		public static void DisableShieldIcon(Button button)
		{
			uint BCM_FIRST = 0x1600; // Normal button
			uint BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
			// Input validation
			if (button == null) return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(new HandleRef(button, button.Handle), BCM_SETSHIELD, new IntPtr(0), new IntPtr(0));
		}

		public static Process CreateDefaultProcess(string fileName)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = true;
			psi.WorkingDirectory = Environment.CurrentDirectory;
			psi.FileName = fileName;
			psi.CreateNoWindow = true;
			psi.Verb = "runas";
			Process process = new Process();
			// Must enable Exited event for both sync and async scenarios.
			process.EnableRaisingEvents = true;
			process.StartInfo = psi;
			return process;
		}

		/// <summary>
		/// Start program in elevated mode.
		/// </summary>
		/// <param name="fileName"></param>
		public static int RunElevated(string fileName)
		{
			int exitCode = -1;
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateDefaultProcess(fileName))
			{
				try
				{
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
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateDefaultProcess(fileName))
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

		[return: MarshalAs(UnmanagedType.Interface)]
		public static  object RunElevatedComObject(Guid Clsid, Guid InterfaceID)
		{
			string CLSID = Clsid.ToString("B"); // B formatting directive: returns {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx} 
			string monikerName = "Elevation:Administrator!new:" + CLSID;
			BIND_OPTS3 bo = new BIND_OPTS3();
			bo.cbStruct = (uint)Marshal.SizeOf(bo);
			bo.hwnd = IntPtr.Zero;
			bo.dwClassContext = (int)CLSCTX.CLSCTX_ALL;
			object retVal = NativeMethods.CoGetObject(monikerName, ref bo, InterfaceID);
			return (retVal);
		}

		// Restart curent app in elevated mode.
		public static void RunElevated()
		{
			if (IsElevated()) throw new ApplicationException("Elevated already");
			RunElevatedAsync(Application.ExecutablePath, null);
			//Close this instance because we have an elevated instance
			Application.Exit();
		}

		public static bool IsInAdministratorRole
		{
			get
			{
				WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
				return pricipal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		/// <summary>
		/// The function gets the integrity level of the current process. Integrity 
		/// level is only available on Windows Vista and newer operating systems, thus 
		/// GetProcessIntegrityLevel throws a C++ exception if it is called on systems 
		/// prior to Windows Vista.
		/// </summary>
		/// <returns>
		/// Returns the integrity level of the current process. It is usually one of 
		/// these values:
		/// 
		///    SECURITY_MANDATORY_UNTRUSTED_RID - means untrusted level. It is used 
		///    by processes started by the Anonymous group. Blocks most write access.
		///    (SID: S-1-16-0x0)
		///    
		///    SECURITY_MANDATORY_LOW_RID - means low integrity level. It is used by
		///    Protected Mode Internet Explorer. Blocks write acess to most objects 
		///    (such as files and registry keys) on the system. (SID: S-1-16-0x1000)
		/// 
		///    SECURITY_MANDATORY_MEDIUM_RID - means medium integrity level. It is 
		///    used by normal applications being launched while UAC is enabled. 
		///    (SID: S-1-16-0x2000)
		///    
		///    SECURITY_MANDATORY_HIGH_RID - means high integrity level. It is used 
		///    by administrative applications launched through elevation when UAC is 
		///    enabled, or normal applications if UAC is disabled and the user is an 
		///    administrator. (SID: S-1-16-0x3000)
		///    
		///    SECURITY_MANDATORY_SYSTEM_RID - means system integrity level. It is 
		///    used by services and other system-level applications (such as Wininit, 
		///    Winlogon, Smss, etc.)  (SID: S-1-16-0x4000)
		/// 
		/// </returns>
		/// <exception cref="System.ComponentModel.Win32Exception">
		/// When any native Windows API call fails, the function throws a Win32Exception 
		/// with the last error code.
		/// </exception>
		internal int GetProcessIntegrityLevel()
		{
			int IL = -1;
			SafeTokenHandle hToken = null;
			int cbTokenIL = 0;
			IntPtr pTokenIL = IntPtr.Zero;

			try
			{
				// Open the access token of the current process with TOKEN_QUERY.
				if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, WinNT.TOKEN_QUERY, out hToken))
				{
					throw new Win32Exception();
				}

				// Then we must query the size of the integrity level information 
				// associated with the token. Note that we expect GetTokenInformation 
				// to return false with the ERROR_INSUFFICIENT_BUFFER error code 
				// because we've given it a null buffer. On exit cbTokenIL will tell 
				// the size of the group information.
				if (!NativeMethods.GetTokenInformation(hToken,
					TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, IntPtr.Zero, 0,
					out cbTokenIL))
				{
					int error = Marshal.GetLastWin32Error();
					if (error != WinNT.ERROR_INSUFFICIENT_BUFFER)
					{
						// When the process is run on operating systems prior to 
						// Windows Vista, GetTokenInformation returns false with the 
						// ERROR_INVALID_PARAMETER error code because 
						// TokenIntegrityLevel is not supported on those OS's.
						throw new Win32Exception(error);
					}
				}

				// Now we allocate a buffer for the integrity level information.
				pTokenIL = Marshal.AllocHGlobal(cbTokenIL);
				if (pTokenIL == IntPtr.Zero)
				{
					throw new Win32Exception();
				}

				// Now we ask for the integrity level information again. This may fail 
				// if an administrator has added this account to an additional group 
				// between our first call to GetTokenInformation and this one.
				if (!NativeMethods.GetTokenInformation(hToken,
					TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pTokenIL, cbTokenIL,
					out cbTokenIL))
				{
					throw new Win32Exception();
				}

				// Marshal the TOKEN_MANDATORY_LABEL struct from native to .NET object.
				TOKEN_MANDATORY_LABEL tokenIL = (TOKEN_MANDATORY_LABEL)
					Marshal.PtrToStructure(pTokenIL, typeof(TOKEN_MANDATORY_LABEL));

				// Integrity Level SIDs are in the form of S-1-16-0xXXXX. (e.g. 
				// S-1-16-0x1000 stands for low integrity level SID). There is one 
				// and only one subauthority.
				IntPtr pIL = NativeMethods.GetSidSubAuthority(tokenIL.Label.Sid, 0);
				IL = Marshal.ReadInt32(pIL);
			}
			finally
			{
				// Centralized cleanup for all allocated resources. 
				if (hToken != null)
				{
					hToken.Close();
					hToken = null;
				}
				if (pTokenIL != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(pTokenIL);
					pTokenIL = IntPtr.Zero;
					cbTokenIL = 0;
				}
			}
			return IL;
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



	}

}
