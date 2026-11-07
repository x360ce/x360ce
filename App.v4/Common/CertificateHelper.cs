using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace x360ce.App
{
    public class CertificateHelper
    {

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

        /// <summary>Whether a file carries a valid signature from a trusted root, as Windows decides it: without a revocation lookup, so offline too.</summary>
        /// <param name="fileName">The file to check.</param>
        /// <param name="error">Why the file is not trusted, or null.</param>
        public static bool IsSignedAndTrusted(string fileName, out Exception error)
        {
            VerifySignature(fileName, out error);
            return error == null;
        }

        /// <summary>The subject of the certificate that signed a file, or null when it has no valid Authenticode signature.</summary>
        /// <remarks>Checked by Windows, offline: no revocation lookup, so the answer is the same without a network.</remarks>
        public static string GetSigner(string path)
        {
            Exception error;
            VerifySignature(path, out error);
            if (error != null)
                return null;
            try
            {
                return X509Certificate.CreateFromSignedFile(path).Subject;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
        }

        /// <summary>The organisation (O) a certificate's subject names, or null when it names none, or more than one.</summary>
        public static string OrganisationOf(string subject)
        {
            if (string.IsNullOrEmpty(subject))
                return null;
            X500DistinguishedName name;
            try
            {
                name = new X500DistinguishedName(subject);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
            // One part a line; a value holding a comma or a quote comes quoted, its quotes doubled.
            var found = name.Decode(X500DistinguishedNameFlags.UseNewLines)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.StartsWith("O=", StringComparison.Ordinal))
                .Select(x => x.Substring(2).Trim())
                .Select(x => x.Length > 1 && x[0] == '"' && x[x.Length - 1] == '"' ? x.Substring(1, x.Length - 2).Replace("\"\"", "\"") : x)
                .ToArray();
            return found.Length == 1 ? found[0] : null;
        }

    }

}
