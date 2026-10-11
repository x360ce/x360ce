using System;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>Defines a settings file item for list-based user interfaces with selection, status, and icon serialization.</summary>
	/// <remarks>
	/// Provides grouping metadata properties for UI grouping support:
	/// ListGroupTimeSortKey, ListGroupPathSortKey, ListGroupNameSortKey,
	/// ListGroupTime, ListGroupPath, and ListGroupName.
	/// </remarks>
	public interface ISettingsListFileItem : ISettingsFileItem
	{
		bool IsChecked { get; set; }
		string StatusText { get; set; }
		System.Windows.MessageBoxImage StatusCode { get; set; }
		DrawingImage Icon { get; }

		/// <summary>Icon file extension or type (e.g. ".svg").</summary>
		string IconType { get; set; }

		/// <summary>Base64-encoded icon data.</summary>
		string IconData { get; set; }

		/// <summary>Sets the icon data by encoding the provided contents as base64.</summary>
		/// <param name="contents">Raw icon content (e.g. SVG).</param>
		/// <param name="type">File extension/type (default ".svg").</param>
		void SetIcon(string contents, string type = ".svg");

		// List Properties.
		bool IsPinned { get; set; }
		DateTime? Created { get; set; }
		DateTime? Modified { get; set; }
		int ListGroupTimeSortKey { get; }
		string ListGroupPathSortKey { get; }
		string ListGroupNameSortKey { get; }
		string ListGroupTime { get; }
		string ListGroupPath { get; }
		string ListGroupName { get; }
	}
}