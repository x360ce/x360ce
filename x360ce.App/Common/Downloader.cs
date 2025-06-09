using System;
using System.IO;
using System.Net;
using System.Threading;

namespace x360ce.App
{
	public class Downloader: IDisposable
	{
		public Downloader()
		{
			retryTimer = new System.Timers.Timer();
			retryTimer.AutoReset = false;
			retryTimer.Elapsed += RetryTimer_Elapsed;
		}

		private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var ts = new ThreadStart(Load);
			var t = new Thread(ts);
			t.IsBackground = true;
			t.Start();
		}

		public event EventHandler<DownloaderEventArgs> Progress;

		System.Timers.Timer retryTimer;
		public DownloaderParams Params;

		public void LoadAsync(string url, int timeout = 60, int retries = 4, int sleep = 5)
		{
			Params = new DownloaderParams(url, timeout, retries, sleep);
			// Call Close() to prevent timer auto-restart when Interval changed.
			retryTimer.Close();
			retryTimer.Interval = sleep * 1000;
			retryTimer.Start();
		}

		void Load()
		{
			var p = Params;
			p.RetriesLeft--;
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
									TotalBytesToReceive = p.Response.ContentLength,
								};
								var ev = Progress;
								if (ev != null)
								{
									ev(this, e);
								}
							}
							var e2 = new DownloaderEventArgs
							{
								BytesReceived = ms.Length,
								TotalBytesToReceive = ms.Length,
							};
							p.ResponseData = ms.ToArray();
							var ev2 = Progress;
							if (ev2 != null)
							{
								ev2(this, e2);
							}
						}
						break;

					default:
						// This is unexpected.
						Console.WriteLine(p.Response.StatusCode);
						break;
				}
				p.Success = true;
			}
			catch (WebException ex)
			{
				Console.WriteLine(":Exception " + ex.Message);
				p.Response = ex.Response as HttpWebResponse;
				// If update retries left then schedule next attempt.
				if (p.RetriesLeft > 0)
					retryTimer.Start();
			}
		}

        #region ■ IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                retryTimer.Dispose();
            }
        }

        #endregion
    }
}
