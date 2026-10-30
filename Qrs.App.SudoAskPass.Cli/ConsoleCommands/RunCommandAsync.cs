using Qrs.App.SudoAskPass.Cli.Context;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Qrs.App.SudoAskPass.Cli.ConsoleCommands
{
    /// <summary>
    /// Default (askpass) command: decrypts the sudo password and writes it to stdout.
    ///
    /// This is the flow sudo invokes via SUDO_ASKPASS. sudo passes a single argument -
    /// the prompt string - which is captured here as the optional [prompt] argument.
    /// </summary>
    public class RunCommandAsync : CommandBaseAsync<RunCommandAsync.Settings>
    {
        public class Settings : CommandSettings
        {
            [Description("Prompt string passed by sudo (shown on the terminal for context only).")]
            [CommandArgument(0, "[prompt]")]
            public string? Prompt { get; init; }

            [Description("Path to the qrs-encrypted sudo password file. Defaults to the well known location.")]
            [CommandOption("-f|--file <FILE>")]
            public string? File { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var filePath = string.IsNullOrWhiteSpace(settings.File)
                ? SudoAskPassPaths.DefaultPasswordFilePath
                : settings.File!;
            return await Context.RunCommandAsync(filePath, settings.Prompt);
        }
    }
}
