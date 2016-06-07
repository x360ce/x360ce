using System;

namespace JocysCom.ClassLibrary.Processes
{
	public class MouseHookEventArgs : EventArgs
	{

		public MouseHookEventArgs(MSLLHOOKSTRUCT data, CURSORINFO info, MouseKey keys, int param, int lastX, int lastY)
		{
			_data = data;
			_info = info;
			_keys = keys;
			_Param = param;
			_LastX = lastX;
			_LastY = lastY;
		}

		MSLLHOOKSTRUCT _data;
		CURSORINFO _info;
		MouseKey _keys;
		int _Param;
		int _LastX;
		int _LastY;
		public MSLLHOOKSTRUCT Data { get { return _data; } }
		public CURSORINFO Info { get { return _info; } }
		public MouseKey Keys { get { return _keys; } }
		public int Param { get { return _Param; } }
		public int LastX  { get { return _LastX; } }
		public int LastY  { get { return _LastY; } }
		}
}
