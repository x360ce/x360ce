using System;
using x360ce.Engine;

namespace x360ce.App
{
    public class SettingChangedEventArgs : EventArgs
    {
        public SettingChangedEventArgs(SettingsMapItem item)
        {
            _Item = item;
        }

        /// <summary>A controller page loaded with other settings, with no one box behind the change.</summary>
        public SettingChangedEventArgs(MapTo page)
        {
            _Page = page;
        }

        public SettingsMapItem Item { get { return _Item; } }
        SettingsMapItem _Item;

        /// <summary>The controller page the change belongs to, or <see cref="MapTo.None"/> for none, as for an Options box.</summary>
        public MapTo Page { get { return _Item == null ? _Page : _Item.MapTo; } }
        MapTo _Page;

    }
}
