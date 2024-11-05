using SharpDX.DirectInput;
using System;
using System.Collections.Generic;

namespace x360ce.Engine
{
	public static class CustomDiHelper
	{

		/// <summary>
		/// Must have same order as in axis.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// MouseUpdate.Offset here points to a field in the RawMouseState structure.
		/// </summary>
		public static List<MouseOffset>MouseAxisOffsets = new List<MouseOffset>()
		{
			MouseOffset.X,
			MouseOffset.Y,
			MouseOffset.Z,
		};

		/// <summary>
		/// Must have same order as in axis.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// JoystickUpdate.Offset here points to a field in the RawJoystickState structure.
		/// </summary>
		public static List<JoystickOffset> AxisOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.X,
			JoystickOffset.Y,
			JoystickOffset.Z,
			JoystickOffset.RotationX,
			JoystickOffset.RotationY,
			JoystickOffset.RotationZ,
			JoystickOffset.AccelerationX, 
			JoystickOffset.AccelerationY,
			JoystickOffset.AccelerationZ,
			JoystickOffset.AngularAccelerationX,
			JoystickOffset.AngularAccelerationY,
			JoystickOffset.AngularAccelerationZ,
			JoystickOffset.ForceX,
			JoystickOffset.ForceY,
			JoystickOffset.ForceZ,
			JoystickOffset.TorqueX,
			JoystickOffset.TorqueY,
			JoystickOffset.TorqueZ,
			JoystickOffset.VelocityX,
			JoystickOffset.VelocityY,
			JoystickOffset.VelocityZ,
			JoystickOffset.AngularVelocityX,
			JoystickOffset.AngularVelocityY,
			JoystickOffset.AngularVelocityZ,
		};

