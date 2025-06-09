using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;

namespace x360ce.App
{
	public partial class SettingsManager
	{
		private static void UserDevices_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If list item was not changed then return.
			if (e.ListChangedType != ListChangedType.ItemChanged)
				return;
			// If not IsHidden changed then return.
			if (e.PropertyDescriptor?.Name != nameof(Engine.Data.UserDevice.IsHidden))
				return;
			var ud = UserDevices.Items[e.NewIndex];
			// If device allowed to be hidden then...
			if (ud.AllowHide)
			{
				var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
				if (canModify)
				{
					var ids = new string[] { ud.DevDeviceId };
					// Use begin invoke which will prevent mouse multi-select rows.
					ControlsHelper.BeginInvoke(()
						=> AppHelper.SynchronizeToHidGuardian(ud.InstanceGuid));
				}
				else
				{
					var form = new MessageBoxWindow();
					form.ShowDialog("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied",
						System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
				}
			}
		}
	}
}
