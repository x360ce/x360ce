using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using x360ce.Engine;
using System.Security.AccessControl;
using System.Security.Principal;
using x360ce.Engine.Data;
using SharpDX.XInput;
using JocysCom.ClassLibrary.Win32;
using x360ce.App.ViGEm;
using x360ce.App.DInput;

namespace x360ce.App
{
	public static class AppHelper
	{
		#region DLL Functions

		static void Elevate()
		{
			// If this is Vista/7 and is not elevated then elevate.
			if (WinAPI.IsVista && !WinAPI.IsElevated())
				WinAPI.RunElevated();
		}

		public static bool WriteFile(string resourceName, string destinationFileName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var sr = assembly.GetManifestResourceStream(resourceName);
			FileStream sw = null;
			try
			{
				sw = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			var buffer = new byte[1024];
			while (true)
			{
				var count = sr.Read(buffer, 0, buffer.Length);
				if (count == 0)
					break;
				sw.Write(buffer, 0, count);
			}
			sr.Close();
			sw.Close();
			return true;
		}

		public static bool CopyFile(string sourceFileName, string destFileName)
		{
			try
			{
				File.Copy(sourceFileName, destFileName, true);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			return true;
		}

		public static DeviceObjectItem[] GetDeviceObjects(Joystick device)
		{
			var items = new List<DeviceObjectItem>();
			if (device == null)
				return items.ToArray();
			var og = typeof(SharpDX.DirectInput.ObjectGuid);
			var guidFileds = og.GetFields().Where(x => x.FieldType == typeof(Guid));
			List<Guid> typeGuids = guidFileds.Select(x => (Guid)x.GetValue(og)).ToList();
			List<string> typeName = guidFileds.Select(x => x.Name).ToList();
			var objects = device.GetObjects(DeviceObjectTypeFlags.All).OrderBy(x => x.ObjectId.Flags).ThenBy(x => x.ObjectId.InstanceNumber).ToArray();
			foreach (var o in objects)
			{
				var item = new DeviceObjectItem()
				{
					Name = o.Name,
					Offset = o.Offset,
					Aspect = o.Aspect,
					Flags = o.ObjectId.Flags,
					ObjectId = (int)o.ObjectId,
					Instance = o.ObjectId.InstanceNumber,
					Type = o.ObjectType,
					DiIndex = o.ObjectId.InstanceNumber - 1,
				};
				var isAxis = o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.Axis);
				isAxis |= o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.AbsoluteAxis);
				isAxis |= o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis);
				if (isAxis)
				{
					try
					{
						var p = device.GetObjectPropertiesById(o.ObjectId);
						if (p != null)
						{
							item.DeadZone = p.DeadZone;
							item.Granularity = p.Granularity;
							item.LogicalRangeMin = p.LogicalRange.Minimum;
							item.LogicalRangeMax = p.LogicalRange.Maximum;
							item.PhysicalRangeMin = p.PhysicalRange.Minimum;
							item.PhysicalRangeMax = p.PhysicalRange.Maximum;
							item.RangeMin = p.Range.Minimum;
							item.RangeMax = p.Range.Maximum;
							item.Saturation = p.Saturation;
						}

					}
					catch (Exception ex)
					{
						_ = ex.Message;
					}
				}
				items.Add(item);
			}
			// Update Button DIndexes.
			var buttons = items.Where(x => x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key).OrderBy(x => x.Instance).ToArray();
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].DiIndex = i;
			}
			return items.ToArray();
		}


		/// <summary>
		/// Device must be acquired in exclusive mode to get effects.
		/// </summary>
		public static DeviceEffectItem[] GetDeviceEffects(Joystick device)
		{
			var items = new List<DeviceEffectItem>();
			if (device == null)
				return items.ToArray();
			// Check if device supports force feedback.
			var forceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
			if (!forceFeedback)
				return items.ToArray();
			lock (Controller.XInputLock)
			{
				// Unload XInput.
				var isLoaded = Controller.IsLoaded;
				if (isLoaded)
				{
					Controller.FreeLibrary();
				}
				IList<EffectInfo> effects = new List<EffectInfo>();
				try
				{
					effects = device.GetEffects(EffectType.All);
				}
				catch (Exception ex)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				}
				foreach (var eff in effects)
				{
					items.Add(new DeviceEffectItem()
					{
						Name = eff.Name,
						StaticParameters = eff.StaticParameters,
						DynamicParameters = eff.DynamicParameters,
					});
				}
				// If XInput was loaded then...
				if (isLoaded)
				{
					Exception error;
					Controller.ReLoadLibrary(Controller.LibraryName, out error);
				}
			}
			return items.ToArray();
		}

		#endregion

		public static void LoadHelp(System.Windows.Forms.RichTextBox box, string resourceName)
		{
			var stream = EngineHelper.GetResourceStream(resourceName);
			// A help document that is missing or renamed leaves the box empty. It used to throw
			// from the constructor of whatever control asked for it, which took the whole screen
			// down over text nobody had read yet.
			if (stream == null)
				return;
			var sr = new StreamReader(stream);
			// The document is Markdown and there is only one copy of it. It becomes what this box
			// can show here, when it is opened, so nothing has to be generated, committed, or kept
			// in step with anything else.
			box.Rtf = x360ce.Engine.MarkdownRtf.ToRtf(sr.ReadToEnd());
			box.LinkClicked += (object sender, System.Windows.Forms.LinkClickedEventArgs e) =>
			{
				JocysCom.ClassLibrary.Controls.ControlsHelper.OpenUrl(e.LinkText);
			};
		}

	
		// Use cache so same image won't processed multiple times.
		public static Dictionary<Bitmap, Bitmap> DisabledImageCache = new Dictionary<Bitmap, Bitmap>();
		static object DisabledImageLock = new object();

		/// <summary>
		/// Generates disabled Image. Images are cached so do not use method for random images.
		/// </summary>
		public static Bitmap GetDisabledImage(Bitmap image)
		{
			lock (DisabledImageLock)
			{
				if (!DisabledImageCache.ContainsKey(image))
				{
					var newImage = (Bitmap)image.Clone();
					JocysCom.ClassLibrary.Drawing.Effects.GrayScale(newImage);
					JocysCom.ClassLibrary.Drawing.Effects.Transparent(newImage, 50);
					DisabledImageCache.Add(image, newImage);
				}
				return DisabledImageCache[image];
			}
		}

		/// <summary>
		/// Remove explicit file rules and leave inherited rules only.
		/// Allow built-in users to write and modify file.
		/// </summary>
		public static bool CheckExplicitAccessRulesAndAllowToModify(string fileName, bool applyFix)
		{
			var fileInfo = new FileInfo(fileName);
			var fileSecurity = fileInfo.GetAccessControl();
			fileSecurity.SetAccessRuleProtection(false, false);
			var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			// Get explicit file rules of FileSystemAccessRule type.
			var rules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount)).OfType<FileSystemAccessRule>();
			var referenceValue = ((NTAccount)identity.Translate(typeof(NTAccount))).Value;
			// Remove explicit permission.
			var allowsWrite = false;
			var allowsModify = false;
			var rulesChanged = false;
			foreach (var rule in rules)
			{
				if (rule.AccessControlType == AccessControlType.Allow && rule.IdentityReference.Value == referenceValue)
				{
					if (rule.FileSystemRights.HasFlag(FileSystemRights.Write))
					{
						allowsWrite = true;
						continue;
					}
					if (rule.FileSystemRights.HasFlag(FileSystemRights.Modify))
					{
						allowsModify = true;
						continue;
					}
				}
				// If rule is not inherited from parent directory then...
				if (!rule.IsInherited)
				{
					// Remove rules.
					fileSecurity.RemoveAccessRule(rule);
					rulesChanged = true;
				}
			}
			if (applyFix)
			{
				if (!allowsWrite)
				{
					fileSecurity.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.Write, AccessControlType.Allow));
					rulesChanged = true;
				}
				if (!allowsModify)
				{
					fileSecurity.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.Modify, AccessControlType.Allow));
					rulesChanged = true;
				}
				if (rulesChanged)
				{
					fileInfo.SetAccessControl(fileSecurity);
				}
			}
			return rulesChanged;
		}

		/// <summary>
		/// Update (wipe all old records) DataGridView is such way that it won't loose selection.
		/// </summary>
		public static void UpdateList<T>(IList<T> source, IList<T> destination)
		{
			if (source == null)
				source = new List<T>();
			var sCount = source.Count;
			var dCount = destination.Count;
			var length = Math.Min(sCount, dCount);
			for (int i = 0; i < length; i++)
				destination[i] = source[i];
			// Add extra rows.
			if (sCount > dCount)
			{
				for (int i = dCount; i < sCount; i++)
					destination.Add(source[i]);
			}
			else if (dCount > sCount)
			{
				for (int i = dCount - 1; i >= sCount; i--)
					destination.RemoveAt(i);
			}
		}

		public static Engine.Data.UserSetting GetNewSetting(UserDevice device, Engine.Data.UserGame game, MapTo mapTo)
		{
			// Create new setting for game/device.
			var newSetting = new Engine.Data.UserSetting();
			newSetting.InstanceGuid = device.InstanceGuid;
			newSetting.InstanceName = device.InstanceName;
			newSetting.ProductGuid = device.ProductGuid;
			newSetting.ProductName = device.ProductName;
			newSetting.DeviceType = device.CapType;
			newSetting.FileName = game.FileName;
			newSetting.FileProductName = game.FileProductName;
			newSetting.DateCreated = DateTime.Now;
			newSetting.IsEnabled = true;
			newSetting.MapTo = (int)mapTo;
			return newSetting;
		}

		public static MapToMask GetMapFlag(MapTo mapTo)
		{
			switch (mapTo)
			{
				case MapTo.Controller1:
					return MapToMask.Controller1;
				case MapTo.Controller2:
					return MapToMask.Controller2;
				case MapTo.Controller3:
					return MapToMask.Controller3;
				case MapTo.Controller4:
					return MapToMask.Controller4;
				default:
					return MapToMask.None;
			}
		}

		#region HID Guardian

		public static void InitializeHidGuardian()
		{
			// If can't fix and modify registry then return.
			if (!ViGEm.HidGuardianHelper.CanModifyParameters(true))
				return;
			ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
			ViGEm.HidGuardianHelper.ClearWhiteList(true, true);
		}

		public static void UnInitializeHidGuardian()
		{
			// If can't modify registry then return.
			if (!ViGEm.HidGuardianHelper.CanModifyParameters())
				return;
			if (SettingsManager.Options.HidGuardianConfigureAutomatically)
				UnhideAllDevices();
			ViGEm.HidGuardianHelper.RemoveCurrentProcessFromWhiteList();
		}

		/// <summary>
		/// Must be executed before program close.
		/// </summary>
		/// <returns></returns>
		public static bool UnhideAllDevices()
		{
			var affected = ViGEm.HidGuardianHelper.GetAffected();
			// Clear list of hidden devices.
			ViGEm.HidGuardianHelper.ClearAffected();
			var devices = SettingsManager.UserDevices.ItemsToArraySyncronized();
			// Unhide all devices.
			for (int i = 0; i < devices.Length; i++)
				devices[i].IsHidden = false;
			HidGuardianHelper.ResetDevices(affected);
			return true;
		}

		public static bool SynchronizeToHidGuardian(params Guid[] instanceGuids)
		{
			var game = SettingsManager.CurrentGame;
			// Affected devices.
			UserDevice[] devices;
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				devices = instanceGuids == null || instanceGuids.Length == 0
					? SettingsManager.UserDevices.Items.ToArray()
					: SettingsManager.UserDevices.Items.Where(x => instanceGuids.Contains(x.InstanceGuid)).ToArray();
			}
			// Get all Ids.
			var idsToHide = new List<string>();
			var idsToShow = new List<string>();
			foreach (var ud in devices)
			{
				var hardwareId = (ud.HidHardwareIds ?? "")
					// Split lines into arraty and exclude empty ones.
					.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					// Get all Hardware IDs with vendor code and product code.
					.Where(x => HidGuardianHelper.HardwareIdRegex.IsMatch(x)).ToList()
					// Put longest ID on top.
					.OrderByDescending(x => x)
					// Take most detail Hardware ID.
					.FirstOrDefault();
				// If hardware is not available then create from device id.
				if (string.IsNullOrEmpty(hardwareId) && !string.IsNullOrEmpty(ud.DevDeviceId))
					hardwareId = HidGuardianHelper.ConvertToHidVidPid(ud.DevDeviceId).FirstOrDefault();
				if (string.IsNullOrEmpty(hardwareId))
					continue;
				// If must hide and device is not keyboard or mouse.
				if (ud.IsHidden && !ud.IsKeyboard && !ud.IsMouse)
					idsToHide.Add(hardwareId);
				else if(!ud.IsHidden)
					idsToShow.Add(hardwareId);
			}
			var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
			if (canModify)
			{
				var idsToHide2 = idsToHide.Distinct().ToArray();
				var idsToShow2 = idsToShow.Distinct().ToArray();
				ViGEm.HidGuardianHelper.RemoveFromAffected(idsToShow2);
				ViGEm.HidGuardianHelper.InsertToAffected(idsToHide2);
			}
			return canModify;
		}

		#endregion

		#region Grid cells

		/// <summary>The lamp beside a device: lit while it is plugged in, grey while it is away.</summary>
		/// <param name="online">Whether the device is present.</param>
		/// <summary>What a grid row is showing, or null when the list behind it no longer has it.</summary>
		/// <remarks>
		/// A grid keeps its rows for a moment after the list behind them has shrunk, and goes on
		/// drawing and measuring them in that moment: the row under the pointer is measured on
		/// every mouse move. Asking such a row what it is showing fails, because the binding
		/// counts fewer items than the grid has rows. So the list is asked first.
		/// </remarks>
		public static T BoundItem<T>(System.Windows.Forms.DataGridView grid, int rowIndex) where T : class
		{
			var items = grid.DataSource as IList;
			if (items is null || rowIndex < 0 || rowIndex >= items.Count || rowIndex >= grid.Rows.Count)
				return null;
			return grid.Rows[rowIndex].DataBoundItem as T;
		}

		/// <summary>Every XInput place a game can feel a device through, as it reads in a list.</summary>
		/// <remarks>
		/// There are two ways in, and a device can use both at once.
		///
		/// An Xbox controller is one piece of hardware with two faces, one read through DirectInput and
		/// one through XInput, so it sits in a place of its own and a game reads it there whether this
		/// program is running or not. Nothing else has that second face. Where two of them share the
		/// places left over, neither can be named and neither is - a place stated wrongly is worse than
		/// one left blank, because somebody would map a controller against it.
		///
		/// Everything else reaches a game only through the controller tabs it is mapped to, each of which
		/// has a place of its own. A device mapped to two tabs is felt in two places at once and both are
		/// named: a device left mapped to a tab somebody has forgotten about is still driving it.
		///
		/// Asked here rather than by each list. Three lists show this column and each had its own answer;
		/// the one on the controller tabs asked only about the place the device holds itself, so every
		/// device that was not an Xbox controller read as blank on the very page where it was mapped.
		/// </remarks>
		public static string GetXInputPlaces(Engine.Data.UserDevice device)
		{
			if (device == null)
				return string.Empty;
			var carried = new List<int>();
			var helper = Global.DHelper;
			var fileName = SettingsManager.CurrentGame?.FileName;
			if (helper != null && fileName != null)
				foreach (var setting in SettingsManager.GetSettings(fileName))
					if (setting.InstanceGuid == device.InstanceGuid
						&& setting.MapTo >= 1 && setting.MapTo <= helper.XiPlaceForPad.Length)
						carried.Add(helper.XiPlaceForPad[setting.MapTo - 1]);
			var own = XInputPlaces.PlaceFor(device.HidDeviceId, device.DevDeviceId);
			return XInputPlaces.Describe(own,
				XInputPlaces.IsMadeNotPluggedIn(device.HidDeviceId, device.DevDeviceId),
				XInputPlaces.IsOneOfOurs(device.HidDeviceId, device.DevDeviceId),
				carried);
		}

		/// <summary>Which XInput place a controller tab passes force feedback on to, or -1 for none.</summary>
		/// <remarks>
		/// An Xbox controller offers its motors through XInput and nowhere else - its DirectInput face
		/// declares no force feedback at all - so a game rumbling an emulated controller reaches nothing
		/// the person is holding. Passing it on is the only way the force arrives.
		///
		/// Where to send it is either said outright or worked out. Said outright is one of the four
		/// places. Worked out means the place the mapped device itself holds, which is the sensible
		/// answer and the only one that stays right when the places move about.
		/// </remarks>
		public static int GetForcePassThroughPlace(MapTo mapTo)
		{
			var fileName = SettingsManager.CurrentGame?.FileName;
			if (fileName == null)
				return -1;
			foreach (var setting in SettingsManager.GetSettings(fileName, mapTo))
			{
				var ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
				if (ps == null || ps.ForcePassThrough != "1")
					continue;
				int wanted;
				// One to four names a place outright. Zero, empty, or anything unreadable means work it out.
				if (int.TryParse(ps.ForcePassThroughIndex, out wanted) && wanted >= 1 && wanted <= 4)
					return wanted - 1;
				var device = SettingsManager.GetDevice(setting.InstanceGuid);
				if (device == null)
					continue;
				// The place the device itself holds. Only a controller with an XInput face has one, which is
				// exactly the kind whose motors cannot be reached any other way.
				var place = XInputPlaces.PlaceFor(device.HidDeviceId, device.DevDeviceId);
				if (place >= 0)
					return place;
			}
			return -1;
		}

		#region Status lights

		/// <summary>The colours a status light is drawn in, as a person would name them.</summary>
		/// <remarks>
		/// Pastel and flat rather than glass. The glass ones spent their height on a near-black rim and a
		/// white highlight, leaving about a third of the icon showing the colour at all, and at arm's
		/// length green and grey were the same dark smudge. Colour is the whole message here, so the
		/// colour is what the icon is mostly made of.
		///
		/// The warm three are a ramp rather than three unrelated warnings: amber, orange, red, in that
		/// order, with less green in each. More red means more wrong, so severity is legible without
		/// reading anything. Grey is kept noticeably paler than the rest so that "nothing here" reads
		/// as absence at a glance rather than as one more colour to tell apart.
		/// </remarks>
		public const string StatusGreen = "#5FBF60";
		public const string StatusRed = "#D95C52";
		public const string StatusAmber = "#EFC94C";
		public const string StatusOrange = "#E08A38";
		public const string StatusBlue = "#6FA8DC";
		public const string StatusGrey = "#CFD4D8";

		/// <summary>The warm ramp, mildest first. A light is somewhere along it.</summary>
		static readonly string[] Ramp = { StatusGreen, StatusAmber, StatusOrange, StatusRed };

		/// <summary>The colour for a controller with this much wrong with it.</summary>
		/// <remarks>
		/// Five fixed lights could say which of five states a controller was in, and no more. A controller
		/// can have more than one thing wrong at once, and two faults were shown exactly as one - so the
		/// tab that most needed looking at was indistinguishable from the tab that least did.
		///
		/// So the colour is mixed rather than chosen: none the wrong side of green, everything wrong at
		/// red, and the mixture in between. Somebody can pick the worst tab out of four without reading a
		/// word, which is the one thing colour does better than words.
		/// </remarks>
		/// <param name="severity">Nothing wrong at 0, everything wrong at 1.</param>
		public static string StatusColor(double severity)
		{
			if (severity <= 0)
				return Ramp[0];
			if (severity >= 1)
				return Ramp[Ramp.Length - 1];
			var steps = Ramp.Length - 1;
			var at = severity * steps;
			var step = (int)at;
			var into = at - step;
			var from = ColorTranslator.FromHtml(Ramp[step]);
			var to = ColorTranslator.FromHtml(Ramp[step + 1]);
			var mixed = Color.FromArgb(
				(int)(from.R + (to.R - from.R) * into),
				(int)(from.G + (to.G - from.G) * into),
				(int)(from.B + (to.B - from.B) * into));
			return ColorTranslator.ToHtml(mixed);
		}

		static readonly Dictionary<string, Bitmap> StatusIcons = new Dictionary<string, Bitmap>();

		/// <summary>A status light in the colour given, as "#RRGGBB".</summary>
		/// <remarks>
		/// Drawn rather than kept as a picture, so a new state costs a colour rather than an image file,
		/// and every light is the same shape by construction instead of by whoever drew them agreeing.
		///
		/// Kept once made. A cell or a tab asks for one many times a second, and a bitmap made for a
		/// single paint is never given back.
		/// </remarks>
		/// <param name="hex">The colour, as "#RRGGBB" or "RRGGBB".</param>
		/// <param name="size">Width and height in pixels.</param>
		public static Bitmap GetStatusIcon(string hex, int size = 16)
		{
			return GetStatusIcon(hex, hex, size);
		}
		
		/// <summary>A light in two colours, left half and right half.</summary>
		/// <remarks>
		/// A controller tab answers two questions at once - is a device of yours connected, and is there
		/// an emulated controller for a game to read - and one colour had to stand for both. So the state
		/// somebody most needs, which half is missing, was the one thing the light could not say, and the
		/// words underneath had to say it instead.
		///
		/// Two halves say it without words: your device on the left, the emulated controller on the right.
		/// Both green and it is working, and looks exactly as a single green light always did.
		/// </remarks>
		public static Bitmap GetStatusIcon(string leftHex, string rightHex, int size = 16)
		{
			var key = leftHex + "|" + rightHex + "@" + size;
			lock (StatusIcons)
			{
				Bitmap kept;
				if (StatusIcons.TryGetValue(key, out kept))
					return kept;
				var made = DrawStatusIcon(leftHex, rightHex, size);
				StatusIcons[key] = made;
				return made;
			}
		}
		/// <summary>How light a colour reads, on the measure eyes actually use.</summary>
		static double Lightness(Color c)
		{
			return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
		}


		static Bitmap DrawStatusIcon(string leftHex, string rightHex, int size)
		{
			var left = ColorTranslator.FromHtml(leftHex.StartsWith("#") ? leftHex : "#" + leftHex);
			var right = ColorTranslator.FromHtml(rightHex.StartsWith("#") ? rightHex : "#" + rightHex);
			// The edge takes after the darker half, so it belongs to the colour rather than outlining it
			// in black.
			var darker = Lightness(left) <= Lightness(right) ? left : right;
			var edge = Color.FromArgb(darker.A, darker.R * 7 / 10, darker.G * 7 / 10, darker.B * 7 / 10);
			var image = new Bitmap(size, size);
			using (var g = Graphics.FromImage(image))
			{
				// Square, and the size the lights have always been: ten pixels across in the middle of
				// sixteen. Filling the whole icon made it bigger than everything beside it, and rounding
				// the corners spent its few pixels on a curve nobody can see at this size.
				var inset = Math.Max(1, size * 3 / 16);
				var side = Math.Max(4, size - inset * 2);
				// Left crisp on purpose. Smoothing a ten pixel square only blurs the one edge it has.
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				var half = side / 2;
				using (var brush = new SolidBrush(left))
					g.FillRectangle(brush, new Rectangle(inset, inset, half, side));
				using (var brush = new SolidBrush(right))
					g.FillRectangle(brush, new Rectangle(inset + half, inset, side - half, side));
				// A hint of gloss across the top, not a window pane. Enough to stop it looking printed on,
				// little enough that the colour underneath is still the colour.
				using (var sheen = new SolidBrush(Color.FromArgb(46, Color.White)))
					g.FillRectangle(sheen, new Rectangle(inset, inset, side, Math.Max(1, side * 2 / 5)));
				using (var pen = new Pen(edge, 1f))
					g.DrawRectangle(pen, new Rectangle(inset, inset, side - 1, side - 1));
			}
			return image;
		}

		#endregion

		public static Bitmap GetOnlineIcon(bool online)
		{
			return online
				? GetStatusIcon(StatusGreen)
				: GetStatusIcon(StatusGrey);
		}

		/// <summary>The icon of the port a device is attached through, or a blank of the same size.</summary>
		/// <remarks>
		/// One blank is kept and handed out again. A cell is drawn many times a second, and a bitmap made
		/// for a single paint is never given back.
		/// </remarks>
		/// <param name="connectionClass">The device class of the port, or empty when it is not known.</param>
		public static Bitmap GetConnectionClassIcon(Guid connectionClass)
		{
			if (connectionClass == Guid.Empty)
				return BlankIcon;
			return JocysCom.ClassLibrary.IO.DeviceDetector.GetClassIcon(connectionClass, 16)?.ToBitmap() ?? BlankIcon;
		}

		private static readonly Bitmap BlankIcon = new Bitmap(16, 16);

		#endregion

	}
}