		/// <summary>
		/// Must have same order as in Sliders[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> SliderOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.Sliders0,
			JoystickOffset.Sliders1,
			JoystickOffset.AccelerationSliders0,
			JoystickOffset.AccelerationSliders1,
			JoystickOffset.ForceSliders0,
			JoystickOffset.ForceSliders1,
			JoystickOffset.VelocitySliders0,
			JoystickOffset.VelocitySliders1,
		};

		/// <summary>
		/// Must have same order as in POVs[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> POVOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.PointOfViewControllers0,
			JoystickOffset.PointOfViewControllers1,
			JoystickOffset.PointOfViewControllers2,
			JoystickOffset.PointOfViewControllers3,
		};

		/// <summary>
		/// Must have same order as in POVs[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> ButtonOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.Buttons0,
			JoystickOffset.Buttons1,
			JoystickOffset.Buttons2,
			JoystickOffset.Buttons3,
			JoystickOffset.Buttons4,
			JoystickOffset.Buttons5,
			JoystickOffset.Buttons6,
			JoystickOffset.Buttons7,
			JoystickOffset.Buttons8,
			JoystickOffset.Buttons9,
			JoystickOffset.Buttons10,
			JoystickOffset.Buttons11,
			JoystickOffset.Buttons12,
			JoystickOffset.Buttons13,
			JoystickOffset.Buttons14,
			JoystickOffset.Buttons15,
			JoystickOffset.Buttons16,
			JoystickOffset.Buttons17,
			JoystickOffset.Buttons18,
			JoystickOffset.Buttons19,
			JoystickOffset.Buttons20,
			JoystickOffset.Buttons21,
			JoystickOffset.Buttons22,
			JoystickOffset.Buttons23,
			JoystickOffset.Buttons24,
			JoystickOffset.Buttons25,
			JoystickOffset.Buttons26,
			JoystickOffset.Buttons27,
			JoystickOffset.Buttons28,
			JoystickOffset.Buttons29,
			JoystickOffset.Buttons30,
			JoystickOffset.Buttons31,
			JoystickOffset.Buttons32,
			JoystickOffset.Buttons33,
			JoystickOffset.Buttons34,
			JoystickOffset.Buttons35,
			JoystickOffset.Buttons36,
			JoystickOffset.Buttons37,
			JoystickOffset.Buttons38,
			JoystickOffset.Buttons39,
			JoystickOffset.Buttons40,
			JoystickOffset.Buttons41,
			JoystickOffset.Buttons42,
			JoystickOffset.Buttons43,
			JoystickOffset.Buttons44,
			JoystickOffset.Buttons45,
			JoystickOffset.Buttons46,
			JoystickOffset.Buttons47,
			JoystickOffset.Buttons48,
			JoystickOffset.Buttons49,
			JoystickOffset.Buttons50,
			JoystickOffset.Buttons51,
			JoystickOffset.Buttons52,
			JoystickOffset.Buttons53,
			JoystickOffset.Buttons54,
			JoystickOffset.Buttons55,
			JoystickOffset.Buttons56,
			JoystickOffset.Buttons57,
			JoystickOffset.Buttons58,
			JoystickOffset.Buttons59,
			JoystickOffset.Buttons60,
			JoystickOffset.Buttons61,
			JoystickOffset.Buttons62,
			JoystickOffset.Buttons63,
			JoystickOffset.Buttons64,
			JoystickOffset.Buttons65,
			JoystickOffset.Buttons66,
			JoystickOffset.Buttons67,
			JoystickOffset.Buttons68,
			JoystickOffset.Buttons69,
			JoystickOffset.Buttons70,
			JoystickOffset.Buttons71,
			JoystickOffset.Buttons72,
			JoystickOffset.Buttons73,
			JoystickOffset.Buttons74,
			JoystickOffset.Buttons75,
			JoystickOffset.Buttons76,
			JoystickOffset.Buttons77,
			JoystickOffset.Buttons78,
			JoystickOffset.Buttons79,
			JoystickOffset.Buttons80,
			JoystickOffset.Buttons81,
			JoystickOffset.Buttons82,
			JoystickOffset.Buttons83,
			JoystickOffset.Buttons84,
			JoystickOffset.Buttons85,
			JoystickOffset.Buttons86,
			JoystickOffset.Buttons87,
			JoystickOffset.Buttons88,
			JoystickOffset.Buttons89,
			JoystickOffset.Buttons90,
			JoystickOffset.Buttons91,
			JoystickOffset.Buttons92,
			JoystickOffset.Buttons93,
			JoystickOffset.Buttons94,
			JoystickOffset.Buttons95,
			JoystickOffset.Buttons96,
			JoystickOffset.Buttons97,
			JoystickOffset.Buttons98,
			JoystickOffset.Buttons99,
			JoystickOffset.Buttons100,
			JoystickOffset.Buttons101,
			JoystickOffset.Buttons102,
			JoystickOffset.Buttons103,
			JoystickOffset.Buttons104,
			JoystickOffset.Buttons105,
			JoystickOffset.Buttons106,
			JoystickOffset.Buttons107,
			JoystickOffset.Buttons108,
			JoystickOffset.Buttons109,
			JoystickOffset.Buttons110,
			JoystickOffset.Buttons111,
			JoystickOffset.Buttons112,
			JoystickOffset.Buttons113,
			JoystickOffset.Buttons114,
			JoystickOffset.Buttons115,
			JoystickOffset.Buttons116,
			JoystickOffset.Buttons117,
			JoystickOffset.Buttons118,
			JoystickOffset.Buttons119,
			JoystickOffset.Buttons120,
			JoystickOffset.Buttons121,
			JoystickOffset.Buttons122,
			JoystickOffset.Buttons123,
			JoystickOffset.Buttons124,
			JoystickOffset.Buttons125,
			JoystickOffset.Buttons126,
			JoystickOffset.Buttons127,
		};

		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> ButtonUsageDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			// Buttons.
			{ 1, (0x01, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button0", JoystickOffset.Buttons0 /*48*/) },
			{ 2, (0x02, 1, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button1", JoystickOffset.Buttons1 /*49*/) },
			{ 3, (0x03, 2, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button2", JoystickOffset.Buttons2 /*50*/) },
			{ 4, (0x04, 3, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button3", JoystickOffset.Buttons3 /*51*/) },
			{ 5, (0x05, 4, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button4", JoystickOffset.Buttons4 /*52*/) },
			{ 6, (0x06, 5, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button5", JoystickOffset.Buttons5 /*53*/) },
			{ 7, (0x07, 6, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button6", JoystickOffset.Buttons6 /*54*/) },
			{ 8, (0x08, 7, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button7", JoystickOffset.Buttons7 /*55*/) },
			{ 9, (0x09, 8, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button8", JoystickOffset.Buttons8 /*56*/) },
			{ 10, (0x0A, 9, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button9", JoystickOffset.Buttons9 /*57*/) },
			{ 11, (0x0B, 10, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button10", JoystickOffset.Buttons10 /*58*/) },
			{ 12, (0x0C, 11, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button11", JoystickOffset.Buttons11 /*59*/) },
			{ 13, (0x0D, 12, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button12", JoystickOffset.Buttons12 /*60*/) },
			{ 14, (0x0E, 13, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button13", JoystickOffset.Buttons13 /*61*/) },
			{ 15, (0x0F, 14, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button14", JoystickOffset.Buttons14 /*62*/) },
			{ 16, (0x10, 15, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button15", JoystickOffset.Buttons15 /*63*/) },
			{ 17, (0x11, 16, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button16", JoystickOffset.Buttons16 /*64*/) },
			{ 18, (0x12, 17, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button17", JoystickOffset.Buttons17 /*65*/) },
			{ 19, (0x13, 18, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button18", JoystickOffset.Buttons18 /*66*/) },
			{ 20, (0x14, 19, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button19", JoystickOffset.Buttons19 /*67*/) },
			{ 21, (0x15, 20, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button20", JoystickOffset.Buttons20 /*68*/) },
			{ 22, (0x16, 21, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button21", JoystickOffset.Buttons21 /*69*/) },
			{ 23, (0x17, 22, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button22", JoystickOffset.Buttons22 /*70*/) },
			{ 24, (0x18, 23, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button23", JoystickOffset.Buttons23 /*71*/) },
			{ 25, (0x19, 24, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button24", JoystickOffset.Buttons24 /*72*/) },
			{ 26, (0x1A, 25, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button25", JoystickOffset.Buttons25 /*73*/) },
			{ 27, (0x1B, 26, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button26", JoystickOffset.Buttons26 /*74*/) },
			{ 28, (0x1C, 27, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button27", JoystickOffset.Buttons27 /*75*/) },
			{ 29, (0x1D, 28, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button28", JoystickOffset.Buttons28 /*76*/) },
			{ 30, (0x1E, 29, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button29", JoystickOffset.Buttons29 /*77*/) },
			{ 31, (0x1F, 30, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button30", JoystickOffset.Buttons30 /*78*/) },
			{ 32, (0x20, 31, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button31", JoystickOffset.Buttons31 /*79*/) },
			// Other buttons.
			{ 33, (0x20, 32, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button32", JoystickOffset.Buttons32 /*80*/) },
			{ 34, (0x20, 33, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button33", JoystickOffset.Buttons33 /*81*/) },
			{ 35, (0x20, 34, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button34", JoystickOffset.Buttons34 /*82*/) },
			{ 36, (0x20, 35, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button35", JoystickOffset.Buttons35 /*83*/) },
			{ 37, (0x20, 36, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button36", JoystickOffset.Buttons36 /*84*/) },
			{ 38, (0x20, 37, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button37", JoystickOffset.Buttons37 /*85*/) },
			{ 39, (0x20, 38, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button38", JoystickOffset.Buttons38 /*86*/) },
			{ 40, (0x20, 39, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button39", JoystickOffset.Buttons39 /*87*/) },
			{ 41, (0x20, 40, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button40", JoystickOffset.Buttons40 /*88*/) },
			{ 42, (0x20, 41, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button41", JoystickOffset.Buttons41 /*89*/) },
			{ 43, (0x20, 42, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button42", JoystickOffset.Buttons42 /*90*/) },
			{ 44, (0x20, 43, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button43", JoystickOffset.Buttons43 /*91*/) },
			{ 45, (0x20, 44, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button44", JoystickOffset.Buttons44 /*92*/) },
			{ 46, (0x20, 45, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button45", JoystickOffset.Buttons45 /*93*/) },
			{ 47, (0x20, 46, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button46", JoystickOffset.Buttons46 /*94*/) },
			{ 48, (0x20, 47, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button47", JoystickOffset.Buttons47 /*95*/) },
			{ 49, (0x20, 48, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button48", JoystickOffset.Buttons48 /*96*/) },
			{ 50, (0x20, 49, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button49", JoystickOffset.Buttons49 /*97*/) },
			{ 51, (0x20, 50, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button50", JoystickOffset.Buttons50 /*98*/) },
			{ 52, (0x20, 51, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button51", JoystickOffset.Buttons51 /*99*/) },
			{ 53, (0x20, 52, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button52", JoystickOffset.Buttons52 /*100*/) },
			{ 54, (0x20, 53, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button53", JoystickOffset.Buttons53 /*101*/) },
			{ 55, (0x20, 54, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button54", JoystickOffset.Buttons54 /*102*/) },
			{ 56, (0x20, 55, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button55", JoystickOffset.Buttons55 /*103*/) },
			{ 57, (0x20, 56, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button56", JoystickOffset.Buttons56 /*104*/) },
			{ 58, (0x20, 57, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button57", JoystickOffset.Buttons57 /*105*/) },
			{ 59, (0x20, 58, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button58", JoystickOffset.Buttons58 /*106*/) },
			{ 60, (0x20, 59, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button59", JoystickOffset.Buttons59 /*107*/) },
			{ 61, (0x20, 60, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button60", JoystickOffset.Buttons60 /*108*/) },
			{ 62, (0x20, 61, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button61", JoystickOffset.Buttons61 /*109*/) },
			{ 63, (0x20, 62, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button62", JoystickOffset.Buttons62 /*110*/) },
			{ 64, (0x20, 63, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button63", JoystickOffset.Buttons63 /*111*/) },
			{ 65, (0x20, 64, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button64", JoystickOffset.Buttons64 /*112*/) },
			{ 66, (0x20, 65, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button65", JoystickOffset.Buttons65 /*113*/) },
			{ 67, (0x20, 66, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button66", JoystickOffset.Buttons66 /*114*/) },
			{ 68, (0x20, 67, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button67", JoystickOffset.Buttons67 /*115*/) },
			{ 69, (0x20, 68, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button68", JoystickOffset.Buttons68 /*116*/) },
			{ 70, (0x20, 69, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button69", JoystickOffset.Buttons69 /*117*/) },
			{ 71, (0x20, 70, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button70", JoystickOffset.Buttons70 /*118*/) },
			{ 72, (0x20, 71, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button71", JoystickOffset.Buttons71 /*119*/) },
			{ 73, (0x20, 72, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button72", JoystickOffset.Buttons72 /*120*/) },
			{ 74, (0x20, 73, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button73", JoystickOffset.Buttons73 /*121*/) },
			{ 75, (0x20, 74, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button74", JoystickOffset.Buttons74 /*122*/) },
			{ 76, (0x20, 75, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button75", JoystickOffset.Buttons75 /*123*/) },
			{ 77, (0x20, 76, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button76", JoystickOffset.Buttons76 /*124*/) },
			{ 78, (0x20, 77, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button77", JoystickOffset.Buttons77 /*125*/) },
			{ 79, (0x20, 78, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button78", JoystickOffset.Buttons78 /*126*/) },
			{ 80, (0x20, 79, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button79", JoystickOffset.Buttons79 /*127*/) },
			{ 81, (0x20, 80, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button80", JoystickOffset.Buttons80 /*128*/) },
			{ 82, (0x20, 81, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button81", JoystickOffset.Buttons81 /*129*/) },
			{ 83, (0x20, 82, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button82", JoystickOffset.Buttons82 /*130*/) },
			{ 84, (0x20, 83, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button83", JoystickOffset.Buttons83 /*131*/) },
			{ 85, (0x20, 84, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button84", JoystickOffset.Buttons84 /*132*/) },
			{ 86, (0x20, 85, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button85", JoystickOffset.Buttons85 /*133*/) },
			{ 87, (0x20, 86, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button86", JoystickOffset.Buttons86 /*134*/) },
			{ 88, (0x20, 87, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button87", JoystickOffset.Buttons87 /*135*/) },
			{ 89, (0x20, 88, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button88", JoystickOffset.Buttons88 /*136*/) },
			{ 90, (0x20, 89, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button89", JoystickOffset.Buttons89 /*137*/) },
			{ 91, (0x20, 90, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button90", JoystickOffset.Buttons90 /*138*/) },
			{ 92, (0x20, 91, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button91", JoystickOffset.Buttons91 /*139*/) },
			{ 93, (0x20, 92, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button92", JoystickOffset.Buttons92 /*140*/) },
			{ 94, (0x20, 93, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button93", JoystickOffset.Buttons93 /*141*/) },
			{ 95, (0x20, 94, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button94", JoystickOffset.Buttons94 /*142*/) },
			{ 96, (0x20, 95, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button95", JoystickOffset.Buttons95 /*143*/) },
			{ 97, (0x20, 96, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button96", JoystickOffset.Buttons96 /*144*/) },
			{ 98, (0x20, 97, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button97", JoystickOffset.Buttons97 /*145*/) },
			{ 99, (0x20, 98, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button98", JoystickOffset.Buttons98 /*146*/) },
			{ 100, (0x20, 99, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button99", JoystickOffset.Buttons99 /*147*/) },
			{ 101, (0x20, 100, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button100", JoystickOffset.Buttons100 /*148*/) },
			{ 102, (0x20, 101, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button101", JoystickOffset.Buttons101 /*149*/) },
			{ 103, (0x20, 102, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button102", JoystickOffset.Buttons102 /*150*/) },
			{ 104, (0x20, 103, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button103", JoystickOffset.Buttons103 /*151*/) },
			{ 105, (0x20, 104, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button104", JoystickOffset.Buttons104 /*152*/) },
			{ 106, (0x20, 105, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button105", JoystickOffset.Buttons105 /*153*/) },
			{ 107, (0x20, 106, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button106", JoystickOffset.Buttons106 /*154*/) },
			{ 108, (0x20, 107, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button107", JoystickOffset.Buttons107 /*155*/) },
			{ 109, (0x20, 108, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button108", JoystickOffset.Buttons108 /*156*/) },
			{ 110, (0x20, 109, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button109", JoystickOffset.Buttons109 /*157*/) },
			{ 111, (0x20, 110, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button110", JoystickOffset.Buttons110 /*158*/) },
			{ 112, (0x20, 111, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button111", JoystickOffset.Buttons111 /*159*/) },
			{ 113, (0x20, 112, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button112", JoystickOffset.Buttons112 /*160*/) },
			{ 114, (0x20, 113, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button113", JoystickOffset.Buttons113 /*161*/) },
			{ 115, (0x20, 114, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button114", JoystickOffset.Buttons114 /*162*/) },
			{ 116, (0x20, 115, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button115", JoystickOffset.Buttons115 /*163*/) },
			{ 117, (0x20, 116, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button116", JoystickOffset.Buttons116 /*164*/) },
			{ 118, (0x20, 117, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button117", JoystickOffset.Buttons117 /*165*/) },
			{ 119, (0x20, 118, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button118", JoystickOffset.Buttons118 /*166*/) },
			{ 120, (0x20, 119, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button119", JoystickOffset.Buttons119 /*167*/) },
			{ 121, (0x20, 120, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button120", JoystickOffset.Buttons120 /*168*/) },
			{ 122, (0x20, 121, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button121", JoystickOffset.Buttons121 /*169*/) },
			{ 123, (0x20, 122, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button122", JoystickOffset.Buttons122 /*170*/) },
			{ 124, (0x20, 123, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button123", JoystickOffset.Buttons123 /*171*/) },
			{ 125, (0x20, 124, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button124", JoystickOffset.Buttons124 /*172*/) },
			{ 126, (0x20, 125, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button125", JoystickOffset.Buttons125 /*173*/) },
			{ 127, (0x20, 126, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button126", JoystickOffset.Buttons126 /*174*/) },
			{ 128, (0x20, 127, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button127", JoystickOffset.Buttons127 /*175*/) },
		};

		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> MouseAxisIndexDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			{ 0, (0x30, 0, new Guid("a36d02e0-c9f3-11cf-bfc7-444553540000"), "X", JoystickOffset.X /*0*/) },
			{ 1, (0x30, 0, new Guid("a36d02e1-c9f3-11cf-bfc7-444553540000"), "Y", JoystickOffset.Y /*4*/) },
			{ 2, (0x30, 0, new Guid("a36d02e2-c9f3-11cf-bfc7-444553540000"), "Z", JoystickOffset.Z /*8*/) },
		};

		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> MouseButtonIndexDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			// Buttons.
			{ 3, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button0", JoystickOffset.Buttons0 /*48*/) },
			{ 4, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button1", JoystickOffset.Buttons1 /*49*/) },
			{ 5, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button2", JoystickOffset.Buttons2 /*50*/) },
			{ 6, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button3", JoystickOffset.Buttons3 /*51*/) },
			{ 7, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button4", JoystickOffset.Buttons4 /*52*/) },
			{ 8, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button5", JoystickOffset.Buttons5 /*53*/) },
			{ 9, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button6", JoystickOffset.Buttons6 /*54*/) },
			{ 10, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button7", JoystickOffset.Buttons7 /*55*/) },
			{ 11, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button8", JoystickOffset.Buttons8 /*56*/) },
			{ 12, (0x30, 0, new Guid("a36d02f0-c9f3-11cf-bfc7-444553540000"), "Button9", JoystickOffset.Buttons9 /*57*/) },
		};

		//public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> MouseAxisUsageDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		//{
		//	// Axes.
		//	{ 48, (0x30, 0, new Guid("a36d02e0-c9f3-11cf-bfc7-444553540000"), "X", JoystickOffset.X /*0*/) },
		//	{ 49, (0x31, 1, new Guid("a36d02e1-c9f3-11cf-bfc7-444553540000"), "Y", JoystickOffset.Y /*4*/) },
		//	{ 50, (0x32, 2, new Guid("a36d02e2-c9f3-11cf-bfc7-444553540000"), "Z", JoystickOffset.Z /*8*/) },
		//};

		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> AxisUsageDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			// Axes.
			{ 48, (0x30, 0, new Guid("a36d02e0-c9f3-11cf-bfc7-444553540000"), "X", JoystickOffset.X /*0*/) },
			{ 49, (0x31, 1, new Guid("a36d02e1-c9f3-11cf-bfc7-444553540000"), "Y", JoystickOffset.Y /*4*/) },
			{ 50, (0x32, 2, new Guid("a36d02e2-c9f3-11cf-bfc7-444553540000"), "Z", JoystickOffset.Z /*8*/) },
			{ 51, (0x33, 3, new Guid("a36d02f4-c9f3-11cf-bfc7-444553540000"), "RotationX", JoystickOffset.RotationX /*12*/) },
			{ 52, (0x34, 4, new Guid("a36d02f5-c9f3-11cf-bfc7-444553540000"), "RotationY", JoystickOffset.RotationY /*16*/) },
			{ 53, (0x35, 5, new Guid("a36d02e3-c9f3-11cf-bfc7-444553540000"), "RotationZ", JoystickOffset.RotationZ /*20*/) },
			// Other
			{ 55, (0x37, 0, new Guid("a36d02e5-c9f3-11cf-bfc7-444553540000"), "Dial", (JoystickOffset)28) },
			{ 56, (0x38, 0, new Guid("a36d02e6-c9f3-11cf-bfc7-444553540000"), "Wheel", (JoystickOffset)32) },
			// Axes.
			{ 64, (0x40, 6, new Guid("a36d02e7-c9f3-11cf-bfc7-444553540000"), "VelocityX", JoystickOffset.VelocityX /*176*/) },
			{ 65, (0x41, 7, new Guid("a36d02e8-c9f3-11cf-bfc7-444553540000"), "VelocityY", JoystickOffset.VelocityY /*180*/) },
			{ 66, (0x42, 8, new Guid("a36d02e9-c9f3-11cf-bfc7-444553540000"), "VelocityZ", JoystickOffset.VelocityZ /*184*/) },
			{ 67, (0x43, 9, new Guid("a36d02ea-c9f3-11cf-bfc7-444553540000"), "AngularVelocityX", JoystickOffset.AngularVelocityX /*188*/) },
			{ 68, (0x44, 10, new Guid("a36d02eb-c9f3-11cf-bfc7-444553540000"), "AngularVelocityY", JoystickOffset.AngularVelocityY /*192*/) },
			{ 69, (0x45, 11, new Guid("a36d02ec-c9f3-11cf-bfc7-444553540000"), "AngularVelocityZ", JoystickOffset.AngularVelocityZ /*196*/) },
			{ 70, (0x46, 12, new Guid("a36d02ed-c9f3-11cf-bfc7-444553540000"), "ForceX", JoystickOffset.ForceX /*240*/) },
			{ 71, (0x47, 13, new Guid("a36d02ee-c9f3-11cf-bfc7-444553540000"), "ForceY", JoystickOffset.ForceY /*244*/) },
			{ 72, (0x48, 14, new Guid("a36d02ef-c9f3-11cf-bfc7-444553540000"), "ForceZ", JoystickOffset.ForceZ /*248*/) },
			{ 73, (0x49, 15, new Guid("a36d02f6-c9f3-11cf-bfc7-444553540000"), "ForceRotationX", (JoystickOffset)212) },
			{ 74, (0x4A, 16, new Guid("a36d02f7-c9f3-11cf-bfc7-444553540000"), "ForceRotationY", (JoystickOffset)216) },
			{ 75, (0x4B, 17, new Guid("a36d02f8-c9f3-11cf-bfc7-444553540000"), "ForceRotationZ", (JoystickOffset)220)},
			{ 80, (0x50, 18, new Guid("a36d02f9-c9f3-11cf-bfc7-444553540000"), "AccelerationX", JoystickOffset.AccelerationX /*208*/) },
			{ 81, (0x51, 19, new Guid("a36d02fa-c9f3-11cf-bfc7-444553540000"), "AccelerationY", JoystickOffset.AccelerationY /*212*/) },
			{ 82, (0x52, 20, new Guid("a36d02fb-c9f3-11cf-bfc7-444553540000"), "AccelerationZ", JoystickOffset.AccelerationZ /*216*/) },
			{ 83, (0x53, 21, new Guid("a36d02fc-c9f3-11cf-bfc7-444553540000"), "AngularAccelerationX", JoystickOffset.AngularAccelerationX /*220*/) },
			{ 84, (0x54, 22, new Guid("a36d02fd-c9f3-11cf-bfc7-444553540000"), "AngularAccelerationY", JoystickOffset.AngularAccelerationY /*224*/) },
			{ 85, (0x55, 23, new Guid("a36d02fe-c9f3-11cf-bfc7-444553540000"), "AngularAccelerationZ", JoystickOffset.AngularAccelerationZ /*228*/) },
			{ 86, (0x56, 24, new Guid("a36d02ff-c9f3-11cf-bfc7-444553540000"), "TorqueX", JoystickOffset.TorqueX /*252*/) },
			{ 87, (0x57, 25, new Guid("a36d0300-c9f3-11cf-bfc7-444553540000"), "TorqueY", JoystickOffset.TorqueY /*256*/) },
			{ 88, (0x58, 26, new Guid("a36d0301-c9f3-11cf-bfc7-444553540000"), "TorqueZ", JoystickOffset.TorqueZ /*260*/) },
			{ 89, (0x59, 27, new Guid("a36d0302-c9f3-11cf-bfc7-444553540000"), "PositionX", (JoystickOffset)260) },
			{ 90, (0x5A, 28, new Guid("a36d0303-c9f3-11cf-bfc7-444553540000"), "PositionY", (JoystickOffset)264) },
			{ 91, (0x5B, 29, new Guid("a36d0304-c9f3-11cf-bfc7-444553540000"), "PositionZ", (JoystickOffset)268) },
		};

		// { SharpDX.DirectInput.DeviceObjectInstance.Usage(DEC), (Usage(HEX), ObjectId.InstanceNumber, ObjectType(Guid), name, JoystickOffset) }.
		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> POVIndexDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			// POVs.
			{ 0, (0x39, 57, new Guid("a36d02f2-c9f3-11cf-bfc7-444553540000"), "PointOfViewControllers0", JoystickOffset.PointOfViewControllers0 /*32*/) },
			{ 1, (0x39, 57, new Guid("a36d02f2-c9f3-11cf-bfc7-444553540000"), "PointOfViewControllers1", JoystickOffset.PointOfViewControllers1 /*36*/) },
			{ 2, (0x39, 57, new Guid("a36d02f2-c9f3-11cf-bfc7-444553540000"), "PointOfViewControllers2", JoystickOffset.PointOfViewControllers2 /*40*/) },
			{ 3, (0x39, 57, new Guid("a36d02f2-c9f3-11cf-bfc7-444553540000"), "PointOfViewControllers3", JoystickOffset.PointOfViewControllers3 /*44*/) },
			//{ 144, (0x90, 0, new Guid("a36d02f1-c9f3-11cf-bfc7-444553540000"), "DPadUp", (JoystickOffset)276) },
			//{ 145, (0x91, 1, new Guid("a36d02f1-c9f3-11cf-bfc7-444553540000"), "DPadDown", (JoystickOffset)280) },
			//{ 146, (0x92, 2, new Guid("a36d02f1-c9f3-11cf-bfc7-444553540000"), "DPadRight", (JoystickOffset)284) },
			//{ 147, (0x93, 3, new Guid("a36d02f1-c9f3-11cf-bfc7-444553540000"), "DPadLeft", (JoystickOffset)288) },
		};

		public static Dictionary<int, (int, int, Guid, string, JoystickOffset)> SliderIndexDictionary = new Dictionary<int, (int, int, Guid, string, JoystickOffset)>
		{
			// Sliders.
			{ 0, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "Slider0", JoystickOffset.Sliders0 /*24*/) },
			{ 1, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "Slider1", JoystickOffset.Sliders1 /*28*/) },
			{ 2, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "AccelerationSliders0", JoystickOffset.AccelerationSliders0 /*232*/) },
			{ 3, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "AccelerationSliders1", JoystickOffset.AccelerationSliders1 /*236*/) },
			{ 4, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "ForceSliders0", JoystickOffset.ForceSliders0 /*264*/) },
			{ 5, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "ForceSliders1", JoystickOffset.ForceSliders1 /*268*/) },
			{ 6, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "VelocitySliders0", JoystickOffset.VelocitySliders0 /*200*/) },
			{ 7, (0x36, 54, new Guid("a36d02e4-c9f3-11cf-bfc7-444553540000"), "VelocitySliders1", JoystickOffset.VelocitySliders1 /*204*/) },
		};
	}
}
