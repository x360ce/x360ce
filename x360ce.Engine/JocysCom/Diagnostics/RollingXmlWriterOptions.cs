using System.Collections.Specialized;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public partial class RollingXmlWriterOptions
	{
		public const string SectionName = "RollingXmlWriter";

		public string SuffixPattern { get; set; }

		//initializeData
		public string FolderPath { get; set; }

		public long? LogFileMaxFiles { get; set; }

		public long? LogFileMaxBytes { get; set; }

		public void Load(StringDictionary attributes)
		{
			// File will roll over every day.
			SuffixPattern = ParseString(attributes, nameof(SuffixPattern), "_{0:yyyyMMdd}");
			LogFileMaxFiles = ParseLong(attributes, nameof(LogFileMaxFiles), null);
			LogFileMaxBytes = ParseLong(attributes, nameof(LogFileMaxBytes), null);
		}

		private static string ParseString(StringDictionary attributes, string name, string defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!attributes.ContainsKey(name))
				return defaultValue;
			return attributes[name] ?? "";
		}

		private static long? ParseLong(StringDictionary attributes, string name, long? defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!attributes.ContainsKey(name))
				return defaultValue;
			var s = attributes[name];
			long value;
			return long.TryParse(s, out value)
				? value
				: defaultValue;
		}

		public static string[] GetSupportedAttributes()
		{
			return new string[] {
				nameof(SuffixPattern),
				nameof(LogFileMaxFiles),
				nameof(LogFileMaxBytes),
			};
		}

	}
}
