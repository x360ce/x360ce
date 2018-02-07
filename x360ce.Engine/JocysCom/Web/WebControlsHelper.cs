using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JocysCom.ClassLibrary.Web
{
	public partial class WebControlsHelper
	{

		public static System.Web.UI.Control FindControlRecursive(System.Web.UI.Control root, string id)
		{
			if (root.ID == id)
				return root;
			foreach (System.Web.UI.Control control in root.Controls)
			{
				var t = FindControlRecursive(control, id);
				if (t != null)
					return t;
			}
			return null;
		}

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

		#region Apply Date Suffix

		/// <summary>
		/// All scripts, managed by ScriptManager and server side StyleSheets will be suffixed with v=LastWriteTime.
		/// How to use:
		/// protected void Page_PreRender(object sender, System.EventArgs e)
		/// {
		/// 	JocysCom.ClassLibrary.Web.WebControlsHelper.ApplyDateSuffix(Page);
		/// }
		/// </summary>
		/// <param name="page"></param>
		public static void ApplyDateSuffix(System.Web.UI.Page page)
		{
			foreach (var control in page.Header.Controls)
			{
				var link = control as System.Web.UI.HtmlControls.HtmlLink;
				if (link == null)
					continue;
				var isCss =
					"text/css".Equals(link.Attributes["type"], StringComparison.CurrentCultureIgnoreCase) ||
					"stylesheet".Equals(link.Attributes["rel"], StringComparison.CurrentCultureIgnoreCase) ||
					"icon".Equals(link.Attributes["rel"], StringComparison.CurrentCultureIgnoreCase);
				if (!isCss)
					continue;
				link.Href = GetFileWithSuffix(link.Href, page);
			}
			// ScriptManager requires System.Web.Extensions.dll
			var sm = System.Web.UI.ScriptManager.GetCurrent(page);
			if (sm == null)
				return;
			sm.ResolveScriptReference += ScriptManager_ResolveScriptReference;
		}


		/// <summary>
		/// ScriptReferenceEventArgs requires System.Web.Extensions.dll
		/// </summary>
		private static void ScriptManager_ResolveScriptReference(object sender, System.Web.UI.ScriptReferenceEventArgs e)
		{
			// ScriptManager requires System.Web.Extensions.dll
			var sm = (System.Web.UI.ScriptManager)sender;
			e.Script.Path = GetFileWithSuffix(e.Script.Path, sm.Page);
		}

		static string GetFileWithSuffix(string path, System.Web.UI.Page page)
		{
			if (string.IsNullOrEmpty(path))
				return path;
			// If path is absolute then return.
			if (path.Contains(":"))
				return path;
			var resolvedPath = page.ResolveUrl(path);
			// Check if path contains query.
			var index = resolvedPath.IndexOf('?');
			if (index > -1)
				resolvedPath = resolvedPath.Substring(0, index);
			var localPath = page.MapPath(resolvedPath);
			var fi = new System.IO.FileInfo(localPath);
			if (fi.Exists)
			{
				var v = string.Format("v={0:yyyyMMddHHmmss}", fi.LastWriteTime);
				if (path.Contains(v))
					return path;
				path += (index > -1 ? "&" : "?") + v;
			}
			return path;
		}

		#endregion

	}
}
