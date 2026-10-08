using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>One report a controller sent through Raw Input, and the state DirectInput read for its twin right after it.</summary>
	internal sealed class RawInputSample
	{
		/// <summary>Why the sample was chosen: the control it shows and where.</summary>
		public string Shows;
		/// <summary>The report as Raw Input delivered it, report ID first, in hexadecimal.</summary>
		public string Report;
		/// <summary>X, Y, Z, RotationX, RotationY and RotationZ: the first six axes of the state.</summary>
		public int[] Axes;
		/// <summary>The first two sliders of the state.</summary>
		public int[] Sliders;
		/// <summary>The four hat switches of the state.</summary>
		public int[] Povs;
		/// <summary>The indexes of the pressed buttons.</summary>
		public int[] Buttons;

		/// <summary>The report as bytes.</summary>
		public byte[] Bytes()
		{
			var bytes = new byte[Report.Length / 2];
			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = Convert.ToByte(Report.Substring(i * 2, 2), 16);
			return bytes;
		}
	}

	/// <summary>A controller recorded through Raw Input and DirectInput at once: what the HID parser knows of it, its DirectInput twin's controls, and chosen reports.</summary>
	internal sealed class RawInputFixture
	{
		public string Name;
		public int VendorId;
		public int ProductId;
		/// <summary>The device's preparsed data (RIDI_PREPARSEDDATA), in Base64.</summary>
		public string Preparsed;
		/// <summary>The device's row of <see cref="RawInputDevice.Descriptions"/>, for one whose own description names no control; null otherwise.</summary>
		public RawInputDescription Description;
		/// <summary>The twin's controls as the engine keeps them once it has read the device: axes and sliders carry the slot DirectInput reports, buttons their order, and hats the instance number less one, which is not a slot.</summary>
		public DeviceObjectItem[] Objects;
		public RawInputSample[] Samples;

		public byte[] PreparsedBytes()
		{
			return Convert.FromBase64String(Preparsed);
		}

		/// <summary>The recorded controller as Raw Input lists it, with the identity it would have on a machine.</summary>
		/// <param name="handle">Its Raw Input handle, which the hub keys it by. A recorded report's hDevice is 0.</param>
		/// <param name="path">Its interface path, which its instance GUID is made from.</param>
		/// <param name="productName">The name it reports, or null for none.</param>
		/// <param name="usage">Its top-level collection: 0x04 joystick, 0x05 gamepad, or its vendor's usage for a device with a <see cref="Description"/>.</param>
		public RawInputDevice Device(long handle, string path, string productName, int usage)
		{
			var device = RawInputDevice.FromPreparsedData(PreparsedBytes(), Description);
			Assert.IsNotNull(device, Name + ": the HID parser refused the recorded preparsed data.");
			Set(device, "Handle", new IntPtr(handle));
			Set(device, "InterfacePath", path);
			Set(device, "VendorId", VendorId);
			Set(device, "ProductId", ProductId);
			Set(device, "Version", 0x0100);
			Set(device, "UsagePage", Description == null ? 1 : Description.UsagePage);
			Set(device, "Usage", usage);
			Set(device, "ProductName", productName);
			Set(device, "InstanceGuid", RawInputDevice.GetInstanceGuid(path));
			Set(device, "ProductGuid", RawInputDevice.GetProductGuid(VendorId, ProductId));
			return device;
		}

		static void Set(object target, string property, object value)
		{
			target.GetType().GetProperty(property).SetValue(target, value);
		}
	}

	/// <summary>Controllers recorded on one machine through Raw Input, with the state DirectInput read for each one's twin; the G13 has none.</summary>
	/// <remarks>
	/// Chosen from a recording of the three read at once through Raw Input and DirectInput: each one at rest, each
	/// axis at both ends, at its centre and on either side of it, and each hat direction and button the recording
	/// caught. The G27's recording ends before its hat and most of its buttons moved, so those show only at rest.
	/// Only reports whose whole DirectInput state is the one the report describes are kept, since DirectInput can lag
	/// a report behind on a fast movement. The Xbox One controller's DirectInput state is not read while the program
	/// is in the background, so its samples carry the state the HID parser's values give, and no DirectInput reading.
	/// </remarks>
	internal static class RawInputFixtures
	{
		static DeviceObjectItem O(Guid type, int diIndex, int usagePage, int usage, string name)
		{
			return new DeviceObjectItem { Type = type, DiIndex = diIndex, UsagePage = usagePage, Usage = usage, Name = name };
		}

		static RawInputSample S(string shows, string report, int[] axes, int[] sliders, int[] povs, params int[] buttons)
		{
			return new RawInputSample { Shows = shows, Report = report, Axes = axes, Sliders = sliders, Povs = povs, Buttons = buttons };
		}

		/// <summary>Logitech Cordless RumblePad 2, 046D:C219, top-level usage 1:05.</summary>
		public static readonly RawInputFixture RumblePad2 = new RawInputFixture
		{
			Name = "Logitech Cordless RumblePad 2",
			VendorId = 0x046D,
			ProductId = 0xC219,
			Preparsed =
				"SGlkUCBLRFIFAAEAAAAAAAAACAAIAAgACAABAAkACAAJAAAACQAAAKgDBAABAAEACAABAAQACAACAAAABQABAAEAAAAIAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1ADUAAAAAAAAAAAAAAAAAAAAAAAAAAAD/AAAAAAAAAP8AAAAAAAAAAAAAAAEA" +
				"AQAIAAEAAwAIAAIAAAAEAAEAAQAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADIAMgAAAAAAAAAAAAEAAQAA" +
				"AAAAAAAAAP8AAAAAAAAA/wAAAAAAAAAAAAAAAQABAAgAAQACAAgAAgAAAAMAAQABAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAMQAxAAAAAAAAAAAAAgACAAAAAAAAAAAA/wAAAAAAAAD/AAAAAAAAAAAAAAABAAEACAABAAEACAACAAAAAgAB" +
				"AAEAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwADAAAAAAAAAAAAADAAMAAAAAAAAAAAD/AAAAAAAAAP8A" +
				"AAAAAAAAAAAAAAEAAQAEAAEABQAEAEIAAAAGAAEAAQAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADkAOQAA" +
				"AAAAAAAAAAQABAABAAAAAAAAAAcAAAAAAAAAOwEAABQAAAAAAAAACQABBAEADAAFAAwAAgAAAAcAAQABAAAAHAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAMAAAAAAAAAAAABQAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wEACAAB" +
				"AAcACAACAAAACAABAAEAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAARABEAAAAAAAAA" +
				"AAD/AAAAAAAAAP8AAAAAAAAAAAAAAAD/AgAIAAcAAQA4AAIAAAAIAAIAAP8AAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAMAAwAAAAAAAAAAABIAEgAAAAAAAAAAAP8AAAAAAAAA/wAAAAAAAAAAAAAAAP8DAAgABwABADgAAgAAAAgAAwAA/wAA" +
				"CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAAAAAAD/AAAAAAAA" +
				"AAAAAAAFAAEAAAADAAAAAwABAAAAAAABAAAAAAAAAAAAAgAAAAAAAP8AAAAAAQAAAAIAAAAAAAD/AAAAAAIAAAACAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
			Objects = new[]
			{
				O(ObjectGuid.XAxis, 0, 0x1, 0x30, "X Axis"),
				O(ObjectGuid.PovController, -1, 0x1, 0x39, "Hat Switch"),
				O(ObjectGuid.Button, 0, 0x9, 0x01, "Button 0"),
				O(ObjectGuid.YAxis, 1, 0x1, 0x31, "Y Axis"),
				O(ObjectGuid.Button, 1, 0x9, 0x02, "Button 1"),
				O(ObjectGuid.ZAxis, 2, 0x1, 0x32, "Z Axis"),
				O(ObjectGuid.Button, 2, 0x9, 0x03, "Button 2"),
				O(ObjectGuid.Button, 3, 0x9, 0x04, "Button 3"),
				O(ObjectGuid.Button, 4, 0x9, 0x05, "Button 4"),
				O(ObjectGuid.RzAxis, 5, 0x1, 0x35, "Z Rotation"),
				O(ObjectGuid.Button, 5, 0x9, 0x06, "Button 5"),
				O(ObjectGuid.Button, 6, 0x9, 0x07, "Button 6"),
				O(ObjectGuid.Button, 7, 0x9, 0x08, "Button 7"),
				O(ObjectGuid.Button, 8, 0x9, 0x09, "Button 8"),
				O(ObjectGuid.Button, 9, 0x9, 0x0A, "Button 9"),
				O(ObjectGuid.Button, 10, 0x9, 0x0B, "Button 10"),
				O(ObjectGuid.Button, 11, 0x9, 0x0C, "Button 11"),
			},
			Samples = new[]
			{
				S("at rest", "017F7F807F080074", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 = 0", "01CEF48000080074", new[] { 52792, 62674, 32767, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 = 255", "019F007CFF080074", new[] { 40570, 0, 31743, 0, 0, 65535 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 = 128", "010157FF80080074", new[] { 256, 22271, 65535, 0, 0, 32767 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 = 127", "017F7F807F080174", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 4),
				S("Z Rotation 1:35 = 129", "017F818281080074", new[] { 32511, 32768, 33028, 0, 0, 32768 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 = 156", "014C8B739C08C074", new[] { 19455, 35369, 29439, 0, 0, 39790 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("Z Rotation 1:35 ~ 63", "01FF6C003F080074", new[] { 65535, 27647, 0, 0, 0, 16128 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Rotation 1:35 ~ 190", "015BFF07BE080074", new[] { 23295, 65535, 1792, 0, 0, 48631 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 = 0", "016FFF00AE080074", new[] { 28415, 65535, 0, 0, 0, 44471 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 = 255", "013312FF3C080074", new[] { 13056, 4608, 65535, 0, 0, 15360 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 = 127", "017F7F7F7F180074", new[] { 32511, 32511, 32511, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 0),
				S("Z Axis 1:32 = 129", "01D8E88100080074", new[] { 55393, 59554, 32768, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 = 64", "01EFD540F4080074", new[] { 61374, 54613, 16384, 0, 0, 62674 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 ~ 63", "01074F3FD908C074", new[] { 1792, 20223, 16128, 0, 0, 55653 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("Z Axis 1:32 ~ 191", "0142FEBFFA080074", new[] { 16895, 65275, 48891, 0, 0, 64235 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 = 255", "0139FF21E3080074", new[] { 14592, 65535, 8448, 0, 0, 58253 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 = 128", "018080807F080074", new[] { 32767, 32767, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 = 64", "017F407E9908C074", new[] { 32511, 16384, 32255, 0, 0, 39009 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("Y Axis 1:31 = 156", "01809C807F088074", new[] { 32767, 39790, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 11),
				S("Y Axis 1:31 ~ 63", "017F3F7E9808C074", new[] { 32511, 16128, 32255, 0, 0, 38749 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("Y Axis 1:31 ~ 191", "01A9BF807F080074", new[] { 43170, 48891, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 = 0", "01005BB3FE088074", new[] { 0, 23295, 45771, 0, 0, 65275 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 11),
				S("X Axis 1:30 = 255", "01FFB7007E080074", new[] { 65535, 46811, 0, 0, 0, 32255 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 = 128", "01807F808F08C074", new[] { 32767, 32511, 32767, 0, 0, 36409 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("X Axis 1:30 = 129", "0181701921080C74", new[] { 32768, 28671, 6400, 0, 0, 8448 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 6, 7),
				S("X Axis 1:30 ~ 63", "013FFF17D9080074", new[] { 16128, 65535, 5888, 0, 0, 55653 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 ~ 191", "01BFF9007F080074", new[] { 48891, 63975, 0, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Hat Switch 1:39 = 0", "01807F807F000174", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 0, -1, -1, -1 }, 4),
				S("Hat Switch 1:39 = 2", "01807F807F020074", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 9000, -1, -1, -1 }),
				S("Hat Switch 1:39 = 3", "01807F807F030074", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 13500, -1, -1, -1 }),
				S("Hat Switch 1:39 = 4", "01807F807F040074", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 18000, -1, -1, -1 }),
				S("Hat Switch 1:39 = 5", "01807F807FC50374", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 22500, -1, -1, -1 }, 2, 3, 4, 5),
				S("Hat Switch 1:39 = 6", "01807F807F060374", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 27000, -1, -1, -1 }, 4, 5),
				S("Hat Switch 1:39 = 7", "01807F807F070174", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { 31500, -1, -1, -1 }, 4),
				S("Button 0 9:01 pressed", "017F7F807F180074", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 0),
				S("Button 1 9:02 pressed", "017F7F7E7F280074", new[] { 32511, 32511, 32255, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 1),
				S("Button 2 9:03 pressed", "017F7F7E7F480074", new[] { 32511, 32511, 32255, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 2),
				S("Button 3 9:04 pressed", "017F7F7E7F880074", new[] { 32511, 32511, 32255, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 3),
				S("Button 5 9:06 pressed", "01807F807F080374", new[] { 32767, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 4, 5),
				S("Button 6 9:07 pressed", "017F7F807F080C74", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 6, 7),
				S("Button 8 9:09 pressed", "017F7F807F083074", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 8, 9),
				S("Button 9 9:0A pressed", "017F7F807F082074", new[] { 32511, 32511, 32767, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 9),
				S("Button 10 9:0B pressed", "017F367E9908C074", new[] { 32511, 13824, 32255, 0, 0, 39009 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10, 11),
				S("Button 11 9:0C pressed", "017F7F7E7F088074", new[] { 32511, 32511, 32255, 0, 0, 32511 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 11),
			},
		};

		/// <summary>Controller (Xbox One For Windows), 045E:02FF, top-level usage 1:05.</summary>
		public static readonly RawInputFixture XboxOne = new RawInputFixture
		{
			Name = "Controller (Xbox One For Windows)",
			VendorId = 0x045E,
			ProductId = 0x02FF,
			Preparsed =
				"SGlkUCBLRFIFAAEAAAAAAAAABwAHABAABwAAAAcAAAAHAAAABwAAANgCBAABAAAAEAABAAMAEAACAAAABQABAAEAAAAIAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAxADEAAAAAAAAAAAAAAAAAAAAAAAAAAAD/////AAAAAP////8AAAAAAAAAAAEA" +
				"AAAQAAEAAQAQAAIAAAADAAEAAQAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAAMAAAAAAAAAAAAAEAAQAA" +
				"AAAAAAAAAP////8AAAAA/////wAAAAAAAAAAAQAAABAAAQAHABAAAgAAAAkAAgABAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAANAA0AAAAAAAAAAAAAgACAAAAAAAAAAAA/////wAAAAD/////AAAAAAAAAAABAAAAEAABAAUAEAACAAAABwAC" +
				"AAEAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAzADMAAAAAAAAAAAADAAMAAAAAAAAAAAD/////AAAAAP//" +
				"//8AAAAAAAAAAAEAAAAQAAEACQAQAAIAAAALAAMAAQAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADIAMgAA" +
				"AAAAAAAAAAQABAAAAAAAAAAAAP////8AAAAA/////wAAAAAAAAAACQAAAAEAEAALABAAAgAAAA0AAAABAAUAHAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAQAAAAAAAAAAAABQAUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAABAAB" +
				"AA0ABABCAAAADgAAAAEABQAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA5ADkAAAAAAAAAAAAVABUAAQAAAAEA" +
				"AAAIAAAAAAAAADsQAAAOAAAAAAAAAAUAAQAAAAMAAAADAAEAAAAAAAEAAAAAAAAAAAAAAAAAAAABAAAAAAABAAAAAAAAAAAAAQAA" +
				"AAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==",
			Objects = new[]
			{
				O(ObjectGuid.XAxis, 0, 0x1, 0x30, "X Axis"),
				O(ObjectGuid.Button, 0, 0x9, 0x01, "Button 0"),
				O(ObjectGuid.PovController, -1, 0x1, 0x39, "Hat Switch"),
				O(ObjectGuid.YAxis, 1, 0x1, 0x31, "Y Axis"),
				O(ObjectGuid.Button, 1, 0x9, 0x02, "Button 1"),
				O(ObjectGuid.ZAxis, 2, 0x1, 0x32, "Z Axis"),
				O(ObjectGuid.Button, 2, 0x9, 0x03, "Button 2"),
				O(ObjectGuid.RxAxis, 3, 0x1, 0x33, "X Rotation"),
				O(ObjectGuid.Button, 3, 0x9, 0x04, "Button 3"),
				O(ObjectGuid.RyAxis, 4, 0x1, 0x34, "Y Rotation"),
				O(ObjectGuid.Button, 4, 0x9, 0x05, "Button 4"),
				O(ObjectGuid.Button, 5, 0x9, 0x06, "Button 5"),
				O(ObjectGuid.Button, 6, 0x9, 0x07, "Button 6"),
				O(ObjectGuid.Button, 7, 0x9, 0x08, "Button 7"),
				O(ObjectGuid.Button, 8, 0x9, 0x09, "Button 8"),
				O(ObjectGuid.Button, 9, 0x9, 0x0A, "Button 9"),
				O(ObjectGuid.Button, 10, 0x9, 0x0B, "Button 10"),
				O(ObjectGuid.Button, 11, 0x9, 0x0C, "Button 11"),
				O(ObjectGuid.Button, 12, 0x9, 0x0D, "Button 12"),
				O(ObjectGuid.Button, 13, 0x9, 0x0E, "Button 13"),
				O(ObjectGuid.Button, 14, 0x9, 0x0F, "Button 14"),
				O(ObjectGuid.Button, 15, 0x9, 0x10, "Button 15"),
			},
			Samples = new[]
			{
				S("at rest", "005F85E681D182388500800000000000", new[] { 34142, 33253, 32767, 33488, 34103, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 = 0", "0027890000A0AA000000800000000000", new[] { 35110, 0, 32767, 43679, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 = 65535", "00EE59FFFF2460E1F200800000000000", new[] { 23021, 65535, 32767, 24611, 62177, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 ~ 16228", "0055FC643F9955000080900000000000", new[] { 64597, 16228, 36991, 21912, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Axis 1:31 ~ 49049", "00FDB999BF9C371C4A00970003000000", new[] { 47612, 49048, 38655, 14236, 18971, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 8, 9),
				S("X Axis 1:30 = 0", "000000A61EA6FCE7AD00800000000000", new[] { 0, 7846, 32767, 64678, 44518, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 = 65535", "00FFFFCFAD0000E08500800000000000", new[] { 65535, 44494, 32767, 0, 34271, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 = 32769", "0001800D7EBC84DC8400800000000000", new[] { 32768, 32268, 32767, 33979, 34011, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 ~ 16391", "000740FFFF6FFCEAB800800000000000", new[] { 16390, 65535, 32767, 64623, 47337, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Axis 1:30 ~ 49041", "0091BF779C9C37C124809A0003000000", new[] { 49040, 40054, 39551, 14236, 9409, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 8, 9),
				S("Y Rotation 1:34 = 0", "0096FAC94390350000809E0000000000", new[] { 64150, 17352, 40575, 13712, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Rotation 1:34 = 65535", "00DC69FFFF30A4FFFF00800000000000", new[] { 27099, 65535, 32767, 42031, 65535, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Rotation 1:34 ~ 16487", "00DC8457815A07674000800000000000", new[] { 34011, 33110, 32767, 1882, 16486, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Y Rotation 1:34 ~ 49150", "00333D5A9A0D62FEBF00800000000000", new[] { 15667, 39513, 32767, 25100, 49149, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Rotation 1:33 = 0", "0023E941D7000017AB00800000000000", new[] { 59683, 55105, 32767, 0, 43798, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Rotation 1:33 = 65535", "001D25910CFFFFA52B00800000000000", new[] { 9501, 3217, 32767, 65535, 11173, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("X Rotation 1:33 = 32767", "00FFFF87A1FF7F53B000800002000000", new[] { 65535, 41350, 32767, 32766, 45138, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 9),
				S("X Rotation 1:33 ~ 16400", "00896F549B1040F5D900800003000000", new[] { 28552, 39763, 32767, 16399, 55797, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 8, 9),
				S("X Rotation 1:33 ~ 49160", "00DC84578108C0F2F500800000000000", new[] { 34011, 33110, 32767, 49160, 62962, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Z Axis 1:32 ~ 16256", "003A85207FA357D682803F0400050000", new[] { 34105, 32543, 16256, 22434, 33493, 0 }, new[] { 0, 0 }, new[] { 18000, -1, -1, -1 }, 2),
				S("Z Axis 1:32 ~ 49024", "00098506803484D68080BF4000000000", new[] { 34056, 32773, 49023, 33843, 32981, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 6),
				S("Button 0 9:01 pressed", "000985F17F3484958000800500000000", new[] { 34056, 32752, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 0, 2),
				S("Button 1 9:02 pressed", "00098506803484D68000800200000000", new[] { 34056, 32773, 32767, 33843, 32981, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 1),
				S("Button 2 9:03 pressed", "000985F17F3484958000800400000000", new[] { 34056, 32752, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 2),
				S("Button 3 9:04 pressed", "00098506803484D68000800800000000", new[] { 34056, 32773, 32767, 33843, 32981, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 3),
				S("Button 4 9:05 pressed", "006B85A57DEE83EB7E00803000000000", new[] { 34154, 32164, 32767, 33773, 32490, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 4, 5),
				S("Button 6 9:07 pressed", "00098506803484D68080FF4000000000", new[] { 34056, 32773, 65408, 33843, 32981, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 6),
				S("Button 7 9:08 pressed", "00098506803484D6800080C000000000", new[] { 34056, 32773, 32767, 33843, 32981, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 6, 7),
				S("Button 8 9:09 pressed", "00E069F396DA486CCD00800003000000", new[] { 27103, 38642, 32767, 18649, 52588, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 8, 9),
				S("Button 9 9:0A pressed", "00C66A5B9E9976F18400800002000000", new[] { 27333, 40538, 32767, 30360, 34032, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 9),
				S("Button 10 9:0B pressed", "00CA851E7DEE83EB7E00800004000000", new[] { 34249, 32029, 32767, 33773, 32490, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 10),
				S("Hat Switch 1:39 = 1", "00098582813484958000800000010000", new[] { 34056, 33153, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 0, -1, -1, -1 }),
				S("Hat Switch 1:39 = 2", "00098539817884958000800000020000", new[] { 34056, 33080, 32767, 33911, 32916, 0 }, new[] { 0, 0 }, new[] { 4500, -1, -1, -1 }),
				S("Hat Switch 1:39 = 3", "00098568803484958000800000030000", new[] { 34056, 32871, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 9000, -1, -1, -1 }),
				S("Hat Switch 1:39 = 4", "000985D8803484958000800000040000", new[] { 34056, 32983, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 13500, -1, -1, -1 }),
				S("Hat Switch 1:39 = 5", "00098569813484958000800000050000", new[] { 34056, 33128, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 18000, -1, -1, -1 }),
				S("Hat Switch 1:39 = 6", "00098569813484958000800000060000", new[] { 34056, 33128, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 22500, -1, -1, -1 }),
				S("Hat Switch 1:39 = 7", "00098569813484958000800000070000", new[] { 34056, 33128, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 27000, -1, -1, -1 }),
				S("Hat Switch 1:39 = 8", "000985C5813484958000800000080000", new[] { 34056, 33220, 32767, 33843, 32916, 0 }, new[] { 0, 0 }, new[] { 31500, -1, -1, -1 }),
			},
		};

		/// <summary>Logitech G27 Racing Wheel USB, 046D:C29B, top-level usage 1:04.</summary>
		public static readonly RawInputFixture G27 = new RawInputFixture
		{
			Name = "Logitech G27 Racing Wheel USB",
			VendorId = 0x046D,
			ProductId = 0xC29B,
			Preparsed =
				"SGlkUCBLRFIEAAEAAAAAAAAACgAKAA0ACgABAAsACAALAAEADACRAOAEAQABAAAABAABAAEABABCAAAAAgAAAAEABAAIAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA5ADkAAAAAAAAAAAAAAAAAAQAAAAAAAAAHAAAAAAAAADsBAAAUAAAAAAAAAAkA" +
				"AAQBABYAAQAWAAIAAAAFAAAAAQAEABwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAFgAAAAAAAAAAAAEAFgAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAg4AAQAEAA4AAgAAAAYAAAABAAQACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAMAAwAAAAAAAAAAAAFwAXAAAAAAAAAAAA/z8AAAAAAAD/PwAAAAAAAAAAAAABAAAACAABAAgACAACAAAACQAA" +
				"AAEABAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAxADEAAAAAAAAAAAAYABgAAAAAAAAAAAD/AAAAAAAAAP8A" +
				"AAAAAAAAAAAAAAEAAAAIAAEABwAIAAIAAAAIAAAAAQAEAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADUANQAA" +
				"AAAAAAAAABkAGQAAAAAAAAAAAP8AAAAAAAAA/wAAAAAAAAAAAAAAAQAAAAgAAQAGAAgAAgAAAAcAAAABAAQACAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMgAyAAAAAAAAAAAAGgAaAAAAAAAAAAAA/wAAAAAAAAD/AAAAAAAAAAAAAAAA/wAACAAC" +
				"AAkAEAACAAAACwAAAAEABAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAEAAAAAAAAAAAAbABsAAAAAAAAA" +
				"AAD/AAAAAAAAAP8AAAAAAAAAAAAAAAkAAAABAAEACwABAAIAAAAMAAAAAQAEAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAABcAFwAAAAAAAAAAABwAHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAQEABwALAAcAAgAAAAwAAAABAAQA" +
				"DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQABAAAAAAAAAAAAHQAdAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAABAAAACAABAAwACAACAAAADQAAAAEABAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2ADYAAAAAAAAA" +
				"AAAeAB4AAAAAAAAAAAD/AAAAAAAAAP8AAAAAAAAAAAAAAAD/AAAIAAcAAQA4AAIAAAAIAAAAAQAEAAgAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAIAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAAAAAAA/wAAAAAAAAAAAAAAAP8AAAgAkAABAIAE" +
				"AgAAAJEAAAABAAQACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwADAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAA" +
				"AAAAAAD/AAAAAAAAAAAAAAAEAAEAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
			Objects = new[]
			{
				O(ObjectGuid.PovController, -1, 0x1, 0x39, "Hat Switch"),
				O(ObjectGuid.Button, 0, 0x9, 0x01, "Button 0"),
				O(ObjectGuid.XAxis, 0, 0x1, 0x30, "Wheel axis"),
				O(ObjectGuid.Button, 1, 0x9, 0x02, "Button 1"),
				O(ObjectGuid.Slider, 0, 0x1, 0x31, "Combined pedals"),
				O(ObjectGuid.Button, 2, 0x9, 0x03, "Button 2"),
				O(ObjectGuid.YAxis, 1, 0x1, 0x32, "Accelerator"),
				O(ObjectGuid.Button, 3, 0x9, 0x04, "Button 3"),
				O(ObjectGuid.Button, 4, 0x9, 0x05, "Button 4"),
				O(ObjectGuid.Button, 5, 0x9, 0x06, "Button 5"),
				O(ObjectGuid.RzAxis, 5, 0x1, 0x35, "Brake"),
				O(ObjectGuid.Button, 6, 0x9, 0x07, "Button 6"),
				O(ObjectGuid.Slider, 1, 0x1, 0x36, "Clutch"),
				O(ObjectGuid.Button, 7, 0x9, 0x08, "Button 7"),
				O(ObjectGuid.Button, 8, 0x9, 0x09, "Button 8"),
				O(ObjectGuid.Button, 9, 0x9, 0x0A, "Button 9"),
				O(ObjectGuid.Button, 10, 0x9, 0x0B, "Button 10"),
				O(ObjectGuid.Button, 11, 0x9, 0x0C, "Button 11"),
				O(ObjectGuid.Button, 12, 0x9, 0x0D, "Button 12"),
				O(ObjectGuid.Button, 13, 0x9, 0x0E, "Button 13"),
				O(ObjectGuid.Button, 14, 0x9, 0x0F, "Button 14"),
				O(ObjectGuid.Button, 15, 0x9, 0x10, "Button 15"),
				O(ObjectGuid.Button, 16, 0x9, 0x11, "Button 16"),
				O(ObjectGuid.Button, 17, 0x9, 0x12, "Button 17"),
				O(ObjectGuid.Button, 18, 0x9, 0x13, "Button 18"),
				O(ObjectGuid.Button, 19, 0x9, 0x14, "Button 19"),
				O(ObjectGuid.Button, 20, 0x9, 0x15, "Button 20"),
				O(ObjectGuid.Button, 21, 0x9, 0x16, "Button 21"),
				O(ObjectGuid.Button, 22, 0x9, 0x17, "Button 22"),
			},
			Samples = new[]
			{
				S("at rest", "00080000307EFFFF807D689CFF", new[] { 32303, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Button 6 9:07 pressed", "00080400747EFFFF807D679CFF", new[] { 32371, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 6),
				S("Button 7 9:08 pressed", "00080800907EFFFF807D679CFF", new[] { 32399, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 7),
				S("Button 19 9:14 pressed", "00080080847EFFFF807D689CFF", new[] { 32387, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 19),
				S("Button 20 9:15 pressed", "000800009D7EFFFF807D689CFF", new[] { 32411, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 20),
				S("Button 21 9:16 pressed", "00080000767EFFFF807D689CFF", new[] { 32371, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 21),
				S("Wheel axis 1:30 = 0", "000800000000FFFF807D689CFF", new[] { 0, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Wheel axis 1:30 = 16383", "00080000FCFFFFFF807D689CFF", new[] { 65535, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Wheel axis 1:30 = 8193", "000800000480FFFF807D679CFF", new[] { 32768, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Wheel axis 1:30 ~ 4095", "00080000FC3FFFFF807D679CFF", new[] { 16380, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Wheel axis 1:30 ~ 12291", "000800000CC0FFFF807D689CFF", new[] { 49164, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }),
				S("Button 22 9:17 pressed", "00080000D47EFFFF807D689DFF", new[] { 32467, 65535, 0, 0, 0, 65535 }, new[] { 32767, 65535 }, new[] { -1, -1, -1, -1 }, 22),
			},
		};
		/// <summary>Logitech G13, 046D:C21C, top-level collection on the vendor page 0xFF00: its description names no control, so it is read from its row of <see cref="RawInputDevice.Descriptions"/>.</summary>
		/// <remarks>
		/// DirectInput lists it only as a device of no kind and reads nothing from it, so its samples carry the state its
		/// row gives, not a DirectInput reading. The reports are laid out as a recording of it showed: report 1, the
		/// stick's X and Y from 0 to 255, then 40 key bits, one key at a time.
		/// </remarks>
		public static readonly RawInputFixture G13 = new RawInputFixture
		{
			Name = "Logitech G13",
			VendorId = 0x046D,
			ProductId = 0xC21C,
			Description = RawInputDevice.DescriptionOf(0x046D, 0xC21C, 0xFF00),
			Preparsed =
				"SGlkUCBLRFIAAAD/AAAAAAAAAQABAAgAAQABAAIA4AMCAAQABgACAXACAQAA/wEACAAHAAEAOAACAAAACAAAAAD/AAAIAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAAAAAAAAD/" +
				"AwAIAN8DAQD4HgIAAADgAwAAAP8AAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAgAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAP8HAAgABAABACAAAgAAAAUAAAAA/wAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAwADAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAAAAAAAA/wQACAAEAAEAIAACAAAABQAA" +
				"AAD/AAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAQAAAAAAAAAAAABAAEAAAAAAAAAAAD/AAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAD/BQAIAAQAAQAgAAIAAAAFAAAAAP8AAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAUABQAA" +
				"AAAAAAAAAAIAAgAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAP8GAAgAAQEBAAgIAgAAAAIBAAAA/wAACAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABgAGAAAAAAAAAAAAAwADAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAD/AAAA" +
				"AAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
				"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
			Samples = new[]
			{
				S("at rest", "0180800000000000", new[] { 32767, 32767, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Stick left and up", "0100000000000000", new[] { 0, 0, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Stick right and down", "01FFFF0000000000", new[] { 65535, 65535, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("Stick a quarter of the way", "0140800000000000", new[] { 16384, 32767, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }),
				S("G1 pressed", "0180800100000000", new[] { 32767, 32767, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 0),
				S("The two buttons beside the stick pressed", "0180800000000006", new[] { 32767, 32767, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 33, 34),
				S("The last key bit on", "0180800000000080", new[] { 32767, 32767, 0, 0, 0, 0 }, new[] { 0, 0 }, new[] { -1, -1, -1, -1 }, 39),
			},
		};
	}
}
