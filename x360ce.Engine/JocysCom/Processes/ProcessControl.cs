using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Security;
using System.Drawing;

namespace JocysCom.ClassLibrary.Processes
{
	/// <summary>
	/// Summary description for ProcessControl.
	/// </summary>
	public class ProcessControl
	{
		public System.Text.StringBuilder Log;

		public ProcessControl()
		{
			this.Log = new System.Text.StringBuilder();

		}

		//http://www.bousoft.com/wffaq/win32.php
		//http://subscribe.ru/archive/comp.soft.prog.compu.faq/200402/13010616.text


		//values from Winuser.h in Microsoft SDK.
		[Flags]
		public enum MouseFlags : int
		{
			Move = 0x0001,
			LeftDown = 0x0002,
			LeftUp = 0x0004,
			RightDown = 0x0008,
			RightUp = 0x0010,
			Absolute = 0x8000
		};

		public class WinAPI
		{

			public const int WM_SYSCOMMAND = 0x0112; //command from the Window menu
													 //WPARAM wParam
													 //	LPARAM lParam
													 //If the wParam is SC_KEYMENU, lParam contains the character code of the key that is used with the ALT key to display the popup menu. For example, pressing ALT+F to display the File popup will cause a WM_SYSCOMMAND with wParam equal to SC_KEYMENU and lParam equal to 'f'.

			//public const int SC_MAXIMIZE = 0xF030; 
			//public const int SC_CLOSE = 0xF060; 

			// API Constants;
			public const int MF_BYCOMMAND = 0xF0;
			public const int MF_BYPOSITION = 0xF400;
			public const int MF_BITMAP = 0xF4;
			public const int MF_CHECKED = 0xF8;
			public const int MF_DISABLED = 0xF2;
			public const int MF_ENABLED = 0xF0;
			public const int MF_GRAYED = 0xF1;
			public const int MF_MENUBARBREAK = 0xF20;
			public const int MF_MENUBREAK = 0xF40;
			public const int MF_OWNERDRAW = 0xF100;
			public const int MF_POPUP = 0xF10;
			public const int MF_SEPARATOR = 0xF800;
			public const int MF_STRING = 0xF0;
			public const int MF_UNCHECKED = 0xF0;

			public const int SC_CLOSE = 0xF060; //Closes the window.
			public const int SC_HOTKEY = 0xF150;
			public const int SC_HSCROLL = 0xF080;
			public const int SC_KEYMENU = 0xF100;
			public const int SC_MAXIMIZE = 0xF030; //Maximizes the window.
			public const int SC_MINIMIZE = 0xF020;
			public const int SC_MOUSEMENU = 0xF090;
			public const int SC_MOVE = 0xF010;
			public const int SC_NEXTWINDOW = 0xF040;
			public const int SC_PREVWINDOW = 0xF050;
			public const int SC_RESTORE = 0xF120;
			public const int SC_SCREENSAVE = 0xF140;
			public const int SC_SIZE = 0xF000;
			public const int SC_TASKLIST = 0xF130;
			public const int SC_VSCROLL = 0xF070;

			public const int WM_LBUTTONDOWN = 0x0201;
			public const int WM_LBUTTONUP = 0x0202;
			public const int MK_LBUTTON = 0x0001;

			//			SC_CONTEXTHELP //Changes the cursor to a question mark with a pointer. If the user then clicks a control in the dialog box, the control receives a WM_HELP message.
			//			SC_DEFAULT //Selects the default item; the user double-clicked the window menu.
			//			SC_HOTKEY //Activates the window associated with the application-specified hot key. The lParam parameter identifies the window to activate.
			//			SC_HSCROLL //Scrolls horizontally.
			//			SC_KEYMENU //Retrieves the window menu as a result of a keystroke. For more information, see the Remarks section.
			//			SC_MINIMIZE //Minimizes the window.
			//			SC_MONITORPOWER //Sets the state of the display. This command supports devices that have power-saving features, such as a battery-powered personal computer. 
			//			//The lParam parameter can have the following values:
			//			//1 - the display is going to low power
			//			//2 - the display is being shut off
			//		    SC_MOUSEMENU //Retrieves the window menu as a result of a mouse click.
			//			SC_MOVE //Moves the window.
			//			SC_NEXTWINDOW //Moves to the next window.
			//			SC_PREVWINDOW //Moves to the previous window.
			//			SC_RESTORE //Restores the window to its normal position and size.
			//			SC_SCREENSAVE //Executes the screen saver application specified in the [boot] section of the System.ini file.
			//			SC_SIZE //Sizes the window.
			//			SC_TASKLIST //Activates the Start menu.
			//			SC_VSCROLL //Scrolls vertically.

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			internal static extern IntPtr FindWindow(
				string lpClassName,  // class name 
				string lpWindowName  // window name 
				);

			[DllImport("user32.dll")]
			internal static extern void mouse_event(MouseFlags dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

			static uint keybd_event(byte bVk, byte bScan, int dwFlags, uint dwExtraInfo)
			{
				var dwExtraInfoPtr = new UIntPtr(dwExtraInfo);
				return keybd_event(bVk, bScan, dwFlags, dwExtraInfoPtr);
			}

			[DllImport("user32.dll")]
			internal static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);

			public static void KeyDown(System.Windows.Forms.Keys key)
			{
				keybd_event((byte)key, 0, 0, 0);
			}

			public static void KeyUp(System.Windows.Forms.Keys key)
			{
				keybd_event((byte)key, 0, 0x7F, 0);
			}

