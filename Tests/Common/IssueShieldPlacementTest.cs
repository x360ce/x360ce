// @under-test: Engine/JocysCom/Controls/IssuesControl/IssuesUserControl.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace x360ce.Tests
{
	/// <summary>
	/// Where the Administrator shield sits on a fix button.
	/// </summary>
	/// <remarks>
	/// It was first drawn at a fixed distance from the left of the cell, and the button centres its
	/// own label, so the icon sat on top of the word. Moving the label out of the way instead moved
	/// the whole button, which put the icon outside it. The rule that works is to place the icon from
	/// where the label actually lands, and these hold that it can never land on the label again.
	/// </remarks>
	[TestClass]
	public class IssueShieldPlacementTest
	{

		static int TextLeft(Rectangle cell, int textWidth)
		{
			return cell.Left + (cell.Width - textWidth) / 2;
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The shield never lands on the label, at any width")]
		public void The_shield_never_lands_on_the_label()
		{
			// Swept rather than sampled, because the fault only showed at some combinations: it looked
			// right at the width it was written for and wrong at the width it shipped at.
			for (var cellWidth = 40; cellWidth <= 400; cellWidth += 5)
			{
				for (var textWidth = 10; textWidth < cellWidth; textWidth += 5)
				{
					var cell = new Rectangle(100, 50, cellWidth, 24);
					var shield = IssuesUserControl.ShieldBounds(cell, textWidth);
					if (shield.IsEmpty)
						continue;
					Assert.IsTrue(shield.Right <= TextLeft(cell, textWidth),
						"The shield reaches into the label at cell width " + cellWidth
						+ " with a label " + textWidth + " wide. That is the icon drawn over the word.");
					Assert.IsTrue(shield.Left >= cell.Left,
						"The shield is outside its own cell at cell width " + cellWidth + ".");
					Assert.IsTrue(shield.Top >= cell.Top && shield.Bottom <= cell.Bottom,
						"The shield is taller than the row it sits in.");
				}
			}
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("A button wide enough for the label gets the shield")]
		public void A_button_wide_enough_for_the_label_gets_the_shield()
		{
			// The other half. Refusing to draw whenever it is awkward would pass the test above and warn
			// nobody, so an ordinary button has to actually get one.
			var cell = new Rectangle(0, 0, 96, 24);
			var shield = IssuesUserControl.ShieldBounds(cell, 48);
			Assert.IsFalse(shield.IsEmpty,
				"A button of the size the Issues page uses got no shield, so nobody is told the " +
				"Administrator prompt is coming.");
		}

	}
}
