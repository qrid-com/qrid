using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Crypto;
using Qrid.Common.Http;
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
        public async Task<int> InitCommandAsync(string ymlFile)
        {
            WriteHeader($"Starting [blue]init[/] command on [blue]{ymlFile}[/]");

            // read the file
            var cf = GetCommandFile(ymlFile);

            // create payload wrapper
            CommandKeyWrapper = new CommandKeyWrapper(this, cf);

            // generate new random CommandKey
            CommandKeyWrapper.Payload = CryptoUtils.GetRandomSymKey();
            
            // cycle through .Environment and ask user to input the values for each variable

            WriteHeader($"[red]Please provide values for {cf.Environment.Count} environment variables defined in your YML file[/]");

            var encryptedCount = 0;

            foreach (var env in cf.Environment)
            {
                if (string.IsNullOrWhiteSpace(env.Value))
                {
                InputValue:
                    var value = AnsiConsole.Ask<string>($"Value for [green]{env.Name}[/]? ", string.Empty);
                    var prefix = "load-from-file:";
                    if (value.StartsWith(prefix))
                    {
                        var filePath = value.Substring(prefix.Length).Trim();
                        if (File.Exists(filePath))
                        {
                            value = File.ReadAllText(filePath);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"File path [red]'{filePath}'[/] does not exist, or you do not have permissions to read it. Please provide another value");
                            goto InputValue;
                        }

                    }
                    // adding AssociatedData for envar
                    var envarAd = CommandKeyWrapper.GetAssociatedDataForEnvarItem(env.Name);
                    env.EncryptedValue = CryptoUtils.SymEncryptPaddedString(value, CommandKeyWrapper.Payload, envarAd).GetBase64String();
                    encryptedCount++;
                }
                else
                {
                    // Value is specified. That means this env variable is not sensitive, and user wants it stored in a plain text, i.e. unencrypted
                    AnsiConsole.MarkupLine($"Value for [green]{env.Name}[/]: {env.Value}");
                    env.EncryptedValue = null;
                }
            }

            if (encryptedCount == 0)
            {
                WriteInfo($"Found no environment variables to encrypt. No changes has been done to the [blue]{ymlFile}[/] file");
                return 0;
            }

            // initializing QRS client
            await InitFromConfigAsync();


            // wrap CommandFileWrapper.Payload via QRS app
            WriteHeader($"[red]Scan below QR code with QRS app[/]");
            var response = await WrapNewPayload(CommandKeyWrapper);
            if (!response.IsSuccess())
            {
                WriteError(response.Message);
                return 1;
            }

            // save wrapped payload envelope into Key
            cf.Key = GetKeyString(CommandKeyWrapper.QrsEnvelope);

            // serialize into same file (omg)
            SaveCommandFile(cf, ymlFile);

            WriteHeader($"[green]SUCCESS[/] - [blue]init[/] command on [blue]{ymlFile}[/] has been completed");

            //Program.ProgramInstance.ConsoleWriteLine("Press any key to exit ...");
            WriteLine("Press any key to exit ...");
            Console.ReadKey();

            return 0;
        }


    }
}
