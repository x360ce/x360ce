using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace x360ce.Engine.Data
{
	public partial class Program: IProgram
	{
		public static Program FromDisk(string fileName)
		{
			var item = new Program();
			var fi = new FileInfo(fileName);
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			item.Comment = vi.Comments;
			item.DateCreated = DateTime.Now;
			item.DateUpdated = item.DateCreated;
			item.FileName = fi.Name;
			item.FileProductName = EngineHelper.FixName(vi.ProductName, item.FileName);
			item.InstanceCount = 0;
			item.IsEnabled = true;
			item.ProgramId = Guid.NewGuid();
			item.HookMask = 0;
			item.XInputMask = 0;
			item.XInputPath = "";
			item.DInputMask = 0;
			item.DInputFile = "";
			item.FakeVID = 0;
			item.FakePID = 0;
			item.Timeout = -1;
			item.Weight = 1;
			return item;
		}

		/// <summary>What an import of a file that holds no games settings says.</summary>
		public const string NotAGamesSettingsFileMessage = "The file is not a games settings file.";

		/// <summary>Reads a games settings file as Export writes it: XML, or XML compressed with GZip when the name ends in .gz.</summary>
		/// <remarks>
		/// People send each other these files, so the XML goes through the shared reader, which ignores
		/// document type definitions and has no resolver: an entity in the file reaches neither another
		/// file nor the network. Every entry must name its program file, the key the list is merged on.
		/// </remarks>
		/// <exception cref="InvalidDataException">The file is not a games settings file.</exception>
		/// <exception cref="IOException">The file could not be read.</exception>
		/// <exception cref="UnauthorizedAccessException">The file may not be read.</exception>
		public static List<Program> FromFile(string path)
		{
			var bytes = File.ReadAllBytes(path);
			List<Program> programs;
			try
			{
				if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
					bytes = EngineHelper.Decompress(bytes);
				programs = bytes.Length == 0
					? null
					: JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlBytes<List<Program>>(bytes);
			}
			catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
			{
				throw new InvalidDataException(NotAGamesSettingsFileMessage + " " + ex.GetBaseException().Message, ex);
			}
			if (programs == null || programs.Any(x => x == null || string.IsNullOrEmpty(x.FileName)))
				throw new InvalidDataException(NotAGamesSettingsFileMessage);
			return programs;
		}

		/// <summary>Writes games settings as Import reads them: XML, or XML compressed with GZip when the name ends in .gz.</summary>
		/// <exception cref="IOException">The file could not be written.</exception>
		/// <exception cref="UnauthorizedAccessException">The file may not be written.</exception>
		public static void ToFile(IEnumerable<Program> programs, string path)
		{
			if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
			{
				var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(programs, System.Text.Encoding.UTF8, true);
				var bytes = EngineHelper.Compress(System.Text.Encoding.UTF8.GetBytes(xml));
				File.WriteAllBytes(path, bytes);
			}
			else
			{
				JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlFile(programs, path, System.Text.Encoding.UTF8, true);
			}
		}

	}
}
