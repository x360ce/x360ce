using System;

namespace JocysCom.ClassLibrary.Configuration
{
	public partial class AssemblyInfo
	{
		public static string ExpandPath(string path)
		{
			// Variables are quoted with '%' (percent) sign.
			path = Environment.ExpandEnvironmentVariables(path);
			// Variables are quoted with '{' and '}' sign.
			path = JocysCom.ClassLibrary.Text.Helper.Replace(path, Entry, false);
			return path;
		}

		public static string ParameterizePath(string path)
		{
			// Variables are quoted with '{' and '}' sign.
			path = JocysCom.ClassLibrary.Text.Helper.Replace(Entry, path, false);
			return path;
		}

		//public static string GetExpandedPath(string path)
		//{
		//	path = ExpandPath(path);
		//	path = IO.PathHelper.ConvertFromSpecialFoldersPattern(path, "{", "}");
		//	return path;
		//}

		//public static string GetParametrizedPath(string path)
		//{
		//	path = IO.PathHelper.ConvertToSpecialFoldersPattern(path, "{", "}");
		//	path = ParameterizePath(path);
		//	return path;
		//}

	}
}
