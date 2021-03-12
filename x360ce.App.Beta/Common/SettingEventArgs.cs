using System;

namespace x360ce.App
{
	public class SettingEventArgs : EventArgs
	{

		public SettingEventArgs(string name, int count)
		{
			_name = name;
			_count = count;
		}

		public string Name => _name;
		string _name;

		public int Count => _count;
		int _count;

	}
}
