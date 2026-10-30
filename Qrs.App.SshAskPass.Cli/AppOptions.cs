using Qrid.Common.Client.UI;
using Qrs.App.SshAskPass.Cli.Context;

namespace Qrs.App.SshAskPass.Cli
{
    public enum AppMode
    {
        AskPass,
        Init
    }

    /// <summary>
    /// Parsed command-line options and shared runtime state for the qrs-ssh-askpass helper.
    /// </summary>
    public class AppOptions
    {
        public AppMode Mode { get; set; } = AppMode.AskPass;

        /// <summary>The single prompt argument ssh passes in the askpass flow.</summary>
        public string? Prompt { get; set; }

        /// <summary>The ssh key file name (extracted from the prompt, or supplied to init).</summary>
        public string? SshKeyFileName { get; set; }

        /// <summary>Path to the qrs-encrypted password file.</summary>
        public string PasswordFilePath { get; set; } = string.Empty;

        /// <summary>init flow: overwrite an existing password file.</summary>
        public bool Force { get; set; }

        public string TunnelServiceUri { get; set; } = string.Empty;
        public string TunnelServiceDomain { get; set; } = string.Empty;

        /// <summary>Optional API key required by some Tunnel Servers to create new sessions.</summary>
        public string? ApiKey { get; set; }

        /// <summary>The QRS client context, created by the App once Avalonia has initialised.</summary>
        public SshAskPassContext? Context { get; set; }

        /// <summary>The IUserInteraction driving the UI (the App instance).</summary>
        public IUserInteraction? UserInteraction { get; set; }
    }
}
