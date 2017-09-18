using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace x360ce.App
{
    public class Downloader
    {

        public event EventHandler<DownloaderEventArgs> Progress;
        public ISynchronizeInvoke SynchronizingObject;

        public void LoadAsync(string url, int timeout = 60, int retries = 4, int sleep = 0)
        {
            var p = new DownloaderParams(url, timeout, retries, sleep);
            var ts = new ParameterizedThreadStart(Load);
            var t = new Thread(ts);
            t.IsBackground = true;
            t.Start(p);
        }

        public void Load(object args)
        {
            var p = args as DownloaderParams;
            for (int retry = 0; retry < p.Retries; retry++)
            {
                try
                {
                    var request = WebRequest.Create(p.Url) as HttpWebRequest;
                    request.AllowAutoRedirect = true;
                    // Accept all certificates.
                    ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    if (p.Credential != null)
                        request.Credentials = p.Credential;
                    request.Headers = p.Headers;
                    request.Timeout = p.Timeout * 1000;
                    p.Response = request.GetResponse() as HttpWebResponse;
                    switch (p.Response.StatusCode)
                    {
                        case HttpStatusCode.Found:
                            // Redirect to an error page.
                            Console.WriteLine("Found (302), ignoring ");
                            break;
                        case HttpStatusCode.OK:
                            using (var sr = p.Response.GetResponseStream())
                            using (var ms = new MemoryStream())
                            {
                                //long i = 0;
                                for (int b; (b = sr.ReadByte()) != -1;)
                                {
                                    ms.WriteByte((byte)b);
                                    if (p.Cancel)
                                    {
                                        return;
                                    }
                                    var e = new DownloaderEventArgs
                                    {
                                        BytesReceived = ms.Length,
                                        TotalBytesToReceive = p.Response.ContentLength
                                    };
                                    var ev = Progress;
                                    if (ev != null)
                                    {
                                        if (SynchronizingObject == null)
                                        {
                                            Progress(this, e);
                                        }
                                        else
                                        {
                                            SynchronizingObject.Invoke(Progress, new object[] { this, e });
                                        }
                                    }
                                }
                                p.ResponseData = ms.ToArray();
                            }
                            break;

                        default:
                            // This is unexpected.
                            Console.WriteLine(p.Response.StatusCode);
                            break;
                    }
                    p.Success = true;
                    break;
                }
                catch (WebException ex)
                {
                    Console.WriteLine(":Exception " + ex.Message);
                    p.Response = ex.Response as HttpWebResponse;
                    if (ex.Status == WebExceptionStatus.Timeout && p.Sleep > 0)
                    {
                        Thread.Sleep(p.Sleep * 1000);
                        continue;
                    }
                    break;
                }
            }
        }

    }
}
