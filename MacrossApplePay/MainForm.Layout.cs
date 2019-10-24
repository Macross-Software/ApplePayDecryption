using System.Windows.Forms;

namespace Macross
{
	public partial class MainForm
	{
#pragma warning disable CA2000
#pragma warning disable CA2213
#pragma warning disable IDE0069
		private const string BrowseRootCertificateAuthorityTag = "RootCA";
		private const string BrowsePaymentProcessingCertificateTag = "PaymentCert";

		private readonly Label _BrowseCertificateAuthorityLabel = new Label
		{
			Text = "[Root Certificate Authority not loaded, click browse.]",
			AutoSize = true,
			Anchor = AnchorStyles.Left
		};

		private readonly Label _BrowsePaymentProcessingCertificateLabel = new Label
		{
			Text = "[Payment Processing Certificate not loaded, click browse.]",
			AutoSize = true,
			Anchor = AnchorStyles.Left
		};

		private readonly TextBox _PaymentProcessingCertificatePasswordTextBox = new TextBox
		{
			Dock = DockStyle.Fill,
			PasswordChar = '*'
		};

		private readonly TextBox _CipherTextBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			WordWrap = false,
			ScrollBars = ScrollBars.Both,
			Text =
@"	{
		""paymentData"": {
			""version"": ""EC_v1"",
			""data"": null,
			""signature"": null,
			""header"": {
				""ephemeralPublicKey"": null,
				""publicKeyHash"": null,
				""transactionId"": null
			}
		},
		""paymentMethod"": {
			""displayName"": null,
			""network"": null,
			""type"": null
		},
		""transactionIdentifier"": null
	}"
		};

		private readonly CheckBox _ValidateSigningTimeCheckBox = new CheckBox
		{
			Checked = true,
			Anchor = AnchorStyles.Left
		};

		private readonly TextBox _PlaintextTextBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			WordWrap = false,
			ScrollBars = ScrollBars.Both
		};

		private void CreateControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			MainLayout.Controls.Add(CreateRootCertificateControls(), 0, 0);
			MainLayout.Controls.Add(CreatePaymentProcessingCertificateControls(), 0, 1);
			MainLayout.Controls.Add(CreatePaymentProcessingCertificatePasswordControls(), 0, 2);
			MainLayout.Controls.Add(CreateCipherControls(), 0, 3);
			MainLayout.Controls.Add(CreateSigningTimeValidationControls(), 0, 4);
			MainLayout.Controls.Add(CreateDecryptionControls(), 0, 5);
			MainLayout.Controls.Add(CreatePlaintextControls(), 0, 6);

			Controls.Add(MainLayout);
		}

		private Control CreateRootCertificateControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // File path label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Browse button

			MainLayout.Controls.Add(new Label
			{
				Text = "Root Certificate Authority",
				AutoSize = true,
				Anchor = AnchorStyles.Left
			}, 0, 0);

			Button BrowseButton = new Button
			{
				Text = "Browse",
				Tag = BrowseRootCertificateAuthorityTag,
				AutoSize = true
			};

			BrowseButton.Click += OnBrowseButtonClick;

			MainLayout.Controls.Add(_BrowseCertificateAuthorityLabel, 1, 0);

			MainLayout.Controls.Add(BrowseButton, 2, 0);

			return MainLayout;
		}

		private Control CreatePaymentProcessingCertificateControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // File path label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Browse button

			MainLayout.Controls.Add(new Label
			{
				Text = "Payment Processing Certificate",
				AutoSize = true,
				Anchor = AnchorStyles.Left
			}, 0, 0);

			Button BrowseButton = new Button
			{
				Text = "Browse",
				Tag = BrowsePaymentProcessingCertificateTag,
				AutoSize = true
			};

			BrowseButton.Click += OnBrowseButtonClick;

			MainLayout.Controls.Add(_BrowsePaymentProcessingCertificateLabel, 1, 0);

			MainLayout.Controls.Add(BrowseButton, 2, 0);

			return MainLayout;
		}

		private Control CreatePaymentProcessingCertificatePasswordControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Password textbox

			MainLayout.Controls.Add(new Label
			{
				Text = "Payment Processing Certificate Password",
				AutoSize = true,
				Anchor = AnchorStyles.Left
			}, 0, 0);

			MainLayout.Controls.Add(_PaymentProcessingCertificatePasswordTextBox, 1, 0);

			return MainLayout;
		}

		private Control CreateCipherControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // File path label

			MainLayout.Controls.Add(new Label
			{
				Text = "Payment Token JSON",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Top
			}, 0, 0);

			MainLayout.Controls.Add(_CipherTextBox, 1, 0);

			return MainLayout;
		}

		private Control CreateSigningTimeValidationControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Signing time validation controls

			MainLayout.Controls.Add(new Label
			{
				Text = "Validate Signing Time",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Top
			}, 0, 0);

			MainLayout.Controls.Add(_ValidateSigningTimeCheckBox, 1, 0);

			return MainLayout;
		}

		private Control CreateDecryptionControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			MainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			MainLayout.Controls.Add(new Label
			{
				BorderStyle = BorderStyle.Fixed3D,
				Height = 2,
				Dock = DockStyle.Top,
				Margin = new Padding(10)
			}, 0, 0);

			MainLayout.Controls.Add(new Label
			{
				BorderStyle = BorderStyle.Fixed3D,
				Height = 2,
				Dock = DockStyle.Top,
				Margin = new Padding(10)
			}, 0, 2); ;

			Button DecryptButton = new Button
			{
				Text = "Decrypt",
				Anchor = AnchorStyles.None,
				AutoSize = true
			};

			DecryptButton.Click += OnDecryptButtonClick;

			MainLayout.Controls.Add(DecryptButton, 0, 1);

			return MainLayout;
		}

		private Control CreatePlaintextControls()
		{
			TableLayoutPanel MainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true
			};

			MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Heading label
			MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // File path label

			MainLayout.Controls.Add(new Label
			{
				Text = "Payment Data JSON",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Top
			}, 0, 0);

			MainLayout.Controls.Add(_PlaintextTextBox, 1, 0);

			return MainLayout;
		}
#pragma warning restore CA2000
#pragma warning restore CA2213
#pragma warning restore IDE0069
	}
}
