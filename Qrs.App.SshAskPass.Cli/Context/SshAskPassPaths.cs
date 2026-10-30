using System.Runtime.InteropServices;

namespace Qrs.App.SshAskPass.Cli.Context
{
    /// <summary>
    /// Well known locations used by the qrs-ssh-askpass helper.
    ///
    /// Everything lives under the per-user configuration directory so the helper works
    /// the same way on a local desktop session and over ssh, on Linux, macOS and Windows:
    ///   ~/.config/qrs-ssh-askpass/prod/        (Linux/macOS, via ApplicationData)
    ///   %APPDATA%\qrs-ssh-askpass\prod\        (Windows)
    /// containing the encrypted password file plus the QRS public-key / cookie state.
    /// </summary>
    public static class SshAskPassPaths
    {
        public const string ENVIRONMENT_NAME = "prod";
        public const string AppFolderName = "qrs-ssh-askpass";
        public const string DefaultPasswordFileName = "ssh-password.qrs";

        public static string AppRootPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName,
                ENVIRONMENT_NAME);

        public static string DefaultPasswordFilePath =>
            Path.Combine(AppRootPath, DefaultPasswordFileName);

        public static string GetHostName()
        {
            // Prefer the network host name, fall back to the machine name.
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                if (!string.IsNullOrWhiteSpace(hostName))
                    return hostName;
            }
            catch
            {
                // ignore and fall back below
            }
            return Environment.MachineName;
        }

        /// <summary>Restricts the file to be readable/writable by its owner only (chmod 600).</summary>
        public static void RestrictToOwner(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                File.SetUnixFileMode(filePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // best effort - do not fail the operation because of permissions
            }
        }
    }
}
