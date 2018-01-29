using System;
using System.Runtime.Serialization;

namespace Nefarius.ViGEm.Client.Exceptions
{
    [Serializable]
    public class VigemAlreadyConnectedException : Exception
    {
        public VigemAlreadyConnectedException()
            : base() { }

        public VigemAlreadyConnectedException(string message)
            : base(message) { }

        public VigemAlreadyConnectedException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public VigemAlreadyConnectedException(string message, Exception innerException)
            : base(message, innerException) { }

        public VigemAlreadyConnectedException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected VigemAlreadyConnectedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
