// @under-test: Web/WebServices/x360ce.asmx.cs
// @area: webservice   @layer: integration-api
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace x360ce.Tests
{
	/// <summary>
	/// The service contract every released v3 and v4 program was built against.
	/// </summary>
	/// <remarks>
	/// <c>live.wsdl</c> beside this file is what <c>https://www.x360ce.com/webservices/x360ce.asmx?WSDL</c>
	/// answered on 2026-09-19. A program compiled years ago serialises its calls from that
	/// shape and reads the replies by element name. The target may add elements anywhere
	/// (<c>XmlSerializer</c> matches members by name, and skips names it does not know); it may
	/// not remove, rename or retype one, and it may not lose an operation.
	/// </remarks>
	[TestClass]
	public class X360ceWsdlTest
	{
		const string WsdlNs = "http://schemas.xmlsoap.org/wsdl/";
		const string XsdNs = "http://www.w3.org/2001/XMLSchema";

		static XmlDocument Snapshot()
		{
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebServices", "live.wsdl");
			var doc = new XmlDocument();
			doc.Load(path);
			return doc;
		}

		static XmlNamespaceManager Manager(XmlDocument doc)
		{
			var ns = new XmlNamespaceManager(doc.NameTable);
			ns.AddNamespace("wsdl", WsdlNs);
			ns.AddNamespace("s", XsdNs);
			return ns;
		}

		static string[] Operations(XmlDocument doc)
		{
			var ns = Manager(doc);
			return doc.SelectNodes("//wsdl:portType[@name='x360ceSoap']/wsdl:operation", ns)
				.Cast<XmlElement>().Select(x => x.GetAttribute("name")).OrderBy(x => x).ToArray();
		}

		/// <summary>Every named type and message element, with its elements as name:type in document order.</summary>
		static Dictionary<string, string[]> Types(XmlDocument doc)
		{
			var ns = Manager(doc);
			var result = new Dictionary<string, string[]>();
			foreach (XmlElement type in doc.SelectNodes("//s:complexType[@name]", ns))
				result[type.GetAttribute("name")] = Elements(type, ns);
			foreach (XmlElement element in doc.SelectNodes("/wsdl:definitions/wsdl:types/s:schema/s:element[s:complexType]", ns))
				result["message:" + element.GetAttribute("name")] = Elements(element, ns);
			return result;
		}

		static string[] Elements(XmlElement owner, XmlNamespaceManager ns)
		{
			return owner.SelectNodes(".//s:element", ns).Cast<XmlElement>()
				.Select(x => x.GetAttribute("name") + ":" + x.GetAttribute("type")).ToArray();
		}

		[TestMethod, TestCategory("webservice"), TestCategory("wsdl")]
		[Description("The target publishes every operation the live service publishes")]
		public void The_target_publishes_every_live_operation()
		{
			var expected = Operations(Snapshot());
			var actual = Operations(WebServiceTarget.Wsdl());
			var missing = expected.Except(actual).ToArray();
			Assert.AreEqual(0, missing.Length, "Operations missing on the target: " + string.Join(", ", missing));
			var added = actual.Except(expected).ToArray();
			Assert.AreEqual(0, added.Length, "Operations the live service does not have: " + string.Join(", ", added));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("wsdl")]
		[Description("No type, element or element type a released program knows has been removed, renamed or retyped")]
		public void No_known_element_is_removed_renamed_or_retyped()
		{
			var expected = Types(Snapshot());
			var actual = Types(WebServiceTarget.Wsdl());
			var problems = new List<string>();
			foreach (var pair in expected)
			{
				string[] actualElements;
				if (!actual.TryGetValue(pair.Key, out actualElements))
				{
					problems.Add(pair.Key + ": type missing");
					continue;
				}
				foreach (var element in pair.Value.Except(actualElements))
					problems.Add(pair.Key + ": " + element + " missing or retyped");
			}
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("wsdl")]
		[Description("What the target adds beyond the live contract is reported, so a change is a decision and not a surprise")]
		public void Additions_beyond_the_live_contract_are_listed()
		{
			var expected = Types(Snapshot());
			var actual = Types(WebServiceTarget.Wsdl());
			var additions = new List<string>();
			foreach (var pair in actual)
			{
				string[] expectedElements;
				if (!expected.TryGetValue(pair.Key, out expectedElements))
				{
					additions.Add(pair.Key + ": new type");
					continue;
				}
				foreach (var element in pair.Value.Except(expectedElements))
					additions.Add(pair.Key + ": +" + element);
			}
			// Additions are allowed; the list is the evidence a reviewer reads.
			Console.WriteLine(additions.Count == 0
				? "Target contract equals the live contract."
				: "Target adds:" + Environment.NewLine + string.Join(Environment.NewLine, additions));
			// The only additions this release makes are the 4.22 wheel columns of PadSetting.
			var unexpected = additions.Where(x => !x.StartsWith("PadSetting: +Force") && !x.StartsWith("PadSetting: +WheelRange")).ToArray();
			Assert.AreEqual(0, unexpected.Length, "Unplanned additions: " + string.Join("; ", unexpected));
		}
	}
}
