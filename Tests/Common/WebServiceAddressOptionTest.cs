// @under-test: App.v4/Common/Options.cs, App.v4/Common/SettingsManager.LoadAndSync.cs, App.v4/Controls/OptionsInternetUserControl.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The saved web service address is the one the Options page shows and the one the program uses.
	/// </summary>
	/// <remarks>
	/// The address box is a list of known addresses with the saved one written into it. A saved
	/// address that was not in the list was not shown: the box showed the first entry, and the
	/// first change the box raised wrote that entry back over the saved value. Somebody who had
	/// pointed the program at their own server saw the public address on every start, and Load
	/// Preset asked whichever of the two the last write had left.
	/// </remarks>
	[TestClass]
	public class WebServiceAddressOptionTest
	{
		const string OwnServer = "http://localhost:8081/webservices/x360ce.asmx";

		[TestMethod, TestCategory("critical")]
		[Description("Saved plain HTTP addresses of the program's own servers are read as HTTPS; any other address is kept")]
		public void Saved_plain_http_addresses_of_our_servers_become_https()
		{
			var options = new Options
			{
				InternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx",
				InternetDatabaseUrls = new System.ComponentModel.BindingList<string>
				{
					"http://www.x360ce.com/webservices/x360ce.asmx",
					"https://www.x360ce.com/webservices/x360ce.asmx",
					"http://localhost:20360/webservices/x360ce.asmx",
					OwnServer,
				},
			};
			options.InitDefaults();
			Assert.AreEqual(Engine.SettingName.DefaultInternetDatabaseUrl, options.InternetDatabaseUrl, "The saved address stayed on plain HTTP.");
			CollectionAssert.AreEqual(new[]
			{
				Engine.SettingName.DefaultInternetDatabaseUrl,
				Engine.SettingName.LocalInternetDatabaseUrl,
				OwnServer,
			}, options.InternetDatabaseUrls, "The list still offers plain HTTP, lists an address twice, or lost the person's own server.");
			StringAssert.StartsWith(Engine.SettingName.DefaultInternetDatabaseUrl, "https://www.x360ce.com/");
		}

		[TestMethod]
		public void A_saved_address_missing_from_the_list_is_added_to_it()
		{
			var options = new Options();
			options.InitDefaults();
			options.InternetDatabaseUrl = OwnServer;
			options.InitDefaults();
			Assert.IsTrue(options.InternetDatabaseUrls.Contains(OwnServer), "The saved address is not among the choices.");
			Assert.AreEqual(1, options.InternetDatabaseUrls.Count(x => x == OwnServer), "The saved address was added more than once.");
		}

		[TestMethod]
		public void The_options_page_shows_the_saved_address_and_leaves_it_alone()
		{
			var options = SettingsManager.Options;
			var originalUrl = options.InternetDatabaseUrl;
			var originalList = options.InternetDatabaseUrls.ToArray();
			try
			{
				options.InternetDatabaseUrl = OwnServer;
				options.InitDefaults();
				Ui.OnUiThread(() =>
				{
					using (var form = new Form())
					using (var panel = new OptionsInternetUserControl())
					{
						form.Controls.Add(panel);
						// Bound and shown the way the main window does it: the map first, the handle after.
						panel.UpdateSettingsMap();
						form.Show();
						Application.DoEvents();
						Assert.AreEqual(OwnServer, panel.InternetDatabaseUrlComboBox.Text, "The box does not show the saved address.");
						Assert.AreEqual(OwnServer, options.InternetDatabaseUrl, "Showing the page changed the saved address.");
					}
				});
			}
			finally
			{
				options.InternetDatabaseUrls.Clear();
				foreach (var url in originalList)
					options.InternetDatabaseUrls.Add(url);
				options.InternetDatabaseUrl = originalUrl;
			}
		}
	}
}
