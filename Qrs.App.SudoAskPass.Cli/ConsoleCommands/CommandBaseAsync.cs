using Spectre.Console.Cli;

namespace Qrs.App.SudoAskPass.Cli.ConsoleCommands
{
    public class CommandBaseAsync<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
    {
        public Program Instance => Program.ProgramInstance;

        public Context.SudoAskPassContext Context => Program.ProgramInstance.Context;

        public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
        {
            return 0;
        }
    }
}
