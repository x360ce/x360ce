using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App
{
	public class CloudItem
	{
		public CloudAction Action { get; set; }
		public CloudState State { get; set; }
		public object Item { get; set; }
		public DateTime Date { get; set; }
		public string Description
		{
			get
			{
				var dm = Item as IDisplayName;
				var name = dm == null ? string.Format("{0}", Item) :
					string.Format("{0}: {1}", Item.GetType().Name, dm.DisplayName);
				return name;
			}
		}

	}

}
