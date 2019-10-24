using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

namespace Macross
{
    internal static partial class NativeMethods
    {
        public enum CertificateProperty
        {
            KeyProviderInfo = 2, // CERT_KEY_PROV_INFO_PROP_ID
            KeyContext = 5, // CERT_KEY_CONTEXT_PROP_ID
            NCryptKeyHandle = 78, // CERT_NCRYPT_KEY_HANDLE_PROP_ID
        }

        public enum AcquireCertificateKeyOptions
        {
            None = 0x00000000,
            AcquireOnlyNCryptKeys = 0x00040000,   // CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG
        }

        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertGetCertificateContextProperty(
            SafeCertContextHandle pCertContext,
            CertificateProperty dwPropId,
            [Out] out IntPtr pvData,
            [In, Out] ref int pcbData);

        [DllImport("crypt32.dll")]
        public static extern SafeCertContextHandle CertDuplicateCertificateContext(IntPtr certContext);

        [DllImport("crypt32.dll", SetLastError = true), ResourceExposure(ResourceScope.None)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptAcquireCertificatePrivateKey(
            SafeCertContextHandle pCert,
            AcquireCertificateKeyOptions dwFlags,
            IntPtr pvReserved, // void *
            [Out] out SafeNCryptKeyHandle phCryptProvOrNCryptKey,
            [Out] out int dwKeySpec,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool pfCallerFreeProvOrNCryptKey);

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0068 // Use recommended dispose pattern
        internal static SafeNCryptKeyHandle? TryAcquireCngPrivateKey(
            SafeCertContextHandle certificateContext,
            out CngKeyHandleOpenOptions openOptions)
        {
            Debug.Assert(certificateContext != null, "certificateContext != null");
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid,
                         "!certificateContext.IsClosed && !certificateContext.IsInvalid");

            // If the certificate has a key handle instead of a key prov info, return the
            // ephemeral key
            {
                int cbData = IntPtr.Size;

                if (CertGetCertificateContextProperty(
                    certificateContext,
                    CertificateProperty.NCryptKeyHandle,
                    out IntPtr privateKeyPtr,
                    ref cbData))
                {
                    openOptions = CngKeyHandleOpenOptions.EphemeralKey;
                    return new SafeNCryptKeyHandle(privateKeyPtr, certificateContext);
                }
            }

            openOptions = CngKeyHandleOpenOptions.None;

            bool freeKey = true;
            SafeNCryptKeyHandle? privateKey = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (!CryptAcquireCertificatePrivateKey(
                    certificateContext,
                    AcquireCertificateKeyOptions.AcquireOnlyNCryptKeys,
                    IntPtr.Zero,
                    out privateKey,
                    out int keySpec,
                    out freeKey))
                {

                    // The documentation for CryptAcquireCertificatePrivateKey says that freeKey
                    // should already be false if "key acquisition fails", and it can be presumed
                    // that privateKey was set to 0.  But, just in case:
                    freeKey = false;
                    privateKey?.SetHandleAsInvalid();
                    return null;
                }
            }
            finally
            {
                // It is very unlikely that Windows will tell us !freeKey other than when reporting failure,
                // because we set neither CRYPT_ACQUIRE_CACHE_FLAG nor CRYPT_ACQUIRE_USE_PROV_INFO_FLAG, which are
                // currently the only two success situations documented. However, any !freeKey response means the
                // key's lifetime is tied to that of the certificate, so re-register the handle as a child handle
                // of the certificate.
                if (!freeKey && privateKey != null && !privateKey.IsInvalid)
                {
                    SafeNCryptKeyHandle newKeyHandle = new SafeNCryptKeyHandle(privateKey.DangerousGetHandle(), certificateContext);
                    privateKey.SetHandleAsInvalid();
                    privateKey = newKeyHandle;
                }
            }

            return privateKey;
        }
#pragma warning restore IDE0068 // Use recommended dispose pattern
#pragma warning restore CA2000 // Dispose objects before losing scope

        internal static SafeCertContextHandle GetCertificateContext(X509Certificate certificate)
        {
            SafeCertContextHandle certificateContext = CertDuplicateCertificateContext(certificate.Handle);
            // Make sure to keep the X509Certificate object alive until after its certificate context is
            // duplicated, otherwise it could end up being closed out from underneath us before we get a
            // chance to duplicate the handle.
            GC.KeepAlive(certificate);
            return certificateContext;
        }

        internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeCertContextHandle() : base(true) { }

            protected override bool ReleaseHandle() => CertFreeCertificateContext(handle);
        }

        public enum ErrorCode
        {
            Success = 0x00000000, // STATUS_SUCCESS

            InvalidHandle = unchecked((int)0xC0000008), // STATUS_INVALID_HANDLE
            InvalidParameter = unchecked((int)0xC000000D), // STATUS_INVALID_PARAMETER
            NotEnoughMemoryAvailable = unchecked((int)0xC0000017), // STATUS_NO_MEMORY
            BufferTooSmall = unchecked((int)0xC0000023), //STATUS_BUFFER_TOO_SMALL
            NotSupported = unchecked((int)0xC00000BB), //STATUS_NOT_SUPPORTED
            InvalidBufferSize = unchecked((int)0xC0000206), // STATUS_INVALID_BUFFER_SIZE
            ObjectNotFound = unchecked((int)0xC0000225), // STATUS_NOT_FOUND

            AuthTagMismatch = unchecked((int)0xC000A002), // STATUS_AUTH_TAG_MISMATCH

            SecurityInvalidHandle = unchecked((int)0x80090026), // NTE_INVALID_HANDLE
            SecurityInvalidParameter = unchecked((int)0x80090027) // NTE_INVALID_PARAMETER
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCryptBufferDesc
        {
            public uint ulVersion;

            public int cBuffers;

            public IntPtr pBuffers;
        }

        public enum BufferType
        {
            KDF_ALGORITHMID = 8,
            KDF_PARTYUINFO = 9,
            KDF_PARTYVINFO = 10,
            KDF_SUPPPUBINFO = 11,
            KDF_SUPPPRIVINFO = 12,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCryptBuffer
        {
            public uint cbBuffer;

            public BufferType BufferType;

            public IntPtr pvBuffer;
        }

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ErrorCode NCryptDeriveKey(
            SafeNCryptSecretHandle hSharedSecret,
            [In] string pwszKDF,
            [In] ref NCryptBufferDesc pParameterList,
            [MarshalAs(UnmanagedType.LPArray), In, Out] byte[] pbDerivedKey,
            int cbDerivedKey,
            [Out] out int pcbResult,
            int dwFlags);
    }
}
