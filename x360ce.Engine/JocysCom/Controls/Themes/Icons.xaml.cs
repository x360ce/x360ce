using System.Windows;

namespace JocysCom.ClassLibrary.Controls.Themes
{
	partial class Icons : ResourceDictionary
	{
		public Icons()
		{
			InitializeComponent();
		}

		public static Icons Current => _Current = _Current ?? new Icons();
		private static Icons _Current;

		public const string Icon_Add = nameof(Icon_Add);
		public const string Icon_Cancel = nameof(Icon_Cancel);
		public const string Icon_Delete = nameof(Icon_Delete);
		public const string Icon_Edit = nameof(Icon_Edit);
		public const string Icon_Error = nameof(Icon_Error);
		public const string Icon_Exit = nameof(Icon_Exit);
		public const string Icon_Export = nameof(Icon_Export);
		public const string Icon_FolderOpen = nameof(Icon_FolderOpen);
		public const string Icon_Ignore = nameof(Icon_Ignore);
		public const string Icon_Import = nameof(Icon_Import);
		public const string Icon_Information = nameof(Icon_Information);
		public const string Icon_InformationGrey = nameof(Icon_InformationGrey);
		public const string Icon_OK = nameof(Icon_OK);
		public const string Icon_Play = nameof(Icon_Play);
		public const string Icon_ProcessLeft = nameof(Icon_ProcessLeft);
		public const string Icon_ProcessRight = nameof(Icon_ProcessRight);
		public const string Icon_Question = nameof(Icon_Question);
		public const string Icon_Record = nameof(Icon_Record);
		public const string Icon_Refresh = nameof(Icon_Refresh);
		public const string Icon_Remove = nameof(Icon_Remove);
		public const string Icon_Reset = nameof(Icon_Reset);
		public const string Icon_Save = nameof(Icon_Save);
		public const string Icon_SelectAll = nameof(Icon_SelectAll);
		public const string Icon_SelectInverse = nameof(Icon_SelectInverse);
		public const string Icon_SelectNone = nameof(Icon_SelectNone);
		public const string Icon_Stop = nameof(Icon_Stop);
		public const string Icon_ToggleOff = nameof(Icon_ToggleOff);
		public const string Icon_ToggleOn = nameof(Icon_ToggleOn);
		public const string Icon_Warning = nameof(Icon_Warning);

	}
}
