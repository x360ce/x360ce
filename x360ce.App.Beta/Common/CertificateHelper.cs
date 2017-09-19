using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static CSCreateCabinet.Signature.NativeMethods;

namespace x360ce.App
{
    public class CertificateHelper
    {

        #region Security

        protected class NativeMethods
        {

            [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
            static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);

        }

        #endregion

        /// <summary>
        /// Check is file certificate is valid.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="mode">Online - validate certificate online. Online - validate certificate offline.</param>
        /// <param name="error"></param>
        /// <returns></returns>
        bool IsSigned(string filePath, out string message, X509RevocationMode mode = X509RevocationMode.Online)
        {
            X509Certificate2 certificate;
            try
            {
                var singingCertificate = X509Certificate.CreateFromSignedFile(filePath);
                certificate = new X509Certificate2(singingCertificate);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
            // Make sure that certificate is not self-signed.
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.RevocationMode = mode;
            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            var isValid = chain.Build(certificate);
            if (isValid)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Publisher Information: {0}\r\n", certificate.SubjectName.Name);
                sb.AppendFormat("Valid From: {0}\r\n", certificate.GetEffectiveDateString());
                sb.AppendFormat("Valid To: {0}\r\n", certificate.GetExpirationDateString());
                sb.AppendFormat("Issued By: {0}\r\n", certificate.Issuer);
                message = sb.ToString();
            }
            else
            {
                message = "Certificate is self-signed.";
            }
            return isValid;
        }

        public static Exception WinVerifyTrust(string fileName)
        {
            using (WINTRUST_DATA wtd = new WINTRUST_DATA(fileName))
            {

                Guid guidAction = new Guid(WINTRUST_ACTION_GENERIC_VERIFY_V2);
                int result = CSCreateCabinet.Signature.NativeMethods.WinVerifyTrust(
                    INVALID_HANDLE_VALUE, guidAction, wtd);
                if (result != 0)
                {
                    return Marshal.GetExceptionForHR(result);
                }
                return null;
            }
        }

        public static bool IsTrusted(string fileName)
        {
            return WinVerifyTrust(fileName) == null;
        }
    }

}
