using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Threading;

namespace x360ce.App.Issues
{
	/// <summary>An issue whose check reads files or asks Windows, so it runs once, and again only after what it reads may have changed.</summary>
	/// <remarks>Issues that read what the program holds in memory stay on the round of checks.</remarks>
	public abstract class OnChangeIssue : IssueItem
	{
		/// <summary>How many changes were told.</summary>
		static int _changes;

		/// <summary>The count of changes this issue was last checked at, none yet.</summary>
		int _checkedAt = -1;

		protected OnChangeIssue()
		{
			// Added first, so the round that follows a fix finds the change.
			Fixed += (sender, e) => Changed();
		}

		/// <summary>The count of changes told, for a check that reads what these issues read.</summary>
		public static int Changes => Volatile.Read(ref _changes);

		/// <summary>Says that what these issues read may have changed: a device came or went, a setting changed, or the person opened the Issues page.</summary>
		public static void Changed()
		{
			Interlocked.Increment(ref _changes);
			DInput.VirtualDriverInstaller.ForgetHidHideState();
		}

		/// <inheritdoc/>
		public override bool IsDue()
		{
			var changes = Changes;
			if (changes == _checkedAt)
				return false;
			_checkedAt = changes;
			return true;
		}
	}
}
