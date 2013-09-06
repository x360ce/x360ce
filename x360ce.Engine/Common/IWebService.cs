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
		string SaveSetting(Setting s, PadSetting ps);
		SearchResult SearchSettings(SearchParameter[] args);
		string DeleteSetting(Setting s);
		SearchResult LoadSetting(Guid[] checksum);
		List<Vendor> GetVendors();
		SettingsData GetSettingsData();
		List<Program> GetPrograms(bool? isEnabled, int? minInstanceCount);
		Program GetProgram(string fileName, string fileProductName);
		string SetProgram(Program p);
		KeyValueList SignOut();
		KeyValueList SignIn(string username, string password);

	}
}
