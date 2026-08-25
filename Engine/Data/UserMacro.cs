using System;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class UserMacro: IDisplayName
	{
		public UserMacro()
		{
			Id = Guid.NewGuid();
			Created = DateTime.Now;
			Updated = Created;
		}

		public void LoadGuideButton()
		{
			Text = "{LWin}{G}";
			MapType = (int)Engine.MapType.Button;
			MapRangeMin = 1;
			MapRangeMax = 1;
			Name = "Guide Button Map";
		}

		#region Interface: IDisplayName

		string IDisplayName.DisplayName => Name;

		#endregion

	}
}
