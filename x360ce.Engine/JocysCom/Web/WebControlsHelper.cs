using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class WebControlsHelper
	{

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<System.Web.UI.Control> GetAll(System.Web.UI.Control control, Type type = null, bool includeTop = false)
		{
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			return controls
				// Get children controls and flatten resulting sequences into one sequence.
				.SelectMany(x => GetAll(x))
				// Merge controls with their children.
				.Concat(controls)
				// Include top control if required.
				.Concat(includeTop ? new[] { control } : new System.Web.UI.Control[0])
				// Filter controls by type.
				.Where(x => type == null || (type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType())));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(System.Web.UI.Control control, bool includeTop = false)
		{
			if (control == null) return new T[0];
			var type = typeof(T);
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			// Get children of controls and flatten resulting sequences into one sequence.
			var result = controls.SelectMany(x => GetAll(x)).ToArray();
			// Merge controls with their children.
			result = result.Concat(controls).ToArray();
			// Include top control if required.
			if (includeTop) result = result.Concat(new[] { control }).ToArray();
			// Filter controls by type.
			result = type.IsInterface
				? result.Where(x => x.GetType().GetInterfaces().Contains(type)).ToArray()
				: result.Where(x => type.IsAssignableFrom(x.GetType())).ToArray();
			// Cast to required type.
			var result2 = result.Select(x => (T)(object)x).ToArray();
			return result2;
		}

	}
}
