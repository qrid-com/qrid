using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.SshAskPass.Cli.Context;
using Qrs.Tunnel.Client;

namespace Qrs.App.SshAskPass.Cli
{
    public partial class App : Application, IUserInteraction
    {
        private MainWindow _main = null!;
        private SshAskPassContext _context = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _main = new MainWindow { Host = this };
                desktop.MainWindow = _main;

                // create the QRS client context, with this App acting as the IUserInteraction
                var asymKeyItemProvider = new FileAsymKeyItemProvider(SshAskPassPaths.AppRootPath);
                var cookieProvider = new FileCookieProvider(SshAskPassPaths.AppRootPath);
                _context = new SshAskPassContext(
                    this,
                    Program.Options.TunnelServiceDomain,
                    Program.Options.TunnelServiceUri,
                    asymKeyItemProvider,
                    cookieProvider,
                    Program.Options.ApiKey);
                Program.Options.Context = _context;
                Program.Options.UserInteraction = this;

                // start the flow once the window has actually opened
                _main.Opened += (_, _) => Dispatcher.UIThread.Post(async () => await RunAsync());
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void CancelQrPolling()
        {
            _context.CancelQrCodePolling();
        }

        #region Flow orchestration

        private async Task RunAsync()
        {
            try
            {
                if (Program.Options.Mode == AppMode.Init)
                    await RunInitAsync();
                else
                    await RunAskPassAsync();
            }
            catch (Exception ex)
            {
                await _main.ShowFinalMessageAsync($"Unexpected error: {ex.Message}");
                ExitWith(1);
            }
        }

        /// <summary>
        /// askpass flow (invoked by ssh via SSH_ASKPASS): decrypt the stored passphrase and
        /// write it to stdout. Only the passphrase is ever written to stdout.
        /// </summary>
        private async Task RunAskPassAsync()
        {
            var keyFileName = SshPromptParser.ExtractKeyFileName(Program.Options.Prompt);

            _main.SetTitle("QRS SSH AskPass");
            _main.SetSubtitle($"Host: {SshAskPassPaths.GetHostName()}\nSSH key: {keyFileName}");

            var response = await _context.DecryptPasswordAsync(Program.Options.PasswordFilePath, keyFileName);
            if (response.IsError())
            {
                await _main.ShowFinalMessageAsync($"Could not get the ssh passphrase:\n\n{response.Message}");
                ExitWith(1);
                return;
            }

            // ONLY the passphrase is written to stdout (this is what ssh reads).
            Console.Out.Write(response.Data.StringValue);
            Console.Out.Write('\n');
            Console.Out.Flush();

            ExitWith(0);
        }

        /// <summary>
        /// init flow: ask the user for the passphrase of a given ssh key file and qrs-encrypt
        /// it into the well known password file for later use by the askpass flow.
        /// </summary>
        private async Task RunInitAsync()
        {
            _main.SetTitle("QRS SSH AskPass - Setup");
            _main.SetSubtitle($"Host: {SshAskPassPaths.GetHostName()}");

            var form = await _main.ShowInitFormAsync(Program.Options.SshKeyFileName);
            if (form == null)
            {
                // cancelled by the user
                ExitWith(1);
                return;
            }

            if (File.Exists(Program.Options.PasswordFilePath) && !Program.Options.Force)
            {
                await _main.ShowFinalMessageAsync(
                    $"Password file already exists:\n{Program.Options.PasswordFilePath}\n\nRe-run with --force to overwrite it.");
                ExitWith(1);
                return;
            }

            var response = await _context.EncryptPasswordAsync(
                Program.Options.PasswordFilePath, form.SshKeyFileName, form.Password);
            if (response.IsError())
            {
                await _main.ShowFinalMessageAsync($"Failed to store the ssh passphrase:\n\n{response.Message}");
                ExitWith(1);
                return;
            }

            await _main.ShowFinalMessageAsync(
                $"SUCCESS\n\nEncrypted ssh passphrase stored in:\n{Program.Options.PasswordFilePath}");
            ExitWith(0);
        }

        private void ExitWith(int exitCode)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                Dispatcher.UIThread.Post(() => desktop.Shutdown(exitCode));
            else
                Environment.Exit(exitCode);
        }

        #endregion

        #region IUserInteraction implementation

        void IUserInteraction.ShowUsernameWindow(Action<string> continueWithUsername)
        {
            // not used by the askpass helper
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            Dispatcher.UIThread.Post(() => _main.ShowQr(qrString));
        }

        Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            Dispatcher.UIThread.Post(() => _main.ShowQr(qrString));
            return Task.CompletedTask;
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _main.HideQr();
                _main.SetStatus("Processing ...");
            });
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            Dispatcher.UIThread.Post(() => _main.SetStatus(title));
        }

        void IUserInteraction.ShowMessage(string message)
        {
            Dispatcher.UIThread.Post(() => _main.SetStatus(message));
        }

        void IUserInteraction.ShowError(string message)
        {
            Dispatcher.UIThread.Post(() => _main.SetStatus($"ERROR: {message}"));
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            var label = inputContext == "pairing-code"
                ? "Pairing with your QRS app.\nEnter the Pairing Code displayed by your phone:"
                : inputContext;

            var value = await Dispatcher.UIThread.InvokeAsync(() => _main.ShowPairingInputAsync(label));

            if (string.IsNullOrWhiteSpace(value))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled (no Pairing Code was entered).");
            return CommonContext.SuccessStringResponse(value.Trim());
        }

        void IUserInteraction.HideMessageWindow()
        {
            // nothing to clear
        }

        void IUserInteraction.SetDescription(string title, string subtitle)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _main.SetTitle(title);
                _main.SetSubtitle(subtitle);
            });
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            Dispatcher.UIThread.Post(() => _main.SetStatus($"Tunnel Service: {tunnelServiceUri}"));
        }

        #endregion
    }
}
