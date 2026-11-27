namespace x360ce.App
{
	/// <summary>
	/// How a mapping is lit, on its mark on the controller picture and on its box alike. A later state outranks an earlier
	/// one: <see cref="Controls.PadControlImager.Light"/> picks it.
	/// </summary>
	public enum NavImageType
	{
		/// <summary>The mark is shown, because the pointer is near it on the picture. Its box is not lit.</summary>
		Normal,
		/// <summary>The control the box names is moving or pressed, as the Source Input panel lights it.</summary>
		Active,
		/// <summary>The pointer is on the box or on its mark.</summary>
		Over,
		/// <summary>The box is being recorded.</summary>
		Record,
	}
}
