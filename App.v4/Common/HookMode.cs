namespace x360ce.App
{
	enum HookMode
	{
		/// <summary>Disabled</summary>
		Disabled = 0,
		/// <summary>Make device XInput compatible only, but not XBOX 360 gamepad.</summary>
		Normal = 1,
		/// <summary>Faking FakeWMI + FakeDI, ie will spoof all needed data to game, will make (almost) XBOX 360 Controller for Windows.</summary>
		Compatibility = 2,
		/// <summary>Full compatibility mode.</summary>
		All = 3
	}
}