			// GetCursorPos: API used to get the current mouse pointer location
			//Parameters
			//lpPoint 
			//[out] Long pointer to a POINT structure that receives the screen coordinates of
			//the cursor. 
			//Return Values
			//Nonzero indicates success. Zero indicates failure. To get extended error '
			//information, call GetLastError. 
			//Remarks
			//The cursor position is always given in screen coordinates and is not affected by
			//the mapping mode of the window that contains the cursor. 
			[DllImport("user32.dll")]
			internal static extern int GetCursorPos(ref Point lpPoint);

			/// <summary>
			/// API used to send a message to another window
			/// </summary>
			/// <param name="hWnd">Handle to the window whose window procedure will receive the message.
			/// If this parameter is HWND_BROADCAST, the message is sent to all top-level windows in 'the' system,
			/// including disabled or invisible unowned windows, overlapped windows,
			/// 'and pop'-up windows; but the message is not sent to child windows. 
			/// </param>
			/// <param name="Msg">Specifies the message to be sent.</param>
			/// <param name="wParam">Specifies additional message-specific information.</param>
			/// <param name="lParam">Specifies additional message-specific information.</param>
			/// <returns>The return value specifies the result of the message processing and depends on the message sent.</returns>
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

			public static int SendMessage(int hWnd, uint Msg, int wParam, int lParam)
			{
				return SendMessage(new IntPtr(hWnd), Msg, new IntPtr(wParam), new IntPtr(lParam)).ToInt32();
			}

			public static int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam)
			{
				return SendMessage(hWnd, Msg, new IntPtr(wParam), new IntPtr(lParam)).ToInt32();
			}

			//WindowFromPoint: API is used to get the window handle by giving the
			// location
			//    Parameters
			//Point 
			//Specifies a POINT structure that defines the point to be checked. 
			//Return Values
			//A handle to the window that contains the point indicates success. NULL indicates
			//that no window exists at the specified point. A handle to the window under the
			//static' text control indicates that the point is over a static text control. 
			[DllImport("user32.dll")]
			internal static extern int WindowFromPoint(int xPoint, int yPoint);

			// GetClassName: By passing window handle this will return the class name of
			// window object.
			//    Parameters
			//hWnd 
			//Handle to the window and, indirectly, the class to which the window belongs. 
			//lpClassName 
			//Long pointer to the buffer that is to receive the class name string. 
			//nMaxCount 
			//Specifies the length, in characters, of the buffer pointed to by the lpClassName
			//parameter. The class name string is truncated if it is longer than the buffer. 
			//Return Values
			//    'The number of characters copied to the specified buffer indicates success.
			// Zero indicates failure. To get extended error information, call GetLastError. 
			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			internal static extern int GetClassName(ref IntPtr hWnd, string lpClassName, ref int nMaxCount);

		}

		public System.Diagnostics.Process RunAndMaximizeProcess(string strValFileName)
		{
			//=================================================================
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.FileName = strValFileName;
			// To run document use this:
			//p.StartInfo.UseShellExecute = true;
			//p.StartInfo.RedirectStandardOutput=false;
			p.Start();
			//p.WaitForExit();
			//p.Dispose(); 
			// if you wanted to get an event message when the process closes, you should do this:
			//p.EnableRaisingEvents = true;
			//p.CloseMainWindow() if the process has a Graphical Interface.
			//p.Close() if it doesn't have a Graphical Interface.
			WinAPI.SendMessage(p.MainWindowHandle, WinAPI.WM_SYSCOMMAND, WinAPI.SC_MAXIMIZE, 0);
			//-----------------------------------------------------------------			
			return p;
		}

		public System.Diagnostics.Process RunProcess(string strValFileName)
		{
			//=================================================================
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.FileName = strValFileName;
			// To run document use this:
			p.StartInfo.UseShellExecute = true;
			//p.StartInfo.RedirectStandardOutput=false;
			p.Start();
			//p.WaitForExit();
			//p.Dispose(); 
			// if you wanted to get an event message when the process closes, you should do this:
			//p.EnableRaisingEvents = true;
			//p.CloseMainWindow() if the process has a Graphical Interface.
			//p.Close() if it doesn't have a Graphical Interface.
			//WinAPI.SendMessage(p.MainWindowHandle, WinAPI.WM_SYSCOMMAND,WinAPI.SC_MAXIMIZE,0);
			//-----------------------------------------------------------------			
			return p;
		}


		public IntPtr FindProcessID(string strValClassName, string strWindowName)
		{
			// Determine the handle to the Application window. 
			var iHandle = WinAPI.FindWindow(strValClassName, strWindowName);
			// Post a message to Application to end its existence. 
			//int j=WinAPI.SendMessage(iHandle, WinAPI.WM_SYSCOMMAND, WinAPI.SC_CLOSE, 0); 
			return iHandle;
		}


		//=================================================================
		public void KillProcess(string strValProcessName)
		{
			System.Diagnostics.Process myproc = new System.Diagnostics.Process();
			//Get all instances of proc that are open, attempt to close them.
			try
			{
				foreach (System.Diagnostics.Process thisproc in System.Diagnostics.Process.GetProcessesByName(strValProcessName))
				{
					if (!thisproc.CloseMainWindow())
					{
						//If closing is not successful or no desktop window handle, then force termination.
						thisproc.Kill();
					}
				} // next proc
			}
			catch (Exception Exc)
			{
				this.Log.Append("Attempt to kill " + strValProcessName + " Failed. " + Exc.Message);
			}
		}


	}
}
