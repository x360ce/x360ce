using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace x360ce.App
{
    public class CertificateHelper
    {

        /// <summary>
        /// Check is file certificate is trusted.
        /// </summary>
        /// <param name="mode">Online - validate certificate online. Online - validate certificate offline.</param>
        /// <returns>True - trusted certificate, false - erorr or self-signed certificate.</returns>
        public static bool VerifyCertificate(string filePath, out X509Certificate2 certificate, out Exception error, X509RevocationMode mode = X509RevocationMode.Online)
        {
            certificate = null;
            error = null;
            try
            {
                var singingCertificate = X509Certificate.CreateFromSignedFile(filePath);
                certificate = new X509Certificate2(singingCertificate);
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.RevocationMode = mode;
            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            // Check if trusted and not self-signed certificate.
            var isTrusted = chain.Build(certificate);
            return isTrusted;
        }

        public static string ToString(X509Certificate2 certificate)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Publisher Information: {0}\r\n", certificate.SubjectName.Name);
            sb.AppendFormat("Valid From: {0}\r\n", certificate.GetEffectiveDateString());
            sb.AppendFormat("Valid To: {0}\r\n", certificate.GetExpirationDateString());
            sb.AppendFormat("Issued By: {0}\r\n", certificate.Issuer);
            return sb.ToString();
        }

        /// <summary>
        /// Veridfy if digital signature is valid.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="error"></param>
        public static void VerifySignature(string fileName, out Exception error)
        {
            error = null;
            using (var wtd = new CSCreateCabinet.Signature.NativeMethods.WINTRUST_DATA(fileName))
            {
                var guidAction = new Guid(CSCreateCabinet.Signature.NativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2);
                int result = CSCreateCabinet.Signature.NativeMethods.WinVerifyTrust(
                 CSCreateCabinet.Signature.NativeMethods.INVALID_HANDLE_VALUE, guidAction, wtd);
                if (result != 0)
                {
                    error = Marshal.GetExceptionForHR(result);
                }
            }
        }

        public static bool IsSignedAndTrusted(string fileName, out X509Certificate2 certificate, out Exception error)
        {
            VerifyCertificate(fileName, out certificate, out error);
            if (error != null)
                return false;
            VerifySignature(fileName, out error);
            return error == null;
        }

    }

}
