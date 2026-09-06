namespace x360ce.App
{
	/// <summary>How much an AI assistant or a script may do through the program's tools, once access is switched on. Chosen on the Options page, never by the caller.</summary>
	public enum AiAccess
	{
		/// <summary>Read the interface, the devices and the help, and point at things. Changes no setting.</summary>
		Read = 1,
		/// <summary>Everything a person does on the tabs. Windows may still ask, as it asks a person, when HID Guardian is set to configure automatically.</summary>
		Configure = 2,
		/// <summary>Also the actions that install or remove drivers and switch on debug mode.</summary>
		Administer = 3,
	}
}
