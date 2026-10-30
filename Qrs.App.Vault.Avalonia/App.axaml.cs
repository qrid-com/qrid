using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.Vault.Avalonia.Controls;
using Qrs.App.Vault.Avalonia.Dialogs;
using Qrs.App.Vault.Avalonia.ViewModels;
using Qrs.App.Vault.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Google.Protobuf.Reflection.MessageDescriptor;

namespace Qrs.App.Vault.Avalonia
{
    public partial class App : Application, IUserInteraction
    {
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsVault", ""); // ENV_NAME

        public Qrs.App.Vault.Client.VaultContext Context { get; set; }

        //public ILogger<App> Log { get; set; }

        // Home Page (aka Main Page)
        public Qrs.App.Vault.Avalonia.MainWindow HomePage;
        
        //public NavigationPage NavPage;


        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                HomePage = new MainWindow();
                desktop.MainWindow = HomePage;
                PrepareHomePages();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public async Task Initalize()
        {
            await PrepareContext();

            // clear vault files and UI panels
            ResetVault();

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                // user double-clicked on a vault file, need to process it here
                await DoProcessCommandLine(args[1]);
                //Done("Command Line Arguments", string.Join("<br>", args), null);
            }
            
        }

        public void ResetVault()
        {
            Context.VaultInit();
            HomePage.NewVaultPanel.InitFileCollectionFromContext();
            HomePage.VaultPanel.InitFileCollectionFromContext();
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
                    // Error
                    DoneWithFatalError($"Cannot continue b/c of the following error: {response.Message}");
            }
        }

        public void SetStatus(string status)
        {
            HomePage.StatusTextBlock.Text = status;
        }

        public void SetTheme()
        {
            //AppTheme currentTheme = Application.Current.RequestedTheme;
        }

        public void PrepareHomePages()
        {
            //HomePage ??= new HomePage();
            ////NavPage = new NavigationPage(HomePage);
        }



        public void SetQrTitle(string title, string subtitle)
        {
            ((IUserInteraction)this).SetDescription(title, subtitle);
        }

        #region IUserInteraction implementation

        void IUserInteraction.ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            HomePage.HideAll();
            HomePage.QrPanel.SetQrCodeContent(qrString);
            HomePage.QrPanel.IsVisible = true;

        }

        Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            HomePage.HideAll();
            HomePage.QrPanel.SetQrCodeContent(qrString);
            HomePage.QrPanel.IsVisible = true;
            return Task.CompletedTask;
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            Context.LogDebug($"start - HideQrCodeWindow");
            HomePage.HideAll();
            HomePage.QrPanel.SetQrCodeContent(string.Empty);

            // Show "Processing..."
            // b/c encrypting 1 Gb may take about 5-7 seconds
            HomePage.ProcessingPanel.IsVisible = true;
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            Context.LogDebug($"Message Window with TITLE - {title}"); ; // TODO - this may not be a good fit here
        }

        void IUserInteraction.ShowMessage(string message)
        {
            Context.LogDebug($"Message - {message}");
            HomePage.DisplayAlert("Message", message, "OK"); // TODO - this may not be a good fit here
        }

        void IUserInteraction.ShowError(string message)
        {
            Context.LogDebug($"ERROR - {message}");
            HomePage.DisplayAlert("ERROR", message, "OK"); // TODO - this may not be a good fit here
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            var inputTitle = inputContext == "pairing-code" ? "Pairing Code from QRS app:" : inputContext;
            var res = await HomePage.ShowInput(inputTitle);
            return string.IsNullOrWhiteSpace(res) ? CommonContext.ErrorStringResponse("Could not pair with your QRS app b/c you did not enter a Pairing Code! Please try again!") : CommonContext.SuccessStringResponse(res);
        }

        void IUserInteraction.HideMessageWindow()
        {
            Context.LogDebug("Hiding Message Window ..."); // TODO - this may not be a good fit here
        }

        void IUserInteraction.SetDescription(string title, string subtitle)
        {
            HomePage.QrPanel.SetTitle(title, subtitle);
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            SetStatus($"Tunnel Service : {tunnelServiceUri}");
        }

        #endregion





        //protected override Window CreateWindow(IActivationState? activationState)
        //{
        //    return new Window(new AppShell());
        //}



        public void ShowError(string message)
        {
            HomePage.ShowError(message);
            if (HomePage.IsAllHidden())
            {
                DoHome();
            }
        }

        public void Shutdown()
        {
            if (Application.Current is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
            else
                Environment.Exit(0);
        }

        public void DoHome()
        {
            HomePage.HideAll();
            HomePage.HomePanel.IsVisible = true;
        }

        public void ShowProcessing()
        {
            HomePage.HideAll();
            HomePage.ProcessingPanel.IsVisible = true;
        }

        public async void OnStartProcessing(object? sender, RoutedEventArgs e)
        {
            var files = new List<string> { "file1.txt", "file2.txt", "file3.txt" };

            var vm = new ProgressDialogViewModel();
            var dialog = new ProgressDialog
            {
                DataContext = vm,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Start background work
            _ = vm.RunAsync(files);

            // Wait for dialog to close and get result
            bool success = await dialog.ShowDialog<bool>(HomePage);

            if (success)
            {
                ShowError("Processing completed successfully!");
            }
            else
            {
                ShowError("Processing was cancelled or failed.");
            }
        }



        public async Task<ResponseModel> DoVaultOpen(string vaultFile = null)
        {
            if (string.IsNullOrWhiteSpace(vaultFile))
            {
                // let user pick the new file name
                var files = await HomePage.Window.StorageProvider
                .OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Select .vault file to open", AllowMultiple = false }); // todo -  FileTypeFilter = "*.vault"
                if (files.Count < 1) return Context.ErrorResponse("Cancelled by user"); // user cancelled;
                vaultFile = files[0].TryGetLocalPath();
            }

            SetQrTitle("Opening Vault:", vaultFile);

            var response = await Context.VaultOpen(vaultFile);
            if (response.IsError())
            {
                ShowError(response.Message);
                return response;
            }

            var fileInfo = new FileInfo(vaultFile);

            // remember this vault as the most recently used one
            await Context.AddRecentFile(fileInfo.FullName);

            // prepare UI
            ShowVaultPanel(fileInfo.FullName);

            return Context.SuccessResponse();
        }

        // Shows the vault panel with the current in-memory file list. Used after opening,
        // creating, or saving a vault so the open vault stays visible.
        public void ShowVaultPanel(string subtitle)
        {
            HomePage.HideAll();
            HomePage.VaultPanel.HeaderPanel.Subtitle = subtitle;
            HomePage.VaultPanel.InitFileCollectionFromContext();
            HomePage.VaultPanel.IsExpanded = true;
            HomePage.VaultPanel.IsVisible = true;
        }

        public async Task DoVaultNew()
        {
            HomePage.HideAll();
            ResetVault();
            HomePage.NewVaultPanel.IsVisible = true;
        }

        // Creates a new, empty vault in memory and shows the vault panel so the user can
        // add and edit files before saving the whole vault to an encrypted .vault file.
        public async Task DoVaultNewInMemory()
        {
            HomePage.HideAll();

            Context.VaultNewInMemory();

            ShowVaultPanel("New in-memory vault (not yet saved)");
        }

        // Adds a new, empty file to the open vault in memory and opens it in the editor.
        public async Task DoVaultAddNewFile()
        {
            if (Context.VaultWrapper?.Payload == null)
            {
                ShowError("Please create or open a vault before adding a file.");
                return;
            }

            var name = await HomePage.ShowInput("Enter a name for the new file (e.g. notes.txt):");
            if (string.IsNullOrWhiteSpace(name)) return;

            // canonical in-vault path (OS-invariant slash)
            name = name.Trim().Replace('\\', '/');

            var response = Context.VaultAddNewFile(name);
            if (response.IsError())
            {
                ShowError(response.Message);
                return;
            }

            HomePage.VaultPanel.AddFileToCollection(name);

            // open the new (empty) file straight in the editor
            await DoVaultEditFile(name);
        }

        public async Task<ResponseModel> DoVaultExtract()
        {
            Context.ContentFolder = Context.GetContentFolder(Context.VaultWrapper.File);

            // extract into content folder
            var response = await Context.VaultContentExtract(Context.VaultWrapper, Context.ContentFolder);
            if (response.IsError())
            {
                ShowError(response.Message);
                return response;
            }

            // prepare UI
            HomePage.ExtractedPanel.ContextFolderTextInput.Text = Context.ContentFolder;
            HomePage.ExtractedPanel.IsVisible = true;
            HomePage.VaultPanel.IsVisible = false;

            return Context.SuccessResponse();
        }


        public async Task DoVaultOpenExtractView(string vaultFile = null)
        {
            // open 
            var openResponse = await DoVaultOpen(vaultFile);
            if (openResponse.IsError()) return;

            // extract
            var extractResponse = await DoVaultExtract();
            if (openResponse.IsError()) return;

            // open content folder to view content
            Context.OpenFolder(Context.ContentFolder);

            // done
            Done("Success", $"Files have been extracted into this folder:%path%", Context.ContentFolder);
        }

        public async Task DoProcessCommandLine(string cmdArgument)
        {
            if (Context.IsVaultCloseCommand(cmdArgument))
                await DoVaultSaveAndClose(Context.GetContentFolderFromVaultCloseCommand(cmdArgument));
            else
                await DoVaultOpenExtractView(cmdArgument);
        }

        public async Task DoVaultSaveAndClose(string contentFolder)
        {
            var vaultFile = Context.GetVaultFileNameFromContentFolder(contentFolder);
            SetQrTitle("Saving files into:", vaultFile);
            var response = await Context.VaultContentSaveAndClose(contentFolder);
            if (response.IsSuccess())
                Done("Success", $"Files have been encrypted and saved back into %path% Content folder has been deleted", vaultFile);
            else
                ShowError(response.Message);
        }

        public string GetFileCount(List<string> fileNames)
        {
            return fileNames.Count == 1 ? "1 file" : $"{fileNames.Count} files";
        }

        public string GetFileCount(int fileCount)
        {
            return fileCount == 1 ? "1 file" : $"{fileCount} files";
        }

        public async Task DoVaultSaveNew()
        {
            // let user pick the new file name

            // this will create an empty file, which we will overwrite at the end of this method
            // we need to know the file name before we have file content b/c we use it in Associated Data when encrypting the vault

            // user picks the file name ("new.vault" is a default choice)
            var filePickerResult = await HomePage.Window.StorageProvider
                .SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Select file name to save", SuggestedFileName = "new.vault", ShowOverwritePrompt = true, DefaultExtension = "vault" });
            if (filePickerResult == null) return; // user cancelled
            var vaultPath = filePickerResult.TryGetLocalPath();
            if (vaultPath == null) return;

            SetQrTitle($"Saving {GetFileCount(Context.VaultHelper.Files.Count)} into:", vaultPath);

            var response = await Context.VaultSaveNew(vaultPath);
            if (response.IsSuccess())
                Done("Success", $"Saved {GetFileCount(Context.VaultHelper.Files.Count)} into a new encrypted vault file:%path%", vaultPath);
            else
                ShowError(response.Message);
        }

        public void Done(string title, string message, string path, bool buttonDone = true, bool buttonExit = true)
        {
            SetQrTitle("Scan with QRS app", null); // reset back to "standard" title when we dont know the intended operation description

            HomePage.HideAll();
            HomePage.DonePanel.TitleHeader.Title = title;
            HomePage.DonePanel.SetMessageAndPath(message, path);
            HomePage.DonePanel.SetButtonDoneVisible(buttonDone);
            HomePage.DonePanel.SetButtonExitVisible(buttonExit);
            HomePage.DonePanel.IsVisible = true;
        }

        public void DoneWithFatalError(string message)
        {
            HomePage.TopDockPanel.IsVisible = false;
            Done("Fatal Error", message, null, false, true);
        }

        public bool DoCancelQrCodePolling()
        {
            return Context.CancelQrCodePolling();
        }

        public async Task DoVaultClose()
        {
            // let user pick the .content folder to be closed
            var files = await HomePage.Window.StorageProvider
            .OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select .content folder to save into its .vault file", AllowMultiple = false }); // todo -  FileTypeFilter = "*.vault"
            if (files.Count < 1) return; // Context.ErrorResponse("Cancelled by user"); // user cancelled;
            var contentFolder = files[0].TryGetLocalPath();
            await DoVaultSaveAndClose(contentFolder);
        }

        // Returns the in-memory bytes of a file inside the currently open vault, or null if not found.
        public byte[] GetVaultFileContent(string inVaultPath)
        {
            var file = Context.VaultWrapper?.Payload?.Files?.FirstOrDefault(f => f.Path == inVaultPath);
            return file?.Content?.ToByteArray();
        }

        // Opens the Edit dialog for the given file (relative in-vault path) selected in the vault list.
        public async Task DoVaultEditFile(string inVaultPath)
        {
            if (Context.VaultWrapper?.Payload == null)
            {
                ShowError("Please open a vault before editing its files.");
                return;
            }

            if (string.IsNullOrWhiteSpace(inVaultPath))
            {
                ShowError("Please select a file to edit.");
                return;
            }

            var content = GetVaultFileContent(inVaultPath);
            if (content == null)
            {
                ShowError($"File '{inVaultPath}' was not found in the open vault.");
                return;
            }

            var dialog = new EditFileDialog(inVaultPath, content);
            await dialog.ShowDialog(HomePage);
        }

        // Updates the in-memory bytes of a file in the open vault and then encrypts and writes the
        // whole vault back to its .vault file. The vault stays open (the DEK is reused). If the vault
        // is a new in-memory vault that has not been saved yet, the user is asked where to save it.
        public async Task<ResponseModel> DoVaultSaveEditedFile(string inVaultPath, byte[] newContent)
        {
            var vaultWrapper = Context.VaultWrapper;
            if (vaultWrapper?.Payload == null)
                return Context.ErrorResponse("No vault is currently open.");

            var file = vaultWrapper.Payload.Files.FirstOrDefault(f => f.Path == inVaultPath);
            if (file == null)
                return Context.ErrorResponse($"File '{inVaultPath}' was not found in the open vault.");

            // update the file bytes inside the open (decrypted, in-memory) vault
            file.Content = Google.Protobuf.ByteString.CopyFrom(newContent);

            // a new in-memory vault has no file on disk yet - ask the user where to save it
            if (string.IsNullOrWhiteSpace(vaultWrapper.File))
            {
                var vaultPath = await PickVaultSavePath();
                if (string.IsNullOrWhiteSpace(vaultPath))
                    return Context.ErrorResponse("Cancelled by user");

                var fi = new FileInfo(vaultPath);
                vaultWrapper.File = fi.FullName;
                vaultWrapper.FileName = fi.Name; // used as Associated Data when encrypting the vault
                vaultWrapper.Payload.VaultFileName = fi.Name;
            }

            // mark payload as modified so VaultSave re-encrypts it
            vaultWrapper.IsModifiedPayload = true;

            SetQrTitle("Saving Vault:", vaultWrapper.File);

            var response = await Context.VaultSave(vaultWrapper);
            if (response.IsSuccess())
            {
                vaultWrapper.IsModifiedPayload = false;
                // restore the vault view (the QR flow for a first-time save hides all panels)
                ShowVaultPanel(vaultWrapper.File);
            }

            return response;
        }

        // Asks the user to pick a .vault file location to save into.
        public async Task<string> PickVaultSavePath()
        {
            var filePickerResult = await HomePage.Window.StorageProvider
                .SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Select file name to save", SuggestedFileName = "new.vault", ShowOverwritePrompt = true, DefaultExtension = "vault" });
            return filePickerResult?.TryGetLocalPath();
        }

        // Default zip path: same folder as the open vault file, with the .vault extension replaced by .zip.
        public string GetDefaultZipPath()
        {
            var vaultFile = Context.VaultWrapper?.File;
            if (string.IsNullOrWhiteSpace(vaultFile))
                return null;

            var dir = Path.GetDirectoryName(vaultFile);
            var name = Path.GetFileNameWithoutExtension(vaultFile) + ".zip";
            return string.IsNullOrEmpty(dir) ? name : Path.Combine(dir, name);
        }

        // Default export folder: same folder as the open vault file.
        public string GetDefaultExportFolder()
        {
            var vaultFile = Context.VaultWrapper?.File;
            if (string.IsNullOrWhiteSpace(vaultFile))
                return null;

            return Path.GetDirectoryName(vaultFile);
        }

        // Opens the Export dialog for the files currently selected in the vault list.
        public async Task DoVaultExportSelectedFiles()
        {
            if (Context.VaultWrapper?.Payload == null)
            {
                ShowError("Please open a vault before exporting files.");
                return;
            }

            var selected = HomePage.VaultPanel.VaultContentList.SelectedItems?.Cast<string>().ToList() ?? new List<string>();
            if (selected.Count == 0)
            {
                ShowError("Please select one or more files to export.");
                return;
            }

            var defaultZipPath = GetDefaultZipPath();
            var defaultGpgFolder = GetDefaultExportFolder();
            var reusePasswordFiles = Context.GetPasswordFileNames();

            var dialog = new ExportDialog(selected, defaultZipPath, defaultGpgFolder, reusePasswordFiles);
            await dialog.ShowDialog(HomePage);
        }

        // Creates the encrypted output (a single zip or one gpg file per selected file) and, when the
        // password is new (generated or manually typed), stores it in a new root-level vault file and
        // persists the vault.
        public async Task<ResponseModel<List<string>>> DoVaultExport(ExportFormat format, List<string> selectedPaths, string outputPath, string password, bool isNewPassword)
        {
            var exportResponse = format == ExportFormat.Gpg
                ? Context.ExportSelectedFilesToGpg(selectedPaths, outputPath, password)
                : Context.ExportSelectedFilesToZip(selectedPaths, outputPath, password);
            if (exportResponse.IsError())
                return exportResponse;

            if (isNewPassword)
            {
                SetQrTitle("Saving export password into vault:", Context.VaultWrapper.File);

                var saveResponse = await Context.AddPasswordFileAndSave(password);
                if (saveResponse.IsError())
                    return ResponseModel.ErrorResponse<List<string>>($"The files were exported, but the password could not be stored in the vault: {saveResponse.Message}");

                // refresh the vault view so the new password file shows up in the list
                ShowVaultPanel(Context.VaultWrapper.File);
            }

            return exportResponse;
        }
    }
}