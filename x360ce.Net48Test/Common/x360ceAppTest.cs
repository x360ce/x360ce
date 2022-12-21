using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.App;
using x360ce.App.Controls;

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

    }
}
