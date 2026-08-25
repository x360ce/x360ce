using x360ce.Engine.Data;
using System.Collections.Generic;
using System;
namespace x360ce.Engine
{
	public interface IWebService
	{
		// Version 3.x
		string SaveSetting(UserSetting s, PadSetting ps);
		SearchResult SearchSettings(SearchParameter[] args);
		string DeleteSetting(UserSetting s);
		SearchResult LoadSetting(Guid[] checksum);
		List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount);
		// Version 4.x
		List<Vendor> GetVendors();
		SettingsData GetSettingsData();
		CloudMessage Execute(CloudMessage command);

    }
}
