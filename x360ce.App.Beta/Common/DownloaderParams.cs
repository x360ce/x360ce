using System.Net;
using System.Text;

namespace x360ce.App
{
    public class DownloaderParams
    {

        public DownloaderParams(string url, int timeout = 60, int retries = 4, int sleep = 0)
        {
            Headers = new WebHeaderCollection();
            Timeout = timeout;
            Retries = retries;
            Sleep = sleep;
            Url = url;
        }

        public string Url { get; set; }

        public WebHeaderCollection Headers { get; set; }

        public NetworkCredential Credential { get; set; }

        public int Timeout { get; set; }

        public int Retries { get; set; }

        public int Sleep { get; set; }

        public bool Cancel { get; set; }

        public bool Success { get; set; }

        public HttpWebResponse Response { get; set; }

        public byte[] ResponseData { get; set; }

        public string GetReponseAsString()
        {
            if (ResponseData == null)
                return null;
            var encoder = string.IsNullOrEmpty(Response.ContentEncoding) ? Encoding.UTF8 : Encoding.GetEncoding(Response.ContentEncoding);
            return encoder.GetString(ResponseData);
        }

    }
}
