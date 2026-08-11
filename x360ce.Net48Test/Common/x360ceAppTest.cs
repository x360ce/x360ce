using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Diagnostics;
using x360ce.App.DInput;
using x360ce.App.Issues;

namespace x360ce.Tests
{
    [TestClass]
    public class x360ceAppTest
    {
        [TestMethod]
        public void Test_All() =>
        MemoryLeakHelper.Test(typeof(App.App).Assembly, null, new Type[] {
            typeof(Nefarius.ViGEm.Client.ViGEmClient),
            typeof(x360ce.App.App),
            typeof(x360ce.App.MainBodyControl),
            typeof(x360ce.App.MainControl),
            typeof(x360ce.App.MainWindow),
            typeof(x360ce.App.Forms.DebugWindow),
            typeof(x360ce.App.Forms.WebBrowserWindow),
            typeof(x360ce.App.Forms.HardwareWindow),
            typeof(x360ce.App.SettingsManager),
            typeof(DebugControl),
            typeof(OptionsControl),
            typeof(OptionsHidGuardianControl),
            typeof(OptionsRemoteControllerControl),
            typeof(OptionsVirtualDeviceControl),
            typeof(JocysCom.ClassLibrary.IO.HardwareControl),
        });

        /// <summary>
        /// Test fails without resources supplied in Application class.
        /// </summary>
        [TestMethod]
        public void Test_AboutUserControl() =>
            MemoryLeakHelper.Test<AboutUserControl>();

        [TestMethod]
        public void Test_PadItem_AdvancedControl() =>
            MemoryLeakHelper.Test<PadItem_AdvancedControl>();

        [TestMethod]
        public void Test_PadItem_AxisToButtonControl() =>
            MemoryLeakHelper.Test<AxisToButtonControl>();

        [TestMethod]
        public void Test_PadItem_CloudControl() =>
            MemoryLeakHelper.Test<CloudControl>();

        [TestMethod]
        public void Test_PadItem_SettingsManager() =>
        MemoryLeakHelper.Test<SettingsManager>();

        [TestMethod]
        public void CppRuntimeDetector_RecognizesV1451X86From32BitRegistryView()
        {
            var registry = new FakeCppRuntimeRegistry();
            registry.Values[RegistryView.Registry32] = new CppRuntimeRegistryValue
            {
                Installed = 1,
                Version = "v14.51.36247.00",
            };

            var result = new CppRuntimeDetector(registry, true).Detect(CppRuntimeArchitecture.X86);

            Assert.IsTrue(result.IsInstalled);
            Assert.AreEqual(new Version(14, 51, 36247, 0), result.Version);
            Assert.AreEqual(RegistryView.Registry32, result.RegistryView);
        }

        [TestMethod]
        public void CppRuntimeDetector_RecognizesV1451X64From64BitRegistryView()
        {
            var registry = new FakeCppRuntimeRegistry();
            registry.Values[RegistryView.Registry64] = new CppRuntimeRegistryValue
            {
                Installed = 1,
                Version = "v14.51.36247.00",
            };

            var result = new CppRuntimeDetector(registry, true).Detect(CppRuntimeArchitecture.X64);

            Assert.IsTrue(result.IsInstalled);
            Assert.AreEqual(new Version(14, 51, 36247, 0), result.Version);
            Assert.AreEqual(RegistryView.Registry64, result.RegistryView);
        }

        [TestMethod]
        public void CppRuntimeDetector_RequiresInstalledFlagAndValidV14Version()
        {
            var registry = new FakeCppRuntimeRegistry();
            registry.Values[RegistryView.Registry32] = new CppRuntimeRegistryValue
            {
                Installed = 0,
                Version = "v14.51.36247.00",
            };

            var result = new CppRuntimeDetector(registry, true).Detect(CppRuntimeArchitecture.X86);

            Assert.IsFalse(result.IsInstalled);
        }

        [TestMethod]
        public void CppRuntimeDetector_UsesNumericComponentsWhenVersionStringIsMissing()
        {
            var registry = new FakeCppRuntimeRegistry();
            registry.Values[RegistryView.Registry32] = new CppRuntimeRegistryValue
            {
                Installed = 1,
                Major = 14,
                Minor = 51,
                Build = 36247,
                Revision = 0,
            };

            var result = new CppRuntimeDetector(registry, true).Detect(CppRuntimeArchitecture.X86);

            Assert.IsTrue(result.IsInstalled);
            Assert.AreEqual(new Version(14, 51, 36247, 0), result.Version);
        }

