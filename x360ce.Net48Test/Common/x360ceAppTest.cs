using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace x360ce.Tests
{
	[TestClass]
	public class x360ceAppTest
	{
		[TestMethod]
		public void Test_x360ce_App() =>
		MemoryLeakHelper.Test(typeof(App.App).Assembly);

		[TestMethod]
		public void Test_x360ce_App_PadItem_AdvancedControl() =>
			MemoryLeakHelper.Test<App.Controls.PadItem_AdvancedControl>();

	}
}
