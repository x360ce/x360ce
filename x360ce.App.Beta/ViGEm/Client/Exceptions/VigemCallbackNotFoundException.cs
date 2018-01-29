using System;
using System.Runtime.Serialization;

namespace Nefarius.ViGEm.Client.Exceptions
{
    public class VigemCallbackNotFoundException : Exception
    {
        public VigemCallbackNotFoundException()
            : base() { }

        public VigemCallbackNotFoundException(string message)
            : base(message) { }

        public VigemCallbackNotFoundException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public VigemCallbackNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }

        public VigemCallbackNotFoundException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected VigemCallbackNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
