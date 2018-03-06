namespace x360ce.App
{
	public class SettingsMapItem
	{

		#region INI

		/// <summary>
		/// Property Section inside INI file.
		/// </summary>
		public string IniSection { get; set; }

		/// <summary>
		/// Property key inside INI file.
		/// </summary>
		public string IniKey { get; set; }

		/// <summary>
		/// Property path inside INI file.
		/// </summary>
		public string IniPath { get { return string.Format("{0}\\{1}", IniSection, IniKey); } }

		#endregion

		/// <summary>
		/// Property control inside windows application.
		/// </summary>
		public System.Windows.Forms.Control Control { get; set; }
		/// <summary>
		/// Property name on .NET class.
		/// </summary>
		public string PropertyName { get; set;}
		/// <summary>
		/// Description of the setting.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Setting is mapped to controller.
		/// </summary>
		public Engine.MapTo MapTo { get; set; }

		/// <summary>
		/// Default value if property is null or empty.
		/// </summary>
		public string DefaultValue { get; set; }

	}
}
