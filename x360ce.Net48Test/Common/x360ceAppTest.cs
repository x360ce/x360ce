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

		/// <summary>
		/// Test fails without resources supplied in Application class.
		/// </summary>
		[TestMethod]
		public void Test_AboutUserControl() =>
			MemoryLeakHelper.Test<AboutUserControl>();

		[TestMethod]
		public void Test_PadItem_AdvancedControl() =>
			MemoryLeakHelper.Test<PadItem_AdvancedControl>();

	}
}
