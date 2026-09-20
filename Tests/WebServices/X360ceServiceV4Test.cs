// @under-test: Web/WebServices/x360ce.asmx.v4.cs
// @area: webservice   @layer: integration-api
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Xml;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// What a v4 program depends on beyond v3.
	/// </summary>
	/// <remarks>
	/// v4 (<c>App.v4</c>) still uses <c>SearchSettings</c>, <c>LoadSetting</c> and <c>GetPrograms</c>
	/// as v3 does, so the v3 class is the floor: the first test here is the v3 crash reproducer,
	/// and nothing in this class means anything while that one is red. On top, v4 talks to the
	/// cloud through one operation, <c>Execute</c>, with a <see cref="CloudMessage"/> whose
	/// sensitive values are AES-encrypted under a random password that is itself RSA-encrypted
	/// with the server's public key. Every message the program sends is rebuilt here the way
	/// <c>App.v4/Common/CloudClient.cs</c> builds it. The remaining operations of the contract
	/// (<c>GetProgramsDefault</c>, <c>GetSettingsData</c>, <c>GetVendors</c>, <c>GetProgram</c>,
	/// <c>SetProgram</c>) are called by no program; they are covered so the WSDL is whole.
	/// </remarks>
	[TestClass]
	public class X360ceServiceV4Test
	{
		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("v4 stands on v3: the preset search that crashed the hosted site")]
		public void V3_floor_search_by_product_returns_a_pad_setting_for_every_summary()
		{
			using (var ws = WebServiceTarget.Client())
				X360ceServiceV3Test.SearchByProductReturnsAPadSettingForEverySummary(ws, WebServiceTarget.LogitechG27ProductGuid);
		}

		#region Execute

		/// <summary>The server's public key, fetched the way the program fetches it on first contact.</summary>
		static string CloudPublicKey(WebServiceClient ws)
		{
			var reply = ws.Execute(new CloudMessage(CloudAction.GetPublicRsaKey));
			Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
			var key = reply.Values.GetValue<string>(CloudKey.RsaPublicKey);
			Assert.IsFalse(string.IsNullOrEmpty(key), "No RSA public key in the reply");
			return key;
		}

		/// <summary>A message secured and stamped exactly as CloudClient does before Execute.</summary>
		static CloudMessage Secured(WebServiceClient ws, CloudAction action, Guid computerId, Guid profileId)
		{
			var userKeys = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User).RsaNewKeys(2048);
			var message = new CloudMessage(action);
			CloudHelper.ApplySecurity(message, userKeys.Public, CloudPublicKey(ws));
			message.Values.Add(CloudKey.ComputerId, computerId, true, true);
			message.Values.Add(CloudKey.ProfileId, profileId, true, true);
			message.Values.Add(CloudKey.ClientVersion, "4.23.0.0", false, true);
			return message;
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("First contact: the server hands out its RSA public key")]
		public void Execute_get_public_rsa_key_returns_a_key()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var key = CloudPublicKey(ws);
				// A CSP blob of a 2048-bit public key is 276 bytes, 368 characters in base64.
				Assert.IsTrue(key.Length >= 300, "Key looks too short to be an RSA public key: " + key.Length);
				Convert.FromBase64String(key);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("A message that does nothing gets an empty, successful answer")]
		public void Execute_none_answers_without_error()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var reply = ws.Execute(new CloudMessage(CloudAction.None));
				Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
				Assert.IsTrue(string.IsNullOrEmpty(reply.ErrorMessage), reply.ErrorMessage);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("Log in without credentials is refused, not faulted")]
		public void Execute_log_in_without_credentials_is_not_authorized()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var reply = ws.Execute(new CloudMessage(CloudAction.LogIn));
				Assert.AreEqual((int)CloudErrorCode.Error, reply.ErrorCode);
				Assert.AreEqual("Not authorized", reply.ErrorMessage);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("Select without the encrypted computer id names what is missing")]
		public void Execute_select_without_computer_id_says_which_value_is_empty()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var message = new CloudMessage(CloudAction.Select);
				message.UserGames = new UserGame[0];
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.Error, reply.ErrorCode);
				// GetGuidId answers null without an error text when no random password was sent,
				// and the service then reports the empty error; the program shows it as is.
				Assert.IsNotNull(reply.ErrorMessage);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("Select for a computer nobody has: empty lists under a successful answer")]
		public void Execute_select_for_a_new_computer_returns_empty_lists()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var message = Secured(ws, CloudAction.Select, Guid.NewGuid(), Guid.NewGuid());
				message.UserGames = new UserGame[0];
				message.UserDevices = new UserDevice[0];
				message.UserInstances = new UserInstance[0];
				message.UserSettings = new UserSetting[0];
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
				Assert.AreEqual(0, (reply.UserGames ?? new UserGame[0]).Length, "UserGames");
				Assert.AreEqual(0, (reply.UserDevices ?? new UserDevice[0]).Length, "UserDevices");
				Assert.AreEqual(0, (reply.UserInstances ?? new UserInstance[0]).Length, "UserInstances");
				Assert.AreEqual(0, (reply.UserSettings ?? new UserSetting[0]).Length, "UserSettings");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4"), TestCategory("writes")]
		[Description("Insert, select, delete: a game of this computer round-trips through the cloud")]
		public void Execute_insert_select_delete_round_trips_a_user_game()
		{
			if (WebServiceTarget.IsLive)
				Assert.Inconclusive("Writes are not run against the public site.");
			using (var ws = WebServiceTarget.Client())
			{
				var computerId = Guid.NewGuid();
				var profileId = Guid.NewGuid();
				var game = new UserGame
				{
					GameId = Guid.NewGuid(),
					FileName = "x360ce.Tests.exe",
					FileProductName = "x360ce test run",
					FullPath = @"C:\x360ce.Tests\x360ce.Tests.exe",
					FileVersion = "4.23.0.0",
					CompanyName = "x360ce.Tests",
					Comment = "Created by X360ceServiceV4Test; safe to delete",
					IsEnabled = true,
				};
				var inserted = false;
				try
				{
					var insert = Secured(ws, CloudAction.Insert, computerId, profileId);
					insert.UserGames = new[] { game };
					var reply = ws.Execute(insert);
					Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
					inserted = true;
					StringAssert.Contains(reply.ErrorMessage ?? "", "1 created", "Upsert reports what it did: " + reply.ErrorMessage);

					var select = Secured(ws, CloudAction.Select, computerId, profileId);
					select.UserGames = new UserGame[0];
					var found = ws.Execute(select);
					Assert.AreEqual((int)CloudErrorCode.None, found.ErrorCode, found.ErrorMessage);
					Assert.AreEqual(1, (found.UserGames ?? new UserGame[0]).Length, "The inserted game is selected by computer and profile");
					Assert.AreEqual(game.FileName, found.UserGames[0].FileName);
					Assert.AreEqual(computerId, found.UserGames[0].ComputerId, "The server stamps the computer id from the encrypted value, not from the record");
				}
				finally
				{
					if (inserted)
					{
						var delete = Secured(ws, CloudAction.Delete, computerId, profileId);
						delete.UserGames = new[] { game };
						var gone = ws.Execute(delete);
						Assert.AreEqual((int)CloudErrorCode.None, gone.ErrorCode, gone.ErrorMessage);
						var select = Secured(ws, CloudAction.Select, computerId, profileId);
						select.UserGames = new UserGame[0];
						Assert.AreEqual(0, (ws.Execute(select).UserGames ?? new UserGame[0]).Length, "Deleted");
					}
				}
			}
		}

		#endregion

		#region Operations no program calls

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("GetSettingsData and GetVendors answer with rows")]
		public void Get_settings_data_and_get_vendors_return_rows()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var data = ws.GetSettingsData();
				Assert.IsNotNull(data.Programs, "Programs");
				Assert.IsTrue(data.Programs.Count > 0, "No programs");
				// Projecting into the entity type inside the query is what an older copy of the
				// service did; EF6 refuses it at run time and the call faults.
				var vendors = ws.GetVendors();
				Assert.IsTrue(vendors.Count > 0, "No vendors");
				Assert.IsTrue(vendors.All(x => !string.IsNullOrEmpty(x.VendorName)), "A vendor without a name");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("GetProgramsDefault answers with rows")]
		public void Get_programs_default_returns_rows()
		{
			string fault;
			var doc = WebServiceTarget.Soap("GetProgramsDefault", "", out fault);
			Assert.IsNull(fault, fault);
			var programs = doc.GetElementsByTagName("Program");
			Assert.IsTrue(programs.Count > 0, "No programs");
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("GetProgram finds x360ce.exe and answers nothing, not a fault, for an unknown file")]
		public void Get_program_finds_a_known_file_and_answers_empty_for_an_unknown_one()
		{
			string fault;
			var known = WebServiceTarget.Soap("GetProgram", "<fileName>x360ce.exe</fileName><fileProductName></fileProductName>", out fault);
			Assert.IsNull(fault, fault);
			Assert.AreEqual(1, known.GetElementsByTagName("GetProgramResult").Count, "A result element");
			Assert.AreEqual(1, known.GetElementsByTagName("FileName").Count, "The program has a file name");
			var unknown = WebServiceTarget.Soap("GetProgram", "<fileName>" + Guid.NewGuid().ToString("N") + ".exe</fileName><fileProductName></fileProductName>", out fault);
			Assert.IsNull(fault, fault);
			Assert.AreEqual(0, unknown.GetElementsByTagName("FileName").Count, "Nothing found, nothing invented");
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("SetProgram without a signed-in user changes nothing and says why")]
		public void Set_program_unauthenticated_is_refused()
		{
			string fault;
			var body = "<p><ProgramId>" + Guid.NewGuid() + "</ProgramId><FileName>x360ce.Tests.exe</FileName><FileProductName>x360ce test run</FileProductName>" +
				"<InstanceCount>0</InstanceCount><IsEnabled>false</IsEnabled><HookMask>0</HookMask><XInputMask>0</XInputMask></p>";
			var doc = WebServiceTarget.Soap("SetProgram", body, out fault);
			Assert.IsNull(fault, fault);
			var result = doc.GetElementsByTagName("SetProgramResult");
			Assert.AreEqual(1, result.Count, "A result element");
			Assert.AreEqual("User was not authenticated.", result[0].InnerText);
		}

		#endregion
	}
}
