using System;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Macross
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // .NET Core 3 WinForm Designer doesn't support much yet, so we have to stitch everything up manually. Yuck!
            CreateControls();
        }

        private void OnBrowseButtonClick(object? sender, EventArgs e)
        {
            if (!(sender is Button BrowseButton))
                return;

            using OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Certificate File"
            };

            string? ButtonTag = BrowseButton.Tag as string;

            switch (ButtonTag)
            {
                case BrowseRootCertificateAuthorityTag:
                    if (_BrowseCertificateAuthorityLabel.Text[0] != '[')
                        openFileDialog.FileName = _BrowseCertificateAuthorityLabel.Text;
                    openFileDialog.Filter = "Certificates (*.cer)|*.cer";
                    break;
                case BrowsePaymentProcessingCertificateTag:
                    if (_BrowsePaymentProcessingCertificateLabel.Text[0] != '[')
                        openFileDialog.FileName = _BrowsePaymentProcessingCertificateLabel.Text;
                    openFileDialog.Filter = "PKCS #12 Certificates (*.pfx)|*.pfx";
                    break;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                switch (ButtonTag)
                {
                    case BrowseRootCertificateAuthorityTag:
                        _BrowseCertificateAuthorityLabel.Text = openFileDialog.FileName;
                        break;
                    case BrowsePaymentProcessingCertificateTag:
                        _BrowsePaymentProcessingCertificateLabel.Text = openFileDialog.FileName;
                        break;
                }
            }
        }

        private void OnDecryptButtonClick(object? sender, EventArgs e)
        {
            if (_BrowseCertificateAuthorityLabel.Text[0] == '[' || _BrowsePaymentProcessingCertificateLabel.Text[0] == '[')
            {
                MessageBox.Show(
                    "Please load Root Certificate Authority and/or Payment Processing Certificate.",
                    "Certificates Not Loaded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using X509Certificate2? RootCertificateAuthority = LoadCertificate(
                "Root Certificate Authority",
                _BrowseCertificateAuthorityLabel.Text,
                null);

            using X509Certificate2? PaymentProcessingCertificate = LoadCertificate(
                "Payment Processing Certificate",
                _BrowsePaymentProcessingCertificateLabel.Text,
                _PaymentProcessingCertificatePasswordTextBox.Text);

            ApplePayPaymentToken? Token = ParseApplePayPaymentToken();

            if (PaymentProcessingCertificate == null
                || RootCertificateAuthority == null
                || Token == null)
                return;

            if (!VerifyApplePaySignature(RootCertificateAuthority, Token))
                return;

            if (!VerifyApplePayPaymentProcessingCertificate(PaymentProcessingCertificate, Token))
                return;

            byte[]? KeyMaterial = DeriveApplePayKeyMaterial(PaymentProcessingCertificate, Token);
            if (KeyMaterial == null)
                return;

            byte[]? Plaintext = DecryptApplePayData(KeyMaterial, Token);
            if (Plaintext == null)
                return;

            ApplePayDecryptedPaymentData PaymentData = JsonSerializer.Deserialize<ApplePayDecryptedPaymentData>(Encoding.UTF8.GetString(Plaintext));

            _PlaintextTextBox.Text = JsonSerializer.Serialize(
                PaymentData,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }

#pragma warning disable CA1031 // Do not catch general exception types
        private X509Certificate2? LoadCertificate(string name, string path, string? password)
        {
            try
            {
                return new X509Certificate2(path, password);
            }
            catch (Exception certificateException)
            {
                MessageBox.Show(
                    $"{name} could not be loaded:\r\n\r\n{certificateException}",
                    "Certificate Load Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        private ApplePayPaymentToken? ParseApplePayPaymentToken()
        {
            try
            {
                return JsonSerializer.Deserialize<ApplePayPaymentToken>(_CipherTextBox.Text);
            }
            catch (Exception paymentTokenException)
            {
                MessageBox.Show(
                    $"Payment Token JSON could not be parsed:\r\n\r\n{paymentTokenException}",
                    "JSON Parse Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        private bool VerifyApplePaySignature(X509Certificate2 rootCertificateAuthority, ApplePayPaymentToken token)
        {
            try
            {
                if (token.PaymentData?.Signature == null
                    || token.PaymentData?.Data == null
                    || token.PaymentData?.Header?.EphemeralPublicKey == null
                    || token.PaymentData?.Header?.TransactionId == null)
                    throw new InvalidOperationException("Required signature data was not found on Payment Token JSON.");

                ApplePayHelper.VerifyApplePaySignature(
                    rootCertificateAuthority,
                    token.PaymentData.Signature,
                    token.PaymentData.Data,
                    token.PaymentData.Header.EphemeralPublicKey,
                    token.PaymentData.Header.TransactionId.ToByteArray(),
                    token.PaymentData.Header.ApplicationData?.ToByteArray(),
                    _ValidateSigningTimeCheckBox.Checked ? 300 : (int?)null);

                return true;
            }
            catch (Exception signatureValidationException)
            {
                MessageBox.Show(
                    $"Payment Token Signature validation failed:\r\n\r\n{signatureValidationException}",
                    "Apple Pay Signature Verification Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private bool VerifyApplePayPaymentProcessingCertificate(X509Certificate2 paymentProcessingCertificate, ApplePayPaymentToken token)
        {
            try
            {
                if (token.PaymentData?.Header?.PublicKeyHash == null)
                    throw new InvalidOperationException("Required header data was not found on Payment Token JSON.");

                ApplePayHelper.ValidatePaymentProcessingCertificate(
                    paymentProcessingCertificate,
                    token.PaymentData.Header.PublicKeyHash);

                return true;
            }
            catch (Exception paymentProcessingCertificateException)
            {
                MessageBox.Show(
                    $"Payment Processing Certificate validation failed:\r\n\r\n{paymentProcessingCertificateException}",
                    "Apple Pay Payment Processing Certificate Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private byte[]? DeriveApplePayKeyMaterial(X509Certificate2 paymentProcessingCertificate, ApplePayPaymentToken token)
        {
            try
            {
                if (token.PaymentData?.Header?.EphemeralPublicKey == null)
                    throw new InvalidOperationException("Required header data was not found on Payment Token JSON.");

                return ApplePayHelper.DeriveKeyMaterialUsingEllipticCurveDiffieHellmanAlgorithm(
                    paymentProcessingCertificate,
                    token.PaymentData.Header.EphemeralPublicKey);
            }
            catch (Exception keyDerivationException)
            {
                MessageBox.Show(
                    $"Apple Pay key derivation failed:\r\n\r\n{keyDerivationException}",
                    "Apple Pay Key Derivation Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        private byte[]? DecryptApplePayData(byte[] keyMaterial, ApplePayPaymentToken token)
        {
            try
            {
                if (token.PaymentData?.Data == null)
                    throw new InvalidOperationException("Required payment data was not found on Payment Token JSON.");

                return ApplePayHelper.DecryptCipherDataUsingAesGcmAlgorithm(keyMaterial, Convert.FromBase64String(token.PaymentData.Data));
            }
            catch (Exception keyDerivationException)
            {
                MessageBox.Show(
                    $"Apple Pay decryption failed:\r\n\r\n{keyDerivationException}",
                    "Apple Pay Key Decryption Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
