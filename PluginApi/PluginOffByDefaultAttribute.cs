using System;

namespace x360ce.Plugins
{
	/// <summary>Keeps a plugin off until the person ticks it on the Options tab's Plugins page.</summary>
	/// <remarks>
	/// Put on the plugin's assembly, in any of its files: <c>[assembly: x360ce.Plugins.PluginOffByDefault]</c>. x360ce lists
	/// the plugin unticked and makes nothing of it: no device is offered, and no class of it is made. Once ticked, it loads
	/// on every start until it is unticked.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public sealed class PluginOffByDefaultAttribute : Attribute
	{
	}
}
