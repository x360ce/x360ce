/****************************** Module Header ******************************\
 * Module Name:  NativeMethods.cs
 * Project:      CSCreateCabinet
 * Copyright (c) Microsoft Corporation.
 * 
 * This class wraps the extern method WinVerifyTrust in Wintrust.dll.
 *  
 * 
 * This source is subject to the Microsoft Public License.
 * See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
 * All other rights reserved.
 * 
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 * EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 * WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace CSCreateCabinet.Signature
{
    internal static class NativeMethods
    {
        /// <summary>
        /// There is no interactive user. The trust provider performs the verification
        /// action without the user's assistance.
        /// </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        ///  GUID of the action to verify a file or object using the Authenticode
        ///  policy provider.
        /// </summary>
        public const string WINTRUST_ACTION_GENERIC_VERIFY_V2 =
            "{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}";


        /// <summary>
        /// The WinVerifyTrust function performs a trust verification action on a 
        /// specified object. The function passes the inquiry to a trust provider 
        /// that supports the action identifier, if one exists.
        /// 
        /// INVALID_HANDLE_VALUE
        /// Zero
        /// A valid window handle
        /// </summary>
        /// <param name="hwnd">
        /// Optional handle to a caller window. A trust provider can use this value
        /// to determine whether it can interact with the user. However,
        /// trust providers typically perform verification actions without input from the user.
        /// 
        /// INVALID_HANDLE_VALUE
        /// Zero
        /// A valid window handle
        /// 
        /// </param>
        /// <param name="pgActionID">
        /// A pointer to a GUID structure that identifies an action and the trust
        /// provider that supports that action. This value indicates the type of 
        /// verification action to be performed on the structure pointed to by pWinTrustData.
        /// 
        /// DRIVER_ACTION_VERIFY
        /// HTTPSPROV_ACTION
        /// OFFICESIGN_ACTION_VERIFY
        /// WINTRUST_ACTION_GENERIC_CERT_VERIFY
        /// WINTRUST_ACTION_GENERIC_CHAIN_VERIFY
        /// WINTRUST_ACTION_GENERIC_VERIFY_V2
        /// WINTRUST_ACTION_TRUSTPROVIDER_TEST
        /// </param>
        /// <param name="pWVTData">
        /// A pointer that, when cast as a WINTRUST_DATA structure, contains information that the
        /// trust provider needs to process the specified action identifier. 
        /// Typically, the structure includes information that identifies the object that the 
        /// trust provider must evaluate.
        /// </param>
        /// <returns>
        /// If the trust provider verifies that the subject is trusted for the specified action,
        /// the return value is zero. 
        /// No other value besides zero should be considered a successful return.
        /// 
        /// For example, a trust provider might indicate that the subject is not trusted, or is 
        /// trusted but with limitations or warnings. The return value can be a trust-provider-specific 
        /// value described in the documentation for an individual trust provider, or it can be one
        /// of the following error codes.
        /// 
        /// TRUST_E_SUBJECT_NOT_TRUSTED
        /// TRUST_E_PROVIDER_UNKNOWN
        /// TRUST_E_ACTION_UNKNOWN
        /// TRUST_E_SUBJECT_FORM_UNKNOWN
        /// </returns>
        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int WinVerifyTrust(
             [In] IntPtr hwnd,
             [In] [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
             [In] WINTRUST_DATA pWVTData
        );

        /// <summary>
        /// The WINTRUST_DATA structure is used when calling WinVerifyTrust to pass
        /// necessary information into the trust providers.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class WINTRUST_DATA : IDisposable
        {

            /// <summary>
            /// The size, in bytes, of this structure.
            /// </summary>
            public UInt32 cbStruct;

            /// <summary>
            /// A pointer to a data buffer used to pass policy-specific data to a policy
            /// provider. This member can be NULL.
            /// </summary>
            public IntPtr pPolicyCallbackData;

            /// <summary>
            /// A pointer to a data buffer used to pass subject interface package 
            /// (SIP)-specific data to a SIP provider. This member can be NULL.
            /// </summary>
            public IntPtr pSIPClientData;

            /// <summary>
            /// Specifies the kind of user interface (UI) to be used. 
            /// </summary>
            public WTDUIChoice dwUIChoice;

            /// <summary>
            /// Certificate revocation check options. 
            /// </summary>
            public WTDRevocationChecks fdwRevocationChecks;

            /// <summary>
            /// Specifies the union member to be used and, thus, the type of object 
            /// for which trust will be verified. 
            /// </summary>
            public WTDUnionChoice dwUnionChoice;

            /// <summary>
            /// A pointer to a WINTRUST_FILE_INFO structure.
            /// 
            /// The definition of this field is
            /// 
            ///   union {
            ///       struct WINTRUST_FILE_INFO_  *pFile;
            ///       struct WINTRUST_CATALOG_INFO_  *pCatalog;
            ///       struct WINTRUST_BLOB_INFO_  *pBlob;
            ///       struct WINTRUST_SGNR_INFO_  *pSgnr;
            ///       struct WINTRUST_CERT_INFO_  *pCert;
            ///   };
            ///   
            /// We only use the file in this sample.
            /// </summary>
            public IntPtr pFile;

            /// <summary>
            /// Specifies the action to be taken. 
            /// </summary>
            public WTDStateAction dwStateAction;

            /// <summary>
            /// A handle to the state data. The contents of this member depends on 
            /// the value of the dwStateAction member.
            /// </summary>
            public IntPtr hWVTStateData;

            /// <summary>
            /// Reserved for future use. Set to NULL.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public String pwszURLReference;

            /// <summary>
            /// DWORD value that specifies trust provider settings. 
            /// </summary>
            public WTDProvFlags dwProvFlags;

            /// <summary>
            /// A DWORD value that specifies the user interface context for the 
            /// WinVerifyTrust function. This causes the text in the Authenticode
            /// dialog box to match the action taken on the file.
            /// </summary>
            public WTDUIContext dwUIContext;

            // constructor for silent WinTrustDataChoice.File check
            public WINTRUST_DATA(String fileName)
            {
                this.cbStruct = (UInt32)Marshal.SizeOf(typeof(WINTRUST_DATA));
                this.pPolicyCallbackData = IntPtr.Zero;
                this.pSIPClientData = IntPtr.Zero;
                this.dwUIChoice = WTDUIChoice.WTD_UI_NONE;
                this.fdwRevocationChecks = WTDRevocationChecks.WTD_REVOKE_NONE;
                this.dwUnionChoice = WTDUnionChoice.WTD_CHOICE_FILE;
                this.dwStateAction = WTDStateAction.WTD_STATEACTION_IGNORE;
                this.hWVTStateData = IntPtr.Zero;
                this.pwszURLReference = null;
                this.dwProvFlags = WTDProvFlags.WTD_SAFER_FLAG;
                this.dwUIContext = WTDUIContext.WTD_UICONTEXT_EXECUTE;

                WINTRUST_FILE_INFO wtfiData = new WINTRUST_FILE_INFO(fileName);
                this.pFile = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                Marshal.StructureToPtr(wtfiData, pFile, false);
            }

            ~WINTRUST_DATA()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (this.pFile != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pFile);
                }
                GC.SuppressFinalize(this);
            }
        }

        #region ■ WinTrustData struct field enums and structs
        public enum WTDUIChoice : uint
        {

            /// <summary>
            /// Display all UI.
            /// </summary>
            WTD_UI_ALL = 1,

            /// <summary>
            /// Display no UI.
            /// </summary>
            WTD_UI_NONE = 2,

            /// <summary>
            /// Do not display any negative UI.
            /// </summary>
            WTD_UI_NOBAD = 3,

            /// <summary>
            /// Do not display any positive UI.
            /// </summary>
            WTD_UI_NOGOOD = 4
        }

        public enum WTDRevocationChecks : uint
        {

            /// <summary>
            /// No additional revocation checking will be done when the WTD_REVOKE_NONE
            /// flag is used in conjunction with the HTTPSPROV_ACTION value set in the 
            /// pgActionID parameter of the WinVerifyTrust function. To ensure the 
            /// WinVerifyTrust function does not attempt any network retrieval when 
            /// verifying code signatures, WTD_CACHE_ONLY_URL_RETRIEVAL must be set in 
            /// the dwProvFlags parameter.
            /// </summary>
            WTD_REVOKE_NONE = 0,

            /// <summary>
            /// Revocation checking will be done on the whole chain.
            /// </summary>
            WTD_REVOKE_WHOLECHAIN = 1
        }

        public enum WTDUnionChoice : uint
        {

            /// <summary>
            /// Use the file pointed to by pFile.
            /// </summary>
            WTD_CHOICE_FILE = 1,

            /// <summary>
            /// Use the catalog pointed to by pCatalog.
            /// </summary>
            WTD_CHOICE_CATALOG = 2,

            /// <summary>
            /// Use the BLOB pointed to by pBlob.
            /// </summary>
            WTD_CHOICE_BLOB = 3,

            /// <summary>
            /// Use the WINTRUST_SGNR_INFO structure pointed to by pSgnr.
            /// </summary>
            WTD_CHOICE_SIGNER = 4,

            /// <summary>
            /// Use the certificate pointed to by pCert.
            /// </summary>
            WTD_CHOICE_CERT = 5

        }

        public enum WTDStateAction : uint
        {

            /// <summary>
            /// Ignore the hWVTStateData member.
            /// </summary>
            WTD_STATEACTION_IGNORE = 0,

            /// <summary>
            /// Verify the trust of the object (typically a file) that is specified by
            /// the dwUnionChoice member. The hWVTStateData member will receive a handle
            /// to the state data. 
            /// This handle must be freed by specifying the WTD_STATEACTION_CLOSE action
            /// in a subsequent call.
            /// </summary>
            WTD_STATEACTION_VERIFY = 1,

            /// <summary>
            /// Free the hWVTStateData member previously allocated with the 
            /// WTD_STATEACTION_VERIFY action.
            /// This action must be specified for every use of the WTD_STATEACTION_VERIFY action.
            /// </summary>
            WTD_STATEACTION_CLOSE = 2,

            /// <summary>
            /// Write the catalog data to a WINTRUST_DATA structure and then cache that structure. 
            /// This action only applies when the dwUnionChoice member contains WTD_CHOICE_CATALOG.
            /// </summary>
            WTD_STATEACTION_AUTO_CACHE = 3,

            /// <summary>
            /// Flush any cached catalog data. This action only applies when the dwUnionChoice
            /// member contains WTD_CHOICE_CATALOG.
            /// </summary>
            WTD_STATEACTION_AUTO_CACHE_FLUSH = 4

        }

        [Flags]
        public enum WTDProvFlags : uint
        {
            /// <summary>
            /// The trust is verified in the same manner as implemented by 
            /// Internet Explorer 4.0.
            /// </summary>
            WTD_USE_IE4_TRUST_FLAG = 0x1,

            /// <summary>
            /// The Internet Explorer 4.0 chain functionality is not used.
            /// </summary>
            WTD_NO_IE4_CHAIN_FLAG = 0x2,

            /// <summary>
            /// The default verification of the policy provider, such as code
            /// signing for Authenticode, is not performed, and the certificate 
            /// is assumed valid for all usages.
            /// </summary>
            WTD_NO_POLICY_USAGE_FLAG = 0x4,

            /// <summary>
            /// Revocation checking is not performed.
            /// </summary>
            WTD_REVOCATION_CHECK_NONE = 0x10,

            /// <summary>
            /// Revocation checking is performed on the end certificate only.
            /// </summary>
            WTD_REVOCATION_CHECK_END_CERT = 0x20,

            /// <summary>
            /// Revocation checking is performed on the entire certificate chain.
            /// </summary>
            WTD_REVOCATION_CHECK_CHAIN = 0x40,

            /// <summary>
            /// Revocation checking is performed on the entire certificate chain,
            /// excluding the root certificate.
            /// </summary>
            WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x80,

            /// <summary>
            /// Not supported.
            /// </summary>
            WTD_SAFER_FLAG = 0x100,

            /// <summary>
            /// Only the hash is verified.
            /// </summary>
            WTD_HASH_ONLY_FLAG = 0x200,

            /// <summary>
            /// The default operating system version checking is performed.
            /// This flag is only used for verifying catalog-signed files.
            /// </summary>
            WTD_USE_DEFAULT_OSVER_CHECK = 0x400,

            /// <summary>
            /// If this flag is not set, all time stamped signatures are considered
            /// valid forever. Setting this flag limits the valid lifetime of the
            /// signature to the lifetime of the signing certificate. This allows 
            /// time stamped signatures to expire.
            /// </summary>
            WTD_LIFETIME_SIGNING_FLAG = 0x800,

            /// <summary>
            /// Use only the local cache for revocation checks. Prevents revocation
            /// checks over the network. 
            /// </summary>
            WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000,

            /// <summary>
            /// Disable the use of MD2 and MD4 hashing algorithms. If a file is signed by 
            /// using MD2 or MD4 and if this flag is set, an NTE_BAD_ALGID error is returned.
            /// 
            /// Note:
            /// This flag is only supported on Windows 7 with SP1 and later operating systems.
            /// </summary>
            WTD_DISABLE_MD2_MD4 = 0x2000
        }

        public enum WTDUIContext : uint
        {

            /// <summary>
            /// Use when calling WinVerifyTrust for a file that is to be run. 
            /// This is the default value.
            /// </summary>
            WTD_UICONTEXT_EXECUTE = 0,

            /// <summary>
            /// Use when calling WinVerifyTrust for a file that is to be installed.
            /// </summary>
            WTD_UICONTEXT_INSTALL = 1

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcwszFilePath;

            /// <summary>
            /// Optional. File handle to the open file to be verified. This handle must 
            /// be to a file that has at least read permission.
            /// This member can be set to NULL.
            /// </summary>
            public IntPtr hFile;

            /// <summary>
            /// Optional. Pointer to a GUID structure that specifies the subject type. 
            /// This member can be set to NULL.
            /// </summary>
            public IntPtr pgKnownSubject;

            public WINTRUST_FILE_INFO(string filePath)
            {
                this.cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
                this.hFile = IntPtr.Zero;
                this.pgKnownSubject = IntPtr.Zero;
                this.pcwszFilePath = filePath;
            }
        }
        #endregion
    }
}
