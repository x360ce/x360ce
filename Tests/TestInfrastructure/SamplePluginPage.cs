// The pages a plugin adds to the main window, as a plugin author writes them, for the plugin page tests.
using System;
using System.Windows.Forms;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>A page a plugin adds: it keeps the host x360ce gives it.</summary>
	public class SamplePluginPage : UserControl, IPluginPage
	{
		public SamplePluginPage(IPluginHost host)
		{
			Host = host;
			Name = "SamplePluginPage";
		}

		/// <summary>What the page may ask of x360ce.</summary>
		public IPluginHost Host { get; }

		public string PageName { get { return "Sample Page"; } }
	}

	/// <summary>A page that cannot be made, as one whose plugin is broken might be written.</summary>
	public class ThrowingPluginPage : UserControl, IPluginPage
	{
		public ThrowingPluginPage()
		{
			throw new InvalidOperationException("No page today.");
		}

		public string PageName { get { return "Throwing Page"; } }
	}
}
