using System;
using System.Collections.Generic;
using System.Text;

namespace x360ce.App
{
	public class SettingEventArgs : EventArgs
	{

		public SettingEventArgs(string name, int count)
		{
			_name = name;
			_count = count;
		}

		string _name;
		public string Name { get { return _name; } }

		int _count;
		public int Count { get { return _count; } }

	}
}
