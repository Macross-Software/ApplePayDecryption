using System;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Macross
{
    public partial class CertificateSigningRequestForm : Form
    {
        private byte[]? _LastGeneratedCSRContent;
        private RSACng? _LastGeneratedRSAPrivateKey;
        private ECDsaCng? _LastGeneratedECDsaPrivateKey;
        private X509Certificate2? _LastImportedSignedCertificate;

        public CertificateSigningRequestForm()
        {
            InitializeComponent();

            // .NET Core 3 WinForm Designer doesn't support much yet, so we have to stitch everything up manually. Yuck!
            CreateControls();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _LastGeneratedRSAPrivateKey?.Dispose();
                _LastGeneratedECDsaPrivateKey?.Dispose();
                _LastImportedSignedCertificate?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnGenerateButtonClick(object? sender, EventArgs e)
        {
            if (_MerchantCertificateRadioButton.Checked)
            {
                _LastGeneratedRSAPrivateKey = new RSACng(2048);

                _LastGeneratedECDsaPrivateKey?.Dispose();
                _LastGeneratedECDsaPrivateKey = null;

                CertificateRequest CertificateRequest = new CertificateRequest(
                    $"E={_EmailAddressTextBox.Text}, CN={_CommonNameTextBox.Text}, O=US",
                    _LastGeneratedRSAPrivateKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                _LastGeneratedCSRContent = CertificateRequest.CreateSigningRequest();
            }
            else
            {
                _LastGeneratedECDsaPrivateKey = new ECDsaCng(256);

                _LastGeneratedRSAPrivateKey?.Dispose();
                _LastGeneratedRSAPrivateKey = null;

                CertificateRequest CertificateRequest = new CertificateRequest(
                    $"E={_EmailAddressTextBox.Text}, CN={_CommonNameTextBox.Text}, O=US",
                    _LastGeneratedECDsaPrivateKey,
                    HashAlgorithmName.SHA256);
                _LastGeneratedCSRContent = CertificateRequest.CreateSigningRequest();
            }

            _CertificateSigningRequestContentTextBox.Text = CertificateExtensions.EncodeDERDataInPEMFormat(_LastGeneratedCSRContent, "CERTIFICATE REQUEST");

            _ExportCertificateSigningRequestButton.Enabled = true;
        }

        private void OnExportCertificateSigningRequestButtonClick(object? sender, EventArgs e)
        {
            string SubjectName = string.IsNullOrWhiteSpace(_CommonNameTextBox.Text) ? "Certificate Signing Request" : _CommonNameTextBox.Text;

            using SaveFileDialog SaveFileDialog = new SaveFileDialog
            {
                Title = "Save Certificate Signing Request",
                FileName = $"{SubjectName}.certSigningRequest",
                Filter = "Certificate Signing Requests (*.certSigningRequest)|*.certSigningRequest"
            };

            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using Stream FileStream = SaveFileDialog.OpenFile();

                using StreamWriter Writer = new StreamWriter(FileStream);

                Writer.Write(_CertificateSigningRequestContentTextBox.Text);

                _ImportAndCombineButton.Enabled = true;
            }
        }

        private void OnImportAndCombineButtonClick(object? sender, EventArgs e)
        {
            using OpenFileDialog OpenFileDialog = new OpenFileDialog
            {
                Title = "Open Certificate",
                FileName = _MerchantCertificateRadioButton.Checked ? "merchant_id.cer" : "apple_pay.cer",
                Filter = "Certificates (*.cer)|*.cer"
            };

            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                _LastImportedSignedCertificate?.Dispose();

                _LastImportedSignedCertificate = new X509Certificate2(OpenFileDialog.FileName);

                _CertificateContentTextBox.Text = CertificateExtensions.EncodeDERDataInPEMFormat(_LastImportedSignedCertificate.Export(X509ContentType.Cert), "CERTIFICATE");

                _SaveCertificateButton.Enabled = true;
            }
        }

        private void OnSaveCertificateButtonClick(object? sender, EventArgs e)
        {
            Debug.Assert(_LastImportedSignedCertificate != null);

            string SubjectName = string.IsNullOrWhiteSpace(_CommonNameTextBox.Text) ? "Certificate" : _CommonNameTextBox.Text;

            using SaveFileDialog SaveFileDialog = new SaveFileDialog
            {
                Title = "Save Certificate",
                FileName = $"{SubjectName}.pfx",
                Filter = "Certificatess (*.pfx)|*.pfx"
            };

            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using Stream FileStream = SaveFileDialog.OpenFile();

                using BinaryWriter Writer = new BinaryWriter(FileStream);

                byte[] CertificateData;

                if (_LastGeneratedRSAPrivateKey != null)
                {
                    using X509Certificate2 CertificateWithPrivateKey = _LastImportedSignedCertificate.CopyWithPrivateKey(_LastGeneratedRSAPrivateKey);

                    CertificateData = CertificateWithPrivateKey.Export(X509ContentType.Pfx, _CertificatePasswordTextBox.Text);
                }
                else
                {
                    Debug.Assert(_LastGeneratedECDsaPrivateKey != null);

                    using NativeMethods.SafeCertContextHandle certificateContext = NativeMethods.GetCertificateContext(_LastImportedSignedCertificate);

                    using SafeNCryptKeyHandle KeyHandle = _LastGeneratedECDsaPrivateKey.Key.Handle;

                    if (!NativeMethods.CertSetCertificateContextProperty(
                        certificateContext,
                        NativeMethods.CertificateProperty.NCryptKeyHandle,
                        NativeMethods.CertificateSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG,
                        KeyHandle))
                    {
                        throw new InvalidOperationException($"ECDsa private key could not be set on Certificate Win32Error [{Marshal.GetLastWin32Error()}] returned.");
                    }

                    using X509Certificate2 CertificateWithPrivateKey = new X509Certificate2(certificateContext.DangerousGetHandle());

                    CertificateData = CertificateWithPrivateKey.Export(X509ContentType.Pfx, _CertificatePasswordTextBox.Text);
                }

                Writer.Write(CertificateData);
            }
        }
    }
}
