using Qrid.Common.Crypto;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Qrs.App.Qrs.Cli.ConsoleCommands
{
    public class InitCommandAsync: CommandBaseAsync<InitCommandAsync.Settings>
    {


        public class Settings : CommandSettings
        {
            [Description("Path to YAML file that contains qrs command definition")]
            [CommandArgument(0, "<file>")]
            public string? File { get; init; }
        }

        public async override Task<int> ExecuteAsync(CommandContext context, InitCommandAsync.Settings settings)
        {
            return await Context.InitCommandAsync(settings.File);
        }


    }
}
