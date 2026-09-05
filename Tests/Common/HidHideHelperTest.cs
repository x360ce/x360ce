// @under-test: App.v4/ViGEm/HidHideHelper.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App.ViGEm;

namespace x360ce.Tests
{
	[TestClass]
	public class HidHideHelperTest
	{
		[TestMethod, TestCategory("devices")]
		[Description("HidHide detection query runs safely without throwing")]
		public void HidHide_status_queries_never_throw()
		{
			// Even on machines where HidHide is not installed, these methods must return safely.
			var installed = HidHideHelper.IsInstalled();
			var active = HidHideHelper.IsActive();
			var whitelist = HidHideHelper.GetWhitelist();
			var blacklist = HidHideHelper.GetBlacklist();

			Assert.IsNotNull(whitelist, "Whitelist must not be null");
			Assert.IsNotNull(blacklist, "Blacklist must not be null");
			Assert.IsFalse(HidHideHelper.IsAppWhitelisted(null), "Null path cannot be whitelisted");
			Assert.IsFalse(HidHideHelper.IsAppWhitelisted(string.Empty), "Empty path cannot be whitelisted");
		}

		[TestMethod, TestCategory("devices")]
		[Description("HidHide invalid arguments are safely rejected")]
		public void HidHide_invalid_arguments_are_rejected()
		{
			Assert.IsFalse(HidHideHelper.WhitelistApplication(null));
			Assert.IsFalse(HidHideHelper.WhitelistApplication("   "));
			Assert.IsFalse(HidHideHelper.HideDevice(null));
			Assert.IsFalse(HidHideHelper.HideDevice(string.Empty));
			Assert.IsFalse(HidHideHelper.UnhideDevice(null));
			Assert.IsFalse(HidHideHelper.UnhideDevice(string.Empty));
		}
	}
}
