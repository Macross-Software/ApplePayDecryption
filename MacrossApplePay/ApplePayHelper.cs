using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Text;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;

namespace Macross
{
    internal static class ApplePayHelper
    {
        private static readonly byte[] s_ApplePayAlgorithmId = Encoding.UTF8.GetBytes((char)0x0d + "id-aes256-GCM");
        private static readonly byte[] s_ApplePayPartyUInfo = Encoding.UTF8.GetBytes("Apple");
        private static readonly byte[] s_ApplePayInitializationVector = new byte[16];

        public static ContentInfo BuildContentForSignatureValidation(
            string data,
            string headerEphemeralPublicKey,
            byte[] headerTransactionId,
            byte[]? headerApplicationData)
        {
            using MemoryStream ConcatenatedData = new MemoryStream();
            using BinaryWriter Writer = new BinaryWriter(ConcatenatedData);

            Writer.Write(Convert.FromBase64String(headerEphemeralPublicKey));
            Writer.Write(Convert.FromBase64String(data));
            Writer.Write(headerTransactionId);
            if (headerApplicationData != null)
                Writer.Write(headerApplicationData);

            return new ContentInfo(ConcatenatedData.ToArray());
        }

        public static void VerifyApplePaySignature(
            X509Certificate2 rootCertificateAuthority,
            string signature,
            string data,
            string headerEphemeralPublicKey,
            byte[] headerTransactionId,
            byte[]? headerApplicationData,
            int? messageTimeToLiveInSeconds = 60 * 5)
        {
            SignedCms SignedCms = new SignedCms(
                BuildContentForSignatureValidation(data, headerEphemeralPublicKey, headerTransactionId, headerApplicationData),
                detached: true);
            try
            {
                SignedCms.Decode(Convert.FromBase64String(signature));

                SignedCms.CheckSignature(verifySignatureOnly: true);
            }
            catch (Exception SignatureException)
            {
                throw new InvalidOperationException("ApplePay signature was invalid.", SignatureException);
            }

            (X509Certificate2 intermediaryCertificate, X509Certificate2 leafCertificate) = VerifySignatureCertificates(SignedCms.Certificates);
            VerifyCertificateChainTrust(rootCertificateAuthority, intermediaryCertificate, leafCertificate);
            if (messageTimeToLiveInSeconds.HasValue)
                VerifyApplePaySignatureSigningTime(SignedCms, messageTimeToLiveInSeconds.Value);
        }

        public static (X509Certificate2 intermediaryCertificate, X509Certificate2 leafCertificate) VerifySignatureCertificates(X509Certificate2Collection signatureCertificates)
        {
            if (signatureCertificates.Count != 2)
                throw new InvalidOperationException("ApplePay signature contained an invalid number of certificates.");

            X509Certificate2? IntermediaryCertificate = null;
            X509Certificate2? LeafCertificate = null;

            foreach (X509Certificate2 Certificate in signatureCertificates)
            {
                if (Certificate.Extensions["Basic Constraints"] is X509BasicConstraintsExtension BasicConstraintsExtension && BasicConstraintsExtension.CertificateAuthority)
                {
                    if (Certificate.Extensions["1.2.840.113635.100.6.2.14"] == null)
                        throw new InvalidOperationException("ApplePay signature intermediary certificate didn't contain Apple custom OID.");

                    IntermediaryCertificate = Certificate;
                    continue;
                }

                if (Certificate.Extensions["1.2.840.113635.100.6.29"] == null)
                    throw new InvalidOperationException("ApplePay signature leaf certificate didn't contain Apple custom OID.");

                LeafCertificate = Certificate;
            }

            if (LeafCertificate == null || IntermediaryCertificate == null)
                throw new InvalidOperationException("Intermediary and/or leaf certificates could not be found in PKCS7 signature.");

            return (IntermediaryCertificate, LeafCertificate);
        }

        public static void VerifyCertificateChainTrust(X509Certificate2 rootCertificateAuthority, X509Certificate2 intermediaryCertificate, X509Certificate2 leafCertificate)
        {
            using X509Chain Chain = new X509Chain();

            Chain.ChainPolicy.ExtraStore.Add(intermediaryCertificate);

            Chain.ChainPolicy.ExtraStore.Add(rootCertificateAuthority);

            Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool IsValid = Chain.Build(leafCertificate);

            IsValid = IsValid ||
                (Chain.ChainStatus.Length == 1 &&
                Chain.ChainStatus[0].Status == X509ChainStatusFlags.UntrustedRoot &&
                Chain.ChainPolicy.ExtraStore.Contains(Chain.ChainElements[Chain.ChainElements.Count - 1].Certificate));

            if (!IsValid)
                throw new InvalidOperationException("Certificate trust could not be established for PKCS7 signature certificates.");
        }

