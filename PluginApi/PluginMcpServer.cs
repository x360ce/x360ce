namespace x360ce.Plugins
{
	/// <summary>Lets a plugin device be reached as an MCP server of its own, by an AI assistant or any program.</summary>
	/// <remarks>
	/// A device class declares it as a property: <c>public PluginMcpServer McpServer { get; } = new PluginMcpServer();</c>.
	/// x360ce then shows the server's settings on the device's tab, as the AI page shows those of x360ce's own: whether it
	/// is open, its level, address, port and token. They are the person's to set and are not given to the plugin. The port
	/// is x360ce's: 371CD, C the controller the device is mapped to and D its number there, so 37141 is the first device on
	/// controller 4. While it is open, x360ce serves the device's settings as tools on it: read, which returns every
	/// setting with its value, and a set_ tool for each setting a person can change, such as set_state. A caller reaches
	/// that device alone, never the rest of x360ce. The same tools are reached through x360ce's own door, with its
	/// devices_list and device_call.
	/// </remarks>
	public sealed class PluginMcpServer
	{
	}
}
