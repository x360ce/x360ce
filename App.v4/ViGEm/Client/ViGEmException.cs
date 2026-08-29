using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Nefarius.ViGEm.Client
{
	[Serializable]
	public class ViGEmException : Exception
	{
		public VIGEM_ERROR Code { get { return _Code; } }
		VIGEM_ERROR _Code;

		/// <summary>Names the failure, because a report that does not is unusable.</summary>
		/// <remarks>
		/// Without a message the framework supplies its own - "exception of type ... was thrown",
		/// in whatever language the machine runs - and the code is lost. Every failure to plug in
		/// a controller then reads alike, so a bus with no free slot, which is a state of the
		/// machine, cannot be told from a missing driver, which is a fault worth fixing.
		/// </remarks>
		public ViGEmException(VIGEM_ERROR code)
			: base(code.ToString()) { _Code = code; }

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
