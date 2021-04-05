using System.Reflection;
using x360ce.Engine;

namespace x360ce.App
{
	public class SettingsMapItem
	{

		#region ■ INI

		/// <summary>
		/// Property Section inside INI file.
		/// </summary>
		public string IniSection { get; set; }

		/// <summary>
		/// Property key inside INI file.
		/// </summary>
		public string IniKey { get; set; }

		#endregion

		/// <summary>
		/// Property control inside windows application.
		/// </summary>
		public object Control { get; set; }
		/// <summary>
		/// Property name on .NET class.
		/// </summary>
		public string PropertyName { get; set; }


		public PropertyInfo Property { get; set; }
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
		public object DefaultValue { get; set; }

		/// <summary>
		/// X360CE PAD Map Code.
		/// </summary>
		public MapCode Code { get; set; }

	}
}
