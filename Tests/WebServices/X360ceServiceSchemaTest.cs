// @under-test: Web/WebServices/x360ce.asmx.v3.cs, Engine/Data/x360ceModel.edmx, Data/dbo/Tables/x360ce_PadSettings.sql
// @area: webservice   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// What the 4.23 server stores that older servers could not.
	/// </summary>
	/// <remarks>
	/// Two things widened between the programs and the database. Since 4.22 the program sends
	/// five force feedback settings a 78-column table has nowhere to put, and a mapping can be a
	/// formula longer than the sixteen characters the columns held. Each is a column change on
	/// the database and a facet change in the model, and either half alone loses data silently:
	/// the model truncates what the database would take, or the database refuses what the model
	/// passes. A round trip through the service is the only place both halves are exercised
	/// together. These run against a database that has both changes; the live server gets them
	/// after this suite is green locally, which is why they carry their own tag.
	/// </remarks>
	[TestClass]
	public class X360ceServiceSchemaTest
	{
		static void RoundTrip(PadSetting sent, Action<PadSetting> check)
		{
			if (WebServiceTarget.IsLive)
				Assert.Inconclusive("Writes are not run against the public site.");
			using (var ws = WebServiceTarget.Client())
			{
				var checksum = sent.CleanAndGetCheckSum();
				var instanceGuid = Guid.NewGuid();
				var setting = new UserSetting
				{
					InstanceGuid = instanceGuid,
					InstanceName = "x360ce.Tests instance",
					ProductGuid = WebServiceTarget.LogitechG27ProductGuid,
					ProductName = "Logitech G27 Racing Wheel USB",
					FileName = "x360ce.Tests.exe",
					FileProductName = "x360ce test run",
					Comment = "Created by X360ceServiceSchemaTest; safe to delete",
					MapTo = 1,
					IsEnabled = true,
				};
				Assert.AreEqual("", ws.SaveSetting(setting, sent), "SaveSetting succeeded");
				try
				{
					var loaded = ws.LoadSetting(new[] { checksum }).PadSettings;
					Assert.AreEqual(1, loaded.Length, "The pad setting is found under the checksum the program computed");
					check(loaded[0]);
				}
				finally
				{
					ws.DeleteSetting(setting);
				}
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("writes"), TestCategory("schema-4-23")]
		[Description("A mapping formula longer than sixteen characters comes back whole")]
		public void A_formula_longer_than_sixteen_characters_round_trips()
		{
			// 33 characters: over the old column width, well inside the new one.
			const string formula = "=sign(a1)*deadzone(abs(a1),0.24)";
			Assert.IsTrue(formula.Length > 16 && formula.Length <= 128);
			RoundTrip(
				new PadSetting { ButtonA = "1", LeftThumbAxisX = formula, LeftTrigger = "a-6" },
				loaded => Assert.AreEqual(formula, loaded.LeftThumbAxisX, "The formula was cut or refused on the way"));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("writes"), TestCategory("schema-4-23")]
		[Description("The wheel settings a 4.22 program sends are stored and returned")]
		public void The_wheel_settings_round_trip()
		{
			RoundTrip(
				new PadSetting { ButtonA = "1", ForceEnable = "1", ForceSpringEnable = "1", ForceSpringStrength = "50", WheelRange = "900", ForcePassThrough = "1", ForcePassThroughIndex = "2" },
				loaded =>
				{
					Assert.AreEqual("1", loaded.ForceSpringEnable, "ForceSpringEnable");
					Assert.AreEqual("50", loaded.ForceSpringStrength, "ForceSpringStrength");
					Assert.AreEqual("900", loaded.WheelRange, "WheelRange");
					Assert.AreEqual("1", loaded.ForcePassThrough, "ForcePassThrough");
					Assert.AreEqual("2", loaded.ForcePassThroughIndex, "ForcePassThroughIndex");
				});
		}
	}
}
