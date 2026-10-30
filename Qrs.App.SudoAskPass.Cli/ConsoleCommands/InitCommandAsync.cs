using Qrs.App.SudoAskPass.Cli.Context;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Qrs.App.SudoAskPass.Cli.ConsoleCommands
{
    /// <summary>
    /// init command: stores the sudo password (qrs-encrypted) into the well known file.
    /// </summary>
    public class InitCommandAsync : CommandBaseAsync<InitCommandAsync.Settings>
    {
        public class Settings : CommandSettings
        {
            [Description("Path to the qrs-encrypted sudo password file. Defaults to the well known location.")]
            [CommandOption("-f|--file <FILE>")]
            public string? File { get; init; }

            [Description("Overwrite the password file if it already exists.")]
            [CommandOption("--force")]
            public bool Force { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var filePath = string.IsNullOrWhiteSpace(settings.File)
                ? SudoAskPassPaths.DefaultPasswordFilePath
                : settings.File!;
            return await Context.InitCommandAsync(filePath, settings.Force);
        }
    }
}
