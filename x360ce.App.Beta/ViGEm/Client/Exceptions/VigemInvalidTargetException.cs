using System;
using System.Runtime.Serialization;

namespace Nefarius.ViGEm.Client.Exceptions
{
    [Serializable]
    public class VigemInvalidTargetException : Exception
    {
        public VigemInvalidTargetException()
            : base() { }

        public VigemInvalidTargetException(string message)
            : base(message) { }

        public VigemInvalidTargetException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public VigemInvalidTargetException(string message, Exception innerException)
            : base(message, innerException) { }

        public VigemInvalidTargetException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected VigemInvalidTargetException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
