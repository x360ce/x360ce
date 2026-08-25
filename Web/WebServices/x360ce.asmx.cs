using System;
using System.Linq;
using System.Web.Services;
using x360ce.Engine.Data;
using x360ce.Engine;
using System.Data;
using JocysCom.ClassLibrary.Data;
using System.Collections.Generic;

namespace x360ce.Web.WebServices
{
	/// <summary>
	/// Summary description for x360ce
	/// </summary>
	[WebService(Namespace = "http://x360ce.com/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	[System.Web.Script.Services.ScriptService]
	public partial class x360ce : System.Web.Services.WebService, IWebService
	{

		#region Settings

		/// <summary>
		/// Search controller settings.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		[WebMethod(EnableSession = true, Description = "Search controller settings.")]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			// Create default value to reply.
			var sr = new SearchResult();
			sr.Presets = new Preset[0];
			sr.PadSettings = new PadSetting[0];
			sr.Summaries = new Summary[0];
			sr.Settings = new UserSetting[0];

			var hasArgs = args != null && args.Length > 0;
			// Workaround fix.
			args = args.Where(x => !x.IsEmpty()).ToArray();
			// Create database.
			var db = new x360ceModelContainer();
			// Get user instances.
			var p = SearchParameterCollection.GetSqlParameter(args);
			// If user device instances supplied.
			var hasInstances = args.Any(x => x.InstanceGuid != Guid.Empty);
			// If user device products supplied.
			var hasProducts = args.Any(x => x.ProductGuid != Guid.Empty);
			List<PadSetting> padSettings = null;
			// If get My Settings" then...
			if (hasInstances)
			{
				var ds = EngineHelper.GetSettings(args);
				sr.Settings = SqlHelper.ConvertToList<UserSetting>(ds.Tables[0]).ToArray();
				AddToList(ds.Tables[1], ref padSettings);
			}
			// If get "Default Settings for My Controllers" then...
			if (hasProducts)
			{
				var ds = EngineHelper.GetPresets(args);
				sr.Summaries = SqlHelper.ConvertToList<Summary>(ds.Tables[0]).ToArray();
				AddToList(ds.Tables[1], ref padSettings);
			}
			// If get "Default Settings for Most Popular Controllers" then...
			if (!hasInstances && !hasProducts && hasArgs)
			{
				// Get presets.
				var ds = EngineHelper.GetPresets(new SearchParameter[0]);
				sr.Presets = SqlHelper.ConvertToList<Preset>(ds.Tables[0]).ToArray();
				AddToList(ds.Tables[1], ref padSettings);
			}
			if (padSettings != null)
				sr.PadSettings = padSettings.ToArray();
			db.Dispose();
			db = null;
			return sr;
		}

		void AddToList(DataTable source, ref List<PadSetting> dest)
		{
			if (dest == null)
				dest = new List<PadSetting>();
			var current = dest.Select(x => x.PadSettingChecksum);
			var ps = SqlHelper.ConvertToList<PadSetting>(source)
				.Where(x => !current.Contains(x.PadSettingChecksum)).ToArray();
			dest.AddRange(ps);
		}


		/// <summary>
		/// Load controller settings.
		/// </summary>
		/// <param name="checksum">List of unique identifiers of PAD setting</param>
		/// <returns>List of PAD settings.</returns>
		[WebMethod(EnableSession = true, Description = "Load controller settings.")]
		public SearchResult LoadSetting(Guid[] checksum)
		{
			var sr = new SearchResult();
			var db = new x360ceModelContainer();
			sr.PadSettings = db.PadSettings.Where(x => checksum.Contains(x.PadSettingChecksum)).ToArray();
			db.Dispose();
			db = null;
			return sr;
		}

		#endregion

	}

}
