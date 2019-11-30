using System.Windows.Forms;
using System.Drawing;

namespace Macross
{
    public partial class CertificateSigningRequestForm
    {
#pragma warning disable CA2000
#pragma warning disable CA2213
        private readonly RadioButton _MerchantCertificateRadioButton = new RadioButton
        {
            Text = "Merchant Identity Certificate (RSA)",
            Checked = true,
            AutoSize = true
        };

        private readonly RadioButton _PaymentProcessingCertificateRadioButton = new RadioButton
        {
            Text = "Payment Processing Certificate (ECC)",
            AutoSize = true
        };

        private readonly TextBox _EmailAddressTextBox = new TextBox
        {
            Dock = DockStyle.Fill
        };

        private readonly TextBox _CommonNameTextBox = new TextBox
        {
            Dock = DockStyle.Fill
        };

        private readonly Button _GenerateButton = new Button
        {
            Text = "Generate CSR",
            AutoSize = true
        };

        private readonly Button _ExportCertificateSigningRequestButton = new Button
        {
            Text = "Export CSR",
            AutoSize = true,
            Enabled = false
        };

        private readonly TextBox _CertificateSigningRequestContentTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Font = new Font(FontFamily.GenericMonospace, 8.25F)
        };

        private readonly Button _ImportAndCombineButton = new Button
        {
            Text = "Import signed certificate and combine with private key",
            AutoSize = true,
            Enabled = false
        };

        private readonly Button _SaveCertificateButton = new Button
        {
            Text = "Save Certificate",
            AutoSize = true,
            Enabled = false
        };

        private readonly TextBox _CertificatePasswordTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PasswordChar = '*'
        };

        private readonly TextBox _CertificateContentTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Font = new Font(FontFamily.GenericMonospace, 8.25F)
        };

        private void CreateControls()
        {
            SuspendLayout();

            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            MainLayout.Controls.Add(CreateTypeSelectionControls(), 0, 0);
            MainLayout.Controls.Add(CreateEmailAddressControls(), 0, 1);
            MainLayout.Controls.Add(CreateCommonNameControls(), 0, 2);
            MainLayout.Controls.Add(CreateGenerateAndExportControls(), 0, 3);
            MainLayout.Controls.Add(CreateCertificateSigningRequestContentControls(), 0, 4);
            MainLayout.Controls.Add(CreateSeparatorControls(), 0, 5);
            MainLayout.Controls.Add(CreateImportControls(), 0, 6);
            MainLayout.Controls.Add(CreateCertificatePasswordControls(), 0, 7);
            MainLayout.Controls.Add(CreateCertificateContentControls(), 0, 8);

            Controls.Add(MainLayout);

            ResumeLayout(false);
            PerformLayout();
        }

        private Control CreateTypeSelectionControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Radio buttons
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Radio buttons
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Remaining space

            MainLayout.Controls.Add(new Label
            {
                Text = "Certificate Type",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            MainLayout.Controls.Add(_MerchantCertificateRadioButton, 1, 0);
            MainLayout.Controls.Add(_PaymentProcessingCertificateRadioButton, 2, 0);

            return MainLayout;
        }

        private Control CreateEmailAddressControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // TextBox

            MainLayout.Controls.Add(new Label
            {
                Text = "User Email Address",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            MainLayout.Controls.Add(_EmailAddressTextBox, 1, 0);

            return MainLayout;
        }

        private Control CreateCommonNameControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // TextBox

            MainLayout.Controls.Add(new Label
            {
                Text = "Certificate Common Name",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            MainLayout.Controls.Add(_CommonNameTextBox, 1, 0);

            return MainLayout;
        }

        private Control CreateGenerateAndExportControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Generate Button
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Export Button
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Remaining space

            _GenerateButton.Click += OnGenerateButtonClick;
            _ExportCertificateSigningRequestButton.Click += OnExportCertificateSigningRequestButtonClick;

            MainLayout.Controls.Add(_GenerateButton, 1, 0);
            MainLayout.Controls.Add(_ExportCertificateSigningRequestButton, 2, 0);

            return MainLayout;
        }

        private Control CreateCertificateSigningRequestContentControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Content TextBox

            MainLayout.Controls.Add(_CertificateSigningRequestContentTextBox, 1, 0);

            return MainLayout;
        }

        private static Control CreateSeparatorControls()
        {
            return new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(10)
            };
        }

        private Control CreateImportControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Import Button
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Remaining space

            MainLayout.Controls.Add(new Label
            {
                Text = "Apple Signed Certificate",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            _ImportAndCombineButton.Click += OnImportAndCombineButtonClick;

            MainLayout.Controls.Add(_ImportAndCombineButton, 1, 0);

            return MainLayout;
        }

        private Control CreateCertificatePasswordControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Password TextBox
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Save Button

            MainLayout.Controls.Add(new Label
            {
                Text = "Certificate Password",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            _SaveCertificateButton.Click += OnSaveCertificateButtonClick;

            MainLayout.Controls.Add(_CertificatePasswordTextBox, 1, 0);
            MainLayout.Controls.Add(_SaveCertificateButton, 2, 0);

            return MainLayout;
        }

        private Control CreateCertificateContentControls()
        {
            TableLayoutPanel MainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
            MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Content TextBox

            MainLayout.Controls.Add(_CertificateContentTextBox, 1, 0);

            return MainLayout;
        }
    }
#pragma warning restore CA2000
#pragma warning restore CA2213
}
