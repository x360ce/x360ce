using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using x360ce.Engine;
using System.Security.AccessControl;
using System.Security.Principal;
using JocysCom.ClassLibrary.Win32;

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
					UsagePage = (ushort)o.UsagePage,
					Usage = (ushort)o.Usage,

				};
				items.Add(item);
			}
			return items.ToArray();
		}

		#endregion

		// Use cache so same image won't processed multiple times.
		static readonly Dictionary<Bitmap, Bitmap> DisabledImageCache = new Dictionary<Bitmap, Bitmap>();
		static readonly object DisabledImageLock = new object();

		/// <summary>
		/// Generates disabled Image. Images are cached so do not use method for random images.
		/// </summary>
		/// <remarks>
		/// Made faded (<see cref="EngineHelper.Faded"/>) from the image as it was drawn at 100% and at each larger size
		/// it was drawn at, and then scaled to the size of the one given, so a faded picture is as sharp as the full one
		/// and is known as enlarged, never enlarged again.
		/// </remarks>
		public static Bitmap GetDisabledImage(Bitmap image)
		{
			lock (DisabledImageLock)
			{
				if (!DisabledImageCache.ContainsKey(image))
				{
					var original = JocysCom.ClassLibrary.Controls.ControlsHelper.GetOriginal(image);
					var faded = EngineHelper.Faded(original);
					JocysCom.ClassLibrary.Controls.ControlsHelper.SetDrawnSizes(faded,
						JocysCom.ClassLibrary.Controls.ControlsHelper.GetDrawnSizes(original).Select(EngineHelper.Faded).ToArray());
					DisabledImageCache.Add(image, (Bitmap)JocysCom.ClassLibrary.Controls.ControlsHelper.ScaleImage(faded, image.Size));
				}
				return DisabledImageCache[image];
			}
		}

		// Use special function or comparison fails.
		public static bool IsSameDevice(Device device, Guid instanceGuid)
		{
			return instanceGuid.Equals(device == null ? Guid.Empty : device.Information.InstanceGuid);
		}

		public static string[] GetFiles(string path, string searchPattern, bool allDirectories = false)
		{
			var dir = new DirectoryInfo(path);
			var fis = new List<FileInfo>();
			AppHelper.GetFiles(dir, ref fis, searchPattern, false);
			return fis.Select(x => x.FullName).ToArray();
		}

		public static void GetFiles(DirectoryInfo di, ref List<FileInfo> fileList, string searchPattern, bool allDirectories)
		{
			try
			{
				if (allDirectories)
				{
					foreach (DirectoryInfo subDi in di.GetDirectories())
					{
						GetFiles(subDi, ref fileList, searchPattern, allDirectories);
					}
				}
			}
			catch { }
			try
			{
				fileList.AddRange(di.GetFiles(searchPattern));
			}
			catch { }
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

	}
}
