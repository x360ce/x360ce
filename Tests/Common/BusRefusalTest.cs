// @under-test: App.v4/ViGEm/Client/Enums/BusAnswer.cs, App.v4/ViGEm/Client/Enums/XUSB_REPORT.cs, App.v4/ViGEm/Client/ViGEmException.cs, App.v4/Issues/VirtualDriverNotWorkingIssue.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;

namespace x360ce.Tests
{
	/// <summary>Every bus answer means fine, gone or sick, quoted the same way everywhere, and a report reaches the bus on the bit it belongs on.</summary>
	/// <remarks>
	/// The bus answers every request with a code, and every answer is read in one place, as fine, gone or
	/// sick, so a report refused with any code is noticed. An answer this program has no name for is quoted
	/// by its number everywhere it is shown, so support can look it up.
	/// </remarks>
	[TestClass]
	public class BusRefusalTest
	{

		/// <summary>Every named answer and what it means.</summary>
		static readonly Dictionary<VIGEM_ERROR, BusAnswer> Meanings = new Dictionary<VIGEM_ERROR, BusAnswer>
		{
			{ VIGEM_ERROR.VIGEM_ERROR_NONE, BusAnswer.Fine },
			{ VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND, BusAnswer.Gone },
			{ VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET, BusAnswer.Gone },
			{ VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_ALREADY_CONNECTED, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_TARGET_UNINITIALIZED, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN, BusAnswer.Gone },
			{ VIGEM_ERROR.VIGEM_ERROR_BUS_VERSION_MISMATCH, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_CALLBACK_ALREADY_REGISTERED, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND, BusAnswer.Sick },
			{ VIGEM_ERROR.VIGEM_ERROR_BUS_ALREADY_CONNECTED, BusAnswer.Sick },
		};

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Every bus answer is fine, gone or sick, and one the enum does not name is sick")]
		public void Every_answer_is_fine_gone_or_sick()
		{
			foreach (VIGEM_ERROR code in Enum.GetValues(typeof(VIGEM_ERROR)))
			{
				Assert.IsTrue(Meanings.ContainsKey(code), code + " is new to the enum; say here what it means.");
				Assert.AreEqual(Meanings[code], BusAnswers.Of(code), code.ToString());
			}
			Assert.AreEqual(BusAnswer.Sick, BusAnswers.Of(Unnamed),
				"An answer the enum does not name was not read as a refusal, so a controller that takes nothing looks well.");
			Assert.AreEqual(BusAnswer.Sick, BusAnswers.Of((VIGEM_ERROR)0), "Zero is not the bus's yes.");
		}

		/// <summary>Each XInput button and the bit the bus reads it from, as ViGEm's XUSB_BUTTON (ViGEm/Common.h) defines it.</summary>
		static readonly KeyValuePair<GamepadButtonFlags, ushort>[] XusbButtons =
		{
			Bit(GamepadButtonFlags.DPadUp, 0x0001),
			Bit(GamepadButtonFlags.DPadDown, 0x0002),
			Bit(GamepadButtonFlags.DPadLeft, 0x0004),
			Bit(GamepadButtonFlags.DPadRight, 0x0008),
			Bit(GamepadButtonFlags.Start, 0x0010),
			Bit(GamepadButtonFlags.Back, 0x0020),
			Bit(GamepadButtonFlags.LeftThumb, 0x0040),
			Bit(GamepadButtonFlags.RightThumb, 0x0080),
			Bit(GamepadButtonFlags.LeftShoulder, 0x0100),
			Bit(GamepadButtonFlags.RightShoulder, 0x0200),
			Bit(GamepadButtonFlags.Guide, 0x0400),
			Bit(GamepadButtonFlags.A, 0x1000),
			Bit(GamepadButtonFlags.B, 0x2000),
			Bit(GamepadButtonFlags.X, 0x4000),
			Bit(GamepadButtonFlags.Y, 0x8000),
		};

		static KeyValuePair<GamepadButtonFlags, ushort> Bit(GamepadButtonFlags button, ushort bit)
		{
			return new KeyValuePair<GamepadButtonFlags, ushort>(button, bit);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Every button reaches the bus on the bit the bus reads it from, Guide included")]
		public void Every_button_reaches_the_bus_on_its_own_bit()
		{
			var all = GamepadButtonFlags.None;
			ushort bits = 0;
			foreach (var pair in XusbButtons)
			{
				var report = DInputHelper.ToReport(new Gamepad { Buttons = pair.Key });
				Assert.AreEqual(pair.Value, report.wButtons,
					pair.Key + " reached the bus as 0x" + report.wButtons.ToString("X4") + ", which the bus reads as another button.");
				all |= pair.Key;
				bits |= pair.Value;
			}
			foreach (GamepadButtonFlags button in Enum.GetValues(typeof(GamepadButtonFlags)))
				Assert.AreEqual(button, all & button, button + " has no bit in the table, so nothing shows the bus reads it.");
			Assert.AreEqual(bits, DInputHelper.ToReport(new Gamepad { Buttons = all }).wButtons, "Every button at once.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Triggers and thumbs reach the bus as they are")]
		public void Triggers_and_thumbs_reach_the_bus_as_they_are()
		{
			var state = new Gamepad
			{
				LeftTrigger = 1,
				RightTrigger = 255,
				LeftThumbX = short.MinValue,
				LeftThumbY = -2,
				RightThumbX = short.MaxValue,
				RightThumbY = 3,
			};
			var report = DInputHelper.ToReport(state);
			Assert.AreEqual(state.LeftTrigger, report.bLeftTrigger, "Left trigger");
			Assert.AreEqual(state.RightTrigger, report.bRightTrigger, "Right trigger");
			Assert.AreEqual(state.LeftThumbX, report.sThumbLX, "Left thumb X");
			Assert.AreEqual(state.LeftThumbY, report.sThumbLY, "Left thumb Y");
			Assert.AreEqual(state.RightThumbX, report.sThumbRX, "Right thumb X");
			Assert.AreEqual(state.RightThumbY, report.sThumbRY, "Right thumb Y");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("An answer is quoted one way everywhere: its name without the prefix, or its number")]
		public void An_answer_is_quoted_one_way_everywhere()
		{
			Assert.AreEqual("INVALID_TARGET", BusAnswers.Name(VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET));
			Assert.AreEqual("0xE0000015", BusAnswers.Name(Unnamed), "Support looks a code up by its number.");
			Assert.AreEqual("none given", BusAnswers.Name(VIGEM_ERROR.VIGEM_ERROR_NONE));
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var issue = File.ReadAllText(Path.Combine(app, "Issues", "VirtualDriverNotWorkingIssue.cs"));
			var client = File.ReadAllText(Path.Combine(app, "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			StringAssert.Contains(issue, "BusAnswers.Name(", "The Issues tab quotes an answer its own way.");
			Assert.IsFalse(issue.Contains("static string Answer("), "The Issues tab keeps its own copy of the naming.");
			StringAssert.Contains(client, "BusAnswers.Name(", "The connection log quotes an answer its own way.");
			StringAssert.Contains(new ViGEmException(Unnamed).Message, "0xE0000015",
				"A fault report quotes the code otherwise than the log and the Issues tab do.");
		}
	}
}