        [TestMethod]
        public void CppRuntimeDetector_RegistryFailureReturnsDiagnosticInsteadOfThrowing()
        {
            var registry = new FakeCppRuntimeRegistry { ReadException = new UnauthorizedAccessException("denied") };

            var result = new CppRuntimeDetector(registry, true).Detect(CppRuntimeArchitecture.X86);

            Assert.IsFalse(result.IsInstalled);
            StringAssert.Contains(result.ErrorMessage, "UnauthorizedAccessException");
        }

        [TestMethod]
        public void CppRuntimeDetector_X64IsNotApplicableOn32BitWindows()
        {
            var result = new CppRuntimeDetector(new FakeCppRuntimeRegistry(), false)
                .Detect(CppRuntimeArchitecture.X64);

            Assert.IsFalse(result.IsApplicable);
            Assert.IsFalse(result.IsInstalled);
        }

        [TestMethod]
        public void ViGEmHealth_WorkingExternalBusIsUsableWithoutInstallAction()
        {
            var probe = new FakeViGEmBusProbe
            {
                Driver = new ViGEmDriverInfo(true, new Version(1, 22, 0), "ViGEm Bus Driver"),
                ServiceState = ViGEmServiceState.Running,
                Client = new ViGEmClientProbeResult(ViGEmClientConnectionState.Successful),
            };

            var result = new ViGEmBusHealthDetector(probe).Detect();

            Assert.IsTrue(result.Installed);
            Assert.IsTrue(result.ServicePresent);
            Assert.IsTrue(result.DriverRunning);
            Assert.IsTrue(result.ApiConnectionSuccessful);
            Assert.IsTrue(result.IsUsable);
            Assert.IsFalse(result.ShouldOfferInstall);
        }

        [TestMethod]
        public void ViGEmHealth_SeparatesStoppedServiceFromMissingDriver()
        {
            var probe = new FakeViGEmBusProbe
            {
                Driver = new ViGEmDriverInfo(true, new Version(1, 22, 0), "ViGEm Bus Driver"),
                ServiceState = ViGEmServiceState.Stopped,
                Client = new ViGEmClientProbeResult(ViGEmClientConnectionState.BusNotFound),
            };

            var result = new ViGEmBusHealthDetector(probe).Detect();

            Assert.IsTrue(result.Installed);
            Assert.IsTrue(result.ServicePresent);
            Assert.IsFalse(result.DriverRunning);
            Assert.IsFalse(result.ApiConnectionSuccessful);
            Assert.IsFalse(result.ShouldOfferInstall);
        }

        [TestMethod]
        public void ViGEmHealth_ReportsVersionMismatchExplicitly()
        {
            var probe = new FakeViGEmBusProbe
            {
                Driver = new ViGEmDriverInfo(true, new Version(1, 14, 3), "ViGEm Bus Driver"),
                ServiceState = ViGEmServiceState.Running,
                Client = new ViGEmClientProbeResult(ViGEmClientConnectionState.VersionIncompatible, "bus version mismatch"),
            };

            var result = new ViGEmBusHealthDetector(probe).Detect();

            Assert.IsTrue(result.VersionIncompatible);
            Assert.IsFalse(result.IsUsable);
            StringAssert.Contains(result.ErrorMessage, "version mismatch");
        }

        [TestMethod]
        public void ViGEmHealth_IsolatesProbeExceptions()
        {
            var probe = new FakeViGEmBusProbe { DriverException = new InvalidOperationException("bad device") };

            var result = new ViGEmBusHealthDetector(probe).Detect();

            Assert.IsFalse(result.Installed);
            StringAssert.Contains(result.ErrorMessage, "bad device");
        }

