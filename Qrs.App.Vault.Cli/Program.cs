using Microsoft.Extensions.Configuration;
using QRCoder;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.Tunnel.Client;
using Qrs.Tunnel.Shared.Models;
using Qrs.App.Vault.Client;
using Spectre.Console;
using System.CommandLine;
using static System.Net.Mime.MediaTypeNames;

namespace Qrs.App.Vault.Cli
{
    public class Program : IUserInteraction
    {
        public static Program ProgramInstance { get; set; }
        public VaultContext Context { get; set; }
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsVaultCli");

        public static async Task Main(string[] args)
        {





            ProgramInstance = new Program(); // omg why dont we create smth like ApplicationService class here

            // create app root folder, but only if it does not exist yet
            Directory.CreateDirectory(ProgramInstance.AppRootPath);

            // create and init context
            await ProgramInstance.PrepareContext();

            // if we have not yet Exit'ed with error, then continue here



            // Define argument for commands (used by both save and extract)
            var fileNameArgument = new Argument<string>(
                name: "vault-file",
                description: "The name of the vault file to extract.");

            var contentFolderArgument = new Argument<string>(
                name: "content-folder",
                description: "The name of the content folder with files to be saved into encrypted vault file.");

            // Define a separate argument for the root command as the default file
            var defaultFileNameArgument = new Argument<string>(
                name: "vault-file",
                description: "The vault file to extract content from, if no other command is specified.");

            // Define the "save" command
            var saveCommand = new Command("save", "saves all files in the content folder into encrypted vault file, then deletes the content folder")
            {
                contentFolderArgument
            };

            // Define the "extract" command
            var extractCommand = new Command("extract", "extracts files from the encrypted vault file into a content folder")
            {
                fileNameArgument
            };

            // Set handlers for commands
            saveCommand.SetHandler(async (fileInfo) =>
            {
                await ProgramInstance.HandleSaveCommand(fileInfo);
            }, contentFolderArgument);

            extractCommand.SetHandler(async (fileInfo) =>
            {
                await ProgramInstance.HandleExtractCommand(fileInfo);
            }, fileNameArgument);


            // Create a root command and add the subcommands and the default file argument
            var rootCommand = new RootCommand("Encrypt files and folders into a vault file. Instead of entering passwords, just scan a QR code.")
            {
                saveCommand,
                extractCommand,
                defaultFileNameArgument // Add the default file argument to the root command
            };

            // Set a handler for the root command to act as the default "extract" command
            rootCommand.SetHandler(async (defaultFileInfo) =>
            {
                await ProgramInstance.HandleExtractCommand(defaultFileInfo); // Call the extract handler with the default file
            }, defaultFileNameArgument);


            // Invoke the command parser
            var exitCode = await rootCommand.InvokeAsync(args);

            ProgramInstance.Exit(exitCode);
        }


        // Command handlers (same as before)
        async Task<int> HandleSaveCommand(string fileInfo)
        {
            var contentFolder = Path.GetFullPath(fileInfo);

            var response = await Context.VaultContentSaveAndClose(contentFolder);
            if (response.IsSuccess())
            {
                WriteInfo($"Success - content have been encrypted into a vault file: '{Context.VaultFullFileName}'");
                return 0;
            }
            else
            {
                WriteError(response.Message);
                return 2;
            }
        }

        async Task<int> HandleExtractCommand(string fileInfo)
        {
            var file = File.Exists(fileInfo) || fileInfo.EndsWith(".vault")
                ? fileInfo
                : $"{fileInfo}.vault";

            var response = await Context.VaultExtract(file);
            if (response.IsSuccess())
            {
                var contentFolderPath = Path.GetFullPath(Context.ContentFolder);
                WriteInfo($"Success - content has been extracted to folder: '{contentFolderPath}'");
                return 0;
            }
            else
            {
                WriteError(response.Message);
                return 2;
            }
        }




        public async Task PrepareContext()
        {
            if (Context == null)
            {
                // create and init context
                var response = await VaultContext.CreateInitialized(AppRootPath, this);

                if (response.IsSuccess())
                    // Success
                    Context = response.Data;
                else
                {
                    // Error
                    ConsoleWriteLine($"Error - cannot continue b/c of the following error: {response.Message}");
                    Exit(1);
                }
                    
            }
        }

        public void Exit(int exitCode = 0)
        {
            System.Environment.Exit(exitCode);
        }

        public void ConsoleWriteLine(string message)
        {
            Console.WriteLine(message);
        }
        public void ConsoleWrite(string message)
        {
            Console.Write(message);
        }


        #region IUserInteraction
        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread
            
            Invoked_HideQrCodeWindow(sessionId);
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            Invoked_ShowQrCodeWindow(sessionId, qrString);
        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            Invoked_ShowQrCodeWindow(sessionId, qrString);
        }

        void IUserInteraction.ShowError(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            
            Invoked_ShowMessage(message, true);
        }

        void IUserInteraction.ShowMessage(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            Invoked_ShowMessage(message, false);
        }

        public void Invoked_ShowMessage(string message, bool isError)
        {
            ConsoleWriteLine($"{(isError ? "ERROR: " : "Message: ")}{message}");
        }



        public void Invoked_ShowQrCodeWindow(string sessionId, string qrString)
        {
            // invoked from UI thread

            //var res = qrWindow.ShowDialog(this);

            ConsoleWriteLine($"{qrString}");


            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.L);
            AsciiQRCode qrCode = new AsciiQRCode(qrCodeData);
            string qrCodeAsAsciiArt = qrCode.GetGraphicSmall(true, false, Environment.NewLine);

            ConsoleWriteLine(qrCodeAsAsciiArt);


            //ConsoleWriteLine($"End   - Invoked_ShowQrCodeWindow");
        }

        public void Invoked_HideQrCodeWindow(string sessionId)
        {
            // invoked from UI thread
            AnsiConsole.Clear();

            //ConsoleWriteLine($"Start - Invoked_HideQrCodeWindow");
            //ConsoleWriteLine(string.Empty);
            //ConsoleWriteLine(string.Empty);
            //ConsoleWriteLine(string.Empty);
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            ConsoleWriteLine($"ShowMessageWindow()");
        }

        void IUserInteraction.HideMessageWindow()
        {
            ConsoleWriteLine($"HideMessageWindow()");
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            AnsiConsole.Clear();
            ConsoleWriteLine($" ");
            ConsoleWriteLine($" ");
            ConsoleWriteLine($" ");
            WriteHeader($"[red]PAIRING with your mobile phone[/]");
            var value = AnsiConsole.Ask<string>($"Enter the [green]Pairing Code[/] displayed by your phone (3 characters, or empty to cancel): ", string.Empty);
            if (string.IsNullOrWhiteSpace(value))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled (by user entering an empty Pairing Code)");
            else
                return CommonContext.SuccessStringResponse(value);
        }

        void IUserInteraction.SetDescription(string title, string description)
        {
            throw new ApplicationException($"Unexpected call to UI.SetDescription(title='{title}', description='{description}')");
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            ConsoleWriteLine($"Connecting via this tunnel service: {tunnelServiceUri}");
        }

        #endregion

        public void WriteHeader(string header)
        {
            var rule = new Rule(header).LeftJustified();
            AnsiConsole.Write(rule);
        }

        public void WriteError(string message)
        {

            AnsiConsole.MarkupLine($"[red]ERROR[/] - {message}");
        }

        public void WriteInfo(string message)
        {

            AnsiConsole.MarkupLine($"{message}");
        }

        public void WriteLine(string message)
        {

            AnsiConsole.WriteLine($"{message}");
        }
    }
}
