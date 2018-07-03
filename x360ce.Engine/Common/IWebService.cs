// -----------------------------------------------------------------------
// <copyright file="IWebService.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using x360ce.Engine.Data;
using System.Collections.Generic;
using System;
namespace x360ce.Engine
{
	public interface IWebService
	{
		string SaveSetting(UserSetting s, PadSetting ps);
		SearchResult SearchSettings(SearchParameter[] args);
		string DeleteSetting(UserSetting s);
		SearchResult LoadSetting(Guid[] checksum);
		List<Vendor> GetVendors();
		SettingsData GetSettingsData();
        List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount);
		Program GetProgram(string fileName, string fileProductName);
		string SetProgram(Program p);
		CloudMessage Execute(CloudMessage command);

    }
}
