using System;

namespace JocysCom.ClassLibrary
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
