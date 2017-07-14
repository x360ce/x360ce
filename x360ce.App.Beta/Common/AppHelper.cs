using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using x360ce.Engine;
using x360ce.Engine.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using x360ce.Engine.Data;
using System.Linq.Expressions;

namespace x360ce.App
{
	public class AppHelper
	{
		#region DLL Functions

		static void Elevate()
		{
			// If this is Vista/7 and is not elevated then elevate.
			if (WinAPI.IsVista && !WinAPI.IsElevated()) WinAPI.RunElevated();
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
				if (count == 0) break;
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
			var og = typeof(SharpDX.DirectInput.ObjectGuid);
			var guidFileds = og.GetFields().Where(x => x.FieldType == typeof(Guid));
			List<Guid> guids = guidFileds.Select(x => (Guid)x.GetValue(og)).ToList();
			List<string> names = guidFileds.Select(x => x.Name).ToList();
			var objects = device.GetObjects(DeviceObjectTypeFlags.All).OrderBy(x => x.Offset).ToArray();
			var items = new List<DeviceObjectItem>();
			foreach (var o in objects)
			{
				var item = new DeviceObjectItem()
				{
					Name = o.Name,
					Offset = o.Offset,
					Instance = o.ObjectId.InstanceNumber,
					Usage = o.Usage,
					Aspect = o.Aspect,
					Flags = o.ObjectId.Flags,
					GuidValue = o.ObjectType,
					GuidName = guids.Contains(o.ObjectType) ? names[guids.IndexOf(o.ObjectType)] : o.ObjectType.ToString(),
				};
				items.Add(item);
			}
			return items.ToArray();
		}

		#endregion

		public static Bitmap GetDisabledImage(Bitmap image)
		{
			var effects = new JocysCom.ClassLibrary.Drawing.Effects();
			var newImage = (Bitmap)image.Clone();
			effects.GrayScale(newImage);
			effects.Transparent(newImage, 50);
			return newImage;
		}

		// Use special function or comparison fails.
		public static bool IsSameDevice(Device device, Guid instanceGuid)
		{
			return instanceGuid.Equals(device == null ? Guid.Empty : device.Information.InstanceGuid);
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
			if (source == null) source = new List<T>();
			var sCount = source.Count;
			var dCount = destination.Count;
			var length = Math.Min(sCount, dCount);
			for (int i = 0; i < length; i++) destination[i] = source[i];
			// Add extra rows.
			if (sCount > dCount)
			{
				for (int i = dCount; i < sCount; i++) destination.Add(source[i]);
			}
			else if (dCount > sCount)
			{
				for (int i = dCount - 1; i >= sCount; i--) destination.RemoveAt(i);
			}
		}

		public static Engine.Data.Setting GetNewSetting(UserDevice device, Engine.Data.UserGame game, MapTo mapTo)
		{
			// Create new setting for game/device.
			var newSetting = new Engine.Data.Setting();
			newSetting.InstanceGuid = device.InstanceGuid;
			newSetting.InstanceName = device.InstanceName;
			newSetting.ProductGuid = device.ProductGuid;
			newSetting.ProductName = device.ProductName;
			newSetting.DeviceType = (int)device.CapType;
			newSetting.FileName = game.FileName;
			newSetting.FileProductName = game.FileProductName;
			newSetting.DateCreated = DateTime.Now;
			newSetting.IsEnabled = true;
			newSetting.MapTo = (int)mapTo;
			return newSetting;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetEnabled(Control control, bool enabled)
		{
			if (control.Enabled != enabled) control.Enabled = enabled;
		}

		public static void SetEnabled(ToolStripButton control, bool enabled)
		{
			if (control.Enabled != enabled) control.Enabled = enabled;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(Control control, string format, params object[] args)
        {
            var text = (args == null)
                ? format
                : string.Format(format, args);
            if (control.Text != text) control.Text = text;
        }

        /// <summary>
        /// Change value if it is different only.
        /// This helps not to trigger control events when doing frequent events.
        /// </summary>
        public static void SetText(ToolStripItem control, string format, params object[] args)
		{
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Text != text) control.Text = text;
		}

        /// <summary>
        /// Change value if it is different only.
        /// This helps not to trigger control events when doing frequent events.
        /// </summary>
        public static void SetChecked(CheckBox control, bool check)
        {
            if (control.Checked != check) control.Checked = check;
        }

		public static MapToMask GetMapFlag(MapTo mapTo)
		{
			switch (mapTo)
			{
				case MapTo.Controller1: return MapToMask.Controller1;
				case MapTo.Controller2: return MapToMask.Controller2;
				case MapTo.Controller3: return MapToMask.Controller3;
				case MapTo.Controller4: return MapToMask.Controller4;
				default: return MapToMask.None;
			}
		}

		public static string GetPropertyName<T>(Expression<Func<T, object>> selector)
		{
			var body = (MemberExpression)((UnaryExpression)selector.Body).Operand;
			var name = body.Member.Name;
			return name;
		}

	}
}
