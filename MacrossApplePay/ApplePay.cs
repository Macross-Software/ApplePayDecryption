using System.Text.Json.Serialization;

namespace Macross
{
    public class ApplePayPaymentDataHeader
    {
        [JsonPropertyName("applicationData")]
        public string? ApplicationData { get; set; }

        [JsonPropertyName("ephemeralPublicKey")]
        public string? EphemeralPublicKey { get; set; }

        [JsonPropertyName("wrappedKey")]
        public string? WrappedKey { get; set; }

        [JsonPropertyName("publicKeyHash")]
        public string? PublicKeyHash { get; set; }

        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }
    }

    public class ApplePayPaymentData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("header")]
        public ApplePayPaymentDataHeader? Header { get; set; }
    }

    public class ApplePayPaymentMethod
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("network")]
        public string? Network { get; set; }

        [JsonPropertyName("type")]
        public string? NetworkType { get; set; }
    }

    public class ApplePayPaymentToken
    {
        [JsonPropertyName("paymentData")]
        public ApplePayPaymentData? PaymentData { get; set; }

        [JsonPropertyName("paymentMethod")]
        public ApplePayPaymentMethod? PaymentMethod { get; set; }

        [JsonPropertyName("transactionIdentifier")]
        public string? TransactionIdentifier { get; set; }
    }

    public class ApplePayDecryptedPaymentData
    {
        [JsonPropertyName("applicationPrimaryAccountNumber")]
        public string? ApplicationPrimaryAccountNumber { get; set; }

        [JsonPropertyName("applicationExpirationDate")]
        public string? ApplicationExpirationDate { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("transactionAmount")]
        public int? TransactionAmount { get; set; }

        [JsonPropertyName("cardholderName")]
        public string? CardholderName { get; set; }

        [JsonPropertyName("deviceManufacturerIdentifier")]
        public string? DeviceManufacturerIdentifier { get; set; }

        [JsonPropertyName("paymentDataType")]
        public string? PaymentDataType { get; set; }

        [JsonPropertyName("paymentData")]
        public ApplePayDecryptedPaymentDataDetails? PaymentData { get; set; }
    }

    public class ApplePayDecryptedPaymentDataDetails
    {
        [JsonPropertyName("onlinePaymentCryptogram")]
        public string? OnlinePaymentCryptogram { get; set; }

        [JsonPropertyName("eciIndicator")]
        public string? EciIndicator { get; set; }

        [JsonPropertyName("emvData")]
        public string? EmvData { get; set; }

        [JsonPropertyName("encryptedPINData")]
        public string? EncryptedPinData { get; set; }
    }
}
