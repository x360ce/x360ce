namespace x360ce.Plugins
{
	/// <summary>A game plugin that keeps files of its own for its games and puts each game right when x360ce starts.</summary>
	/// <remarks>
	/// Optional: a class that implements <see cref="IPluginGame"/> implements this too. x360ce sets <see cref="DataFolder"/>
	/// on every instance it makes, just after <see cref="IPluginGame.TelemetryData"/>. Once x360ce has read its list of games
	/// at start, it makes an instance for each game on its Games tab whose file the plugin is for, sets
	/// <see cref="IPluginGame.GamePath"/>, calls <see cref="Start"/> and disposes the instance, on the interface thread, before
	/// any game's page is made.
	/// </remarks>
	public interface IPluginGameStart
	{
		/// <summary>The plugin's own folder in x360ce's settings folder, such as C:\ProgramData\X360CE\PluginData\MyGame.</summary>
		/// <remarks>The plugin makes it when it first writes there. x360ce loads no plugin from it.</remarks>
		string DataFolder { get; set; }

		/// <summary>Puts the game at <see cref="IPluginGame.GamePath"/> right as x360ce starts. Must return at once; longer work goes on a thread of its own.</summary>
		void Start();
	}
}
