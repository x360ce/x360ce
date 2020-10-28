using System;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class UserKeyboardMap: IDisplayName, IUserRecord
	{
		public UserKeyboardMap()
		{
			Id = Guid.NewGuid();
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
		}

		public void LoadGuideButton()
		{
			ScriptText = "{LWin}{G}";
			MapType = (int)Engine.MapType.Button;
			MapRangeMin = 1;
			MapRangeMax = 1;
			Name = "Guide Button Map";
		}

		[XmlIgnore]
		public string DisplayName
		{
			get { return Name; }
		}

	}
}
