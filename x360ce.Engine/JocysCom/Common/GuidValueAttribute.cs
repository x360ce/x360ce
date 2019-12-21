using System;

namespace JocysCom.WebSites.Engine
{
	public class GuidValueAttribute : System.Attribute
	{

		private Guid _value;

		public GuidValueAttribute(string value)
		{
			_value = new Guid(value);
		}

		public Guid Value
		{
			get { return _value; }
		}

	}
}
