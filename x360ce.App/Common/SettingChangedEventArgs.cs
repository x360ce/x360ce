using System;

namespace x360ce.App
{
    public class SettingChangedEventArgs : EventArgs
    {
        public SettingChangedEventArgs(SettingsMapItem item)
        {
            _Item = item;
        }

        public SettingsMapItem Item { get { return _Item; } }
        SettingsMapItem _Item;

    }
}
