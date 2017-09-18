using System;
namespace x360ce.App
{
    public class DownloaderEventArgs : EventArgs
    {

        public long BytesReceived { get; set;  }
        public long TotalBytesToReceive { get; set; }
    }
}
