using Qrid.Common.Crypto;
using Qrid.Common.Proto.Crypto;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace Qrs.App.Qrs.Cli.ConsoleCommands
{
    public class RunCommandAsync : CommandBaseAsync<RunCommandAsync.Settings>
    {


        public class Settings : CommandSettings
        {
            [Description("Path to YAML file that contains qrs command definition")]
            [CommandArgument(0, "<file>")]
            public string? File { get; init; }
        }

        public async override Task<int> ExecuteAsync(CommandContext context, RunCommandAsync.Settings settings)
        {
            return await Context.RunCommandAsync(settings.File);
        }


    }
}
