using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Crypto;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.App.Qrs.Cli.Models;
using Qrs.App.Qrs.Cli.Payload;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Proto.Extensions;
using Qrs.Shared.Data;
using Qrs.Tunnel.Client;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qrs.App.Qrs.Cli.Context
{
    public partial class QrsCliContext 
    {
        public async Task<int> RunCommandAsync(string ymlFile)
        {
            WriteHeader($"Starting [blue]run[/] command on [blue]{ymlFile}[/]");

            // read the file
            var cf = GetCommandFile(ymlFile);
            // create wrapper, create QrsEnvelope from the key string
            CommandKeyWrapper = new CommandKeyWrapper(this, cf);
            CommandKeyWrapper.QrsEnvelope = QrsEnvelope.Create(GetKeyString(cf.Key).WrappedKey);
            
            // initializing QRS client
            await InitFromConfigAsync();

            // UNWRAP PAYLOAD (CommandKey) via QRS app
            WriteHeader($"[red]Scan below QR code with QRS app[/]");
            
            var response = await UnwrapPayload(CommandKeyWrapper);

            if (response.IsError())
            {
                WriteError(response.Message);
                return 1;
            }
            
            // all is good - now our CommandKey is unwrapped into
            // CommandKeyWrapper.Payload

            // decrypt environment variables using CommandKey
            var envars = new Dictionary<string, string>();
            foreach (var env in cf.Environment)
            {
                if (string.IsNullOrWhiteSpace(env.Value))
                {
                    // only use EncryptedValue if no (unencrypted) Value has been specified
                    // extract and validate AssociatedData from env.EncryptedValue
                    var envarSeb = SymEncryptedBytes.Create(env.EncryptedValue);
                    var envarAd = CommandKeyWrapper.GetAssociatedDataForEnvarItem(env.Name);
                    // decrypt envar value
                    // this will throw if envarAd is not valid
                    var decryptedValue = CryptoUtils.SymDecryptPaddedString(envarSeb, CommandKeyWrapper.Payload, envarAd);
                    envars.Add(env.Name, decryptedValue);
                }
                else
                {
                    // unencrypted Value has been specified, take it, even if EncryptedValue is also there
                    envars.Add(env.Name, env.Value);
                }
            }

            // ========= EXECUTE COMMAND ===========

            AnsiConsole.Clear();
            WriteHeader($"Starting your Process: [blue]{cf.ProcessFile}[/] [green]{cf.ProcessArguments}[/]");

            var executeTask = Execute(cf.ProcessFile, cf.ProcessArguments, envars);
            var res = await executeTask.WaitAsync(TimeSpan.FromSeconds(20));

            WriteHeader($"[green]SUCCESS[/] - [blue]run[/] command on [blue]{ymlFile}[/] has been executed");

            //Instance.ConsoleWriteLine("Press any key to exit ...");
            //Console.ReadKey();

            return 0;
        }


    }
}
