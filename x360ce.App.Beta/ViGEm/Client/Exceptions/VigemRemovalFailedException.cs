using System;
using System.Runtime.Serialization;

namespace Nefarius.ViGEm.Client.Exceptions
{
    [Serializable]
    public class VigemRemovalFailedException : Exception
    {
        public VigemRemovalFailedException()
            : base() { }

        public VigemRemovalFailedException(string message)
            : base(message) { }
        
        public VigemRemovalFailedException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public VigemRemovalFailedException(string message, Exception innerException)
            : base(message, innerException) { }

        public VigemRemovalFailedException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected VigemRemovalFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
