using System.Threading;

namespace x360ce.Engine
{
	/// <summary>Hands the newest of a series of objects from one writer thread to one reader thread, with no lock, and without either side waiting or making anything.</summary>
	/// <remarks>
	/// Three objects take turns. The writer fills <see cref="Back"/> and calls <see cref="Publish"/>, which swaps it with
	/// the object in the middle. The reader calls <see cref="Take"/>, which swaps <see cref="Front"/> with the middle
	/// object when that one is newer, and then reads <see cref="Front"/>. The middle object's index and the mark that it
	/// is newer than the reader's are one number, swapped in one step, so the reader never takes an object the writer is
	/// filling, nor one older than the one it has. An object published twice before the reader takes it is skipped: the
	/// reader always gets the newest. The writer fills the whole of <see cref="Back"/>, which holds whatever an earlier
	/// turn left in it.
	/// </remarks>
	public sealed class TripleBuffer<T> where T : class
	{
		/// <summary>Set beside the middle index while the middle object is newer than the reader's.</summary>
		const int Fresh = 4;
		const int IndexMask = 3;

		readonly T[] items;
		int back;
		/// <summary>The middle object's index, with <see cref="Fresh"/> when the writer put it there.</summary>
		int middle;
		int front;

		/// <summary>Three objects, each of which the writer and the reader use in turn: <paramref name="first"/> starts as the writer's, <paramref name="third"/> as the reader's.</summary>
		public TripleBuffer(T first, T second, T third)
		{
			items = new[] { first, second, third };
			back = 0;
			middle = 1;
			front = 2;
		}

		/// <summary>The object the writer fills next. Writer thread only.</summary>
		public T Back { get { return items[back]; } }

		/// <summary>Hands <see cref="Back"/> to the reader and gives the writer another object to fill. Writer thread only.</summary>
		public void Publish()
		{
			back = Interlocked.Exchange(ref middle, back | Fresh) & IndexMask;
		}

		/// <summary>Makes the newest published object <see cref="Front"/>, when it is newer than the one there. Reader thread only.</summary>
		/// <returns>True when <see cref="Front"/> is another object now.</returns>
		public bool Take()
		{
			if ((Volatile.Read(ref middle) & Fresh) == 0)
				return false;
			front = Interlocked.Exchange(ref middle, front) & IndexMask;
			return true;
		}

		/// <summary>The newest object the reader has taken. The writer does not touch it until the reader takes another. Reader thread only.</summary>
		public T Front { get { return items[front]; } }
	}
}
