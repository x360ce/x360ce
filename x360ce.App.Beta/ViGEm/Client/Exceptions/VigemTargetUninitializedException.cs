using System;
using System.Runtime.Serialization;

namespace Nefarius.ViGEm.Client.Exceptions
{
    [Serializable]
    public class VigemTargetUninitializedException : Exception
    {
        public VigemTargetUninitializedException()
            : base() { }

        public VigemTargetUninitializedException(string message)
            : base(message) { }

        public VigemTargetUninitializedException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public VigemTargetUninitializedException(string message, Exception innerException)
            : base(message, innerException) { }

        public VigemTargetUninitializedException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected VigemTargetUninitializedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
