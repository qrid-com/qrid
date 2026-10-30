using Avalonia;
using Microsoft.Extensions.Configuration;
using Qrs.App.SshAskPass.Cli.Context;

namespace Qrs.App.SshAskPass.Cli
{
    /// <summary>
    /// qrs-ssh-askpass - an SSH_ASKPASS helper (Avalonia UI) that stores an ssh key
    /// passphrase in a qrs-encrypted file and decrypts it on demand (with a QRS Mobile
    /// App confirmation) to print it on stdout, as required by the ssh_askpass contract.
    ///
    /// Usage:
    ///   qrs-ssh-askpass [prompt]        (default askpass flow - invoked by ssh)
    ///   qrs-ssh-askpass init [keyfile]  (init flow - store the passphrase)
    ///
    /// Common options (both flows):
    ///   -f, --file FILE    path to the qrs-encrypted password file (defaults to the
    ///                      well known location).
    ///   --force            (init only) overwrite the password file if it already exists.
    ///
    /// IMPORTANT: in the askpass flow, stdout must only ever carry the decrypted
    /// passphrase. Every interactive element lives in the Avalonia window.
    /// </summary>
    internal class Program
    {
        public static AppOptions Options { get; private set; } = new AppOptions();

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't
        // initialized yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            Options = ParseArgs(args);

            // create app root folder, but only if it does not exist yet
            Directory.CreateDirectory(SshAskPassPaths.AppRootPath);

            var configurationBuilder = new ConfigurationBuilder();
            var appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            configurationBuilder.AddJsonFile(appsettingsPath, optional: true, reloadOnChange: false);
            var configuration = configurationBuilder.Build();

            Options.TunnelServiceUri = configuration["Settings:TunnelServiceUri"] ?? "https://qrid.qrid.com/qrs-tunnel-api/";
            Options.TunnelServiceDomain = configuration["Settings:TunnelServiceDomain"] ?? string.Empty;
            Options.ApiKey = configuration["Settings:ApiKey"];

            // The SshAskPassContext is created by App once Avalonia has initialised, because
            // the App itself is the IUserInteraction that drives the QRS flow in the UI.

            // Avalonia must not try to parse the ssh-supplied prompt as its own arguments.
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static AppOptions ParseArgs(string[] args)
        {
            var options = new AppOptions();
            var positional = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-f":
                    case "--file":
                        if (i + 1 < args.Length)
                            options.PasswordFilePath = args[++i];
                        break;
                    case "--force":
                        options.Force = true;
                        break;
                    default:
                        positional.Add(arg);
                        break;
                }
            }

            if (positional.Count > 0 &&
                string.Equals(positional[0], "init", StringComparison.OrdinalIgnoreCase))
            {
                options.Mode = AppMode.Init;
                // optional ssh key file argument for the init flow
                if (positional.Count > 1)
                    options.SshKeyFileName = positional[1];
            }
            else
            {
                options.Mode = AppMode.AskPass;
                // the single argument ssh passes to the askpass helper is the prompt
                if (positional.Count > 0)
                    options.Prompt = positional[0];
            }

            if (string.IsNullOrWhiteSpace(options.PasswordFilePath))
                options.PasswordFilePath = SshAskPassPaths.DefaultPasswordFilePath;

            return options;
        }
    }
}
