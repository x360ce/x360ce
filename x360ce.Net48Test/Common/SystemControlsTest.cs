using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.Tests
{
	[TestClass]
	public class SystemControlsTest
	{
		[TestMethod]
		public void Test_ClassLibrary_Window() =>
			MemoryLeakHelper.Test<Window>();

		[TestMethod]
		public void Test_ListView() =>
			MemoryLeakHelper.Test<ListView>();

		/// <summary>
		/// GroupBox fails to dispose without custom styling on NET 4.8.
		/// </summary>
		[TestMethod]
		public void Test_GroupBox()
		{
			MemoryLeakHelper.Test<GroupBox>();
		}

		[TestMethod]
		public void Test_DataGrid() =>
			MemoryLeakHelper.Test<DataGrid>();

		[TestMethod]
		public void Test_TextBox() =>
			MemoryLeakHelper.Test<TextBox>();

		[TestMethod]
		public void Test_StackPanel() =>
			MemoryLeakHelper.Test<StackPanel>();

		[TestMethod]
		public void Test_TabControl() =>
			MemoryLeakHelper.Test<TabControl>();


	}
}
