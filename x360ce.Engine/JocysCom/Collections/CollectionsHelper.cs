using System.Collections.Generic;
using System.Linq;

namespace JocysCom.ClassLibrary.Collections
{
	public static partial class CollectionsHelper
	{

		/// <summary>
		/// Synchronize source collection to destination.
		/// </summary>
		public static void Synchronize<T>(IList<T> source, IList<T> target)
		{
			// Convert to array to avoid modification of collection during processing.
			var sList = source.ToArray();
			var t = 0;
			for (var s = 0; s < sList.Length; s++)
			{
				var item = sList[s];
				// If item exists in destination and is in the correct position then continue
				if (t < target.Count && target[t].Equals(item))
				{
					t++;
					continue;
				}
				// If item is in destination but not at the correct position, remove it.
				var indexInDestination = target.IndexOf(item);
				if (indexInDestination != -1)
					target.RemoveAt(indexInDestination);
				// Insert item at the correct position.
				target.Insert(s, item);
				t = s + 1;
			}
			// Remove extra items.
			while (target.Count > sList.Length)
				target.RemoveAt(target.Count - 1);
		}

	}
}
