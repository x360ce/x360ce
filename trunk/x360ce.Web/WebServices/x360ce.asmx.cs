using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using x360ce.Web.Data;
using x360ce.Web.Common;
using System.Data.Objects;
using System.Linq.Expressions;

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
	public class x360ce : System.Web.Services.WebService
	{

		[WebMethod]
		public string SaveSetting(Setting s, PadSetting ps)
		{
			var checksum = ps.GetCheckSum();
			var db = new Data.x360ceModelContainer();
			var s1 = db.Settings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			var n = DateTime.Now;
			if (s1 == null)
			{
				s1 = new Setting();
				s1.SettingId = Guid.NewGuid();
				s1.DateCreated = n;
				db.Settings.AddObject(s1);
			}
			s1.Comment = s.Comment;
			s1.DateUpdated = n;
			s1.DateSelected = n;
			s1.DeviceType = s.DeviceType;
			s1.FileName = s.FileName;
			s1.FileProductName = s.FileProductName;
			s1.InstanceGuid = s.InstanceGuid;
			s1.InstanceName = s.InstanceName;
			s1.ProductGuid = s.ProductGuid;
			s1.ProductName = s.ProductName;
			s1.IsEnabled = s.IsEnabled;
			s1.PadSettingChecksum = checksum;
			// Save Pad Settings.
			var p1 = db.PadSettings.FirstOrDefault(x => x.PadSettingChecksum == checksum);
			if (p1 == null)
			{
				p1 = new PadSetting();
				p1.PadSettingChecksum = checksum;
				db.PadSettings.AddObject(p1);
			}
			p1.AxisToDPadDeadZone = ps.AxisToDPadDeadZone;
			p1.AxisToDPadEnabled = ps.AxisToDPadEnabled;
			p1.AxisToDPadOffset = ps.AxisToDPadOffset;
			p1.ButtonA = ps.ButtonA;
			p1.ButtonB = ps.ButtonB;
			p1.ButtonBig = string.IsNullOrEmpty(ps.ButtonBig) ? "" : ps.ButtonBig;
			p1.ButtonGuide = string.IsNullOrEmpty(ps.ButtonGuide) ? "" : ps.ButtonGuide;
			p1.ButtonBack = ps.ButtonBack;
			p1.ButtonStart = ps.ButtonStart;
			p1.ButtonX = ps.ButtonX;
			p1.ButtonY = ps.ButtonY;
			p1.DPad = ps.DPad;
			p1.DPadDown = ps.DPadDown;
			p1.DPadLeft = ps.DPadLeft;
			p1.DPadRight = ps.DPadRight;
			p1.DPadUp = ps.DPadUp;
			p1.ForceEnable = ps.ForceEnable;
			p1.ForceOverall = ps.ForceOverall;
			p1.ForceSwapMotor = ps.ForceSwapMotor;
			p1.ForceType = ps.ForceType;
			p1.GamePadType = ps.GamePadType;
			p1.LeftMotorPeriod = ps.LeftMotorPeriod;
			p1.LeftShoulder = ps.LeftShoulder;
			p1.LeftThumbAntiDeadZoneX = ps.LeftThumbAntiDeadZoneX;
			p1.LeftThumbAntiDeadZoneY = ps.LeftThumbAntiDeadZoneY;
			p1.LeftThumbAxisX = ps.LeftThumbAxisX;
			p1.LeftThumbAxisY = ps.LeftThumbAxisY;
			p1.LeftThumbButton = ps.LeftThumbButton;
			p1.LeftThumbDeadZoneX = ps.LeftThumbDeadZoneX;
			p1.LeftThumbDeadZoneY = ps.LeftThumbDeadZoneY;
			p1.LeftThumbDown = ps.LeftThumbDown;
			p1.LeftThumbLeft = ps.LeftThumbLeft;
			p1.LeftThumbRight = ps.LeftThumbRight;
			p1.LeftThumbUp = ps.LeftThumbUp;
			p1.LeftTrigger = ps.LeftTrigger;
			p1.LeftTriggerDeadZone = ps.LeftTriggerDeadZone;
			p1.PassThrough = ps.PassThrough;
			p1.RightMotorPeriod = ps.RightMotorPeriod;
			p1.RightShoulder = ps.RightShoulder;
			p1.RightThumbAntiDeadZoneX = ps.RightThumbAntiDeadZoneX;
			p1.RightThumbAntiDeadZoneY = ps.RightThumbAntiDeadZoneY;
			p1.RightThumbAxisX = ps.RightThumbAxisX;
			p1.RightThumbAxisY = ps.RightThumbAxisY;
			p1.RightThumbButton = ps.RightThumbButton;
			p1.RightThumbDeadZoneX = ps.RightThumbDeadZoneX;
			p1.RightThumbDeadZoneY = ps.RightThumbDeadZoneY;
			p1.RightThumbDown = ps.RightThumbDown;
			p1.RightThumbLeft = ps.RightThumbLeft;
			p1.RightThumbRight = ps.RightThumbRight;
			p1.RightThumbUp = ps.RightThumbUp;
			p1.RightTrigger = ps.RightTrigger;
			p1.RightTriggerDeadZone = ps.RightTriggerDeadZone;
			db.SaveChanges();
			return "";
		}

		[WebMethod]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			var sr = new SearchResult();
			var db = new Data.x360ceModelContainer();
			// All instances of user.
			var instances = args.Where(x => x.InstanceGuid != Guid.Empty).Select(x => x.InstanceGuid).Distinct().ToArray();
			if (instances.Length == 0)
			{
				sr.Settings = new Setting[0];
			}
			else
			{
				sr.Settings = db.Settings.Where(x => instances.Contains(x.InstanceGuid))
					.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ToArray();
			}
			// Global Configurations.
			var products = args.Where(x => x.ProductGuid != Guid.Empty).Select(x => x.ProductGuid).Distinct().ToArray();
			var files = args.Where(x => !string.IsNullOrEmpty(x.FileName) || !string.IsNullOrEmpty(x.FileProductName)).ToList();
			if (products.Length == 0)
			{
				sr.Summaries = new Summary[0];
			}
			else
			{
				files.Add(new SearchParameter() { FileName = "", FileProductName = "" });
				Expression body = null;
				var param = Expression.Parameter(typeof(Summary), "x");
				var pgParam = Expression.PropertyOrField(param, "ProductGuid");
				var fnParam = Expression.PropertyOrField(param, "FileName");
				var fpParam = Expression.PropertyOrField(param, "FileProductName");
				for (int i = 0; i < products.Length; i++)
				{
					var productGuid = products[i];
					for (int f = 0; f < files.Count; f++)
					{
						var fileName = files[f].FileName;
						var fileProductName = files[f].FileProductName;
						var exp1 = Expression.AndAlso(
								Expression.Equal(pgParam, Expression.Constant(productGuid)),
								Expression.Equal(fnParam, Expression.Constant(fileName))

						   );
						exp1 = Expression.AndAlso(exp1, Expression.Equal(fpParam, Expression.Constant(fileProductName)));
						body = body == null ? exp1 : Expression.OrElse(body, exp1).Reduce();
					}
				}
				var lambda = Expression.Lambda<Func<Summary, bool>>(body, param);
				sr.Summaries = db.Summaries.Where(lambda).OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenBy(x => x.Users).ToArray();
			}
			return sr;
		}

		[WebMethod]
		public string DeleteSetting(Setting s)
		{
			var db = new Data.x360ceModelContainer();
			var setting = db.Settings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			if (setting == null) return "Setting not found";
			db.Settings.DeleteObject(setting);
			db.SaveChanges();
			return "";
		}

		[WebMethod]
		public SearchResult LoadSetting(Guid[] checksum)
		{
			var sr = new SearchResult();
			var db = new Data.x360ceModelContainer();
			sr.PadSettings = db.PadSettings.Where(x => checksum.Contains(x.PadSettingChecksum)).ToArray();
			return sr;
		}

		[WebMethod]
		public List<Vendor> GetVendors()
		{
			var db = new Data.x360ceModelContainer();
			var q = from row in db.Vendors
					select new Vendor
					{
						VendorId = row.VendorId,
						VendorName = row.VendorName,
						ShortName = row.ShortName,
						WebSite = row.WebSite
					};
			return q.ToList();
		}


	}

}
