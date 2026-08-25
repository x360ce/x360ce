using System;
using System.Drawing;

namespace JocysCom.ClassLibrary.Processes
{
	public class MouseHookEventArgs : EventArgs
	{
		public MouseHookEventArgs()
		{
		}

		public MouseHookEventArgs(MSLLHOOKSTRUCT data, CURSORINFO info, MouseKey keys, int param, int lastX, int lastY, Bitmap image)
		{
			_data = data;
			_info = info;
			_keys = keys;
			_Param = param;
			_LastX = lastX;
			_LastY = lastY;
			_Image = image;
		}

		public MSLLHOOKSTRUCT Data { get { return _data; } }
		MSLLHOOKSTRUCT _data;

		public CURSORINFO Info { get { return _info; } }
		CURSORINFO _info;

		public MouseKey Keys { get { return _keys; } }
		MouseKey _keys;

		public int Param { get { return _Param; } }
		int _Param;

		public int LastX { get { return _LastX; } }
		int _LastX;

		public int LastY { get { return _LastY; } }
		int _LastY;

		public Bitmap Image { get { return _Image; } }
		Bitmap _Image;


	}
}
