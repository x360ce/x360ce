namespace x360ce.Engine
{
	/// <summary>
	///  Custom X360CE direct input update class used for configuration.
	/// </summary>
	public partial class CustomDiUpdate
	{

		public MapType Type;
		public int Index;
		public int Value;

		public CustomDiUpdate(MapType type, int index, int value)
		{
			Type = type;
			Index = index;
			Value = value;
		}

	}
}
