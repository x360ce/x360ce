using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	[TestClass]
	public class x360ceAppTest
	{
		[TestMethod]
		public void Test_All() =>
		MemoryLeakHelper.Test(typeof(App.App).Assembly, null, null);

		[TestMethod]
		public void Test_All_with_Theme() =>
		MemoryLeakHelper.Test(typeof(App.App).Assembly, null, null, typeof(TestWindow));

		[TestMethod]
		public void Test_TestWindow()
		{
			MemoryLeakHelper.Test<TestWindow>();
		}

		[TestMethod]
		public void Test_AboutUserControl() =>
			MemoryLeakHelper.Test<AboutUserControl, TestWindow>();

		[TestMethod]
		public void Test_PadItem_AdvancedControl() =>
			MemoryLeakHelper.Test<PadItem_AdvancedControl, TestWindow>();

	}
}
