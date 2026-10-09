using System;
using x360ce.Engine;

namespace x360ce.App
{
	public class CloudItem: CloudItem<object>
	{
	}
	
	public class CloudItem<T> where T: class
	{
		public CloudAction Action { get; set; }
		public CloudState State { get; set; }
		public T Item { get; set; }
		public DateTime Date;
		public string Description
		{
			get
			{
				return Item.ToString();
			}
		}
		
	}
}
