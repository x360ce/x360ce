using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace x360ce.Setup
{
	public partial class SetupEngine
	{
		private void ConfigurePadSettingsXml()
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				if (File.Exists(PadSettingsXml))
				{
					doc.Load(PadSettingsXml);
				}
				else
				{
					doc.LoadXml("<Data><Items></Items></Data>");
				}

				var itemsNode = doc.SelectSingleNode("//Items");
				if (itemsNode == null)
					return;

				var existingPad = itemsNode.SelectSingleNode(string.Format("PadSetting[PadSettingChecksum='{0}']", VerifiedPadChecksum));
				if (existingPad == null)
				{
					existingPad = doc.CreateElement("PadSetting");
					itemsNode.AppendChild(existingPad);
				}

				SetNodeText(doc, existingPad, "PadSettingChecksum", VerifiedPadChecksum);
				SetNodeText(doc, existingPad, "ButtonA", "3");
				SetNodeText(doc, existingPad, "ButtonB", "2");
				SetNodeText(doc, existingPad, "ButtonX", "4");
				SetNodeText(doc, existingPad, "ButtonY", "1");
				SetNodeText(doc, existingPad, "ButtonBack", "9");
				SetNodeText(doc, existingPad, "ButtonStart", "10");
				SetNodeText(doc, existingPad, "LeftShoulder", "5");
				SetNodeText(doc, existingPad, "RightShoulder", "6");
				SetNodeText(doc, existingPad, "LeftTrigger", "7");
				SetNodeText(doc, existingPad, "RightTrigger", "8");
				SetNodeText(doc, existingPad, "LeftThumbButton", "11");
				SetNodeText(doc, existingPad, "RightThumbButton", "12");
				SetNodeText(doc, existingPad, "DPad", "p1");
				SetNodeText(doc, existingPad, "LeftThumbAxisX", "a1");
				SetNodeText(doc, existingPad, "LeftThumbAxisY", "a-2");
				SetNodeText(doc, existingPad, "RightThumbAxisX", "a6");
				SetNodeText(doc, existingPad, "RightThumbAxisY", "a-3");
				SetNodeText(doc, existingPad, "ForceEnable", "1");
				SetNodeText(doc, existingPad, "ForceType", "1");
				SetNodeText(doc, existingPad, "ForceSpringStrength", "100");

				doc.Save(PadSettingsXml);
			}
			catch { }
		}

		private void RegisterGameInXml(DetectedGameInfo game, List<DetectedControllerInfo> controllers)
		{
			try
			{
				string productName;
				if (game.FileName.Equals("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft: Bedrock Edition";
				}
				else if (game.FileName.Equals("javaw.exe", StringComparison.OrdinalIgnoreCase) ||
						 game.FileName.Equals("java.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft: Java Edition (Runtime)";
				}
				else if (game.FileName.Equals("MinecraftLauncher.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft Launcher";
				}
				else
				{
					productName = Path.GetFileNameWithoutExtension(game.FileName);
				}

				// 1. UserGames.xml
				XmlDocument gDoc = new XmlDocument();
				if (File.Exists(UserGamesXml))
					gDoc.Load(UserGamesXml);
				else
					gDoc.LoadXml("<Data><Items></Items></Data>");

				var gItems = gDoc.SelectSingleNode("//Items");
				if (gItems != null)
				{
					var gameNode = gItems.SelectSingleNode(string.Format("UserGame[FileName='{0}']", game.FileName));
					if (gameNode == null)
					{
						gameNode = gDoc.CreateElement("UserGame");
						gItems.AppendChild(gameNode);
					}

					SetNodeText(gDoc, gameNode, "GameId", Guid.NewGuid().ToString());
					SetNodeText(gDoc, gameNode, "FileName", game.FileName);
					SetNodeText(gDoc, gameNode, "FileProductName", productName);
					SetNodeText(gDoc, gameNode, "FullPath", game.FilePath);
					SetNodeText(gDoc, gameNode, "ProcessorArchitecture", game.Is64Bit ? "9" : "0");
					SetNodeText(gDoc, gameNode, "EmulationType", "2"); // Virtual ViGEm
					SetNodeText(gDoc, gameNode, "EnableMask", "3");    // Player 1 & 2
					SetNodeText(gDoc, gameNode, "IsEnabled", "true");

					gDoc.Save(UserGamesXml);
				}

				// 2. UserSettings.xml (Mapping for Player 1 and Player 2)
				XmlDocument sDoc = new XmlDocument();
				if (File.Exists(UserSettingsXml))
					sDoc.Load(UserSettingsXml);
				else
					sDoc.LoadXml("<Data><Items></Items></Data>");

				var sItems = sDoc.SelectSingleNode("//Items");
				if (sItems != null)
				{
					for (int mapTo = 1; mapTo <= 2; mapTo++)
					{
						var ctrl = controllers.FirstOrDefault(c => c.PlayerIndex == mapTo) ??
								   controllers.FirstOrDefault();

						var ctrlGuid = ctrl?.InstanceGuid ?? (mapTo == 1 ? DefaultTwinCtrl1Guid : DefaultTwinCtrl2Guid);
						var ctrlName = ctrl?.Name ?? "Twin USB Gamepad";
						var prodGuid = ctrl?.ProductGuid ?? "00010810-0000-0000-0000-504944564944";

						var sNode = sItems.SelectSingleNode(string.Format("UserSetting[FileName='{0}' and MapTo='{1}']", game.FileName, mapTo));
						if (sNode == null)
						{
							sNode = sDoc.CreateElement("UserSetting");
							sItems.AppendChild(sNode);
						}

						SetNodeText(sDoc, sNode, "SettingId", Guid.NewGuid().ToString());
						SetNodeText(sDoc, sNode, "InstanceGuid", ctrlGuid);
						SetNodeText(sDoc, sNode, "InstanceName", ctrlName);
						SetNodeText(sDoc, sNode, "ProductGuid", prodGuid);
						SetNodeText(sDoc, sNode, "ProductName", ctrlName);
						SetNodeText(sDoc, sNode, "DeviceType", "20");
						SetNodeText(sDoc, sNode, "FileName", game.FileName);
						SetNodeText(sDoc, sNode, "FileProductName", productName);
						SetNodeText(sDoc, sNode, "IsEnabled", "true");
						SetNodeText(sDoc, sNode, "PadSettingChecksum", VerifiedPadChecksum);
						SetNodeText(sDoc, sNode, "MapTo", mapTo.ToString());
						SetNodeText(sDoc, sNode, "Completion", "100");
					}

					sDoc.Save(UserSettingsXml);
				}
			}
			catch { }
		}

		private void FixSiderDoubleInput(string targetFolder, Action<string> log)
		{
			try
			{
				var siderIni = Path.Combine(targetFolder, "gamepad.ini");
				if (!File.Exists(siderIni))
				{
					var files = Directory.GetFiles(targetFolder, "gamepad.ini", SearchOption.AllDirectories);
					if (files.Length > 0)
						siderIni = files[0];
				}

				if (File.Exists(siderIni))
				{
					var text = File.ReadAllText(siderIni);
					text = System.Text.RegularExpressions.Regex.Replace(text, @"gamepad\.dinput\.enabled\s*=\s*\d+", "gamepad.dinput.enabled = 0");
					text = System.Text.RegularExpressions.Regex.Replace(text, @"gamepad\.xinput\.enabled\s*=\s*\d+", "gamepad.xinput.enabled = 1");
					File.WriteAllText(siderIni, text);
					log?.Invoke("  [FIX] Configured gamepad.ini for pure XInput (double-input prevented)!");
				}
			}
			catch { }
		}

		private static void SetNodeText(XmlDocument doc, XmlNode parent, string nodeName, string value)
		{
			var node = parent.SelectSingleNode(nodeName);
			if (node == null)
			{
				node = doc.CreateElement(nodeName);
				parent.AppendChild(node);
			}
			node.InnerText = value;
		}
	}
}
