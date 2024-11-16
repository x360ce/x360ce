using System;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Configuration
{
	public interface ISettingsListFileItem : ISettingsFileItem
	{
		bool IsChecked { get; set; }
		string StatusText { get; set; }
		System.Windows.MessageBoxImage StatusCode { get; set; }
		DrawingImage Icon { get; }
		string IconType { get; set; }
		string IconData { get; set; }
		void SetIcon(string contents, string type = ".svg");

		// List Properties.
		bool IsPinned { get; set; }
		DateTime Created { get; set; }
		DateTime Modified { get; set; }
		int ListGroupTimeSortKey { get; }
		string ListGroupPathSortKey { get; }
		string ListGroupNameSortKey { get; }
		string ListGroupTime { get; }
		string ListGroupPath { get; }
		string ListGroupName { get; }
	}
}
