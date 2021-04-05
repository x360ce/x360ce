using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace x360ce.App.DInput
{
	[Serializable]
	public class DInputException : Exception
	{
		public DInputException() : base() { }
		public DInputException(string message) : base(message) { }
		public DInputException(string message, Exception innerException) : base(message, innerException) { }

		//public string CustomProperty { get; set; }

		protected DInputException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			//CustomProperty = info.GetString("CustomProperty");
		}

		#region ■ ISerializable

		/// <summary>
		/// Method responsible for storing object fields during the serialization process.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			if (info == null)
				throw new ArgumentNullException("info");
			// Add custom values here.
			//info.AddValue("CustomProperty", CustomProperty);
		}

		#endregion
	}
}
