namespace JocysCom.ClassLibrary.Resources
{
	/// <summary>
	/// Colors are recommended by spectrum.
	/// </summary>
	public enum DataPriority : int
	{
		/// <summary>Highest priority. (Red #F4B0B0)</summary>
		Highest = 4,
		/// <summary>Between Highest and High priority. (Orange #F4CCB0)</summary>
		VeryHigh = 3,
		/// <summary>High priority. (Yellow #F4F4B0)</summary>
		High = 2,
		/// <summary>Between High and Normal priority. (LightYellow #FFFFE2)</summary>
		AboveNormal = 1,
		/// <summary>	Normal priority. (White #FFFFFF)</summary>
		Normal = 0,
		/// <summary>Between Normal and Low priority (LightGreen #E2FFE2).</summary>
		BelowNormal = -1,
		/// <summary>Low priority. (Green #B0F4B0)</summary>
		Low = -2,
		/// <summary>Between Low and Lowest message priority. (Blue #B0B0F4)</summary>
		VeryLow = -3,
		/// <summary>Lowest message priority. (Violet #CCB0F4)</summary>
		Lowest = -4,
	}
}
