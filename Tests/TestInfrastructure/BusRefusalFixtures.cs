using JocysCom.ClassLibrary.Runtime;
using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>The bus answer and helpers shared by the bus-refusal test classes.</summary>
	internal static class BusRefusalFixtures
	{
		/// <summary>A code the enum does not name. Newer bus libraries answer with codes past the enum's end.</summary>
		public const VIGEM_ERROR Unnamed = (VIGEM_ERROR)0xE0000015;

		/// <summary>No failure recorded for any controller, as when the engine starts.</summary>
		public static VIGEM_ERROR[] NoErrors()
		{
			return Enumerable.Repeat(VIGEM_ERROR.VIGEM_ERROR_NONE, 4).ToArray();
		}

		/// <summary>Runs the action and returns every log line it wrote, which goes nowhere else meanwhile.</summary>
		public static List<KeyValuePair<string, TraceLevel>> Logged(Action action)
		{
			var lines = new List<KeyValuePair<string, TraceLevel>>();
			var log = LogHelper.Current;
			var custom = log.WriteLogCustom;
			var console = log.WriteLogConsole;
			var file = log.WriteLogFile;
			log.WriteLogCustom = (message, type) => lines.Add(new KeyValuePair<string, TraceLevel>(message, type));
			log.WriteLogConsole = null;
			log.WriteLogFile = null;
			try
			{
				action();
			}
			finally
			{
				log.WriteLogCustom = custom;
				log.WriteLogConsole = console;
				log.WriteLogFile = file;
			}
			return lines;
		}
	}
}
