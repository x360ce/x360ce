using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>What each mapping picker on the General page is for.</summary>
		/// <remarks>
		/// Every one of them answers the same question about a different part of the controller, so
		/// the wording is written once and the part's name filled in. Written by hand for each, the
		/// thirty would drift apart from one another within a release or two.
		/// </remarks>
		static void AddMappingPickers(Dictionary<string, Text> d)
		{
			Picker(d, "LeftTrigger", "the left trigger");
			Picker(d, "RightTrigger", "the right trigger");
			Picker(d, "LeftShoulder", "the left shoulder button");
			Picker(d, "RightShoulder", "the right shoulder button");
			Picker(d, "ButtonA", "the A button");
			Picker(d, "ButtonB", "the B button");
			Picker(d, "ButtonX", "the X button");
			Picker(d, "ButtonY", "the Y button");
			Picker(d, "ButtonBack", "the Back button");
			Picker(d, "ButtonStart", "the Start button");
			Picker(d, "ButtonGuide", "the Guide button");
			Picker(d, "DPad", "the directional pad");
			Picker(d, "DPadUp", "directional pad up");
			Picker(d, "DPadDown", "directional pad down");
			Picker(d, "DPadLeft", "directional pad left");
			Picker(d, "DPadRight", "directional pad right");
			Picker(d, "LeftThumbAxisX", "the left stick, side to side");
			Picker(d, "LeftThumbAxisY", "the left stick, up and down");
			Picker(d, "LeftThumbButton", "pressing the left stick");
			Picker(d, "LeftThumbUp", "the left stick pushed up");
			Picker(d, "LeftThumbDown", "the left stick pushed down");
			Picker(d, "LeftThumbLeft", "the left stick pushed left");
			Picker(d, "LeftThumbRight", "the left stick pushed right");
			Picker(d, "RightThumbAxisX", "the right stick, side to side");
			Picker(d, "RightThumbAxisY", "the right stick, up and down");
			Picker(d, "RightThumbButton", "pressing the right stick");
			Picker(d, "RightThumbUp", "the right stick pushed up");
			Picker(d, "RightThumbDown", "the right stick pushed down");
			Picker(d, "RightThumbLeft", "the right stick pushed left");
			Picker(d, "RightThumbRight", "the right stick pushed right");
		}

		static void Picker(Dictionary<string, Text> d, string field, string part)
		{
			d["PadControl." + field + "ComboBox"] = new Text(null,
				"Which control on your device works " + part + ".");
		}

		/// <summary>The composite controls themselves, and the device detail page.</summary>
		static void AddDeviceDetails(Dictionary<string, Text> d)
		{
			d[nameof(PadControl)] = new Text(null,
				"Everything about one emulated Xbox controller: what works it, and how.");
			d[nameof(AxisMapUserControl)] = new Text(null,
				"Shapes what the game receives from one control, without changing the device.");
			d[nameof(AxisToButtonUserControl)] = new Text(null,
				"How far a stick or pedal must move before the button mapped to it counts as pressed.");
			d[nameof(WheelChartUserControl)] = new Text(null,
				"The centering spring's force at every angle of the wheel, and the wheel's angle and force now.");
			d[nameof(DirectInputUserControl)] = new Text(null,
				"What the mapped device reports about itself, and its values as they change.");
			d[nameof(MapExpressionToggle)] = new Text(null,
				"Switches one mapping between choosing a control and writing a formula.");
			d[nameof(OptionsUserControl)] = new Text(null,
				"Settings for the program itself, rather than for one controller.");
			d[nameof(OptionsInternetUserControl)] = new Text(null,
				"Whether settings are shared with the online database, and the account used.");
			d[nameof(OptionsSettingsUserControl)] = new Text(null,
				"Where your settings are kept, and how to move them somewhere else.");
			d[nameof(GamesGridUserControl)] = new Text(null,
				"Games this program is set up for.");
			d[nameof(GameDetailsUserControl)] = new Text(null,
				"How the selected game is set up.");
			d[nameof(UserDevicesUserControl)] = new Text(null,
				"Every controller the program can see.");
			d[nameof(CloudUserControl)] = new Text(null,
				"Settings waiting to be sent to or fetched from the online database.");
			d[nameof(IssuesUserControl)] = new Text("Issues list",
				"Problems the program found, and what to do about each one.");
			d[nameof(AboutControl)] = new Text(null,
				"Version, who made the program and what it carries, what changed in each release, and the licence.");

			// What the device says about itself. Each of these is read from the device and shown
			// as it is; none of them can be edited.
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceProductNameTextBox)] = new Text(null,
				"Name the device reports for itself.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceVendorNameTextBox)] = new Text(null,
				"Name of the company that made the device.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceVidTextBox)] = new Text(null,
				"Number identifying the maker, as reported over USB.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DevicePidTextBox)] = new Text(null,
				"Number identifying the model, as reported over USB.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceRevTextBox)] = new Text(null,
				"Hardware revision the device reports.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceTypeTextBox)] = new Text(null,
				"What kind of device Windows considers this to be.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceProductGuidTextBox)] = new Text(null,
				"Identifier shared by every device of this model.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceInstanceGuidTextBox)] = new Text(null,
				"Identifier for this one device, which tells two alike apart.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiCapAxesTextBox)] = new Text(null,
				"How many axes the device has, such as sticks and pedals.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiCapButtonsTextBox)] = new Text(null,
				"How many buttons the device has.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiCapPovsTextBox)] = new Text(null,
				"How many hat switches the device has.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiSlidersTextBox)] = new Text(null,
				"How many sliders the device has.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiCapFfStateTextBox)] = new Text(null,
				"Whether force feedback is available on the device right now.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.ActuatorsTextBox)] = new Text(null,
				"How many motors the device has for force feedback.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiObjectsDataGridView)] = new Text(null,
				"Every axis, button and hat the device reports having.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiEffectsDataGridView)] = new Text(null,
				"The vibration effects the device says it can produce.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiAxisDataGridView)] = new Text(null,
				"What each axis reads right now. Move a control to see which line changes.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiButtonsTextBox)] = new Text("Buttons pressed",
				"Which buttons are pressed right now, by the numbers the mapping boxes use.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiSlidersDataGridView)] = new Text(null,
				"What each slider reads right now.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiPovsDataGridView)] = new Text(null,
				"What each hat switch reads right now.");
		}
	}
}
