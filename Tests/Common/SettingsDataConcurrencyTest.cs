// @under-test: Engine/JocysCom/Configuration/SettingsData.cs, Engine/JocysCom/ComponentModel/BindingListInvoked.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// The device refresh thread reads these collections while the interface thread changes them.
	/// Two locks used to guard one collection: readers took SettingsData.SyncRoot, changes made
	/// through the list took a private lock inside BindingListInvoked, and neither excluded the
	/// other. Reports from 4.18.6.0 and 4.18.7.0 show both halves of that race, as
	/// "Collection was modified" while enumerating and as "Destination array was not long enough"
	/// inside ItemsToArraySyncronized. These tests pin the collection against both.
	/// </summary>
	[TestClass]
	public class SettingsDataConcurrencyTest
	{
		/// <summary>Plain item. The collection places no constraint on what it holds.</summary>
		public class Row { public Guid Id { get; set; } }

		const int Seconds = 3;

		[TestMethod, TestCategory("settings"), TestCategory("smoke")]
		[Description("Reading a snapshot while another thread changes the list does not throw")]
		public void Snapshot_survives_concurrent_changes()
		{
			var data = new JocysCom.ClassLibrary.Configuration.SettingsData<Row>();
			RunRace(data, () => { var items = data.ItemsToArraySyncronized(); GC.KeepAlive(items); });
		}

		[TestMethod, TestCategory("settings")]
		[Description("Enumerating the live list while another thread changes it does not throw")]
		public void Enumeration_survives_concurrent_changes()
		{
			var data = new JocysCom.ClassLibrary.Configuration.SettingsData<Row>();
			RunRace(data, () =>
			{
				// Readers must take the same lock the list takes for its own changes.
				lock (data.SyncRoot)
				{
					var count = 0;
					foreach (var item in data.Items)
						count += item == null ? 0 : 1;
					GC.KeepAlive(count);
				}
			});
		}

		/// <summary>
		/// Runs <paramref name="read"/> in a loop on this thread while a second thread adds and
		/// removes items, and fails with the first exception either thread sees.
		/// </summary>
		static void RunRace(JocysCom.ClassLibrary.Configuration.SettingsData<Row> data, Action read)
		{
			// Start with enough items that a copy is long enough to be caught mid-resize.
			for (int i = 0; i < 64; i++)
				data.Items.Add(new Row { Id = Guid.NewGuid() });
			Exception failure = null;
			var stop = false;
			// The writer does not take SyncRoot. That is the point: a change arriving through the
			// list itself must still exclude a reader, or nothing the reader does can be safe.
			var writer = new Thread(() =>
			{
				try
				{
					while (!Volatile.Read(ref stop))
					{
						data.Items.Add(new Row { Id = Guid.NewGuid() });
						if (data.Items.Count > 128)
							data.Items.RemoveAt(0);
					}
				}
				catch (Exception ex) { Interlocked.CompareExchange(ref failure, ex, null); }
			});
			writer.IsBackground = true;
			writer.Start();
			try
			{
				var watch = Stopwatch.StartNew();
				while (watch.Elapsed < TimeSpan.FromSeconds(Seconds) && Volatile.Read(ref failure) == null)
					read();
			}
			catch (Exception ex) { Interlocked.CompareExchange(ref failure, ex, null); }
			finally
			{
				Volatile.Write(ref stop, true);
				writer.Join(TimeSpan.FromSeconds(5));
			}
			if (failure != null)
				Assert.Fail("Reading the collection while it changed threw "
					+ failure.GetType().Name + ": " + failure.Message);
		}
	}
}