        [TestMethod]
        public void OperationalLog_WritesStructuredStageDuration()
        {
            var folder = Path.Combine(Path.GetTempPath(), "x360ce-log-test-" + Guid.NewGuid());
            try
            {
                string file;
                using (var log = new OperationalLog(folder, 10))
                {
                    using (log.Measure("settings_load"))
                    {
                    }
                    file = log.CurrentFilePath;
                }

                var text = File.ReadAllText(file);
                StringAssert.Contains(text, "\"event\":\"startup_stage_started\"");
                StringAssert.Contains(text, "\"event\":\"startup_stage_completed\"");
                StringAssert.Contains(text, "\"durationMs\":");
                StringAssert.Contains(text, "\"sessionId\":");
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [TestMethod]
        public void OperationalLog_RetainsConfiguredFileCount()
        {
            var folder = Path.Combine(Path.GetTempPath(), "x360ce-log-test-" + Guid.NewGuid());
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    using (var log = new OperationalLog(folder, 3))
                        log.Write("test_launch");
                }

                Assert.AreEqual(3, Directory.GetFiles(folder, "x360ce-*.jsonl").Count());
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [TestMethod]
        public void ControllerPipelineHealth_RequiresSuccessfulStateSubmit()
        {
            var health = new x360ce.App.DInput.ControllerPipelineHealth
            {
                PhysicalInputOk = true,
                MappingOk = true,
                VirtualBusOk = true,
                VirtualTargetConnected = true,
                StateSubmitOk = false,
            };

            Assert.IsFalse(health.IsHealthy);
            health.StateSubmitOk = true;
            Assert.IsTrue(health.IsHealthy);
        }

        [TestMethod]
        public void DiagnosticReport_ContainsDependencyAndPipelineStages()
        {
            var runtimeRegistry = new FakeCppRuntimeRegistry();
            runtimeRegistry.Values[RegistryView.Registry32] = new CppRuntimeRegistryValue
            {
                Installed = 1,
                Version = "14.51.36247",
            };
            runtimeRegistry.Values[RegistryView.Registry64] = new CppRuntimeRegistryValue
            {
                Installed = 1,
                Version = "14.51.36247",
            };
            var runtimeDetector = new CppRuntimeDetector(runtimeRegistry, true);
            var x86 = runtimeDetector.Detect(CppRuntimeArchitecture.X86);
            var x64 = runtimeDetector.Detect(CppRuntimeArchitecture.X64);
            var bus = new ViGEmBusHealthDetector(new FakeViGEmBusProbe
            {
                Driver = new ViGEmDriverInfo(true, new Version(1, 22), "ViGEm Bus Driver"),
                ServiceState = ViGEmServiceState.Running,
                Client = new ViGEmClientProbeResult(ViGEmClientConnectionState.Successful),
            }).Detect();
            var controller = new ControllerPipelineHealth
            {
                PhysicalInputOk = true,
                MappingOk = true,
                VirtualBusOk = true,
                VirtualTargetConnected = true,
                StateSubmitOk = true,
            };

            var text = DiagnosticReport.Build("5.1", "Windows", x86, x64, bus,
                new[] { controller }, new[]
                {
                    "{\"event\":\"startup_stage_completed\",\"durationMs\":12}",
                    "{\"event\":\"unhandled_exception\",\"stackTrace\":\"C:\\\\Users\\\\private\"}",
                });

            StringAssert.Contains(text, "VC++ x86: Installed, 14.51.36247");
            StringAssert.Contains(text, "ViGEm API/client: Successful");
            StringAssert.Contains(text, "Physical input OK: Yes");
            StringAssert.Contains(text, "State submit OK: Yes");
            StringAssert.Contains(text, "Recent sanitized operational events");
            StringAssert.Contains(text, "startup_stage_completed");
            Assert.IsFalse(text.Contains("C:\\Users\\private"));
            Assert.IsFalse(text.Contains("stackTrace"));
        }

        [TestMethod]
        public async Task StartupStageRunner_DeadlineReturnsWithoutWaitingForHungWork()
        {
            var stopwatch = Stopwatch.StartNew();

            var completed = await StartupStageRunner.RunAsync(
                token => Thread.Sleep(1000),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            Assert.IsFalse(completed);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
                "A timed-out startup stage must not hold the UI startup path.");
        }

        private sealed class FakeCppRuntimeRegistry : ICppRuntimeRegistry
        {
            public readonly Dictionary<RegistryView, CppRuntimeRegistryValue> Values =
                new Dictionary<RegistryView, CppRuntimeRegistryValue>();

            public Exception ReadException { get; set; }

            public CppRuntimeRegistryValue Read(RegistryView view, CppRuntimeArchitecture architecture)
            {
				if (ReadException != null)
					throw ReadException;
                Values.TryGetValue(view, out var value);
                return value;
            }
        }

        private sealed class FakeViGEmBusProbe : IViGEmBusProbe
        {
            public ViGEmDriverInfo Driver { get; set; } = new ViGEmDriverInfo(false, null, null);
            public ViGEmServiceState ServiceState { get; set; } = ViGEmServiceState.Missing;
            public ViGEmClientProbeResult Client { get; set; } =
                new ViGEmClientProbeResult(ViGEmClientConnectionState.NotAttempted);
            public Exception DriverException { get; set; }

            public ViGEmDriverInfo GetDriverInfo()
            {
                if (DriverException != null)
                    throw DriverException;
                return Driver;
            }

            public ViGEmServiceState GetServiceState() => ServiceState;

            public ViGEmClientProbeResult ConnectClient() => Client;
        }

    }
}
