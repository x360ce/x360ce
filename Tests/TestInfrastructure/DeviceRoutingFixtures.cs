using System;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The current game and a mapped row for it, shared by the device-routing test classes.</summary>
	internal static class DeviceRoutingFixtures
	{
		/// <summary>The current game, with all four tabs switched on, so every tab can drive force.</summary>
		internal static readonly UserGame Game = new UserGame
		{
			FileName = "routed.exe",
			FileProductName = "Routed",
			EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2 | MapToMask.Controller3 | MapToMask.Controller4),
		};

		internal static UserSetting Row(Guid device, MapTo controller, PadSetting ps = null)
		{
			return new UserSetting
			{
				InstanceGuid = device,
				FileName = Game.FileName,
				MapTo = (int)controller,
				IsEnabled = true,
				PadSettingChecksum = ps == null ? Guid.Empty : ps.PadSettingChecksum,
			};
		}

		/// <summary>Stored settings with the Force feedback enabled switch on or off.</summary>
		internal static PadSetting Force(bool on)
		{
			return new PadSetting { ForceEnable = on ? "1" : "0", PadSettingChecksum = Guid.NewGuid() };
		}

		/// <summary>Stored settings with Pass through on: to a place from 1 to 4, or with 0 to the place the device itself holds.</summary>
		internal static PadSetting PassThrough(int place)
		{
			return new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = place.ToString(), PadSettingChecksum = Guid.NewGuid() };
		}
	}
}
