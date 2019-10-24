using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Macross
{
    internal static class SecurityHelper
    {
        private static readonly byte[] s_Win32_ECDH_Public_256_MagicNumber = new byte[] { 0x45, 0x43, 0x4B, 0x31 };
        private static readonly byte[] s_Win32_ECDH_Public_256_Length = new byte[] { 0x20, 0x00, 0x00, 0x00 };

        public static PublicKey ParsePublicKeyFromX509Format(byte[] x509SubjectPublicKeyInfoBlob)
        {
            Oid? Oid = null;
            AsnEncodedData? Parameters = null;
            AsnEncodedData? KeyValue = null;

            using MemoryStream Stream = new MemoryStream(x509SubjectPublicKeyInfoBlob);
            using BinaryReader Reader = new BinaryReader(Stream);

            try
            {
                while (Stream.Position < Stream.Length)
                {
                    byte Token = Reader.ReadByte();
                    switch (Token)
                    {
                        case 0x30: // SEQUENCE
                        case 0x03: // BIT STRING
                        case 0x06: // OID
                            byte PayloadLength = Reader.ReadByte();
                            if (PayloadLength > 0x7F)
                                throw new InvalidOperationException($"Payload lengths > 0x74 are not supported. Found at position [{Stream.Position - 1}]");

                            if (Token == 0x30)
                                continue;

                            byte UnusedBits = 0;
                            if (Token == 0x03)
                            {
                                UnusedBits = Reader.ReadByte();
                                if (UnusedBits > 0)
                                    throw new InvalidOperationException($"Bit strings with unused bits are not supported. Found at position [{Stream.Position - 1}]");
                                PayloadLength--;
                            }

                            byte[] Payload = new byte[PayloadLength];

                            int BytesRead = Reader.Read(Payload, 0, Payload.Length);
                            if (BytesRead != PayloadLength)
                                throw new InvalidOperationException($"Payload length did not match. Found at position [{Stream.Position - 1}]");

                            if (Token == 0x06)
                            {
                                if (Oid == null)
                                    Oid = new Oid(ConvertOidByteArrayToStringValue(Payload));
                                else
                                    Parameters = new AsnEncodedData(Payload);
                            }
                            if (Token == 0x03)
                                KeyValue = new AsnEncodedData(Payload);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown token byte [{Token:X2}] found at position [{Stream.Position - 1}].");
                    }
                }

                if (Oid == null || Parameters == null || KeyValue == null)
                    throw new InvalidOperationException("Required information could not be found in blob.");

                return new PublicKey(Oid, Parameters, KeyValue);
            }
            catch (Exception ParseException)
            {
                throw new InvalidOperationException("Public key could not be parsed from X.509 format blob.", ParseException);
            }
        }

        public static string ConvertOidByteArrayToStringValue(byte[] oid)
        {
            StringBuilder retVal = new StringBuilder();

            for (int i = 0; i < oid.Length; i++)
            {
                if (i == 0)
                {
                    int b = oid[0] % 40;
                    int a = (oid[0] - b) / 40;
                    retVal.Append($"{a}.{b}");
                }
                else if (oid[i] < 128)
                    retVal.Append($".{oid[i]}");
                else
                {
                    retVal.Append($".{((oid[i] - 128) * 128) + oid[i + 1]}");
                    i++;
                }
            }

            return retVal.ToString();
        }

        public static ECDiffieHellmanPublicKey ParseECDiffieHellmanPublicKey256FromPublicKey(PublicKey publicKey)
        {
            if (publicKey.EncodedKeyValue.RawData.Length != 65)
                throw new InvalidOperationException("KeyData length is invalid.");

            byte[] FinalBuffer = new byte[72];

            using (MemoryStream Stream = new MemoryStream(FinalBuffer))
            {
                using BinaryWriter Writer = new BinaryWriter(Stream);

                Writer.Write(s_Win32_ECDH_Public_256_MagicNumber);
                Writer.Write(s_Win32_ECDH_Public_256_Length);
                Writer.Write(publicKey.EncodedKeyValue.RawData, 1, publicKey.EncodedKeyValue.RawData.Length - 1);
            }

            return ECDiffieHellmanCngPublicKey.FromByteArray(FinalBuffer, CngKeyBlobFormat.EccPublicBlob);
        }
    }
}
