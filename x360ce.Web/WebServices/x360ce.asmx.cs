using System;
using System.Linq;
using System.Web.Services;
using x360ce.Engine.Data;
using x360ce.Engine;
using System.Data;
using JocysCom.ClassLibrary.Data;

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
			// Create database.
			var db = new x360ceModelContainer();
			// Get user instances.
			var p = SearchParameterCollection.GetSqlParameter(args);
			// Get most popular settings for user controllers.
			var products = args.Where(x => x.ProductGuid != Guid.Empty).Select(x => x.ProductGuid).Distinct().ToArray();
			// Get all device instances of the user.
			var hasInstances = args.Any(x => x.InstanceGuid != Guid.Empty);
			if (hasInstances)
			{
				var ds = EngineHelper.GetSettings(args);
				sr.Settings = SqlHelper.ConvertToList<UserSetting>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			else if (products.Length > 0)
			{
				// Get presets.
				var ds = EngineHelper.GetPresets(args);
				sr.Summaries = SqlHelper.ConvertToList<Summary>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			else if (args != null && args.Length > 0)
			{
				// Get presets.
				var ds = EngineHelper.GetPresets(new SearchParameter[0]);
				sr.Presets = SqlHelper.ConvertToList<Preset>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			db.Dispose();
			db = null;
			return sr;
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
