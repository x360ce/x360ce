using System;
using System.Windows.Forms;

namespace x360ce.App
{
    public class LogItem : EventArgs
    {
        public DateTime Date { get; set; }
        public TimeSpan Delay { get; set; }
        public string Message { get; set; }
        public MessageBoxIcon Status { get; set; }
        public Exception Error { get; set; }
    }
}
