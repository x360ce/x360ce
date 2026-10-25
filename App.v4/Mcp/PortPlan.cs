namespace x360ce.App.Mcp
{
	/// <summary>
	/// x360ce's ports, 37ACD: A is the program's instance, 1 for the first; C is 0 for the program's own services, 1 to 4
	/// a controller, and 5 to 9 are left for plugins; D is the service, or the device's number on its controller.
	/// 37101 is AI assistant access; 37141 is the first device on controller 4.
	/// </summary>
	public static class PortPlan
	{
		/// <summary>The program's instance.</summary>
		public const int Instance = 1;

		/// <summary>AI assistant access, the program's own MCP server.</summary>
		public const int AiAccess = 37000 + Instance * 100 + 1;

		/// <summary>The port of a controller's device, or 0 when the controller or the device's number takes more than one digit.</summary>
		public static int Port(int controller, int device)
		{
			if (controller < 1 || controller > 9 || device < 1 || device > 9)
				return 0;
			return 37000 + Instance * 100 + controller * 10 + device;
		}
	}
}
