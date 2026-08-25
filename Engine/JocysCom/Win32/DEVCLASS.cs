using System;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class
	/// </summary>
	public class DEVCLASS
	{
		public static Guid IEEE1394 = new Guid("6bdd1fc1-810f-11d0-bec7-08002be2092f");
		public static Guid IEEE1394DEBUG = new Guid("66f250d6-7801-4a64-b139-eea80a450b24");
		public static Guid IEEE1394P61883 = new Guid("7ebefbc0-3200-11d2-b4c2-00a0c9697d07");
		public static Guid ADAPTER = new Guid("4d36e964-e325-11ce-bfc1-08002be10318");
		public static Guid APMSUPPORT = new Guid("d45b1c18-c8fa-11d1-9f77-0000f805f530");
		public static Guid AVC = new Guid("c06ff265-ae09-48f0-812c-16753d7cba83");
		public static Guid BATTERY = new Guid("72631e54-78a4-11d0-bcf7-00aa00b7b32a");
		public static Guid BIOMETRIC = new Guid("53d29ef7-377c-4d14-864b-eb3a85769359");
		public static Guid BLUETOOTH = new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
		public static Guid CDROM = new Guid("4d36e965-e325-11ce-bfc1-08002be10318");
		public static Guid COMPUTER = new Guid("4d36e966-e325-11ce-bfc1-08002be10318");
		public static Guid DECODER = new Guid("6bdd1fc2-810f-11d0-bec7-08002be2092f");
		public static Guid DISKDRIVE = new Guid("4d36e967-e325-11ce-bfc1-08002be10318");
		public static Guid DISPLAY = new Guid("4d36e968-e325-11ce-bfc1-08002be10318");
		public static Guid DOT4 = new Guid("48721b56-6795-11d2-b1a8-0080c72e74a2");
		public static Guid DOT4PRINT = new Guid("49ce6ac8-6f86-11d2-b1e5-0080c72e74a2");
		public static Guid ENUM1394 = new Guid("c459df55-db08-11d1-b009-00a0c9081ff6");
		public static Guid FDC = new Guid("4d36e969-e325-11ce-bfc1-08002be10318");
		public static Guid FLOPPYDISK = new Guid("4d36e980-e325-11ce-bfc1-08002be10318");
		public static Guid GPS = new Guid("6bdd1fc3-810f-11d0-bec7-08002be2092f");
		public static Guid HDC = new Guid("4d36e96a-e325-11ce-bfc1-08002be10318");
		public static Guid HIDCLASS = new Guid("745a17a0-74d3-11d0-b6fe-00a0c90f57da");
		public static Guid IMAGE = new Guid("6bdd1fc6-810f-11d0-bec7-08002be2092f");
		public static Guid INFINIBAND = new Guid("30ef7132-d858-4a0c-ac24-b9028a5cca3f");
		public static Guid INFRARED = new Guid("6bdd1fc5-810f-11d0-bec7-08002be2092f");
		public static Guid KEYBOARD = new Guid("4d36e96b-e325-11ce-bfc1-08002be10318");
		public static Guid LEGACYDRIVER = new Guid("8ecc055d-047f-11d1-a537-0000f8753ed1");
		public static Guid MEDIA = new Guid("4d36e96c-e325-11ce-bfc1-08002be10318");
		public static Guid MEDIUM_CHANGER = new Guid("ce5939ae-ebde-11d0-b181-0000f8753ec4");
		public static Guid MODEM = new Guid("4d36e96d-e325-11ce-bfc1-08002be10318");
		public static Guid MONITOR = new Guid("4d36e96e-e325-11ce-bfc1-08002be10318");
		public static Guid MOUSE = new Guid("4d36e96f-e325-11ce-bfc1-08002be10318");
		public static Guid MTD = new Guid("4d36e970-e325-11ce-bfc1-08002be10318");
		public static Guid MULTIFUNCTION = new Guid("4d36e971-e325-11ce-bfc1-08002be10318");
		public static Guid MULTIPORTSERIAL = new Guid("50906cb8-ba12-11d1-bf5d-0000f805f530");
		public static Guid NET = new Guid("4d36e972-e325-11ce-bfc1-08002be10318");
		public static Guid NETCLIENT = new Guid("4d36e973-e325-11ce-bfc1-08002be10318");
		public static Guid NETSERVICE = new Guid("4d36e974-e325-11ce-bfc1-08002be10318");
		public static Guid NETTRANS = new Guid("4d36e975-e325-11ce-bfc1-08002be10318");
		public static Guid NODRIVER = new Guid("4d36e976-e325-11ce-bfc1-08002be10318");
		public static Guid PCMCIA = new Guid("4d36e977-e325-11ce-bfc1-08002be10318");
		public static Guid PNPPRINTERS = new Guid("4658ee7e-f050-11d1-b6bd-00c04fa372a7");
		public static Guid PRINTER = new Guid("4d36e979-e325-11ce-bfc1-08002be10318");
		public static Guid PRINTERUPGRADE = new Guid("4d36e97a-e325-11ce-bfc1-08002be10318");
		public static Guid PROCESSOR = new Guid("50127dc3-0f36-415e-a6cc-4cb3be910B65");
		public static Guid SBP2 = new Guid("d48179be-ec20-11d1-b6b8-00c04fa372a7");
		public static Guid SCSIADAPTER = new Guid("4d36e97b-e325-11ce-bfc1-08002be10318");
		public static Guid SECURITYACCELERATOR = new Guid("268c95a1-edfe-11d3-95c3-0010dc4050a5");
		public static Guid SENSOR = new Guid("5175d334-c371-4806-b3ba-71fd53c9258d");
		public static Guid SIDESHOW = new Guid("997b5d8d-c442-4f2e-baf3-9c8e671e9e21");
		public static Guid SMARTCARDREADER = new Guid("50dd5230-ba8a-11d1-bf5d-0000f805f530");
		public static Guid SOUND = new Guid("4d36e97c-e325-11ce-bfc1-08002be10318");
		public static Guid SYSTEM = new Guid("4d36e97d-e325-11ce-bfc1-08002be10318");
		public static Guid TAPEDRIVE = new Guid("6d807884-7d21-11cf-801c-08002be10318");
		public static Guid UNKNOWN = new Guid("4d36e97e-e325-11ce-bfc1-08002be10318");
		public static Guid USB = new Guid("36fc9e60-c465-11cf-8056-444553540000");
		public static Guid VOLUME = new Guid("71a27cdd-812a-11d0-bec7-08002be2092f");
		public static Guid VOLUMESNAPSHOT = new Guid("533c5b84-ec70-11d2-9505-00c04f79deaf");
		public static Guid WCEUSBS = new Guid("25dbce51-6c8f-4a72-8a6d-b54c2b4fc835");
		public static Guid WPD = new Guid("eec5ad98-8080-425f-922a-dabf3de3f69a");
		// Ports
		public static Guid PORTS = new Guid("4d36e978-e325-11ce-bfc1-08002be10318");
		public static Guid COMPORT = new Guid("86e0d1e0-8089-11d0-9ce4-08003e301f73");
		public static Guid PARALLEL = new Guid("97f76ef0-f883-11d0-af1f-0000f800845c");
		public static Guid PARCLASS = new Guid("811fc6a5-f728-11d0-a537-0000f8753ed1");
		// Define filesystem filter classes used for classification and load ordering.
		// Classes are listed below in order from "highest" (i.e. farthest from the
		// filesystem) to "lowest" (i.e. closest to the filesystem).
		public static Guid FSFILTER_ACTIVITYMONITOR = new Guid("b86dff51-a31e-4bac-b3cf-e8cfe75c9fc2");
		public static Guid FSFILTER_UNDELETE = new Guid("fe8f1572-c67a-48c0-bbac-0b5c6d66cafb");
		public static Guid FSFILTER_ANTIVIRUS = new Guid("b1d1a169-c54f-4379-81db-bee7d88d7454");
		public static Guid FSFILTER_REPLICATION = new Guid("48d3ebc4-4cf8-48ff-b869-9c68ad42eb9f");
		public static Guid FSFILTER_CONTINUOUSBACKUP = new Guid("71aa14f8-6fad-4622-ad77-92bb9d7e6947");
		public static Guid FSFILTER_CONTENTSCREENER = new Guid("3e3f0674-c83c-4558-bb26-9820e1eba5c5");
		public static Guid FSFILTER_QUOTAMANAGEMENT = new Guid("8503c911-a6c7-4919-8f79-5028f5866b0c");
		public static Guid FSFILTER_SYSTEMRECOVERY = new Guid("2db15374-706e-4131-a0c7-d7c78eb0289a");
		public static Guid FSFILTER_CFSMETADATASERVER = new Guid("cdcf0939-b75b-4630-bf76-80f7ba655884");
		public static Guid FSFILTER_HSM = new Guid("d546500a-2aeb-45f6-9482-f4b1799c3177");
		public static Guid FSFILTER_COMPRESSION = new Guid("f3586baf-b5aa-49b5-8d6c-0569284c639f");
		public static Guid FSFILTER_ENCRYPTION = new Guid("a0a701c0-a511-42ff-aa6c-06dc0395576f");
		public static Guid FSFILTER_PHYSICALQUOTAMANAGEMENT = new Guid("6a0a8e78-bba6-4fc4-a709-1e33cd09d67e");
		public static Guid FSFILTER_OPENFILEBACKUP = new Guid("f8ecafa6-66d1-41a5-899b-66585d7216b7");
		public static Guid FSFILTER_SECURITYENHANCER = new Guid("d02bc3da-0c8e-4945-9bd5-f1883c226c8c");
		public static Guid FSFILTER_COPYPROTECTION = new Guid("89786ff1-9c12-402f-9c9e-17753c7f4375");
		public static Guid FSFILTER_SYSTEM = new Guid("5d1b9aaa-01e2-46af-849f-272b3f324c46");
		public static Guid FSFILTER_INFRASTRUCTURE = new Guid("e55fa6f9-128c-4d04-abab-630c74b1453a");
		// Audio and Midi
		public static Guid DEVINTERFACE_AUDIO_RENDER = new Guid("e6327cad-dcec-4949-ae8a-991e976a79d2");
		public static Guid DEVINTERFACE_AUDIO_CAPTURE = new Guid("2eef81be-33fa-4800-9670-1cd474972c3f");
		public static Guid DEVINTERFACE_MIDI_OUTPUT = new Guid("6dc23320-ab33-4ce4-80d4-bbb3ebbf2814");
		public static Guid DEVINTERFACE_MIDI_INPUT = new Guid("504be32c-ccf6-4d2c-b73f-6f8b3747e22b");
	}
}
