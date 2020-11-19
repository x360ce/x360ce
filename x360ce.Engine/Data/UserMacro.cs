using System;

namespace x360ce.Engine.Data
{
	public partial class UserMacro : IDisplayName
	{
		public UserMacro()
		{
			Id = Guid.NewGuid();
			Created = DateTime.Now;
			Updated = Created;
			MapRangeMin = 1;
			MapRangeMax = 1;
		}

		public void LoadGuideButton()
		{
			Text = "{7}";
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
