using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class LogTextBoxUserControl : UserControl
	{
		public LogTextBoxUserControl()
		{
			InitializeComponent();
		}

		#region Logs

		class NativeMethods
		{
			[DllImport("user32")]
			public static extern int GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO scrollInfo);

			public struct SCROLLINFO
			{
				public int cbSize;
				public int fMask;
				public int min;
				public int max;
				public int nPage;
				public int nPos;
				public int nTrackPos;
			}
		}

		object AddLogLock = new object();
		bool AddLogInitialized;

		public static bool ReachedBottom(RichTextBox rtb)
		{
			var scrollInfo = new NativeMethods.SCROLLINFO();
			scrollInfo.cbSize = Marshal.SizeOf(scrollInfo);
			//SIF_RANGE = 0x1, SIF_TRACKPOS = 0x10,  SIF_PAGE= 0x2
			scrollInfo.fMask = 0x10 | 0x1 | 0x2;
			NativeMethods.GetScrollInfo(rtb.Handle, 1, ref scrollInfo);//nBar = 1 -> VScrollbar
			return scrollInfo.max <= scrollInfo.nTrackPos + scrollInfo.nPage;
		}

		public int MaxLines = 50;

		//#region Logs

		//public void AddLog(string format, params object[] args)
		//{
		//	Invoke(new Action(() => LogTextBoxPanel.AddLog(format, args)));
		//}

		//#endregion

		public void AddLog(string format, params object[] args)
		{
			lock (AddLogLock)
			{
				var box = LogTextBox;
				var p = new System.Drawing.Point(box.Width, box.Height);
				int charIndex = box.GetCharIndexFromPosition(p);
				var mustScroll = ReachedBottom(box);
				//ShowLabel.Text = string.Format("{0}", mustScroll);
				var selectionStart = box.SelectionStart;
				var selectionLength = box.SelectionLength;
				// Note: use RichTextBox for flicker free updates.
				if (!AddLogInitialized)
				{
					box.DetectUrls = false;
					box.ShortcutsEnabled = false;
					AddLogInitialized = true;
				}
				var lines = box.Lines;
				box.SuspendLayout();
				// Get lines of text.
				// Maximum 50 lines (with one which will be added).
				var extra = lines.Count() - MaxLines - 1;
				if (extra > 0)
				{
					var length = box.GetFirstCharIndexFromLine(extra);
					// Select all extra lines.
					box.Select(0, length);
					box.SelectedText = "";
					// If text was selected then...
					if (selectionLength > 0)
					{
						// Calculate new selection.
						selectionStart -= length;
						if (selectionStart < 0)
						{
							// Cut into selection.
							selectionLength += selectionStart;
						}
					}
				}
				// If box already contains text then...
				var restoreFocus = ActiveControl == box;
				if (restoreFocus && box.Parent.Visible)
				{
					ActiveControl = box.Parent;
				}
				if (box.Text.Length > 0)
				{
					box.AppendText(Environment.NewLine);
				}
				var text = args is null || args.Length == 0
					? format
					: string.Format(format, args);
				box.AppendText(text);
				if (restoreFocus && box.Visible)
					ActiveControl = box;
				if (mustScroll)
				{
					// Scroll to the end.
					box.SelectionStart = box.Text.Length;
					box.ScrollToCaret();
					box.DeselectAll();
					// Set other control as active or AppendText is going to auto-scroll to the bottom.
					if (restoreFocus && box.Visible)
						ActiveControl = box;
				}
				else if (selectionLength > 0)
				{
					// Reselect selected text.
					box.Select(Math.Max(0, selectionStart), selectionLength);
				}
				else
				{
					box.DeselectAll();
				}
				box.ResumeLayout(false);
			}
		}

		#endregion
	}
}
