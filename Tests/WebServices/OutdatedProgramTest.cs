// @under-test: Web/WebServices/x360ce.asmx.v4.cs
// @area: webservice   @layer: integration-api
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Programs before 4.14 (2020) send a message again at once, without end, for as long as the service answers with an
	/// error, and before that add their keys again each time; one sent a 3 MB Delete every few seconds for hours. The
	/// service answers them so that they stop: success when it cannot serve them, and the newest try when keys repeat.
	/// </summary>
	[TestClass]
	public class OutdatedProgramTest
	{
		static string CloudPublicKey(WebServiceClient ws)
		{
			var reply = ws.Execute(new CloudMessage(CloudAction.GetPublicRsaKey));
			Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
			return reply.Values.GetValue<string>(CloudKey.RsaPublicKey);
		}

		/// <summary>One try's keys, made the way the program makes them; the profile ID only when asked for.</summary>
		static KeyValueList Try(string serverKey, Guid computerId, Guid? profileId)
		{
			var userKeys = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User).RsaNewKeys(2048);
			var message = new CloudMessage(CloudAction.Select);
			CloudHelper.ApplySecurity(message, userKeys.Public, serverKey);
			message.Values.Add(CloudKey.ComputerId, computerId, true);
			if (profileId.HasValue)
				message.Values.Add(CloudKey.ProfileId, profileId.Value, true);
			return message.Values;
		}

		static UserDevice NeverSynced()
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Twin USB Gamepad", InstanceName = "Twin USB Gamepad" };
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("A Delete from a program before June 2018, with no profile ID and its keys repeated, gets success and changes nothing")]
		public void A_program_without_a_profile_id_is_told_success()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var key = CloudPublicKey(ws);
				var message = new CloudMessage(CloudAction.Delete);
				message.Values.AddRange(Try(key, Guid.NewGuid(), null));
				message.Values.AddRange(Try(key, Guid.NewGuid(), null));
				message.UserDevices = new[] { NeverSynced() };
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
				StringAssert.Contains(reply.ErrorMessage, "too old");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("Keys repeated by tries: the newest try is read, even when the first one cannot be decrypted")]
		public void Repeated_keys_are_read_from_the_newest_try()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var key = CloudPublicKey(ws);
				var computerId = Guid.NewGuid();
				var profileId = Guid.NewGuid();
				var first = Try(key, computerId, profileId);
				// A first try the service cannot read, as after the service's key changed.
				first.First(x => Equals(x.Key, CloudKey.RandomPassword)).Value = Convert.ToBase64String(new byte[256]);
				var message = new CloudMessage(CloudAction.Select);
				message.Values.AddRange(first);
				message.Values.AddRange(Try(key, computerId, profileId));
				message.UserGames = new UserGame[0];
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
				Assert.IsFalse((reply.ErrorMessage ?? "").Contains("too old"), "The newest try was not served: " + reply.ErrorMessage);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("Keys repeated and no try readable: success, so the program stops sending, with the reason")]
		public void Repeated_keys_that_cannot_be_read_are_told_success()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var key = CloudPublicKey(ws);
				var message = new CloudMessage(CloudAction.Delete);
				for (var i = 0; i < 2; i++)
				{
					var values = Try(key, Guid.NewGuid(), Guid.NewGuid());
					values.First(x => Equals(x.Key, CloudKey.RandomPassword)).Value = Convert.ToBase64String(new byte[256]);
					message.Values.AddRange(values);
				}
				message.UserDevices = new[] { NeverSynced() };
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.None, reply.ErrorCode, reply.ErrorMessage);
				StringAssert.Contains(reply.ErrorMessage, "too old");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v4")]
		[Description("One try the service cannot read still gets UnableToDecrypt and the key, which current programs use")]
		public void One_unreadable_try_still_gets_the_new_key()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var key = CloudPublicKey(ws);
				var message = new CloudMessage(CloudAction.Select);
				message.Values.AddRange(Try(key, Guid.NewGuid(), Guid.NewGuid()));
				message.Values.First(x => Equals(x.Key, CloudKey.RandomPassword)).Value = Convert.ToBase64String(new byte[256]);
				message.UserGames = new UserGame[0];
				var reply = ws.Execute(message);
				Assert.AreEqual((int)CloudErrorCode.UnableToDecrypt, reply.ErrorCode, reply.ErrorMessage);
				Assert.IsFalse(string.IsNullOrEmpty(reply.Values.GetValue<string>(CloudKey.RsaPublicKey)), "No key to retry with.");
			}
		}
	}
}
