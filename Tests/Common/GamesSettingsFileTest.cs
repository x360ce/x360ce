// @under-test: Engine/Data/Program.cs, App.v4/Controls/ProgramsGridUserControl.cs, App.v3/Controls/GameSettingsUserControl.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EngineProgram = x360ce.Engine.Data.Program;

namespace x360ce.Tests
{
	/// <summary>
	/// A games settings file chosen for import: what Export writes reads back, and anything else is
	/// refused with a sentence that says so.
	/// </summary>
	/// <remarks>
	/// People send each other these files, so a chosen file can be anything. One that is not a games
	/// settings file, XML or not, is refused with a message rather than with the XML reader's own error. The
	/// file is read with document type definitions ignored and no resolver, so an entity cannot pull another
	/// file in.
	/// </remarks>
	[TestClass]
	public class GamesSettingsFileTest
	{
		string _folder;

		[TestInitialize]
		public void Before()
		{
			_folder = Path.Combine(Path.GetTempPath(), "x360ce-games-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_folder);
		}

		[TestCleanup]
		public void After()
		{
			Directory.Delete(_folder, true);
		}

		string Write(string name, byte[] bytes)
		{
			var path = Path.Combine(_folder, name);
			File.WriteAllBytes(path, bytes);
			return path;
		}

		string Write(string name, string text)
		{
			return Write(name, Encoding.UTF8.GetBytes(text));
		}

		static List<EngineProgram> TwoGames()
		{
			return new List<EngineProgram>
			{
				new EngineProgram { FileName = "game.exe", FileProductName = "Game" },
				new EngineProgram { FileName = "other.exe", FileProductName = "Other" },
			};
		}

		static void AssertRefused(string path, string why)
		{
			try
			{
				EngineProgram.FromFile(path);
			}
			catch (InvalidDataException ex)
			{
				StringAssert.StartsWith(ex.Message, EngineProgram.NotAGamesSettingsFileMessage, why);
				return;
			}
			Assert.Fail(why + " It was read as games settings.");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("What ToFile writes, plain or compressed, reads back through FromFile as the same games")]
		public void Exported_files_read_back()
		{
			var plain = Path.Combine(_folder, "x360ce_Games.xml");
			var packed = Path.Combine(_folder, "x360ce_Games.xml.gz");
			EngineProgram.ToFile(TwoGames(), plain);
			EngineProgram.ToFile(TwoGames(), packed);
			foreach (var path in new[] { plain, packed })
				CollectionAssert.AreEqual(new[] { "game.exe", "other.exe" },
					EngineProgram.FromFile(path).Select(x => x.FileName).ToArray(), Path.GetFileName(path));
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A file that is not XML at all is refused with a sentence, not raised from the button")]
		public void Text_that_is_not_xml_is_refused()
		{
			// The reported file stopped the reader before its first element.
			AssertRefused(Write("notes.xml", "  Not a settings file.\r\n"), "Plain text.");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("An empty file, XML of another kind, an entry with no program file and a plain file named .gz are refused")]
		public void Other_files_are_refused()
		{
			AssertRefused(Write("empty.xml", new byte[0]), "An empty file.");
			AssertRefused(Write("preset.xml", "<PadSetting><ButtonA>1</ButtonA></PadSetting>"), "A preset.");
			var namelessPath = Path.Combine(_folder, "nameless.xml");
			EngineProgram.ToFile(new List<EngineProgram> { new EngineProgram { FileProductName = "No file" } }, namelessPath);
			AssertRefused(namelessPath, "An entry that names no program file cannot be merged.");
			var plainPath = Path.Combine(_folder, "plain-source.xml");
			EngineProgram.ToFile(TwoGames(), plainPath);
			AssertRefused(Write("plain.xml.gz", File.ReadAllBytes(plainPath)), "A plain file named as compressed.");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("An entity in the file cannot read another file")]
		public void An_entity_cannot_reach_another_file()
		{
			var secret = Write("secret.txt", "SECRET-CONTENT");
			var exportedPath = Path.Combine(_folder, "exported-source.xml");
			EngineProgram.ToFile(TwoGames(), exportedPath);
			var xml = File.ReadAllText(exportedPath, Encoding.UTF8).Replace("<FileName>game.exe</FileName>", "<FileName>&x;</FileName>");
			Assert.IsTrue(xml.Contains("&x;"), "Export no longer writes FileName as an element, so this measured nothing.");
			xml = "<!DOCTYPE ArrayOfProgram [<!ENTITY x SYSTEM \"" + new Uri(secret).AbsoluteUri + "\">]>" + xml;
			var path = Write("entity.xml", xml);
			try
			{
				var games = EngineProgram.FromFile(path);
				Assert.IsFalse(games.Any(x => (x.FileName ?? "").Contains("SECRET")), "The file pulled another file's content in.");
				Assert.Fail("A file whose entity was left undefined was read as games settings.");
			}
			catch (InvalidDataException ex)
			{
				Assert.IsFalse(ex.Message.Contains("SECRET"), "The refusal repeats another file's content.");
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A read-only destination raises the exception type the grids catch, not silence")]
		public void A_readonly_destination_is_refused()
		{
			var path = Write("locked.xml", new byte[0]);
			File.SetAttributes(path, FileAttributes.ReadOnly);
			try
			{
				EngineProgram.ToFile(TwoGames(), path);
				Assert.Fail("A read-only destination was written.");
			}
			catch (UnauthorizedAccessException)
			{
				// The type both App.v4 and App.v3 catch around ToFile.
			}
			finally
			{
				File.SetAttributes(path, FileAttributes.Normal);
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("Both programs read and write through FromFile and ToFile, and say why a file was refused")]
		public void The_grids_say_why_a_file_was_refused()
		{
			foreach (var parts in new[]
			{
				new[] { "App.v4", "Controls", "ProgramsGridUserControl.cs" },
				new[] { "App.v3", "Controls", "GameSettingsUserControl.cs" },
			})
			{
				var name = string.Join("/", parts);
				var source = File.ReadAllText(Path.Combine(new[] { Ui.RepoRoot.FullName }.Concat(parts).ToArray()));
				StringAssert.Contains(source, "x360ce.Engine.Data.Program.FromFile(dialog.FileName)", name + " reads the file some other way.");
				StringAssert.Contains(source,
					"catch (Exception ex) when (ex is System.IO.InvalidDataException || ex is System.IO.IOException || ex is UnauthorizedAccessException)",
					name + " raises a refused import from the button.");
				StringAssert.Contains(source, "x360ce.Engine.Data.Program.ToFile(programs, dialog.FileName)", name + " writes the file some other way.");
				Assert.IsFalse(source.Contains("DeserializeFromXmlFile<List<x360ce.Engine.Data.Program>>"), name + " still reads the file itself.");
			}
		}
	}
}
