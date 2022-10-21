using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Nefarius.ViGEm.Client
{
	[Serializable]
	public class ViGEmException : Exception
	{
		protected ViGEmException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			throw new NotImplementedException();
		}

		public VIGEM_ERROR Code { get { return _Code; } }
		VIGEM_ERROR _Code;

		public ViGEmException(VIGEM_ERROR code)
			: base() { _Code = code; }

		public ViGEmException(VIGEM_ERROR code, string message)
			: base(message) { _Code = code; }

		public ViGEmException(VIGEM_ERROR code, string format, params object[] args)
			: base(string.Format(format, args)) { _Code = code; }

		public ViGEmException(VIGEM_ERROR code, string message, Exception innerException)
			: base(message, innerException) { _Code = code; }

		public ViGEmException(VIGEM_ERROR code, string format, Exception innerException, params object[] args)
			: base(string.Format(format, args), innerException) { _Code = code; }

		protected ViGEmException(VIGEM_ERROR code, SerializationInfo info, StreamingContext context)
			: base(info, context) { _Code = code; }


		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Code", _Code);
		}

	}
}
