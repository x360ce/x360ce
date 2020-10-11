using System.Collections.Generic;
using System.Linq;

namespace JocysCom.ClassLibrary.Collections
{
	public static partial class CollectionsHelper
	{

		/// <summary>
		/// Synchronize source collection to destination.
		/// </summary>
		public static void Synchronize<T>(IList<T> source, IList<T> destination)
		{
			var sList = source.ToArray();
			for (var s = 0; s < sList.Length; s++)
			{
				var sItem = sList[s];
				var d = destination.IndexOf(sItem);
				// If item is missing in destination then...
				if (d < 0)
				{
					// Insert item at same position.
					destination.Insert(s, sItem);
					continue;
				}
				// Remove items which do not exists in source from current position.
				while (!sList.Contains(destination[s]))
					destination.Remove(destination[s]);
				// Refresh destination index.
				d = destination.IndexOf(sItem);
				// If items are not at the same position then continue.
				if (s != d)
				{
					// Move item to current position.
					destination.Remove(sItem);
					destination.Insert(s, sItem);
				}
			}
			// Remove extra deleted records.
			while (destination.Count() > sList.Count())
				destination.RemoveAt(destination.Count - 1);
		}
	}
}
