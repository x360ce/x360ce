namespace x360ce.Engine
{
	public enum TargetType
	{
		None = 0,
		// [0;1]
		Button,
		// [0;255]
		LeftTrigger,
		RightTrigger,
		// [-32768;32767]
		LeftThumbX,
		LeftThumbY,
		RightThumbX,
		RightThumbY,
	}
}
