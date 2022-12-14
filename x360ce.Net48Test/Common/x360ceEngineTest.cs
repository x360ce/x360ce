using JocysCom.ClassLibrary.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.App.Controls;
using x360ce.Engine.Data;

namespace x360ce.Tests
{

	[TestClass]
	public class x360ceEngineTest
	{
		private static Type[] excludeTypes = new Type[] {
			typeof(JocysCom.WebSites.Engine.Security.Data.SecurityEntities),
			typeof(SoapHttpClientBase),
			typeof(x360ceModelContainer),
		};


		[TestMethod]
		public void Test_All()
		{
			MemoryLeakHelper.Test(typeof(Engine.EngineHelper).Assembly,
				// Include types. null = Test all.
				null,
				// Exclude types.
				excludeTypes);
		}

	}
}
