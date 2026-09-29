// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The virtual bus library (ViGEmClient.dll) that fails to load is retried at most once per gated
	/// attempt, using the same gate a refused connect already uses, and the failure is recorded once and
	/// logged once per change.
	/// </summary>
	[TestClass]
	public class ViGEmLibraryLoadTest
	{
		static object StaticField(string name)
		{
			return typeof(ViGEmClient).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		}

		static void SetStaticField(string name, object value)
		{
			typeof(ViGEmClient).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
		}

		static bool LibraryLoadDue()
		{
			return (bool)typeof(ViGEmClient).GetMethod("LibraryLoadDue", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
		}

		static void RecordLoad(bool failed)
		{
			typeof(ViGEmClient).GetMethod("RecordLoad", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { failed });
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A library that failed to load recently is not due again until the gate opens")]
		public void A_failed_load_is_not_due_again_until_the_gate_opens()
		{
			var oldFailed = StaticField("_LastLoadFailed");
			var oldTick = StaticField("_LastLoadFailTick");
			try
			{
				SetStaticField("_LastLoadFailed", true);
				SetStaticField("_LastLoadFailTick", Environment.TickCount);
				Assert.IsFalse(LibraryLoadDue(), "A load that just failed is due again at once.");
				SetStaticField("_LastLoadFailTick", Environment.TickCount - ViGEmClient.ConnectRetryMs - 1);
				Assert.IsTrue(LibraryLoadDue(), "A load that failed long ago is never tried again.");
				SetStaticField("_LastLoadFailed", false);
				Assert.IsTrue(LibraryLoadDue(), "A library that has not failed is gated as if it had.");
			}
			finally
			{
				SetStaticField("_LastLoadFailed", oldFailed);
				SetStaticField("_LastLoadFailTick", oldTick);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A failed load is recorded once, and logged only when the failure state changes")]
		public void A_failed_load_is_logged_once_per_change()
		{
			var oldFailed = StaticField("_LastLoadFailed");
			var oldTick = StaticField("_LastLoadFailTick");
			var oldException = StaticField("LastLoadException");
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				faults.Add(e.Exception);
				e.Cancel = true;
			};
			var log = LogHelper.Current;
			log.WritingException += keep;
			try
			{
				SetStaticField("_LastLoadFailed", false);
				SetStaticField("LastLoadException", new InvalidOperationException("load refused"));
				for (var i = 0; i < 1000; i++)
					RecordLoad(true);
				Assert.AreEqual(1, faults.Count, "1,000 failed loads alike wrote " + faults.Count + " fault reports.");
				RecordLoad(false);
				RecordLoad(true);
				Assert.AreEqual(2, faults.Count, "A failure after a load that succeeded was not written.");
			}
			finally
			{
				log.WritingException -= keep;
				SetStaticField("_LastLoadFailed", oldFailed);
				SetStaticField("_LastLoadFailTick", oldTick);
				SetStaticField("LastLoadException", oldException);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("isVBusExists consults the load gate and records the outcome, instead of loading on every pass")]
		public void The_load_gate_is_wired_into_the_bus_check()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var method = source.Substring(source.IndexOf("public static bool isVBusExists"));
			StringAssert.Contains(method, "LibraryLoadDue()", "isVBusExists loads the library again on every pass while it is failing.");
			StringAssert.Contains(method, "RecordLoad(", "A load's outcome is not recorded, so the gate never closes.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A library that keeps failing to load is written by RecordLoad alone, once per change, not by every attempt")]
		public void A_failed_load_is_written_only_when_it_changes()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var load = Ui.Between(source, "static void LoadLibrary()", "public static void FreeLibrary()");
			StringAssert.Contains(load, "LastLoadException = ex;", "A failed extraction is not kept for RecordLoad to write.");
			Assert.IsFalse(load.Contains("LogHelper.Current.Write"),
				"LoadLibrary writes its own failure, so a library that keeps failing is written on every gated attempt.");
		}
	}
}
