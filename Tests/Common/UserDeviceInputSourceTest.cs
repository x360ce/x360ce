// @under-test: Engine/Common/DatabaseHelper.cs, App.v4/Common/CloudClient.cs, Engine/Data/x360ceModel.ssdl, Engine/Data/x360ceModel.csdl, Engine/Data/x360ceModel.msl, Data/dbo/Tables/x360ce_UserDevices.sql
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A device's input source, DirectInput or Raw Input, on its way between the program and the database.
	/// </summary>
	/// <remarks>
	/// Every device stored before 4.25 is a DirectInput device, and every program before 4.25 reads DirectInput
	/// devices only and sends no input source. The database stores DirectInput as 1. The program keeps it at 0, so
	/// its files and checksums stay as they were. The web service translates between the two and returns a program
	/// only the kinds of device it says it reads. The service's rules are measured here without a database, and the
	/// model the program runs from is measured without a server.
	/// </remarks>
	[TestClass]
	public class UserDeviceInputSourceTest
	{
		const int DirectInput = (int)InputSourceType.DirectInput;
		const int RawInput = (int)InputSourceType.RawInput;

		static UserDevice Device(int inputSource)
		{
			var device = UserDeviceTest.FixedDevice();
			device.InputSourceType = inputSource;
			return device;
		}

		/// <summary>The values of a message from a program that reads the given kinds of device.</summary>
		static KeyValueList Reads(InputSourceType kinds)
		{
			var values = new KeyValueList();
			values.Add(CloudKey.InputSourceTypes, (int)kinds);
			return values;
		}

		static int[] InputSources(UserDevice[] devices)
		{
			return devices.Select(x => x.InputSourceType).ToArray();
		}

		#region Web service

		[TestMethod, TestCategory("devices"), TestCategory("webservice"), TestCategory("critical")]
		[Description("A device sent without an input source is stored as DirectInput")]
		public void A_device_sent_without_an_input_source_is_stored_as_DirectInput()
		{
			// Every program before 4.25 sends none, and this one sends none for a DirectInput device.
			var devices = new[] { Device(0), Device(DirectInput), Device(RawInput) };
			DatabaseHelper.StoreInputSource(devices);
			CollectionAssert.AreEqual(new[] { DirectInput, DirectInput, RawInput }, InputSources(devices));
		}

		[TestMethod, TestCategory("devices"), TestCategory("webservice"), TestCategory("critical")]
		[Description("A program that names no input source gets DirectInput devices only, handed back at 0")]
		public void A_program_that_names_no_input_source_gets_DirectInput_devices_only()
		{
			// Every program before 4.25. A Raw Input device would be one it cannot read, kept in its file for good.
			// A row at 0 counts as DirectInput, though the column's default and StoreInputSource leave none.
			var stored = new[] { Device(DirectInput), Device(RawInput), Device(0) };
			var returned = DatabaseHelper.FilterByInputSource(stored, new KeyValueList());
			CollectionAssert.AreEqual(new[] { stored[0], stored[2] }, returned,
				"A program that reads DirectInput only was given: " + string.Join(", ", InputSources(returned)) + ".");
			CollectionAssert.AreEqual(new[] { 0, 0 }, InputSources(returned),
				"A DirectInput device is handed back as the program keeps it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("webservice"), TestCategory("critical")]
		[Description("A Raw Input device goes only to a program that reads Raw Input")]
		public void A_Raw_Input_device_goes_only_to_a_program_that_reads_Raw_Input()
		{
			var both = DatabaseHelper.FilterByInputSource(new[] { Device(DirectInput), Device(RawInput) },
				Reads(InputSourceType.DirectInput | InputSourceType.RawInput));
			CollectionAssert.AreEqual(new[] { 0, RawInput }, InputSources(both), "A program that reads both.");
			var direct = DatabaseHelper.FilterByInputSource(new[] { Device(DirectInput), Device(RawInput) },
				Reads(InputSourceType.DirectInput));
			CollectionAssert.AreEqual(new[] { 0 }, InputSources(direct), "A program that reads DirectInput only.");
			var raw = DatabaseHelper.FilterByInputSource(new[] { Device(DirectInput), Device(RawInput) },
				Reads(InputSourceType.RawInput));
			CollectionAssert.AreEqual(new[] { RawInput }, InputSources(raw), "A program that reads Raw Input only.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("webservice"), TestCategory("critical")]
		[Description("A stored DirectInput device comes back with the checksum the program took, so it is not sent again")]
		public void A_stored_DirectInput_device_is_not_sent_again()
		{
			// The program sends the checksums of the devices it has. The server stores the device at 1 and
			// measures it again before it answers, so it has to measure it as the program does.
			var mine = Device(0);
			var checksums = EngineHelper.UpdateChecksums(new[] { mine }).ToArray();
			Assert.AreEqual(UserDeviceTest.FixedDeviceChecksum, mine.Checksum);
			var stored = new[] { Device(0) };
			DatabaseHelper.StoreInputSource(stored);
			string error;
			var sent = DatabaseHelper.FilterByChecksum(DatabaseHelper.FilterByInputSource(stored, new KeyValueList()), checksums, out error);
			Assert.AreEqual(0, sent.Length, "An unchanged DirectInput device is sent again (" + error + ").");
		}

		#endregion

		#region Program

		[TestMethod, TestCategory("devices"), TestCategory("webservice"), TestCategory("critical")]
		[Description("The program tells the web service it reads DirectInput and Raw Input devices, as a number")]
		public void The_program_says_it_reads_DirectInput_and_Raw_Input()
		{
			var message = new CloudMessage(CloudAction.Select);
			CloudClient.AddClientValues(message);
			// Written and read as the web service client and the web service write and read a message.
			var writer = new StringWriter();
			var serializer = new XmlSerializer(typeof(CloudMessage));
			serializer.Serialize(writer, message);
			var received = (CloudMessage)serializer.Deserialize(new StringReader(writer.ToString()));
			var value = received.Values.Single(x => Equals(x.Key, CloudKey.InputSourceTypes)).Value;
			Assert.IsInstanceOfType(value, typeof(int), "The value arrives as " + value.GetType().Name + ".");
			Assert.AreEqual((int)(InputSourceType.DirectInput | InputSourceType.RawInput), (int)value);
			var returned = DatabaseHelper.FilterByInputSource(new[] { Device(DirectInput), Device(RawInput) }, received.Values);
			CollectionAssert.AreEqual(new[] { 0, RawInput }, InputSources(returned), "What the web service returns to this program.");
		}

		#endregion

		#region Model

		[TestMethod, TestCategory("devices"), TestCategory("settings"), TestCategory("critical")]
		[Description("The model the program runs from reads the input source column, declared as the table declares it")]
		public void The_embedded_model_maps_the_input_source_column()
		{
			// The program runs from the three files embedded in the engine, not from the .edmx beside them.
			var engine = typeof(EngineHelper).Assembly;
			Func<string, XmlReader> open = name => XmlReader.Create(engine.GetManifestResourceStream(name));
			var conceptual = new EdmItemCollection(new[] { open("Data.x360ceModel.csdl") });
			var storage = new StoreItemCollection(new[] { open("Data.x360ceModel.ssdl") });
			new System.Data.Mapping.StorageMappingItemCollection(conceptual, storage, new[] { open("Data.x360ceModel.msl") });
			var column = storage.GetItems<EntityType>().Single(x => x.Name == "x360ce_UserDevices").Properties["InputSourceType"];
			Assert.AreEqual("int", column.TypeUsage.EdmType.Name);
			Assert.IsFalse(column.Nullable, "The column refuses null.");
			var property = conceptual.GetItems<EntityType>().Single(x => x.Name == "UserDevice").Properties["InputSourceType"];
			Assert.AreEqual("Int32", property.TypeUsage.EdmType.Name);
			Assert.IsFalse(property.Nullable, "The property refuses null.");
			Assert.IsNull(property.DefaultValue, "A default would start every new device at 1, which changes its checksum.");
			Assert.AreEqual(0, new UserDevice().InputSourceType, "A new device starts as the program keeps DirectInput.");
			// The mapping, measured by the query the web service runs. It is only compiled; no server is named.
			using (var db = new x360ceModelContainer("metadata=res://*/Data.x360ceModel.csdl|res://*/Data.x360ceModel.ssdl|res://*/Data.x360ceModel.msl;provider=System.Data.SqlClient;provider connection string=\"\""))
			{
				var sql = db.UserDevices.ToTraceString();
				StringAssert.Contains(sql, "[InputSourceType]", "The query of devices does not read the column: " + sql);
			}
			// The table, declared as the live script adds the column.
			var table = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "Data", "dbo", "Tables", "x360ce_UserDevices.sql"));
			StringAssert.Matches(table, new Regex(@"\[InputSourceType\]\s+INT\s+CONSTRAINT \[DF_x360ce_UserDevices_InputSourceType\] DEFAULT \(\(1\)\) NOT NULL,"),
				"The table file declares the column otherwise than the live script adds it.");
		}

		#endregion
	}
}
