using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;

namespace x360ce.App
{
	public partial class SettingsManager
	{
		private static void UserDevices_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			var pd = e.PropertyDescriptor;
			if (pd != null && e.ListChangedType == ListChangedType.ItemChanged)
			{
				if (e.PropertyDescriptor.Name == nameof(Engine.Data.UserDevice.IsHidden))
				{
					var ud = UserDevices.Items[e.NewIndex];
					if (ud.AllowHide)
					{
						var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
						if (canModify)
						{
							//var ids = AppHelper.GetIdsToAffect(ud.HidDeviceId, ud.HidHardwareIds);
							var ids = new string[] { ud.DevDeviceId };
							//ud.IsHidden = !ud.IsHidden;
							// Use begin invoke which will prevent mouse multi-select rows.
							ControlsHelper.BeginInvoke(() =>
							{
								AppHelper.SynchronizeToHidGuardian(ud.InstanceGuid);
							});
						}
						else
						{
							var form = new MessageBoxForm();
							form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
							form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied",
								System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
						}
					}
				}
			}

		}
	}
}
