using System;

namespace JocysCom.WebSites.Engine.Security
{
	[Serializable]
	public struct ValidationField
	{
		public ValidationField(UserFieldName name, object value, string message = null)
		{
			Name = name;
			Value = value;
			Message = message;
		}

		public UserFieldName Name { get; set; }
		public object Value { get; set; }
		public string Message { get; set; }
	}
}
