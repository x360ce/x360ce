using System;
using System.Runtime.InteropServices;
using System.Security;

namespace JocysCom.ClassLibrary.Security
{
	public static class SecurityHelper
	{
		#region Solution for CWE-316: Clear Text Storage of Sensitive Information in Memory

		/// <summary>
		/// Get Secure string value.
		/// </summary>
		/// <param name="s">Secure string.</param>
		/// <returns>Clear text value.</returns>
		public static string GetSecureStringValue(SecureString s)
		{
			if (s is null)
				return null;
			// Convert encrypted string to plain text.
			var pointer = IntPtr.Zero;
			try
			{
				pointer = Marshal.SecureStringToBSTR(s);
				return Marshal.PtrToStringBSTR(pointer);
			}
			finally
			{
				Marshal.ZeroFreeBSTR(pointer);
			}
		}

		/// <summary>
		/// Set secure string value.
		/// </summary>
		/// <param name="s">Secure string.</param>
		/// <param name="value">Clear text value to secure.</param>
		public static void SetSecureStringValue(ref SecureString s, string value)
		{
			if (value is null)
			{
				if (s != null)
				{
					s.Dispose();
					s = null;
				}
			}
			else
			{
				if (s is null)
					s = new SecureString();
				else
					s.Clear();
				foreach (var c in value)
					s.AppendChar(c);
				s.MakeReadOnly();
			}
		}

		/* Example:

		/// <summary>Class with sensitive property.</summary>
		public class ClassName
		{
			/// <summary>Sensitive Property.</summary>
			public string Password
			{
				get { return SecurityHelper.GetSecureStringValue(_Password); }
				set { SecurityHelper.SetSecureStringValue(ref _Password, value); }
			}
			SecureString _Password;

			~ClassName()
			{
				SecurityHelper.SetSecureStringValue(ref _Password, null);
			}
		}

		*/

		#endregion

		/// <summary>
		/// The following method is invoked by the RemoteCertificateValidationDelegate.
		/// Net.ServicePointManager.ServerCertificateValidationCallback = AddressOf ValidateServerCertificate
		/// </summary>
		/// <remarks>
		/// Add "AllowCertificateErrors" to allow certificate errors: request.Headers.Add("AllowCertificateErrors");
		/// </remarks>
		public static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			// No errors were found.
			if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
			{
				// Allow this client to communicate with unauthenticated servers.
				return true;
			}
			var sp = new Configuration.SettingsParser("CertificateErrors_");
			var allow = sp.Parse("Allow", false);
			var notify = sp.Parse("Notify", true);
			string message = string.Format("Certificate error: {0}", sslPolicyErrors);
			message += allow
				? " Allow this client to communicate with unauthenticated server."
				: " The underlying connection was closed.";
			var ex = new Exception("Validate server certificate error");
			ex.Data.Add("AllowCertificateErrors", allow);
			if (sender != null && sender is System.Net.HttpWebRequest)
			{
				//var request = (System.Net.HttpWebRequest)sender;
				// Allow certificate errors if request contains "AllowCertificateErrors" key.
				//AllowCertificateErrors = request.Headers.AllKeys.Contains("AllowCertificateErrors");
				var hr = (System.Net.HttpWebRequest)sender;
				ex.Data.Add("sender.OriginalString", hr.Address.OriginalString);
			}
			if (certificate != null)
			{
				ex.Data.Add("Certificate.Issuer", certificate.Issuer);
				ex.Data.Add("Certificate.Subject", certificate.Subject);
			}
			if (chain != null)
			{
				for (int i = 0; i < chain.ChainStatus.Length; i++)
				{
					ex.Data.Add("Chain.ChainStatus(" + i + ")", string.Format("{0}, {1}", chain.ChainStatus[i].Status, chain.ChainStatus[i].StatusInformation));
				}
			}
			if (notify)
				Runtime.LogHelper.Current.ProcessException(ex);
			// Allow (or not allow depending on setting value) this client to communicate with unauthenticated servers.
			return allow;
		}

	}
}
