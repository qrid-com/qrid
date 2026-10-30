using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;

namespace Qrs.App.SshAskPass.Cli
{
    public partial class MainWindow : Window
    {
        public App? Host { get; set; }

        private TaskCompletionSource<string?>? _pairingTcs;
        private TaskCompletionSource<InitFormResult?>? _initTcs;
        private TaskCompletionSource<bool>? _closeTcs;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Header / status

        public void SetTitle(string title) => TitleText.Text = title;

        public void SetSubtitle(string subtitle) => SubtitleText.Text = subtitle;

        public void SetStatus(string status) => StatusText.Text = status;

        #endregion

        public void HideAllPanels()
        {
            QrPanel.IsVisible = false;
            PairingPanel.IsVisible = false;
            InitPanel.IsVisible = false;
            MessagePanel.IsVisible = false;
        }

        #region QR code

        public void ShowQr(string qrString)
        {
            HideAllPanels();

            var qrGen = new QRCodeGenerator();
            var data = qrGen.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.L);
            var qrCode = new PngByteQRCode(data);
            var pngBytes = qrCode.GetGraphic(4);
            using var pngStream = new MemoryStream(pngBytes);
            QrImage.Source = new Bitmap(pngStream);

            QrPanel.IsVisible = true;
        }

        public void HideQr()
        {
            QrPanel.IsVisible = false;
        }

        private void CancelQrButton_Click(object? sender, RoutedEventArgs e)
        {
            Host?.CancelQrPolling();
        }

        #endregion

        #region Pairing code input

        public Task<string?> ShowPairingInputAsync(string promptLabel)
        {
            HideAllPanels();
            PairingPrompt.Text = promptLabel;
            PairingInput.Text = string.Empty;
            PairingPanel.IsVisible = true;
            PairingInput.Focus();

            _pairingTcs = new TaskCompletionSource<string?>();
            return _pairingTcs.Task;
        }

        private void PairingOkButton_Click(object? sender, RoutedEventArgs e)
        {
            PairingPanel.IsVisible = false;
            _pairingTcs?.TrySetResult(PairingInput.Text);
        }

        private void PairingCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            PairingPanel.IsVisible = false;
            _pairingTcs?.TrySetResult(null);
        }

        #endregion

        #region Init form

        public Task<InitFormResult?> ShowInitFormAsync(string? prefillKeyFile)
        {
            HideAllPanels();
            KeyFileInput.Text = prefillKeyFile ?? string.Empty;
            PasswordInput.Text = string.Empty;
            ConfirmInput.Text = string.Empty;
            InitPanel.IsVisible = true;
            if (string.IsNullOrWhiteSpace(prefillKeyFile))
                KeyFileInput.Focus();
            else
                PasswordInput.Focus();

            _initTcs = new TaskCompletionSource<InitFormResult?>();
            return _initTcs.Task;
        }

        private void InitSaveButton_Click(object? sender, RoutedEventArgs e)
        {
            var keyFile = KeyFileInput.Text?.Trim() ?? string.Empty;
            var password = PasswordInput.Text ?? string.Empty;
            var confirm = ConfirmInput.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyFile))
            {
                SetStatus("Please enter the ssh key file name.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Passphrase cannot be empty.");
                return;
            }
            if (password != confirm)
            {
                SetStatus("The two passphrases do not match.");
                return;
            }

            SetStatus(string.Empty);
            InitPanel.IsVisible = false;
            _initTcs?.TrySetResult(new InitFormResult(keyFile, password));
        }

        private void InitCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            InitPanel.IsVisible = false;
            _initTcs?.TrySetResult(null);
        }

        #endregion

        #region Final message

        /// <summary>Shows a final message with a Close button and completes when it is closed.</summary>
        public Task ShowFinalMessageAsync(string message)
        {
            HideAllPanels();
            MessageText.Text = message;
            MessagePanel.IsVisible = true;
            CloseButton.Focus();

            _closeTcs = new TaskCompletionSource<bool>();
            return _closeTcs.Task;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            _closeTcs?.TrySetResult(true);
        }

        #endregion
    }

    public record InitFormResult(string SshKeyFileName, string Password);
}
