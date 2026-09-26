using System;
using x360ce.Engine;

namespace x360ce.Web.Controls
{
	/// <summary>
	/// The "Controllers in Database" picture: the cached monthly totals drawn as inline SVG.
	/// </summary>
	/// <remarks>
	/// Two caches sit in front of the database. The control's output is cached for an hour
	/// (the OutputCache directive in the markup), so this code and its query run at most once
	/// an hour per server. Behind that, <c>x360ce_GetNewDeviceStats</c> keeps its monthly counts
	/// in <c>x360ce_NewDeviceStats</c> and recounts the settings table only when that is more
	/// than twelve hours old, so the hourly query is a read of about a hundred rows.
	/// </remarks>
	public partial class ControllersChartControl : System.Web.UI.UserControl
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			ChartSvg.Text = DeviceStatsSvg.Render(EngineHelper.GetNewDeviceStats());
		}
	}
}
