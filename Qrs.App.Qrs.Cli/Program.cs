using Microsoft.Extensions.Configuration;
using QRCoder;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.Qrs.Cli.ConsoleCommands;
using Qrs.App.Qrs.Cli.Context;
using Qrs.Tunnel.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Qrs.App.Qrs.Cli
{
    public class Program : IUserInteraction
    {
        public static Program ProgramInstance { get; set; }
        IConfigurationRoot Configuration { get; set; }
        public QrsCliContext Context { get; set; }

        public const string ENVIRONMENT_NAME = "prod";
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsProcessLauncher", ENVIRONMENT_NAME);
        public string CookieFolderPath => AppRootPath;
        public string PublicKeyFolderPath => AppRootPath;

        public static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            //configurationBuilder.AddUserSecrets(typeof(Program).GetTypeInfo().Assembly, optional: false); // needs this pkg: Microsoft.Extensions.Configuration.UserSecrets
            //configurationBuilder.AddEnvironmentVariables(); // needs this pkg: Microsoft.Extensions.Configuration.EnvironmentVariables

            ProgramInstance = new Program(); // omg why dont we create smth like ApplicationService class here
            ProgramInstance.Configuration = configurationBuilder.Build();
            // todo - better solution to default settings would be to hardcode default TunnelServiceDomain to "qrid.com" and that will make it to "discover" qrs tunnel uri from online qrid.com config (and cache discovery results locally for faster startup time)
            var tunnelServiceUri = ProgramInstance.Configuration["Settings:TunnelServiceUri"] ?? "https://qrid.qrid.com/qrs-tunnel-api/";
            var tunnelServiceDomain = ProgramInstance.Configuration["Settings:TunnelServiceDomain"];
            var apiKey = ProgramInstance.Configuration["Settings:ApiKey"];
            
            // create app root folder, but only if it does not exist yet
            Directory.CreateDirectory(ProgramInstance.AppRootPath);

            var asymKeyItemProvider = new FileAsymKeyItemProvider(ProgramInstance.PublicKeyFolderPath);
            var cookieProvider = new FileCookieProvider(ProgramInstance.CookieFolderPath);

            ProgramInstance.Context = new QrsCliContext(ProgramInstance, tunnelServiceDomain, tunnelServiceUri, asymKeyItemProvider, cookieProvider, apiKey);

            //await ProgramInstance.RunAsync();

            var app = new CommandApp<RunCommandAsync>();
            app.Configure(config =>
            {
                config.AddCommand<InitCommandAsync>("init");
                config.AddCommand<RunCommandAsync>("run");
            });

            int exitCode = app.Run(args);
            ProgramInstance.Exit(exitCode);
        }

        public void Exit(int exitCode = 0)
        {
            System.Environment.Exit(exitCode);
        }

        public void ConsoleWriteLine(string message)
        {
            Console.WriteLine(message);
        }
        public void ConsoleWrite(string message)
        {
            Console.Write(message);
        }

        private async Task RunAsync()
        {

            //var cf = Context.GetCommandFile(@"SampleFiles\example1_template.yml");
            //Context.SaveCommandFile(cf, @"SampleFiles\example1_serialized.yml");

        }



        #region IUserInteraction
        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
                    Invoked_HideQrCodeWindow(sessionId);
            

        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                

        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                

        }

        void IUserInteraction.ShowError(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
                    Invoked_ShowMessage(message, true);
            
        }

        void IUserInteraction.ShowMessage(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
                    Invoked_ShowMessage(message, false);
            
        }

        public void Invoked_ShowMessage(string message, bool isError)
        {
            ConsoleWriteLine($"{(isError ? "ERROR: " : "Message: ")}{message}");
        }



        public void Invoked_ShowQrCodeWindow(string sessionId, string qrString)
        {
            // invoked from UI thread

            //var res = qrWindow.ShowDialog(this);

            ConsoleWriteLine($"{qrString}");


            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.L);
            AsciiQRCode qrCode = new AsciiQRCode(qrCodeData);
            string qrCodeAsAsciiArt = qrCode.GetGraphicSmall(true, false, Environment.NewLine);

            ConsoleWriteLine(qrCodeAsAsciiArt);


            //ConsoleWriteLine($"End   - Invoked_ShowQrCodeWindow");
        }

        public void Invoked_HideQrCodeWindow(string sessionId)
        {
            // invoked from UI thread
            AnsiConsole.Clear();

            //ConsoleWriteLine($"Start - Invoked_HideQrCodeWindow");
            //ConsoleWriteLine(string.Empty);
            //ConsoleWriteLine(string.Empty);
            //ConsoleWriteLine(string.Empty);
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            ConsoleWriteLine($"ShowMessageWindow()");
        }

        void IUserInteraction.HideMessageWindow()
        {
            ConsoleWriteLine($"HideMessageWindow()");
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            AnsiConsole.Clear();
            ConsoleWriteLine($" ");
            ConsoleWriteLine($" ");
            ConsoleWriteLine($" ");
            Context.WriteHeader($"[red]PAIRING with your mobile phone[/]");
            var value = AnsiConsole.Ask<string>($"Enter the [green]Pairing Code[/] displayed by your phone (3 characters, or empty to cancel): ", string.Empty);
            if (string.IsNullOrWhiteSpace(value))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled (by user entering an empty Pairing Code)");
            else
                return CommonContext.SuccessStringResponse(value);
        }

        void IUserInteraction.SetDescription(string title, string description)
        {
            throw new ApplicationException($"Unexpected call to UI.SetDescription(title='{title}', description='{description}')");
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            ConsoleWriteLine($"Connecting via this tunnel service: {tunnelServiceUri}");
        }

        #endregion




    }
}
