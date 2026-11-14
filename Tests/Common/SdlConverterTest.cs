// @under-test: Engine/Common/Sdl/SdlMapping.cs, Engine/Common/Sdl/SdlConverter.cs, Engine/Common/Sdl/SdlPositions.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// SDL's controller rows read as x360ce's mapping boxes for one device, and a device's mapping written back as a row.
	/// </summary>
	/// <remarks>
	/// SDL numbers a device's axes in the order DirectInput lays them out, counting only those it has: X, Y, Z, Rx, Ry,
	/// Rz, then the two sliders. So the same row means different boxes on devices with different axes, and the device's
	/// masks decide it.
	/// </remarks>
	[TestClass]
	public class SdlConverterTest
	{
		/// <summary>The Xbox 360 controller's Windows row: its triggers share Z, so they are its two halves.</summary>
		const string Xbox360Row = "030000005e0400008e02000000000000,Xbox 360 Controller,a:b0,b:b1,back:b6,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,leftshoulder:b4,leftstick:b8,lefttrigger:+a2,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b9,righttrigger:-a2,rightx:a3,righty:a4,start:b7,x:b2,y:b3,platform:Windows,";

		static SdlMapping Row(string text)
		{
			SdlMapping mapping;
			Assert.IsTrue(SdlMapping.TryParse(text, out mapping), "Not read as an SDL row: " + text);
			return mapping;
		}

		/// <summary>A pad with the given axes (X..Rz as bits 0..5) and sliders, twelve buttons and one hat.</summary>
		static SdlDevice Pad(int axes, int sliders = 0)
		{
			return new SdlDevice { AxisMask = axes, SliderMask = sliders, ButtonCount = 12, PovCount = 1, VendorId = 0x045E, ProductId = 0x028E, ProductName = "Controller (XBOX 360 For Windows)" };
		}

		static string Box(SdlTranslation t, MapCode code)
		{
			return SdlConverter.GetValue(t.Setting, code);
		}

		static MapExpression Parse(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position), text + ": " + error);
			return result;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every form of SDL GUID gives its ids, and a CRC or version does not make another controller")]
		public void Guid_forms_give_their_ids()
		{
			var usb = SdlGuid.Parse("03000000300f00000a01000000000000");
			Assert.AreEqual(SdlGuidForm.Standard, usb.Form);
			Assert.AreEqual(0x0003, usb.Bus);
			Assert.AreEqual("USB", usb.BusName);
			Assert.AreEqual(0x0F30, usb.VendorId);
			Assert.AreEqual(0x010A, usb.ProductId);
			Assert.AreEqual("03000000300f00000a01000000000000", SdlGuid.ForDevice(0x0F30, 0x010A));
			var crc = SdlGuid.Parse("0300004112000000e500000000000000");
			Assert.AreEqual(SdlGuidForm.Standard, crc.Form);
			Assert.AreEqual(0x0012, crc.VendorId);
			Assert.AreEqual(0x00E5, crc.ProductId);
			Assert.AreEqual(SdlGuid.Parse("0300000012000000e500000000000000").Key, crc.Key, "A name's CRC made another controller.");
			Assert.AreEqual(usb.Key, SdlGuid.Parse("03000000300F00000A01000011010000").Key, "A version, or upper case, made another controller.");
			var bluetooth = SdlGuid.Parse("050000004c050000cc09000000000000");
			Assert.AreEqual("Bluetooth", bluetooth.BusName);
			Assert.AreEqual(0x054C, bluetooth.VendorId);
			Assert.AreNotEqual(SdlGuid.Parse("030000004c050000cc09000000000000").Key, bluetooth.Key, "USB and Bluetooth rows are one row.");
			// SDL before 2.0.5 wrote DirectInput's product GUID, vendor and product then "PIDVID".
			var old = SdlGuid.Parse("300f0a01000000000000504944564944");
			Assert.AreEqual(SdlGuidForm.ProductGuid, old.Form);
			Assert.AreEqual(0x0F30, old.VendorId);
			Assert.AreEqual(0x010A, old.ProductId);
			Assert.AreEqual(SdlGuidForm.XInput, SdlGuid.Parse("xinput").Form);
			var other = SdlGuid.Parse("03000095090000010000000000000000");
			Assert.AreEqual(SdlGuidForm.Other, other.Form);
			Assert.IsFalse(other.HasIds, "Ids were read from a GUID of no known form.");
			Assert.IsNull(SdlGuid.Parse("0300"));
			Assert.IsNull(SdlGuid.Parse("ButtonA: 3"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A device's ids come from its device node, else from DirectInput's product GUID")]
		public void Device_ids_come_from_the_node_or_the_product_guid()
		{
			var ud = new UserDevice { ProductGuid = RawInputDevice.GetProductGuid(0x0F30, 0x010A), DiAxeMask = 0x3, CapButtonCount = 10 };
			var device = SdlDevice.From(ud);
			Assert.AreEqual(0x0F30, device.VendorId);
			Assert.AreEqual(0x010A, device.ProductId);
			Assert.AreEqual("03000000300f00000a01000000000000", device.Guid);
			ud.DevVendorId = 0x046D;
			ud.DevProductId = 0xC219;
			device = SdlDevice.From(ud);
			Assert.AreEqual(0x046D, device.VendorId, "The device node's vendor was passed over.");
			Assert.AreEqual(0xC219, device.ProductId);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A row reads and writes back as SDL writes it; fields that are not parts of the pad are kept but not mapped")]
		public void A_row_reads_and_writes_back()
		{
			var row = Row(Xbox360Row);
			Assert.AreEqual("Xbox 360 Controller", row.Name);
			Assert.AreEqual("Windows", row.Platform);
			Assert.AreEqual("b0", row.Get("a"));
			Assert.AreEqual(Xbox360Row, row.ToString());
			SdlMapping none;
			Assert.IsFalse(SdlMapping.TryParse("# Windows", out none), "A comment was read as a row.");
			Assert.IsFalse(SdlMapping.TryParse("ButtonA: 3", out none), "A preset line was read as a row.");
			var linux = Row("030000005e0400008e02000010010000,Xbox 360 Controller,a:b0,platform:Linux,");
			Assert.IsFalse(linux.IsForWindows);
			Assert.IsTrue(Row("030000005e0400008e02000000000000,Xbox 360 Controller,a:b0,").IsForWindows, "A row for no platform is SDL's on every platform.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The Xbox 360 row maps the pad as x360ce's own guess does: triggers on the halves of Z, Y read upwards, the hat as the D-Pad")]
		public void Xbox_360_row_maps_halves_inverted_axes_and_the_hat()
		{
			// X, Y, Z, Rx and Ry, as the XInput driver lays them out for DirectInput.
			var t = SdlConverter.ToPadSetting(Row(Xbox360Row), Pad(0x1F));
			Assert.AreEqual(0, t.Skipped.Count, t.SkippedText());
			Assert.AreEqual("1", Box(t, MapCode.ButtonA));
			Assert.AreEqual("2", Box(t, MapCode.ButtonB));
			Assert.AreEqual("3", Box(t, MapCode.ButtonX));
			Assert.AreEqual("4", Box(t, MapCode.ButtonY));
			Assert.AreEqual("8", Box(t, MapCode.ButtonStart));
			Assert.AreEqual("x3", Box(t, MapCode.LeftTrigger), "+a2 is the upper half of Z, HAxis 3.");
			Assert.AreEqual("x-3", Box(t, MapCode.RightTrigger), "-a2 is the lower half of Z, IHAxis 3.");
			Assert.AreEqual("a1", Box(t, MapCode.LeftThumbAxisX));
			Assert.AreEqual("a-2", Box(t, MapCode.LeftThumbAxisY), "SDL's Y grows downward, so lefty:a1 is IAxis 2.");
			Assert.AreEqual("a4", Box(t, MapCode.RightThumbAxisX));
			Assert.AreEqual("a-5", Box(t, MapCode.RightThumbAxisY));
			Assert.AreEqual("p1", Box(t, MapCode.DPad), "Four directions of one hat are the D-Pad.");
			Assert.AreEqual("", Box(t, MapCode.DPadUp), "The hat's directions were left in their own boxes as well.");
			Assert.AreEqual("dpup:h0.1, dpright:h0.2, dpdown:h0.4, dpleft:h0.8", t.Sources[MapCode.DPad]);
			// An inverted axis: SDL's Y read the other way round is XInput's way up.
			var inverted = SdlConverter.ToPadSetting(Row("030000005e0400008e02000000000000,Pad,lefty:a1~,lefttrigger:a2~,righttrigger:a5,platform:Windows,"), Pad(0x3F));
			Assert.AreEqual("a2", Box(inverted, MapCode.LeftThumbAxisY));
			Assert.AreEqual("a-3", Box(inverted, MapCode.LeftTrigger), "A trigger on an inverted axis is IAxis.");
			Assert.AreEqual("a6", Box(inverted, MapCode.RightTrigger), "A trigger on a whole axis is Axis.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("SDL counts only the axes a device has: with X, Y and Rz, a2 is Rz, Axis 6, and a3 is none")]
		public void Gaps_in_the_axes_move_sdl_numbers()
		{
			var row = Row("03000000300f00000a01000000000000,Pad,a:b0,leftx:a0,lefty:a1,lefttrigger:a2,righttrigger:a3,platform:Windows,");
			var t = SdlConverter.ToPadSetting(row, Pad(0x23));
			Assert.AreEqual("a1", Box(t, MapCode.LeftThumbAxisX));
			Assert.AreEqual("a-2", Box(t, MapCode.LeftThumbAxisY));
			Assert.AreEqual("a6", Box(t, MapCode.LeftTrigger), "a2 on a device with X, Y and Rz is Rz.");
			Assert.AreEqual("", Box(t, MapCode.RightTrigger));
			CollectionAssert.Contains(t.Skipped, "righttrigger:a3", "A fourth axis the device does not have was not named.");
			// The sliders come after the axes.
			var sliders = SdlConverter.ToPadSetting(row, Pad(0x03, 0x01));
			Assert.AreEqual("s1", Box(sliders, MapCode.LeftTrigger), "a2 on a device with X, Y and a slider is Slider 1.");
			// A button or hat the device does not have is left out too.
			var few = SdlConverter.ToPadSetting(Row("03000000300f00000a01000000000000,Pad,a:b0,b:b20,x:h1.1,platform:Windows,"), Pad(0x3));
			CollectionAssert.AreEquivalent(new[] { "b:b20", "x:h1.1" }, few.Skipped);
			Assert.AreEqual(1, few.Count);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Half axes press buttons, a hat direction presses a face button, and D-Pad buttons stay buttons")]
		public void Halves_hats_and_buttons_fill_button_boxes()
		{
			// Microsoft SideWinder Plug and Play: its D-Pad is X and Y.
			var axes = SdlConverter.ToPadSetting(Row("030000005e0400002700000000000000,Microsoft SideWinder Plug and Play,a:b0,b:b1,dpdown:+a1,dpleft:-a0,dpright:+a0,dpup:-a1,lefttrigger:b4,righttrigger:b5,x:b2,y:b3,platform:Windows,"), Pad(0x3));
			Assert.AreEqual("x-2", Box(axes, MapCode.DPadUp), "-a1 is the lower half of Y, IHAxis 2.");
			Assert.AreEqual("x2", Box(axes, MapCode.DPadDown));
			Assert.AreEqual("x-1", Box(axes, MapCode.DPadLeft));
			Assert.AreEqual("x1", Box(axes, MapCode.DPadRight));
			Assert.AreEqual("5", Box(axes, MapCode.LeftTrigger), "A trigger on a button is that button.");
			Assert.AreEqual("", Box(axes, MapCode.DPad));
			// 4Play Adapter: its D-Pad is four buttons.
			var buttons = SdlConverter.ToPadSetting(Row("03000000d0160000040d000000000000,4Play Adapter,a:b1,b:b3,back:b4,dpdown:b11,dpleft:b12,dpright:b13,dpup:b10,leftshoulder:b6,leftstick:b14,lefttrigger:b8,leftx:a0,lefty:a1,rightshoulder:b7,rightstick:b15,righttrigger:b9,rightx:a3,righty:a4,start:b5,x:b0,y:b2,platform:Windows,"),
				new SdlDevice { AxisMask = 0x1F, ButtonCount = 16 });
			Assert.AreEqual("11", Box(buttons, MapCode.DPadUp));
			Assert.AreEqual("12", Box(buttons, MapCode.DPadDown));
			Assert.AreEqual("", Box(buttons, MapCode.DPad));
			// A hat direction, and a hat turned a quarter, stay single directions.
			var hats = SdlConverter.ToPadSetting(Row("03000000300f00000a01000000000000,Pad,x:h0.1,dpup:h0.2,dpright:h0.4,dpdown:h0.8,dpleft:h0.1,platform:Windows,"), Pad(0x3));
			Assert.AreEqual("d1", Box(hats, MapCode.ButtonX), "h0.1 is POV 1 Up.");
			Assert.AreEqual("d2", Box(hats, MapCode.DPadUp), "h0.2 is POV 1 Right.");
			Assert.AreEqual("", Box(hats, MapCode.DPad), "A hat whose directions are turned was taken for the D-Pad.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick split over two axes becomes a formula; one axis in two halves is that axis; buttons and hat directions are the stick's direction boxes")]
		public void Stick_halves_become_a_formula_an_axis_or_direction_boxes()
		{
			// Ipega PG9087: the right stick's up half on Ry, its down half on Rz.
			var split = SdlConverter.ToPadSetting(Row("03000000491900000304000000000000,Ipega PG9087,+righty:+a5,-righty:-a4,a:b0,leftx:a0,lefty:a1,rightx:a3,platform:Windows,"), Pad(0x3F));
			var formula = Box(split, MapCode.RightThumbAxisY);
			Assert.AreEqual("=max(-a5,0)-x6", formula);
			Assert.AreEqual("-righty:-a4, +righty:+a5", split.Sources[MapCode.RightThumbAxisY]);
			var e = Parse(formula);
			Func<float, float, short> read = (a5, x6) => MapExpressionUnits.ToThumb(e.Evaluate(e.References.Select(r => r.Type == 'a' ? a5 : x6).ToArray()));
			Assert.AreEqual(short.MaxValue, read(-1f, 0f), "Ry pushed to its low end does not put the stick up.");
			Assert.AreEqual(short.MinValue, read(0f, 1f), "Rz pushed to its high end does not put the stick down.");
			Assert.AreEqual(0, read(0f, 0f), "At rest the stick is not centred.");
			// x360ce 3 has no formulas: each half goes to the stick's direction box, read as a half axis.
			var v3 = SdlConverter.ToPadSetting(Row("03000000491900000304000000000000,Ipega PG9087,+righty:+a5,-righty:-a4,a:b0,platform:Windows,"), Pad(0x3F), false);
			Assert.AreEqual("", Box(v3, MapCode.RightThumbAxisY));
			Assert.AreEqual("x-5", Box(v3, MapCode.RightThumbUp), "Ry's lower half does not push the stick up.");
			Assert.AreEqual("x6", Box(v3, MapCode.RightThumbDown), "Rz's upper half does not push the stick down.");
			// One axis in two halves is the axis, read the right way round.
			var halves = SdlConverter.ToPadSetting(Row("03000000300f00000a01000000000000,Pad,-lefty:-a1,+lefty:+a1,-leftx:+a0,+leftx:-a0,platform:Windows,"), Pad(0x3));
			Assert.AreEqual("a-2", Box(halves, MapCode.LeftThumbAxisY), "-lefty:-a1,+lefty:+a1 is lefty:a1.");
			Assert.AreEqual("a-1", Box(halves, MapCode.LeftThumbAxisX), "-leftx:+a0,+leftx:-a0 is leftx:a0~.");
			// 8BitDo N64: the right stick is four buttons.
			var buttons = SdlConverter.ToPadSetting(Row("03000000c82d00000290000000000000,8BitDo N64,+rightx:b9,+righty:b3,-rightx:b4,-righty:b8,a:b0,b:b1,platform:Windows,"), Pad(0x3));
			Assert.AreEqual("10", Box(buttons, MapCode.RightThumbRight));
			Assert.AreEqual("4", Box(buttons, MapCode.RightThumbDown), "+righty is down: SDL's Y grows downward.");
			Assert.AreEqual("5", Box(buttons, MapCode.RightThumbLeft));
			Assert.AreEqual("9", Box(buttons, MapCode.RightThumbUp));
			Assert.AreEqual("", Box(buttons, MapCode.RightThumbAxisX));
			// Gravis Destroyer Tilt: the left stick is the hat.
			var hat = SdlConverter.ToPadSetting(Row("030000007d0400000840000000000000,Gravis Destroyer Tilt,+leftx:h0.2,+lefty:h0.4,-leftx:h0.8,-lefty:h0.1,a:b1,platform:Windows,"), Pad(0x3));
			Assert.AreEqual("d1", Box(hat, MapCode.LeftThumbUp));
			Assert.AreEqual("d2", Box(hat, MapCode.LeftThumbRight));
			Assert.AreEqual("d3", Box(hat, MapCode.LeftThumbDown));
			Assert.AreEqual("d4", Box(hat, MapCode.LeftThumbLeft));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("x360ce 3 is given SDL's mapping by its own x360ce.ini keys, each one its library takes from SDL, a split stick in its direction keys")]
		public void Version_3_gets_the_mapping_by_its_ini_keys()
		{
			var keys = SdlConverter.ToIniSettings(Row(Xbox360Row), Pad(0x1F));
			Assert.AreEqual(SdlConverter.MappingBoxes.Length, keys.Count, "A box has no x360ce.ini key.");
			var other = keys.Keys.Where(x => !SettingName.IsMapping(x)).ToArray();
			Assert.AreEqual(0, other.Length, "Keys x360ce 3 would save over the database: " + string.Join(", ", other));
			Assert.AreEqual("1", keys[SettingName.ButtonA]);
			Assert.AreEqual("a-2", keys[SettingName.LeftThumbAxisY]);
			Assert.AreEqual("x3", keys[SettingName.LeftTrigger]);
			Assert.AreEqual("p1", keys[SettingName.DPad]);
			Assert.AreEqual("", keys[SettingName.DPadUp], "A box the row leaves empty is not empty.");
			// x360ce 3's library reads "Right Analog Y+ Button=x-5" as Ry's lower half pushing the stick up.
			var split = SdlConverter.ToIniSettings(Row("03000000491900000304000000000000,Ipega PG9087,+righty:+a5,-righty:-a4,a:b0,platform:Windows,"), Pad(0x3F));
			Assert.AreEqual("", split[SettingName.RightThumbAxisY]);
			Assert.AreEqual("x-5", split[SettingName.RightThumbUp]);
			Assert.AreEqual("x6", split[SettingName.RightThumbDown]);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Parts of a pad x360ce has no box for are named as left out; a row's own notes are not")]
		public void Parts_without_a_box_are_named()
		{
			var t = SdlConverter.ToPadSetting(Row("03000000300f00000a01000000000000,Pad,a:b0,misc1:b12,paddle1:b13,touchpad:b14,face:sony,crc:1234,hint:!SDL_X:=1,platform:Windows,"), Pad(0x3));
			CollectionAssert.AreEquivalent(new[] { "misc1:b12", "paddle1:b13", "touchpad:b14" }, t.Skipped);
			StringAssert.Contains(t.SkippedText(), "misc1:b12");
			Assert.AreEqual(1, t.Count);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("presets"), TestCategory("critical")]
		[Description("A device's mapping copied as an SDL row and pasted back gives the same boxes; a database row comes back as it was")]
		public void Copy_and_paste_round_trip()
		{
			var device = Pad(0x23, 0x1);
			var ps = new PadSetting
			{
				ButtonA = "1",
				ButtonB = "2",
				ButtonX = "d1",
				ButtonY = "x-2",
				ButtonBack = "7",
				ButtonStart = "8",
				ButtonGuide = "x6",
				LeftShoulder = "5",
				RightShoulder = "6",
				LeftThumbButton = "9",
				RightThumbButton = "10",
				LeftTrigger = "s1",
				RightTrigger = "x-6",
				LeftThumbAxisX = "a1",
				LeftThumbAxisY = "a-2",
				RightThumbAxisX = "a6",
				RightThumbAxisY = "a2",
				RightThumbUp = "11",
				DPad = "p1",
			};
			var row = SdlConverter.ToMapping(ps, device, "Round Trip Pad");
			Console.WriteLine(row);
			Assert.AreEqual("Windows", row.Platform);
			Assert.AreEqual(device.Guid, row.Guid.Text);
			Assert.AreEqual("a1", row.Get("lefty"), "IAxis 2 on the stick's Y is SDL's plain a1: both grow downward.");
			Assert.AreEqual("a1~", row.Get("righty"), "Axis 2 on the stick's Y is SDL's a1 inverted.");
			Assert.AreEqual("a3", row.Get("lefttrigger"), "Slider 1 on a device with X, Y and Rz is SDL's a3, after the axes.");
			Assert.AreEqual("a2", row.Get("rightx"), "Axis 6, Rz, on a device with X, Y and Rz is SDL's a2.");
			var back = SdlConverter.ToPadSetting(Row(row.ToString()), device);
			Assert.AreEqual(0, back.Skipped.Count, back.SkippedText());
			foreach (var code in SdlConverter.MappingBoxes)
				Assert.AreEqual(SdlConverter.GetValue(ps, code), SdlConverter.GetValue(back.Setting, code), code.ToString());
			// What SDL cannot say is left out: a formula, a button read the other way round.
			ps.ButtonA = "=b1*b2";
			ps.ButtonB = "-2";
			var lossy = SdlConverter.ToMapping(ps, device, "Pad");
			Assert.IsNull(lossy.Get("a"));
			Assert.IsNull(lossy.Get("b"));
			// A row of the database, read for the device it describes and written back, is the row.
			var xbox = Row(Xbox360Row);
			var pad = Pad(0x1F);
			Assert.AreEqual(Xbox360Row, SdlConverter.ToMapping(SdlConverter.ToPadSetting(xbox, pad).Setting, pad, xbox.Name).ToString());
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Face labels follow the row's face field and hints, else the pad's maker and name; SDL's a is always the bottom button")]
		public void Face_labels_follow_the_row_and_the_pad()
		{
			Assert.AreEqual(SdlFaceStyle.Sony, SdlPositions.FaceStyle(Row("03000000300f00000a01000000000000,Pad,a:b0,face:sony,platform:Windows,"), 0, "Pad"));
			Assert.AreEqual(SdlFaceStyle.Axby, SdlPositions.FaceStyle(Row("03000000300f00000a01000000000000,Pad,a:b0,hint:!SDL_GAMECONTROLLER_USE_GAMECUBE_LABELS:=1,platform:Windows,"), 0, "Pad"));
			Assert.AreEqual(SdlFaceStyle.Bayx, SdlPositions.FaceStyle(Row("03000000300f00000a01000000000000,Pad,a:b0,hint:!SDL_GAMECONTROLLER_USE_BUTTON_LABELS:=1,platform:Windows,"), 0, "Pad"));
			Assert.AreEqual(SdlFaceStyle.Sony, SdlPositions.FaceStyle(null, 0x054C, "Wireless Controller"));
			Assert.AreEqual(SdlFaceStyle.Sony, SdlPositions.FaceStyle(null, 0x0F0D, "Hori DualShock 4 Pad"));
			Assert.AreEqual(SdlFaceStyle.Bayx, SdlPositions.FaceStyle(null, 0x057E, "Pro Controller"));
			Assert.AreEqual(SdlFaceStyle.Axby, SdlPositions.FaceStyle(null, 0x057E, "GameCube Adapter"));
			Assert.AreEqual(SdlFaceStyle.Abxy, SdlPositions.FaceStyle(null, 0x045E, "Xbox 360 Controller"));
			Assert.AreEqual("Cross", SdlPositions.Label(SdlFaceStyle.Sony, MapCode.ButtonA));
			Assert.AreEqual("B", SdlPositions.Label(SdlFaceStyle.Bayx, MapCode.ButtonA), "On a Nintendo pad the bottom button is B.");
			Assert.AreEqual("X", SdlPositions.Label(SdlFaceStyle.Axby, MapCode.ButtonB));
			Assert.AreEqual("Bottom face button", SdlPositions.Position(MapCode.ButtonA));
			Assert.AreEqual("Left stick, left-right", SdlPositions.Position(MapCode.LeftThumbAxisX));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The SDL page lists each place, SDL's binding and control beside the box, and marks only real differences")]
		public void Positions_mark_where_the_box_differs()
		{
			var t = SdlConverter.ToPadSetting(Row(Xbox360Row), Pad(0x1F));
			// The same as SDL gives, written another way, and one box changed.
			var current = new PadSetting { ButtonA = "b1", ButtonB = "3", LeftThumbAxisY = "a-2", DPad = "p1", LeftThumbUp = "5" };
			var rows = SdlPositions.Compare(t, current, SdlFaceStyle.Abxy);
			var a = rows.Single(x => x.Code == MapCode.ButtonA);
			Assert.AreEqual("a", a.Field);
			Assert.AreEqual("A", a.Label);
			Assert.AreEqual("b0", a.Binding);
			Assert.AreEqual("Button 1", a.Control);
			Assert.IsFalse(a.Differs, "b1 and 1 are the same button.");
			Assert.IsTrue(rows.Single(x => x.Code == MapCode.ButtonB).Differs);
			Assert.AreEqual("IAxis 2", rows.Single(x => x.Code == MapCode.LeftThumbAxisY).Control);
			Assert.IsTrue(rows.Single(x => x.Code == MapCode.LeftTrigger).Differs, "An empty box beside SDL's HAxis 3 is not marked.");
			Assert.IsTrue(rows.Any(x => x.Code == MapCode.LeftThumbUp), "A direction box that holds something is not listed.");
			Assert.IsFalse(rows.Any(x => x.Code == MapCode.RightThumbUp), "An empty direction box is listed.");
			// Without a row, nothing is marked.
			Assert.IsFalse(SdlPositions.Compare(null, current, SdlFaceStyle.Abxy).Any(x => x.Differs));
		}
	}
}
