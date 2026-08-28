// @under-test: Engine/JocysCom/Controls/ControlsHelper.Windows.cs
// @area: designer   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether the program can tell a designer apart from itself.
	/// </summary>
	/// <remarks>
	/// Controls guard their startup work with this so a form can be opened in the designer without
	/// the program's own machinery starting up underneath it. The guard is written in a constructor,
	/// which is the one place none of the framework's own answers work: UsageMode is Designtime only
	/// for the type the designer was asked to open, and Site and Parent are both still null. So the
	/// process is asked instead.
	///
	/// The risk of asking the process is the opposite failure: answering "designer" while the program
	/// is genuinely running would stop it initialising anything at all, which is far worse than the
	/// fault being fixed. That is what is held here.
	/// </remarks>
	[TestClass]
	public class DesignModeTest
	{

		[TestMethod, TestCategory("designer"), TestCategory("critical")]
		[Description("A running program is never mistaken for a designer")]
		public void A_running_program_is_never_mistaken_for_a_designer()
		{
			Assert.IsFalse(ControlsHelper.IsDesignerProcess,
				"The test runner is not Visual Studio, so this has to be false. Anything answering " +
				"true here would leave every control skipping its own startup while the program runs.");
		}

		[TestMethod, TestCategory("designer")]
		[Description("The answer is the same every time it is asked")]
		public void The_answer_is_the_same_every_time_it_is_asked()
		{
			// Read once and kept, because it cannot change while the process lives and it is asked
			// from constructors which run constantly.
			var first = ControlsHelper.IsDesignerProcess;
			for (var i = 0; i < 100; i++)
				Assert.AreEqual(first, ControlsHelper.IsDesignerProcess);
		}

	}
}
