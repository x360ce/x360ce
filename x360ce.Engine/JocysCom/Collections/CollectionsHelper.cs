using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JocysCom.ClassLibrary.Collections
{
	public static partial class CollectionsHelper
	{

		/// <summary>
		/// Synchronize source collection to destination.
		/// </summary>
		/// <remarks>Try to use quick sort algorithm by using uniqueSortName/index.</remarks>
		public static void Synchronize<T>(IList<T> source, IList<T> target, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;
			// Create a dictionary for fast lookup in source list
			var sourceSet = new Dictionary<T, int>();
			for (int i = 0; i < source.Count; i++)
				sourceSet[source[i]] = i;
			// Iterate over the target, remove items not in source
			for (int i = target.Count - 1; i >= 0; i--)
				if (!sourceSet.Keys.Contains(target[i], comparer))
					target.RemoveAt(i);
			// Iterate over source
			for (int si = 0; si < source.Count; si++)
			{
				// If item is not present in target, insert it.
				// Note: Only check items that have not been synchornized in the target.
				if (!target.Skip(si).Contains(source[si], comparer))
				{
					target.Insert(si, source[si]);
					continue;
				}
				// If item is present in target but not at the right position, move it.
				int ti = -1;
				// Note: Only check items that have not been synchornized in the target.
				for (int i = si; i < target.Count; i++)
				{
					if (comparer.Equals(target[i], source[si]))
					{
						ti = i;
						break;
					}
				}
				if (ti != si)
				{
					var oc = target as ObservableCollection<T>;
					// If observable collection then
					if (oc != null)
					{
						// Move item without triggering remove and insert events.
						oc.Move(si, ti);
					}
					else
					{
						// Removes and inserts item and
						// can disrupt data binding in WPF controls.
						var item = target[ti];
						target.RemoveAt(ti);
						target.Insert(si, item);
					}
				}
			}
			// Remove items at the end of target that exceed source's length
			while (target.Count > source.Count)
				target.RemoveAt(target.Count - 1);
		}

	}
}
