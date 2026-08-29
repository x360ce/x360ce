// @under-test: Engine/JocysCom/ComponentModel/BindingListInvoked.cs
// @area: settings   @layer: unit
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace x360ce.Tests
{
	/// <summary>
	/// The device list is changed from the thread that watches the hardware and drawn by the
	/// thread that owns the window, so changes are carried across. Each change said which
	/// position to act on, and a position counted on one thread means something else by the
	/// time the other thread gets to it: two devices leaving at once removed the wrong one, or
	/// asked for a position past the end and closed the program. These tests pin the list
	/// against both outcomes.
	/// </summary>
	[TestClass]
	public class BindingListStaleIndexTest
	{
		/// <summary>Plain item. The list places no constraint on what it holds.</summary>
		public sealed class Row
		{
			public string Name { get; set; }
			public override string ToString() { return Name; }
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Two removals decided together remove those two items, not their neighbours")]
		public void Two_removals_take_the_items_they_named()
		{
			using (var owner = new DeferredOwner())
			{
				var list = owner.NewList();
				var a = new Row { Name = "A" };
				var b = new Row { Name = "B" };
				var c = new Row { Name = "C" };
				owner.FromAnotherThread(() => { list.Add(a); list.Add(b); list.Add(c); });
				owner.Apply();
				CollectionAssert.AreEqual(new[] { a, b, c }, new List<Row>(list),
					"The list did not start in the state the rest of the test depends on.");

				// Both removals are decided before either is applied, which is what happens when
				// two devices are unplugged together.
				owner.FromAnotherThread(() => { list.Remove(a); list.Remove(b); });
				owner.Apply();

				Assert.AreEqual(0, owner.Failures.Count,
					"Applying the removals failed with: " + Describe(owner.Failures)
					+ " The second removal was written down as a position, and by the time it "
					+ "was applied the list was shorter than that position.");
				CollectionAssert.AreEqual(new[] { c }, new List<Row>(list),
					"The wrong items were removed. Each removal names a position in the list as "
					+ "it was when the removal was decided, so the second one lands on whatever "
					+ "has moved into that position since.");
			}
		}

		[TestMethod, TestCategory("settings")]
		[Description("An item removed twice over is removed once, without failing")]
		public void Removing_an_item_that_has_already_gone_does_nothing()
		{
			using (var owner = new DeferredOwner())
			{
				var list = owner.NewList();
				var a = new Row { Name = "A" };
				var b = new Row { Name = "B" };
				owner.FromAnotherThread(() => { list.Add(a); list.Add(b); });
				owner.Apply();

				owner.FromAnotherThread(() => { list.Remove(a); list.Remove(a); });
				owner.Apply();

				Assert.AreEqual(0, owner.Failures.Count,
					"Removing the same item twice failed with: " + Describe(owner.Failures));
				CollectionAssert.AreEqual(new[] { b }, new List<Row>(list));
			}
		}

		[TestMethod, TestCategory("settings")]
		[Description("Items added while earlier additions are still waiting all arrive")]
		public void Additions_decided_together_all_arrive()
		{
			using (var owner = new DeferredOwner())
			{
				var list = owner.NewList();
				var rows = new List<Row>();
				for (var i = 0; i < 5; i++)
					rows.Add(new Row { Name = "Row " + i });
				owner.FromAnotherThread(() => { foreach (var row in rows) list.Add(row); });
				owner.Apply();

				Assert.AreEqual(0, owner.Failures.Count,
					"Applying the additions failed with: " + Describe(owner.Failures));
				CollectionAssert.AreEqual(rows, new List<Row>(list),
					"The items arrived in the wrong order. Adding to the list asks for the "
					+ "position after the last item, and every addition still waiting asks for "
					+ "the same one.");
			}
		}

		static string Describe(IList<Exception> failures)
		{
			var names = new List<string>();
			foreach (var failure in failures)
				names.Add(failure.GetType().Name + ": " + failure.Message);
			return names.Count == 0 ? "nothing" : string.Join("; ", names.ToArray());
		}

		/// <summary>
		/// Stands in for the thread that owns the window: changes made anywhere else are held
		/// until this thread asks for them, which is the gap a real window leaves while it is
		/// busy drawing.
		/// </summary>
		sealed class DeferredOwner : TaskScheduler, IDisposable
		{
			readonly List<Task> _waiting = new List<Task>();
			readonly SynchronizationContext _replaced;

			public readonly List<Exception> Failures = new List<Exception>();

			public DeferredOwner()
			{
				// The helper decides "am I on the owning thread?" by comparing thread numbers,
				// and binds to the first thread that asks. An earlier test may have bound it to
				// a thread that has since ended, so it is unbound and claimed here.
				_replaced = SynchronizationContext.Current;
				if (_replaced is null)
					SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
				Release();
				ControlsHelper.InitInvokeContext();
			}

			public BindingListInvoked<Row> NewList()
			{
				return new BindingListInvoked<Row> { SynchronizingObject = this, AsynchronousInvoke = true };
			}

			/// <summary>Runs the changes somewhere else, so they have to be carried across.</summary>
			public void FromAnotherThread(Action change)
			{
				Exception failure = null;
				var thread = new Thread(() =>
				{
					try { change(); }
					catch (Exception ex) { failure = ex; }
				});
				thread.IsBackground = true;
				thread.Start();
				Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)),
					"The thread making the change never finished. Making a change was supposed "
					+ "to hand the work over, not wait for the owning thread to take it.");
				if (failure != null)
					throw failure;
			}

			/// <summary>Takes everything that was handed over, the way a window does when it is free again.</summary>
			public void Apply()
			{
				while (true)
				{
					Task[] batch;
					lock (_waiting)
					{
						if (_waiting.Count == 0)
							return;
						batch = _waiting.ToArray();
						_waiting.Clear();
					}
					foreach (var task in batch)
					{
						TryExecuteTask(task);
						if (task.Exception != null)
							Failures.Add(task.Exception.GetBaseException());
					}
				}
			}

			protected override void QueueTask(Task task)
			{
				lock (_waiting)
					_waiting.Add(task);
			}

			protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
			{
				return false;
			}

			protected override IEnumerable<Task> GetScheduledTasks()
			{
				lock (_waiting)
					return _waiting.ToArray();
			}

			public void Dispose()
			{
				// Left unbound so the next test binds it to its own thread rather than to this one.
				Release();
				if (_replaced is null)
					SynchronizationContext.SetSynchronizationContext(null);
			}

			static void Release()
			{
				typeof(ControlsHelper).GetProperty("MainTaskScheduler").SetValue(null, null, null);
			}
		}
	}
}
