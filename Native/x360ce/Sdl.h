#pragma once

#include <string>
#include <dinput.h>

#include "Config.h"

/// <summary>
/// SDL's controller database, SDL_GameControllerDB, read for one device and turned into the library's own mapping; and
/// x360ce's own controller database, whose rows are x360ce.ini keys.
/// </summary>
/// <remarks>
/// The rows are the Windows rows x360ce 4 ships (Engine\Resources\gamecontrollerdb.txt.gz), carried in the library as
/// the resource gamecontrollerdb.windows.txt, sorted by vendor and product so a device is found by a binary search.
/// A gamecontrollerdb.txt found where x360ce.ini is looked for, beside the library and then in %ProgramData%\x360ce,
/// is read too: a device it has a row for takes its row from there.
///
/// The conversion follows x360ce 4's Engine\Common\Sdl\SdlConverter.cs, and both pass the cases in
/// Tests\Common\SdlConverterCases.txt: <c>aK</c> is the K-th axis the device has of X, Y, Z, Rx, Ry, Rz, Slider 0 and
/// Slider 1; <c>bK</c> is button K+1; <c>hH.V</c> is POV H+1 up, right, down or left for V 1, 2, 4 or 8; <c>+aK</c> and
/// <c>-aK</c> are the axis's halves and <c>aK~</c> the axis inverted. SDL's stick Y grows downward, so it is inverted.
/// A stick whose two halves are on two different axes, a formula in x360ce 4, is written into the stick's two
/// direction slots as half axes, which <see cref="Controller::GetState"/> subtracts.
/// </remarks>
namespace Sdl
{
	/// <summary>What SDL needs to know about a device: its ids, its name and which of its controls it has.</summary>
	struct Device
	{
		u16 vendorId = 0;
		u16 productId = 0;
		std::string name;
		/// <summary>X, Y, Z, Rx, Ry and Rz: the library's axes 1 to 6, one bit each.</summary>
		u32 axisMask = 0;
		/// <summary>Slider 0 and 1: the library's sliders 1 and 2, one bit each.</summary>
		u32 sliderMask = 0;
		/// <summary>How many buttons; nought lets any button through.</summary>
		u32 buttonCount = 0;
		/// <summary>How many hats; nought lets any hat through.</summary>
		u32 povCount = 0;
		/// <summary>Whether the device reports force feedback.</summary>
		bool forceFeedback = false;
	};

	/// <summary>Reads what SDL needs from an opened device whose data format is c_dfDIJoystick2.</summary>
	void ReadDevice(IDirectInputDevice8A* device, Device* out);

	/// <summary>Whether a device is an XInput controller: its path carries "IG_", as Microsoft documents.</summary>
	bool IsXInputDevice(IDirectInputDevice8A* device);

	/// <summary>The row for a device, the one of its vendor and product whose name is nearest its own; empty when SDL does not know it.</summary>
	std::string FindRow(const Device& device);

	/// <summary>
	/// The device's row in x360ce's controller database, gamecontrollerdb_x360ce.windows.txt, found as FindRow finds SDL's:
	/// the setting most people use with it, as x360ce.ini keys. Empty when x360ce's database does not know it.
	/// </summary>
	std::string FindX360ceRow(const Device& device);

	/// <summary>
	/// Puts the x360ce.ini keys of an x360ce database row into a section, for Config to read as it reads x360ce.ini; the
	/// keys that say where a controller goes, such as PassThrough or InstanceGUID, are left to the section's own.
	/// </summary>
	void ReadRowKeys(const std::string& row, IniFile* ini, const std::string& section);

	/// <summary>Fills a mapping from a row for a device; returns how many controls the row filled, nought for a row that cannot be read.</summary>
	int Convert(const std::string& row, const Device& device, Config::Mapping* mapping);

	/// <summary>A mapping as x360ce 4 writes its boxes, "ButtonA=1 LeftThumbAxisY=a-2 ...", in name order; for the log and the shared tests.</summary>
	std::string Describe(const Config::Mapping& mapping);
}
