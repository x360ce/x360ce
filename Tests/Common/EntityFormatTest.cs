// @under-test: Engine/Data/*.cs, Engine/Data/Model/dbo/*.cs, Engine/JocysCom/Runtime/RuntimeHelper.Entity.cs, Engine/Common/IWebService.cs, Engine/Common/WebServiceClient.cs, Web/WebServices/x360ce.asmx.cs
// @area: settings   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Services;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Serialization;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// What the data classes write where they leave the program: the settings files, the web service, and the text their
	/// checksums are made from.
	/// </summary>
	/// <remarks>
	/// x360ce 3 and 4 read their settings files with XmlSerializer, and every released program talks to the web service
	/// through these classes, so what they write is frozen. A program does not upload a record whose checksum the service
	/// already has; the checksum is made from the members marked [DataMember], without the key
	/// (<see cref="RuntimeHelper.GetDataMembersString"/>), so those marks are frozen too. Nothing serializes these classes
	/// with a DataContractSerializer.
	///
	/// The classes are generated from the database (<c>Engine\Data\Generate-Models.ps1</c>), so they write their elements in
	/// the order of the table's columns. XmlSerializer reads elements in any order, so a file or a message written in
	/// another order reads the same; a new column is a new element, which older programs pass over.
	///
	/// Each class is filled with values made from its property names and described here, and the description is compared
	/// with the file of the same name in <c>Common\EntityFormat</c>. A difference fails the test and leaves the new text in
	/// <c>Tests\TestResults\EntityFormat</c>; a change that is meant is accepted by copying that file over the recorded one.
	///
	/// The deployed service is still built on the Entity Framework designer's classes, and the programs ship before it
	/// changes. Replies it sent and the WSDL it publishes are in <c>Common\EntityFormat\OldService</c>: the program must read
	/// every value of those replies, and the service every value of the program's requests.
	/// </remarks>
	[TestClass]
	public class EntityFormatTest
	{
		static readonly Type[] Entities =
		{
			typeof(Layout), typeof(PadSetting), typeof(Product), typeof(x360ce.Engine.Data.Program), typeof(Summary),
			typeof(UserComputer), typeof(UserDevice), typeof(UserGame), typeof(UserInstance), typeof(UserProfile),
			typeof(UserSetting), typeof(Vendor),
		};

		const string WsdlFileName = "x360ce.wsdl";

		[TestMethod, TestCategory("settings"), TestCategory("webservice"), TestCategory("critical")]
		[Description("Each data class writes the settings XML and the checksum text it wrote before")]
		public void Each_data_class_writes_what_it_wrote_before()
		{
			var problems = new List<string>();
			foreach (var type in Entities)
				Compare(type.Name + ".txt", Describe(type), problems);
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("wsdl"), TestCategory("critical")]
		[Description("The web service contract the data classes make is the one they made before")]
		public void The_web_service_contract_is_what_it_was_before()
		{
			var wsdl = Wsdl();
			// The stand-in below must offer what the service offers, or it describes some other contract.
			var live = new XmlDocument();
			live.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebServices", "live.wsdl"));
			var mine = new XmlDocument();
			mine.LoadXml(wsdl);
			CollectionAssert.AreEquivalent(Operations(live).Concat(new[] { "GetServerInfo" }).ToArray(), Operations(mine),
				"The stand-in service does not offer the operations of x360ce.asmx.");
			var problems = new List<string>();
			Compare(WsdlFileName, wsdl, problems);
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("critical")]
		[Description("The web service's context maps only the columns of each data class")]
		public void The_context_maps_only_the_columns()
		{
			// Entity Framework maps every public property with a setter as a column, and every collection of objects as
			// a link to another table. The generated columns carry [DataMember]; a property the program adds in a
			// partial class carries [NotMapped], or the context refuses its model, or reads a column the table lacks.
			var problems = new List<string>();
			foreach (var type in Entities)
				foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					var mapped = p.GetSetMethod() != null
						|| p.PropertyType != typeof(string) && p.PropertyType != typeof(byte[])
							&& typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType);
					if (mapped && !Attribute.IsDefined(p, typeof(System.Runtime.Serialization.DataMemberAttribute))
						&& !Attribute.IsDefined(p, typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)))
						problems.Add(type.Name + "." + p.Name);
				}
			Assert.AreEqual(0, problems.Count, "Not columns, and not marked [NotMapped]: " + string.Join(", ", problems));
		}

		#region Description

		/// <summary>The XML, the [DataMember] text and the checksum of an instance of the class, filled from its property names.</summary>
		/// <remarks>Described in the invariant culture: the [DataMember] text writes dates and numbers in the current one.</remarks>
		public static string Describe(Type type)
		{
			var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			try
			{
				return DescribeInCurrentCulture(type);
			}
			finally
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = culture;
			}
		}

		static string DescribeInCurrentCulture(Type type)
		{
			var item = Populate(type);
			var sb = new StringBuilder();
			sb.AppendLine("== XmlSerializer");
			sb.AppendLine(Xml(item));
			sb.AppendLine("== [DataMember] properties, with the key");
			sb.Append(RuntimeHelper.GetDataMembersString(item, false, false));
			sb.AppendLine("== [DataMember] properties the checksum is made from");
			sb.Append(RuntimeHelper.GetDataMembersString(item, true, true));
			var record = item as IChecksum;
			if (record != null)
			{
				EngineHelper.UpdateChecksums(new[] { record });
				sb.AppendLine("== Checksum");
				sb.AppendLine(record.Checksum.ToString());
			}
			return sb.ToString();
		}

		/// <summary>A new instance with every public property a settings file can hold set from its name.</summary>
		static object Populate(Type type)
		{
			var item = Activator.CreateInstance(type);
			foreach (var p in Settable(type))
			{
				object value;
				if (TryMake(p.PropertyType, p.Name, out value))
					p.SetValue(item, value, null);
			}
			return item;
		}

		/// <summary>The public properties of the class a settings file can hold, by name.</summary>
		static IEnumerable<PropertyInfo> Settable(Type type)
		{
			return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(x => x.CanRead && x.GetSetMethod() != null && x.GetIndexParameters().Length == 0)
				.Where(x => !Attribute.IsDefined(x, typeof(XmlIgnoreAttribute)))
				.OrderBy(x => x.Name, StringComparer.Ordinal);
		}

		/// <summary>A value of the type made from the name alone, so adding or removing a property changes no other value.</summary>
		static bool TryMake(Type type, string name, out object value)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			var seed = Seed(name);
			value = null;
			if (type == typeof(string))
				value = name;
			else if (type == typeof(bool))
				value = true;
			else if (type == typeof(int))
				value = 2 + seed % 1000;
			else if (type == typeof(long))
				value = 2L + seed % 100000;
			else if (type == typeof(short))
				value = (short)(2 + seed % 1000);
			else if (type == typeof(byte))
				value = (byte)(2 + seed % 200);
			else if (type == typeof(decimal))
				value = (2 + seed % 1000) / 4m;
			else if (type == typeof(double))
				value = (2 + seed % 1000) / 4d;
			else if (type == typeof(Guid))
				value = NameGuid(name);
			else if (type == typeof(DateTime))
				value = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddMinutes(seed % 100000);
			else if (type == typeof(byte[]))
				value = Encoding.ASCII.GetBytes(name);
			return value != null;
		}

		static Guid NameGuid(string name)
		{
			using (var md5 = MD5.Create())
				return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(name)));
		}

		static int Seed(string name)
		{
			var h = 17;
			foreach (var c in name)
				h = unchecked(h * 31 + c);
			return h & 0x7fffffff;
		}

		static string Xml(object item)
		{
			var serializer = new XmlSerializer(item.GetType());
			var writer = new StringWriter();
			using (var xml = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
				serializer.Serialize(xml, item);
			return writer.ToString();
		}

		#endregion

		#region Web service contract

		/// <summary>The WSDL that ASMX writes for a service with the operations of x360ce.asmx.</summary>
		/// <remarks>
		/// The site's own class is not referenced here, so the stand-in below carries the same operations, names and
		/// namespace. Only the classes of the parameters and replies make the types and messages the programs read.
		/// </remarks>
		public static string Wsdl()
		{
			var reflector = new ServiceDescriptionReflector();
			reflector.Reflect(typeof(ContractService), "http://localhost/webservices/x360ce.asmx");
			var writer = new StringWriter();
			reflector.ServiceDescriptions[0].Write(writer);
			return writer.ToString();
		}

		static string[] Operations(XmlDocument doc)
		{
			var ns = new XmlNamespaceManager(doc.NameTable);
			ns.AddNamespace("wsdl", "http://schemas.xmlsoap.org/wsdl/");
			return doc.SelectNodes("//wsdl:portType[@name='x360ceSoap']/wsdl:operation", ns)
				.Cast<XmlElement>().Select(x => x.GetAttribute("name")).OrderBy(x => x).ToArray();
		}

		/// <summary>The operations of x360ce.asmx, with their parameters and replies; none of them runs.</summary>
		[WebService(Name = "x360ce", Namespace = "http://x360ce.com/")]
		[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
		public class ContractService : WebService, IWebService
		{
			[WebMethod] public string SaveSetting(UserSetting s, PadSetting ps) { throw new NotSupportedException(); }
			[WebMethod] public SearchResult SearchSettings(SearchParameter[] args) { throw new NotSupportedException(); }
			[WebMethod] public string DeleteSetting(UserSetting s) { throw new NotSupportedException(); }
			[WebMethod] public SearchResult LoadSetting(Guid[] checksum) { throw new NotSupportedException(); }
			[WebMethod] public List<x360ce.Engine.Data.Program> GetPrograms(EnabledState isEnabled, int minInstanceCount) { throw new NotSupportedException(); }
			[WebMethod] public List<x360ce.Engine.Data.Program> GetProgramsDefault() { throw new NotSupportedException(); }
			[WebMethod] public x360ce.Engine.Data.Program GetProgram(string fileName, string fileProductName) { throw new NotSupportedException(); }
			[WebMethod] public string SetProgram(x360ce.Engine.Data.Program p) { throw new NotSupportedException(); }
			[WebMethod] public List<Vendor> GetVendors() { throw new NotSupportedException(); }
			[WebMethod] public SettingsData GetSettingsData() { throw new NotSupportedException(); }
			[WebMethod] public CloudMessage Execute(CloudMessage command) { throw new NotSupportedException(); }
			[WebMethod] public ServerInfo GetServerInfo() { throw new NotSupportedException(); }
		}

		#endregion

		#region The service built on the designer's classes

		/// <summary>The folder in <c>Common\EntityFormat</c> of what the deployed service sends and reads.</summary>
		/// <remarks>
		/// The programs ship before the service built on the generated classes, so they must work with the one deployed,
		/// whose data classes are the Entity Framework designer's. <c>x360ce.wsdl</c> is what that service publishes, and
		/// each other file is one of its replies, with the <c>EntityKey</c> of each row it read through its context and its
		/// elements in the designer's order. They were taken from the service built from the commit before the generated
		/// classes, after writing test rows with every column set; the vendors are cut to the first five.
		/// </remarks>
		const string OldServiceFolder = "OldService";

		const string SoapNamespace = "http://x360ce.com/";

		[TestMethod, TestCategory("webservice"), TestCategory("critical")]
		[Description("Every value in a reply of the deployed service, built on the designer's classes, reaches the program")]
		public void The_program_reads_every_value_the_old_service_sends()
		{
			var reading = new Reading();
			string request;
			foreach (var call in new Dictionary<string, Func<ProgramClient, object>>
			{
				{ "LoadSetting", ws => ws.LoadSetting(new[] { Guid.Empty }) },
				{ "GetProgram", ws => ws.GetProgram("", "") },
				{ "SearchSettings", ws => ws.SearchSettings(new SearchParameter[0]) },
				{ "GetVendors", ws => ws.GetVendors() },
				{ "Execute", ws => ws.Execute(new CloudMessage()) },
			})
			{
				var reply = File.ReadAllText(OldServiceFile(call.Key + ".xml"));
				var result = Call(reply, call.Value, out request);
				var values = reading.Values;
				var element = Load(reply).GetElementsByTagName(call.Key + "Result", SoapNamespace).Cast<XmlElement>().Single();
				CompareRead(call.Key + "Result", element, result, reading);
				if (reading.Values == values)
					reading.Problems.Add(call.Key + ": the reply holds no value to compare");
			}
			Console.WriteLine(reading.Values + " values of the replies compared, " + reading.EntityKeys + " EntityKey elements passed over.");
			Assert.AreEqual(0, reading.Problems.Count, string.Join(Environment.NewLine, reading.Problems));
			// The rows the service read through its context: one in LoadSetting, one in GetProgram and four in Execute.
			Assert.AreEqual(6, reading.EntityKeys, "The replies are not the ones the deployed service sends.");
		}

		[TestMethod, TestCategory("webservice"), TestCategory("critical")]
		[Description("The deployed service, built on the designer's classes, reads every value of the program's requests")]
		public void The_old_service_reads_every_value_the_program_sends()
		{
			var service = OldServiceClasses();
			var reading = new Reading();
			string request;
			var setting = (UserSetting)Populate(typeof(UserSetting));
			var pad = (PadSetting)Populate(typeof(PadSetting));
			var saved = NoSystemWebTest.Envelope + "<SaveSettingResponse xmlns=\"" + SoapNamespace + "\"><SaveSettingResult /></SaveSettingResponse>" + NoSystemWebTest.EnvelopeEnd;
			Call(saved, ws => ws.SaveSetting(setting, pad), out request);
			var read = ReadAsOldService(service, request, reading);
			Arrived(setting, read[0], reading);
			Arrived(pad, read[1], reading);
			var search = (SearchParameter)Populate(typeof(SearchParameter));
			Call(File.ReadAllText(OldServiceFile("SearchSettings.xml")), ws => ws.SearchSettings(new[] { search }), out request);
			read = ReadAsOldService(service, request, reading);
			Arrived(search, ((Array)read[0]).GetValue(0), reading);
			// Upload To Cloud, with the values the program adds to every message.
			var upload = new CloudMessage(CloudAction.Insert)
			{
				UserGames = new[] { (UserGame)Populate(typeof(UserGame)) },
				UserDevices = new[] { (UserDevice)Populate(typeof(UserDevice)) },
				UserInstances = new[] { (UserInstance)Populate(typeof(UserInstance)) },
				UserSettings = new[] { setting },
			};
			upload.Values.Add(CloudKey.ClientVersion, "4.0.0.0");
			upload.Values.Add(CloudKey.InputSourceTypes, (int)(InputSourceType.DirectInput | InputSourceType.RawInput));
			Call(File.ReadAllText(OldServiceFile("Execute.xml")), ws => ws.Execute(upload), out request);
			read = ReadAsOldService(service, request, reading);
			Arrived(upload.UserGames[0], First(read[0], nameof(CloudMessage.UserGames)), reading);
			Arrived(upload.UserDevices[0], First(read[0], nameof(CloudMessage.UserDevices)), reading);
			Arrived(upload.UserInstances[0], First(read[0], nameof(CloudMessage.UserInstances)), reading);
			Arrived(setting, First(read[0], nameof(CloudMessage.UserSettings)), reading);
			Console.WriteLine(reading.Values + " values of the requests compared.");
			Assert.AreEqual(0, reading.Problems.Count, string.Join(Environment.NewLine, reading.Problems));
		}

		/// <summary>The program's own client, with the one operation of the service it has no method for.</summary>
		public class ProgramClient : WebServiceClient
		{
			public x360ce.Engine.Data.Program GetProgram(string fileName, string fileProductName)
			{
				return Invoke<x360ce.Engine.Data.Program>("GetProgram", fileName, fileProductName);
			}
		}

		/// <summary>Makes a call through the program's client to a stand-in that answers with the reply given.</summary>
		/// <param name="reply">The SOAP message the stand-in answers with.</param>
		/// <param name="call">The call, as the program makes it.</param>
		/// <param name="request">The SOAP message the program sent.</param>
		/// <returns>What the program read from the reply.</returns>
		static object Call(string reply, Func<ProgramClient, object> call, out string request)
		{
			var service = new NoSystemWebTest.FakeService(NoSystemWebTest.Http("200 OK", "text/xml; charset=utf-8", reply));
			object result;
			using (service)
			using (var ws = new ProgramClient { Url = service.Url })
				result = call(ws);
			request = service.Body;
			return result;
		}

		/// <summary>The classes of the deployed service's contract, made from the WSDL it publishes.</summary>
		/// <remarks>
		/// The designer's classes cannot be compiled beside the generated ones under the same names, and the service read
		/// its requests with nothing else: ASMX wrote its WSDL from the same XmlSerializer mappings it reads requests with.
		/// The classes made from it have those names, namespaces, types and base types, and, like the designer's, no
		/// element order, so they read a request as the service reads it.
		/// </remarks>
		static Type OldServiceClasses()
		{
			var importer = new ServiceDescriptionImporter { ProtocolName = "Soap", CodeGenerationOptions = CodeGenerationOptions.GenerateProperties };
			importer.AddServiceDescription(ServiceDescription.Read(OldServiceFile("x360ce.wsdl")), null, null);
			var unit = new System.CodeDom.CodeCompileUnit();
			var code = new System.CodeDom.CodeNamespace("OldService");
			unit.Namespaces.Add(code);
			var warnings = importer.Import(code, unit);
			Assert.IsFalse(warnings.HasFlag(ServiceDescriptionImportWarnings.NoCodeGenerated), warnings.ToString());
			using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
			{
				var options = new System.CodeDom.Compiler.CompilerParameters(new[] { "System.dll", "System.Xml.dll", "System.Web.Services.dll" }) { GenerateInMemory = true };
				var results = provider.CompileAssemblyFromDom(options, unit);
				Assert.IsFalse(results.Errors.HasErrors, string.Join(Environment.NewLine, results.Errors.Cast<System.CodeDom.Compiler.CompilerError>()));
				return results.CompiledAssembly.GetType("OldService.x360ce", true);
			}
		}

		/// <summary>
		/// Reads a request with the parameters of the service's method, as ASMX reads a wrapped document/literal call, and
		/// compares what it read with the request.
		/// </summary>
		static object[] ReadAsOldService(Type service, string request, Reading reading)
		{
			var body = Load(request).GetElementsByTagName("Body", "http://schemas.xmlsoap.org/soap/envelope/").Cast<XmlElement>().Single();
			var call = body.ChildNodes.OfType<XmlElement>().Single();
			var parameters = service.GetMethod(call.LocalName).GetParameters();
			var members = parameters.Select(x => new XmlReflectionMember { MemberName = x.Name, MemberType = x.ParameterType, XmlAttributes = new XmlAttributes(x) }).ToArray();
			var mapping = new XmlReflectionImporter(SoapNamespace).ImportMembersMapping(call.LocalName, SoapNamespace, members, true);
			var serializer = XmlSerializer.FromMappings(new XmlMapping[] { mapping })[0];
			object[] values;
			// Read where the service reads it, inside the envelope, whose prefixes the values' types use.
			using (var reader = XmlReader.Create(new StringReader(request)))
			{
				reader.ReadToFollowing(call.LocalName, SoapNamespace);
				values = (object[])serializer.Deserialize(reader);
			}
			foreach (var element in call.ChildNodes.OfType<XmlElement>())
			{
				var index = Array.FindIndex(parameters, x => x.Name == element.LocalName);
				if (index < 0)
					reading.Problems.Add(call.LocalName + "." + element.LocalName + ": no parameter of the service reads it");
				else
					CompareRead(call.LocalName + "." + element.LocalName, element, values[index], reading);
			}
			return values;
		}

		/// <summary>Adds a problem for each value the program set on a record that the service did not read.</summary>
		static void Arrived(object sent, object read, Reading reading)
		{
			var name = sent.GetType().Name;
			if (read == null)
			{
				reading.Problems.Add(name + ": not read");
				return;
			}
			foreach (var p in Settable(sent.GetType()))
			{
				var value = p.GetValue(sent, null);
				if (value == null)
					continue;
				var member = Member(read.GetType(), p.Name);
				if (member == null)
					reading.Problems.Add(name + "." + p.Name + ": the service has no member for it");
				else if (!Equals(value, member(read)))
					reading.Problems.Add(name + "." + p.Name + ": sent \"" + value + "\", read \"" + member(read) + "\"");
			}
		}

		/// <summary>The first item of a list member.</summary>
		static object First(object owner, string name)
		{
			var list = (System.Collections.IList)Member(owner.GetType(), name)(owner);
			return list == null || list.Count == 0 ? null : list[0];
		}

		static string OldServiceFile(string fileName)
		{
			return Path.Combine(Ui.RepoRoot.FullName, "Tests", "Common", "EntityFormat", OldServiceFolder, fileName);
		}

		/// <summary>A message read as the client and the service read it: XmlReader makes each CR LF one LF.</summary>
		static XmlDocument Load(string text)
		{
			var doc = new XmlDocument { PreserveWhitespace = true };
			using (var reader = XmlReader.Create(new StringReader(text)))
				doc.Load(reader);
			return doc;
		}

		/// <summary>What comparing a message with the objects read from it found.</summary>
		sealed class Reading
		{
			public readonly List<string> Problems = new List<string>();
			public int Values;
			public int EntityKeys;
		}

		const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

		/// <summary>
		/// Adds a problem for each value in the element that is not the value of the matching member of the object read
		/// from it, found by element name, and counts the values compared.
		/// </summary>
		/// <remarks>
		/// An element holding elements is an object, or a list whose items are those elements in order. An element holding
		/// text is a value, parsed as XmlSerializer writes the member's type. <c>EntityKey</c> is skipped and counted: the
		/// generated classes do not have it.
		/// </remarks>
		static void CompareRead(string path, XmlElement element, object value, Reading reading)
		{
			if (element.GetAttribute("nil", XsiNamespace) == "true")
			{
				if (value != null)
					reading.Problems.Add(path + ": sent as nil, read \"" + value + "\"");
				return;
			}
			if (value == null)
			{
				reading.Problems.Add(path + ": sent, not read");
				return;
			}
			foreach (XmlAttribute attribute in element.Attributes)
				if (attribute.NamespaceURI != XsiNamespace && attribute.Prefix != "xmlns" && attribute.Name != "xmlns")
					reading.Problems.Add(path + ": the attribute " + attribute.Name + " is not compared");
			var children = element.ChildNodes.OfType<XmlElement>().ToArray();
			var type = value.GetType();
			object expected;
			if (IsValue(type))
			{
				reading.Values++;
				if (children.Length > 0 || !TryParse(element.InnerText, type, out expected) || !Equals(expected, value))
					reading.Problems.Add(path + ": sent \"" + element.InnerText + "\", read \"" + value + "\"");
				return;
			}
			var list = value as System.Collections.IEnumerable;
			if (list != null)
			{
				var items = list.Cast<object>().ToArray();
				if (items.Length != children.Length)
					reading.Problems.Add(path + ": " + children.Length + " items sent, " + items.Length + " read");
				else
					for (var i = 0; i < items.Length; i++)
						CompareRead(path + "[" + i + "]", children[i], items[i], reading);
				return;
			}
			foreach (var child in children)
			{
				if (child.LocalName == "EntityKey")
				{
					reading.EntityKeys++;
					continue;
				}
				var member = Member(type, child.LocalName);
				if (member == null)
					reading.Problems.Add(path + "." + child.LocalName + ": no member of " + type.Name + " reads it");
				else
					CompareRead(path + "." + child.LocalName, child, member(value), reading);
			}
		}

		static bool IsValue(Type type)
		{
			return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(decimal);
		}

		/// <summary>Reads the member of the class that XmlSerializer writes under the element name, or null when none does.</summary>
		static Func<object, object> Member(Type type, string elementName)
		{
			foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				if (p.GetIndexParameters().Length == 0 && ElementName(p) == elementName)
					return x => p.GetValue(x, null);
			foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
				if (ElementName(f) == elementName)
					return x => f.GetValue(x);
			return null;
		}

		static string ElementName(MemberInfo member)
		{
			if (Attribute.IsDefined(member, typeof(XmlIgnoreAttribute)) || Attribute.IsDefined(member, typeof(XmlAttributeAttribute)))
				return null;
			var element = member.GetCustomAttributes(typeof(XmlElementAttribute), true).Cast<XmlElementAttribute>().FirstOrDefault();
			if (element != null && !string.IsNullOrEmpty(element.ElementName))
				return element.ElementName;
			var array = member.GetCustomAttributes(typeof(XmlArrayAttribute), true).Cast<XmlArrayAttribute>().FirstOrDefault();
			if (array != null && !string.IsNullOrEmpty(array.ElementName))
				return array.ElementName;
			return member.Name;
		}

		/// <summary>Parses the text of an element as XmlSerializer writes a value of the type.</summary>
		static bool TryParse(string text, Type type, out object value)
		{
			value = null;
			try
			{
				if (type == typeof(string))
					value = text;
				else if (type == typeof(Guid))
					value = XmlConvert.ToGuid(text);
				else if (type == typeof(bool))
					value = XmlConvert.ToBoolean(text);
				else if (type == typeof(DateTime))
					value = XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);
				else if (type == typeof(decimal))
					value = XmlConvert.ToDecimal(text);
				else if (type == typeof(double))
					value = XmlConvert.ToDouble(text);
				else if (type == typeof(float))
					value = XmlConvert.ToSingle(text);
				else if (type.IsEnum)
					value = Enum.Parse(type, text.Trim().Replace(' ', ','));
				else
					value = Convert.ChangeType(text, type, System.Globalization.CultureInfo.InvariantCulture);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		#endregion

		/// <summary>Compares a description with its recorded file, line endings aside, and keeps a different one to compare.</summary>
		static void Compare(string fileName, string actual, List<string> problems)
		{
			var recorded = Path.Combine(Ui.RepoRoot.FullName, "Tests", "Common", "EntityFormat", fileName);
			var expected = File.Exists(recorded) ? File.ReadAllText(recorded) : null;
			var actualText = actual.Replace("\r\n", "\n");
			if (expected != null && expected.Replace("\r\n", "\n") == actualText)
				return;
			var folder = Path.Combine(Ui.RepoRoot.FullName, "Tests", "TestResults", "EntityFormat");
			Directory.CreateDirectory(folder);
			var kept = Path.Combine(folder, fileName);
			File.WriteAllText(kept, actualText.Replace("\n", Environment.NewLine));
			if (expected == null)
			{
				problems.Add(fileName + ": nothing recorded; the description is in " + kept);
				return;
			}
			var a = expected.Replace("\r\n", "\n").Split('\n');
			var b = actualText.Split('\n');
			var line = 0;
			while (line < a.Length && line < b.Length && a[line] == b[line])
				line++;
			problems.Add(fileName + ": differs from line " + (line + 1) + ": recorded \"" + (line < a.Length ? a[line].Trim() : "(end)")
				+ "\", now \"" + (line < b.Length ? b[line].Trim() : "(end)") + "\". The new text is in " + kept);
		}
	}
}