        public static void VerifyApplePaySignatureSigningTime(SignedCms signedCms, int messageTimeToLiveInSeconds)
        {
            Oid SigningTimeOid = new Oid("1.2.840.113549.1.9.5");

            DateTime? SigningTime = null;
            foreach (SignerInfo SignerInfo in signedCms.SignerInfos)
            {
                foreach (CryptographicAttributeObject SignedAttribute in SignerInfo.SignedAttributes)
                {
                    if (SignedAttribute.Oid.Value == SigningTimeOid.Value && SignedAttribute.Values.Count > 0 && SignedAttribute.Values[0] is Pkcs9SigningTime Pkcs9SigningTime)
                    {
                        SigningTime = Pkcs9SigningTime.SigningTime;
                        break;
                    }
                }
            }

            if (!SigningTime.HasValue)
                throw new InvalidOperationException("ApplePay signature SigningTime OID was not found.");

            if (DateTime.UtcNow > SigningTime.Value.AddSeconds(messageTimeToLiveInSeconds))
                throw new InvalidOperationException("ApplePay message has expired.");
        }

        public static void ValidatePaymentProcessingCertificate(X509Certificate2 paymentProcessingCertificate, string publicKeyHash)
        {
            byte[] SuppliedCertificatePublicKeyHash = Convert.FromBase64String(publicKeyHash);

            using HashAlgorithm SHA = new SHA256CryptoServiceProvider();
            byte[] CalculatedHash = SHA.ComputeHash(paymentProcessingCertificate.ExportPublicKeyInDERFormat());

            if (!SuppliedCertificatePublicKeyHash.SequenceEqual(CalculatedHash))
                throw new InvalidOperationException("Payment processing certificate does not match the publicKeyHash on the payment data.");

            if (!paymentProcessingCertificate.HasPrivateKey)
                throw new InvalidOperationException("Payment processing certificate does not have a private key.");
        }

        public static byte[] DeriveKeyMaterialUsingEllipticCurveDiffieHellmanAlgorithm(
            X509Certificate2 paymentProcessingCertificate,
            string headerEphemeralPublicKey)
        {
            string[]? CommonNameParts = paymentProcessingCertificate.GetNameInfo(X509NameType.SimpleName, false)?.Split(':');
            if (CommonNameParts == null || CommonNameParts.Length != 2)
                throw new InvalidOperationException("PaymentProcessingCertificate Common Name could not be read or it does not contain Apple MerchantId.");

            byte[] PartyVInfo;
            using (HashAlgorithm SHA = new SHA256CryptoServiceProvider())
            {
                PartyVInfo = SHA.ComputeHash(Encoding.ASCII.GetBytes(CommonNameParts[1].Trim()));
            }

            using CngKey PrivateKey = paymentProcessingCertificate.GetCngPrivateKey();
            using ECDiffieHellmanCng ECDH = new ECDiffieHellmanCng(PrivateKey);

            PublicKey EphemeralPublicKey = SecurityHelper.ParsePublicKeyFromX509Format(Convert.FromBase64String(headerEphemeralPublicKey));

            return ECDH.DeriveKeyMaterial(
                SecurityHelper.ParseECDiffieHellmanPublicKey256FromPublicKey(EphemeralPublicKey),
                s_ApplePayAlgorithmId,
                s_ApplePayPartyUInfo,
                PartyVInfo);
        }

        public static byte[] DecryptCipherDataUsingAesGcmAlgorithm(byte[] keyMaterial, byte[] cipherData)
        {
            if (keyMaterial.Length != 32)
                throw new InvalidOperationException("KeyMaterial size was invalid.");

            IBufferedCipher Cipher = CipherUtilities.GetCipher("AES/GCM/NoPadding");
            Cipher.Init(
                false,
                new ParametersWithIV(
                    ParameterUtilities.CreateKeyParameter("AES", keyMaterial),
                    s_ApplePayInitializationVector));
            return Cipher.DoFinal(cipherData);
        }
    }
}
