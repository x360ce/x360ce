namespace x360ce.App
{
	/// <summary>How much an AI assistant or a script may do through the program's tools. Chosen on the Options page, never by the caller.</summary>
	public enum AiAccess
	{
		/// <summary>Nothing. The door is closed.</summary>
		Off = 0,
		/// <summary>Read the interface, the devices and the help. Changes nothing.</summary>
		Read = 1,
		/// <summary>Everything a person does on the tabs. Windows may still ask, as it asks a person, when HID Guardian is set to configure automatically.</summary>
		Configure = 2,
		/// <summary>Also the actions that install or remove drivers and switch on debug mode.</summary>
		Administer = 3,
	}
}
