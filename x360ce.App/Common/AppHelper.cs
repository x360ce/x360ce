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

	}
}
