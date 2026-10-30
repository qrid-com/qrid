using Microsoft.Extensions.Configuration;
using QRCoder;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.SudoAskPass.Cli.ConsoleCommands;
using Qrs.App.SudoAskPass.Cli.Context;
using Qrs.App.SudoAskPass.Cli.Terminal;
using Qrs.Tunnel.Client;
using Spectre.Console.Cli;

namespace Qrs.App.SudoAskPass.Cli
{
    /// <summary>
    /// qrs-sudo-askpass - a SUDO_ASKPASS helper that stores the sudo password in a
    /// qrs-encrypted file and decrypts it on demand (with a QRS Mobile App confirmation)
    /// to print it on stdout, as required by the sudo_askpass contract.
    ///
    /// IMPORTANT: stdout must only ever carry the decrypted password. Every interactive
    /// element (QR codes, prompts, status, errors) is written to the controlling
    /// terminal (/dev/tty) or stderr through <see cref="TerminalIo"/>.
    /// </summary>
    public class Program : IUserInteraction
    {
        public static Program ProgramInstance { get; set; } = null!;
        IConfigurationRoot Configuration { get; set; } = null!;
        public SudoAskPassContext Context { get; set; } = null!;
        public TerminalIo Terminal { get; set; } = null!;

        public string AppRootPath => SudoAskPassPaths.AppRootPath;
        public string CookieFolderPath => AppRootPath;
        public string PublicKeyFolderPath => AppRootPath;

        public static int Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            var appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            configurationBuilder.AddJsonFile(appsettingsPath, optional: true, reloadOnChange: false);

            ProgramInstance = new Program();
            ProgramInstance.Configuration = configurationBuilder.Build();
            ProgramInstance.Terminal = new TerminalIo();

            var tunnelServiceUri = ProgramInstance.Configuration["Settings:TunnelServiceUri"] ?? "https://qrid.qrid.com/qrs-tunnel-api/";
            var tunnelServiceDomain = ProgramInstance.Configuration["Settings:TunnelServiceDomain"] ?? string.Empty;
            var apiKey = ProgramInstance.Configuration["Settings:ApiKey"];

            // create app root folder, but only if it does not exist yet
            Directory.CreateDirectory(ProgramInstance.AppRootPath);

            var asymKeyItemProvider = new FileAsymKeyItemProvider(ProgramInstance.PublicKeyFolderPath);
            var cookieProvider = new FileCookieProvider(ProgramInstance.CookieFolderPath);

            ProgramInstance.Context = new SudoAskPassContext(
                ProgramInstance, ProgramInstance.Terminal, tunnelServiceDomain, tunnelServiceUri, asymKeyItemProvider, cookieProvider, apiKey);

            // Default command is the askpass flow (this is how sudo invokes the helper).
            var app = new CommandApp<RunCommandAsync>();
            app.Configure(config =>
            {
                config.SetApplicationName("qrs-sudo-askpass");
                config.AddCommand<InitCommandAsync>("init");
                config.AddCommand<RunCommandAsync>("run");
            });

            int exitCode;
            try
            {
                exitCode = app.Run(args);
            }
            finally
            {
                ProgramInstance.Terminal.Dispose();
            }
            return exitCode;
        }

        #region IUserInteraction

        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            // nothing to clear - QR is rendered inline on the terminal
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            RenderQrCode(qrString);
        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            RenderQrCode(qrString);
            await Task.CompletedTask;
        }

        void IUserInteraction.ShowError(string message)
        {
            Terminal.WriteLine($"ERROR: {message}");
        }

        void IUserInteraction.ShowMessage(string message)
        {
            Terminal.WriteLine($"Message: {message}");
        }

        private void RenderQrCode(string qrString)
        {
            // QR (and its text) must never go to stdout - render it on the terminal/stderr
            Terminal.WriteLine(qrString);

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.L);
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphicSmall(true, false, Environment.NewLine);

            Terminal.WriteLine(qrCodeAsAsciiArt);
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            Terminal.WriteLine(title);
        }

        void IUserInteraction.HideMessageWindow()
        {
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            Terminal.WriteLine();
            Terminal.WriteLine("PAIRING with your mobile phone");
            Terminal.Write("Enter the Pairing Code displayed by your phone (3 characters, or empty to cancel): ");
            var value = Terminal.ReadLine() ?? string.Empty;
            await Task.CompletedTask;
            if (string.IsNullOrWhiteSpace(value))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled (by user entering an empty Pairing Code)");
            else
                return CommonContext.SuccessStringResponse(value.Trim());
        }

        void IUserInteraction.SetDescription(string title, string description)
        {
            throw new ApplicationException($"Unexpected call to UI.SetDescription(title='{title}', description='{description}')");
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            Terminal.WriteLine($"Connecting via this tunnel service: {tunnelServiceUri}");
        }

        #endregion
    }
}
