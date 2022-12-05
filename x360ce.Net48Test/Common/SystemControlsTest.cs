using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.Tests
{
	[TestClass]
	public class SystemControlsTest
	{
		[TestMethod]
		public void Test_ClassLibrary_Widow() =>
			MemoryLeakHelper.Test<Window>();

		/// <summary>
		/// Simple ListView will be garbage collected successfully.
		/// </summary>
		[TestMethod]
		public void Test_ListView() =>
			MemoryLeakHelper.Test<ListView>();

		/// <summary>
		/// Simple DataGrid fails garbage collection and leaks memory.
		/// </summary>
		[TestMethod]
		public void Test_DataGrid() =>
			MemoryLeakHelper.Test<DataGrid>();

		[TestMethod]
		public void Test_TextBox() =>
			MemoryLeakHelper.Test<TextBox>();

		[TestMethod]
		public void Test_StackPanel() =>
			MemoryLeakHelper.Test<StackPanel>();

	}
}
