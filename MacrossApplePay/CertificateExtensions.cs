using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

namespace Macross
{
    internal static class CertificateExtensions
    {
        public static byte[] ExportPublicKeyInDERFormat(this X509Certificate certificate)
        {
            byte[] algOid = CryptoConfig.EncodeOID(certificate.GetKeyAlgorithm());

            byte[] algParams = certificate.GetKeyAlgorithmParameters();

            byte[] algId = BuildSimpleDERSequence(algOid, algParams);

            byte[] publicKey = WrapAsBitString(certificate.GetPublicKey());

            return BuildSimpleDERSequence(algId, publicKey);
        }

        public static string ExportPublicKeyInPEMFormat(this X509Certificate certificate)
            => PEMEncode(ExportPublicKeyInDERFormat(certificate), "PUBLIC KEY");

        // .NET does not have a way to get an ECC public/private key out of an X509 certificate. Remove this and Certificate NativeMethods if it ever gets one!
        public static CngKey GetCngPrivateKey(this X509Certificate2 certificate)
        {
            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("Certificate does not have a PrivateKey.");

            using NativeMethods.SafeCertContextHandle certificateContext = NativeMethods.GetCertificateContext(certificate);

            SafeNCryptKeyHandle? privateKeyHandle = NativeMethods.TryAcquireCngPrivateKey(certificateContext, out CngKeyHandleOpenOptions openOptions);
            if (privateKeyHandle == null)
                throw new InvalidOperationException("Certificate PrivateKey could not be opened.");
            try
            {
                return CngKey.Open(privateKeyHandle, openOptions);
            }
            finally
            {
                privateKeyHandle.Dispose();
            }
        }

        private static string PEMEncode(byte[] derData, string pemLabel)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("-----BEGIN ");
            builder.Append(pemLabel);
            builder.AppendLine("-----");
            builder.AppendLine(Convert.ToBase64String(derData, Base64FormattingOptions.InsertLineBreaks));
            builder.Append("-----END ");
            builder.Append(pemLabel);
            builder.AppendLine("-----");
            return builder.ToString();
        }

        private static byte[] BuildSimpleDERSequence(params byte[][] values)
        {
            int totalLength = values.Sum(v => v.Length);
            byte[] len = EncodeDERLength(totalLength);
            int offset = 1;

            byte[] seq = new byte[totalLength + len.Length + 1];
            seq[0] = 0x30;

            Buffer.BlockCopy(len, 0, seq, offset, len.Length);
            offset += len.Length;

            foreach (byte[] value in values)
            {
                Buffer.BlockCopy(value, 0, seq, offset, value.Length);
                offset += value.Length;
            }

            return seq;
        }

        private static byte[] WrapAsBitString(byte[] value)
        {
            byte[] len = EncodeDERLength(value.Length + 1);
            byte[] bitString = new byte[value.Length + len.Length + 2];
            bitString[0] = 0x03;
            Buffer.BlockCopy(len, 0, bitString, 1, len.Length);
            bitString[len.Length + 1] = 0x00;
            Buffer.BlockCopy(value, 0, bitString, len.Length + 2, value.Length);
            return bitString;
        }

        private static byte[] EncodeDERLength(int length)
        {
            if (length <= 0x7F)
                return new byte[] { (byte)length };

            if (length <= 0xFF)
                return new byte[] { 0x81, (byte)length };

            if (length <= 0xFFFF)
            {
                return new byte[]
                {
                    0x82,
                    (byte)(length >> 8),
                    (byte)length,
                };
            }

            if (length <= 0xFFFFFF)
            {
                return new byte[]
                {
                    0x83,
                    (byte)(length >> 16),
                    (byte)(length >> 8),
                    (byte)length,
                };
            }

            return new byte[]
            {
                0x84,
                (byte)(length >> 24),
                (byte)(length >> 16),
                (byte)(length >> 8),
                (byte)length,
            };
        }
    }
}