using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace x360ce.Tests
{
	[TestClass]
	public class JocysComTest
	{
		[TestMethod]
		public void Test_x360ce_Engine_IssuesUserControl() =>
			MemoryLeakHelper.Test<JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl>();

		[TestMethod]
		public void Test_x360ce_Engine_IssuesControl() =>
			MemoryLeakHelper.Test<JocysCom.ClassLibrary.Controls.IssuesControl.IssuesControl>();

		[TestMethod]
		public void Test_ClassLibrary_MessageBoxWindow()
		{
			MemoryLeakHelper.Test<JocysCom.ClassLibrary.Controls.MessageBoxWindow>();
		}

		[TestMethod]
		public void Test_ClassLibrary_GetAllTest()
		{
			var box = new ErrorReportControl();
			var all = JocysCom.ClassLibrary.Controls.ControlsHelper.GetAll(box);
			var buttons = JocysCom.ClassLibrary.Controls.ControlsHelper.GetAll(box, typeof(Button));
			Assert.IsTrue(buttons.Count() == 6);
		}

		[TestMethod]
		public void Test_ClassLibrary_ErrorReportControl() =>
			MemoryLeakHelper.Test<JocysCom.ClassLibrary.Controls.ErrorReportControl>();

	}
}
