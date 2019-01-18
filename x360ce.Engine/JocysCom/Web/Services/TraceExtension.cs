using System;
using System.IO;
using System.Web.Services.Protocols;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Web.Services
{

	//<!-- Trace Extension -->
	//<add key = "TraceExtension_LogFileOn" value="true"/>
	//<add key = "TraceExtension_LogBrowserCompatible" value="false"/>
	//<add key = "TraceExtension_LogFileDirectory" value="%ProgramData%\JocysCom\WebServiceName"/>

	/// <summary>
	/// Define a SOAP Extension that traces the SOAP request and SOAP
	/// response for the XML Web service method the SOAP extension is
	/// applied to.
	/// </summary>
	/// <remarks>
	/// http://msdn.microsoft.com/en-us/library/ms972353.aspx
	/// You must add attribute &gt;TraceExtensionAttribute()&lt;
	/// on web service method in order for extension to capture traffic:
	///   [WebMethod(Description = "Called when vendor wishes to accept booking."), TraceExtension()]
	///   public string AcceptBooking(int ConciergeRef, string WSContract, string VendorRef)
	///   {
	///   }
	/// </remarks>
	public class TraceExtension : SoapExtension, IDisposable
	{

		// Fields
		public static string DirPath = string.Empty;
		/// <summary>
		/// Newly created stream.
		/// </summary>
		Stream logStream;
		static object objFileLock = new object();
		/// <summary>
		/// Save the old stream.
		/// </summary>
		private Stream oldStream;

		private const string RootNodeName = "SoapMessages";

		#region "Properties"

		public bool ParseBool(string name, bool defaultValue)
		{
			string v = System.Configuration.ConfigurationManager.AppSettings[name];
			if (v == null) return defaultValue;
			return bool.Parse(v);
		}

		public string ParseString(string name, string defaultValue)
		{
			string v = System.Configuration.ConfigurationManager.AppSettings[name];
			if (v == null) return defaultValue;
			return v;
		}

		public bool IsLogFileOn
		{
			get { return ParseBool("TraceExtension_LogFileOn", false); }
		}

		public bool IsLogBrowserCompatible
		{
			get { return ParseBool("TraceExtension_LogBrowserCompatible", true); }
		}

		public string LogDirectory
		{
			get
			{
				if (string.IsNullOrEmpty(_LogDirectory))
				{
					var log = ParseString("TraceExtension_LogFileDirectory", null);
					if (!string.IsNullOrEmpty(log))
					{
						_LogDirectory = Environment.ExpandEnvironmentVariables(log);
					}
				}
				if (string.IsNullOrEmpty(_LogDirectory))
					_LogDirectory = GetLogFolder();

				return _LogDirectory;
			}
			set { _LogDirectory = value; }
		}

		string _LogDirectory;

		#endregion

		// Methods
		/// <summary>
		/// Save the Stream representing the SOAP request or SOAP response into a local memory buffer.
		/// </summary>
		/// <param name="stream">Stream input</param>
		/// <returns>New stream</returns>
		public override Stream ChainStream(System.IO.Stream stream)
		{
			oldStream = stream;
			if (IsLogFileOn)
			{
				logStream = new MemoryStream();
				return logStream;
			}
			return oldStream;
		}

		/// <summary>
		/// Copy one stream to another.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		private void Copy(Stream input, Stream output)
		{
			byte[] buffer = new byte[0x4000];
			int count;
			while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
				output.Write(buffer, 0, count);
		}

		public string GetLogFolder(bool userLevel = false)
		{
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var path = string.Format("{0}\\{1}\\{2}",
				Environment.GetFolderPath(specialFolder),
				Application.CompanyName,
				Application.ProductName);
			return path;
		}

		public string GetFileName(string fileName = null, bool userLevel = false)
		{
			var file = string.IsNullOrEmpty(fileName)
				? string.Format("{0}_{1:yyyyMMdd_HHmmss}.log", GetType().Name, DateTime.Now)
				: fileName;
			var path = Path.Combine(LogDirectory, file);
			return path;
		}

		FileStream GetFileStream()
		{
			string filename = GetFileName();
			FileInfo fi = new FileInfo(Path.Combine(LogDirectory, filename));
			if (!fi.Directory.Exists)
				fi.Directory.Create();
			FileStream fsReturn = new FileStream(fi.FullName, FileMode.Append, FileAccess.Write);
			return fsReturn;
		}


		/// <summary>
		/// The SOAP extension was configured to run using a configuration file
		/// instead of an attribute applied to a specific XML Web service
		/// method.
		/// </summary>
		/// <param name="WebServiceType"></param>
		/// <returns></returns>
		public override object GetInitializer(Type WebServiceType)
		{
			return null;
		}

		/// <summary>
		/// When the SOAP extension is accessed for the first time, the XML Web
		/// service method it is applied to is accessed to store the file
		/// name passed in, using the corresponding SoapExtensionAttribute. 
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
		{
			return null;
		}

		/// <summary>
		/// Receive the file name stored by GetInitializer and store it in a
		/// member variable for this specific instance.
		/// </summary>
		/// <param name="initializer">Initializer object</param>
		public override void Initialize(object initializer)
		{
		}

		public override void ProcessMessage(SoapMessage message)
		{
			lock (objFileLock)
			{
				if (IsLogFileOn)
				{
					switch (message.Stage)
					{
						case SoapMessageStage.BeforeSerialize:
							// Output/Request:
							// The stage before a System.Web.Services.Protocols.SoapMessage is serialized.
							break;
						case SoapMessageStage.AfterSerialize:
							// Output/Request:
							// The stage just after a System.Web.Services.Protocols.SoapMessage is serialized,
							// but before the SOAP message is sent over the wire.
							break;
						case SoapMessageStage.BeforeDeserialize:
							// Input/Response:
							// The stage just before a System.Web.Services.Protocols.SoapMessage is deserialized
							// from the SOAP message sent across the network into an object.
							break;
						case SoapMessageStage.AfterDeserialize:
							// Input/Response:
							//The stage after a System.Web.Services.Protocols.SoapMessage is deserialized
							break;
					}
					Write(message);
				}
			}
		}


		private DateTime oldTime;
		private void Write(SoapMessage message)
		{
			double timePassed = 0;
			var n = DateTime.Now;
			if (oldTime != null) timePassed = n.Subtract(oldTime).TotalSeconds;
			oldTime = n;
			bool writeStream = message.Stage == SoapMessageStage.AfterSerialize | message.Stage == SoapMessageStage.BeforeDeserialize;
			bool isInput = message.Stage == SoapMessageStage.BeforeDeserialize | message.Stage == SoapMessageStage.AfterDeserialize;
			if (writeStream)
			{
				if (isInput)
				{
					Copy(oldStream, logStream);
				}
				else
				{
					logStream.Position = 0;
					Copy(logStream, oldStream);
				}
				logStream.Position = 0;
			}
			FileStream fs = GetFileStream();
			StreamWriter w = new StreamWriter(fs);
			w.WriteLine("----------------------------------------------------------------");
			w.WriteLine(string.Format("{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz} ({1:0.000}) {2} {3}", DateTime.Now, timePassed, message.GetHashCode(), message.Stage));
			if (message.Stage == SoapMessageStage.BeforeSerialize)
			{
				w.WriteLine(string.Format("Action: {0}", message.Url));
				w.WriteLine(string.Format("URL: {0}", message.Action));
			}
			if (writeStream)
			{
				w.WriteLine("----------------");
				w.Flush();
				Copy(logStream, fs);
			}
			w.Close();
			if (isInput) logStream.Position = 0;
		}

		#region "IDisposable"

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Free managed resources.
				if (logStream != null)
				{
					logStream.Dispose();
					logStream = null;
				}
			}
		}

		#endregion

	}
}


