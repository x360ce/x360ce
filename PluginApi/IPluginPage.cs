namespace x360ce.Plugins
{
	/// <summary>A page a plugin adds to x360ce's main window, on a tab of its own after the Devices tab.</summary>
	/// <remarks>
	/// Optional. Implemented by a public Windows Forms control, such as a class derived from UserControl, whose public
	/// constructor takes an <see cref="IPluginHost"/>, or nothing. x360ce makes one of each such class, once, on its
	/// interface thread, after the plugin loads, and places it after the Devices tab, in the order the plugins load. It
	/// stays until x360ce closes, which disposes it. A page that throws while it is made is left out, and the Plugins page
	/// and the Issues tab say why.
	///
	/// Draw it in the system colours and lay it out with tables and automatic sizes, so it follows x360ce's theme, font
	/// and zoom. Give every control a name: x360ce's AI assistant access reaches a control by its name.
	/// </remarks>
	public interface IPluginPage
	{
		/// <summary>The tab's text, such as "Fitness".</summary>
		string PageName { get; }
	}
}
