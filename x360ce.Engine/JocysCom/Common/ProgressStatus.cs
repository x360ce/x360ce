using System;

namespace JocysCom.ClassLibrary
{
	/// <summary>
	/// Universal state that describes a task, including its progress and data processing status.
	/// </summary>
	/// <remarks>Enum allows for the insertion of an additional state between the current states in the future.</remarks>
	[Flags]
	public enum ProgressStatus
	{
		/// <summary>No state has been set.</summary>
		None = 0,

		// Initialization and Setup

		/// <summary>The task has been initialized but not yet started.</summary>
		Initialized = 2,

		/// <summary>The task or the data item has been created.</summary>
		Created = Initialized << 2,

		/// <summary>The task has been started.</summary>
		Started = Created << 2,

		// Active Processing

		/// <summary>The task or the data item is currently being processed.</summary>
		Processing = Started << 2,

		/// <summary>The task or the data item has been updated.</summary>
		Updated = Processing << 2,

		/// <summary>Processing has resumed after a pause.</summary>
		Skipped = Updated << 2,

		/// <summary>The processing has been paused.</summary>
		Paused = Skipped << 2,

		/// <summary>Processing has resumed after a pause.</summary>
		Resumed = Paused << 2,

		// Final States

		/// <summary>The task or the data item has been deleted.</summary>
		Deleted = Resumed << 2,

		/// <summary>The task has been completed successfully.</summary>
		Completed = Deleted << 2,

		/// <summary>The task encountered an error and failed.</summary>
		Failed = Completed << 2,

		/// <summary>The task was canceled by the user or system.</summary>
		Canceled = Failed << 2,

		// Error Handling

		/// <summary>The task encountered an exception.</summary>
		Exception = Canceled << 2,
	}
}
