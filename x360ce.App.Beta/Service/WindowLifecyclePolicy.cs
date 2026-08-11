namespace x360ce.App.Service
{
	public enum WindowCloseAction
	{
		MinimizeToTray,
		Exit,
	}

	public static class WindowLifecyclePolicy
	{
		public static WindowCloseAction DecideClose(bool exitRequested, bool minimizeOnClose)
		{
			return !exitRequested && minimizeOnClose
				? WindowCloseAction.MinimizeToTray
				: WindowCloseAction.Exit;
		}
	}
}
