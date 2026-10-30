using Qrs.App.Qrs.Cli.Context;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Qrs.Cli.ConsoleCommands
{
    public class CommandBaseAsync<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
    {
        public Program Instance => Program.ProgramInstance;

        public QrsCliContext Context => Program.ProgramInstance.Context;

        public async override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
        {
            return 0;
        }
    }
}
