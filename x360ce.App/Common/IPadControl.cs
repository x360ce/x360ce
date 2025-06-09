using JocysCom.ClassLibrary;
using System;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public interface IPadControl
	{
		event EventHandler<EventArgs<UserSetting>> OnSettingChanged;
		UserSetting CurrentUserSetting { get; }
		UserDevice CurrentUserDevice { get; }
	}
}
