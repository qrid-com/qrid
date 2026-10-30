using Google.Protobuf;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Utilities;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Tunnel.Client;
using Qrs.App.Vault.Client.Models;
using Qrs.App.Vault.Client.Payload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.App.Vault.Client
{

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Settings))]
    internal partial class VaultContextJsonContext : JsonSerializerContext
    {
    }
        
    public class VaultContext : ClientContext
    {
        public const string APP_NAME = "QRS VAULT";
        public const string CLOSE_FILE = "_close.vault";

        public static string AppRootPath;
        public new static FileAsymKeyItemProvider AsymKeyItemProvider { get; set; }
        public new static FileCookieProvider CookieProvider { get; set; }


        // current Vault object
        public VaultWrapper VaultWrapper { get; set; }
        public Proto.Vault Vault => VaultWrapper?.Payload;
        
        // current ContentFolder
        public string ContentFolder { get; set; }

        // current vault file with path
        public string VaultFullFileName { get; set; }

        public VaultHelper VaultHelper { get; set; }

        private JsonSerializerOptions JsonOptions { get; set; }

        public static async Task<ResponseModel<VaultContext>> CreateInitialized(string appRootPath, IUserInteraction ui)
        {
            AppRootPath = appRootPath;
            AsymKeyItemProvider = new FileAsymKeyItemProvider(appRootPath);
            CookieProvider = new FileCookieProvider(appRootPath);

            // Create Context
            var context = new VaultContext(ui, AsymKeyItemProvider, CookieProvider);
            
            // Load Settings
            await context.LoadSettings();

            // Init Context with params from Settings
            var response = await context.InitFromConfigAsync(context.Settings.TunnelServerDomain, context.Settings.TunnelServerUri, context.Settings.ApiKey);
            if (response.IsError())
                return NpErrorResponse<VaultContext>(response);

            return NpSuccessResponse(context);
        }


        public VaultContext(IUserInteraction ui, IAsymKeyItemProvider asymKeyItemProvider, ICookieProvider cookieProvider)
            : base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
        {
            VaultHelper = new VaultHelper();
            JsonOptions = new JsonSerializerOptions { TypeInfoResolver = VaultContextJsonContext.Default };
        }

        #region Settings

        public Settings Settings { get; set; }
        public string SettingsFilePath => Path.Combine(AppRootPath, "Settings.json");

        public async Task SaveSettings()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Settings, JsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public async Task LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                LoadDefaultSettings();
                await SaveSettings();
                return;
            }
            var json = await File.ReadAllTextAsync(SettingsFilePath);
            Settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json, JsonOptions);
            Settings.IsConfirmed = true;
        }

        public void LoadDefaultSettings()
        {
            Settings = new Settings();
            Settings.TunnelServerDomain = ""; // todo: change to "qrid.com"
            Settings.TunnelServerUri = "https://qrid.qrid.com/qrs-tunnel-api/"; // todo: change to ""
            Settings.IsConfirmed = false;
        }

        public const int MAX_RECENT_FILES = 6;

        // records the given vault file as the most recently used one, keeping at most
        // MAX_RECENT_FILES entries with the most recent entry first
        public async Task AddRecentFile(string vaultFile)
        {
            if (string.IsNullOrWhiteSpace(vaultFile)) return;

            Settings.RecentFiles ??= new List<string>();

            Settings.RecentFiles.RemoveAll(f => string.Equals(f, vaultFile, StringComparison.OrdinalIgnoreCase));
            Settings.RecentFiles.Insert(0, vaultFile);

            if (Settings.RecentFiles.Count > MAX_RECENT_FILES)
                Settings.RecentFiles.RemoveRange(MAX_RECENT_FILES, Settings.RecentFiles.Count - MAX_RECENT_FILES);

            await SaveSettings();
        }

        #endregion

        public string GetContentFolder(string vaultFile)
        {
            var contentPath = Path.GetDirectoryName(vaultFile);
            var vaultFileName = Path.GetFileName(vaultFile);
            var res = Path.Combine(contentPath, vaultFileName + ".content");
            return res;
        }

        public VaultWrapper VaultNew(string fileName)
        {
            var res = new VaultWrapper(this.AppName);
            res.IsNew = true;
            res.FileName = fileName;
            return res;
        }

        public void VaultInit()
        {
            if (VaultWrapper?.Payload?.Files != null)
                VaultWrapper.Payload.Files.Clear();

            VaultWrapper = null;
            VaultFullFileName = null;
            ContentFolder = null;
            VaultHelper = new VaultHelper();
        }

        // Creates a brand new, empty vault entirely in memory (no file on disk yet).
        // The vault stays open so files can be added and edited before it is saved.
        public VaultWrapper VaultNewInMemory()
        {
            VaultInit(); // reset VaultHelper

            var wrapper = new VaultWrapper(AppName);
            wrapper.IsNew = true;
            wrapper.Payload = new Proto.Vault();
            VaultWrapper = wrapper;

            return wrapper;
        }

        // Adds a new, empty file to the currently open vault in memory.
        public ResponseModel VaultAddNewFile(string inVaultPath)
        {
            if (VaultWrapper?.Payload == null)
                return ErrorResponse("No vault is currently open.");

            if (string.IsNullOrWhiteSpace(inVaultPath))
                return ErrorResponse("File name cannot be empty.");

            if (VaultWrapper.Payload.Files.Any(f => f.Path == inVaultPath))
                return ErrorResponse($"A file named '{inVaultPath}' already exists in the vault.");

            var file = new Proto.File
            {
                Path = inVaultPath,
                NodeType = "f",
                Content = ByteString.Empty,
                Padding = ""
            };
            VaultWrapper.Payload.Files.Add(file);
            VaultHelper.AddFile(inVaultPath, null);

            return SuccessResponse();
        }

        public async Task<ResponseModel> VaultSaveNew(string vaultPath)
        {
            var fileInfo = new FileInfo(vaultPath);

            // prepare vault wrapper object
            var vaultWrapper = new VaultWrapper(AppName);
            vaultWrapper.IsNew = true;
            vaultWrapper.FileName = fileInfo.Name; // will be used as Associated Data when encrypting the vault
            vaultWrapper.File = fileInfo.FullName;
            vaultWrapper.Payload = new Qrs.App.Vault.Client.Proto.Vault();
            vaultWrapper.Payload.VaultFileName = vaultWrapper.FileName;
            await LoadBuilderFilesIntoVault(vaultWrapper.Payload);

            var response = await VaultSave(vaultWrapper);
            return response;
        }

        public async Task LoadBuilderFilesIntoVault(Proto.Vault v)
        {
            foreach (var fileKV in VaultHelper.Files)
            {
                var fileName = fileKV.Value;
                var fileInfo2 = new FileInfo(fileName); // ???
                var bytes = await System.IO.File.ReadAllBytesAsync(fileName);
                var file = new Qrs.App.Vault.Client.Proto.File();
                file.Content = ByteString.CopyFrom(bytes);
                file.Path = fileKV.Key; // relative path with OS-invariant slash
                file.NodeType = "f";
                file.Padding = ""; // TODO - make random length thow away fixed char padding to avoid leaking file length
                v.Files.Add(file);
            }
        }

        public async Task<ResponseModel> VaultSave(VaultWrapper qfw)
        {
            // this will throw if invalid file path
            var fi = new System.IO.FileInfo(qfw.File);

            LogDebug("before Wrap");
            // WRAP PAYLOAD
            var response = qfw.IsNew
                ? await WrapNewPayload(qfw)
                : await WrapUpdatedPayload(qfw);
            LogDebug("after Wrap");

            if (response.IsError())
            {
                qfw.ErrorMessage = response.Message;
                //UI.ShowError(qfw.ErrorMessage);
                return response;
            }

            // Write into QRS file
            await System.IO.File.WriteAllBytesAsync(qfw.File, qfw.QrsEnvelope.GetByteArray());
            VaultFullFileName = qfw.File;

            qfw.IsNew = false;
            qfw.IsModified = false;
            qfw.IsAssociatedDataModified = false;
            return SuccessResponse();
        }

        public async Task<ResponseModel<VaultWrapper>> VaultOpen(string vaultFile)
        {
            VaultInit();

            var res = new VaultWrapper(this.AppName);

            // TODO - handle non existing file, no access to file, etc
            var fi = new FileInfo(vaultFile);
            res.File = vaultFile;
            res.FileName = fi.Name;

            res.QrsEnvelope = QrsEnvelope.Create(System.IO.File.ReadAllBytes(vaultFile));

            // UNWRAP PAYLOAD
            // this will also set res.Payload if success
            var response = await UnwrapPayload(res);
            if (response.IsError())
            {
                res.ErrorMessage = response.Message;
                //UI.ShowError(res.ErrorMessage);
                return NpErrorResponse<VaultWrapper>(response);
            }

            VaultWrapper = res;

            // also, load Vault.Files into VaultHelper b/c our caller will expect it to be in VaultHelper.Files
            foreach (var inVaultPath in Vault.Files.Select(f => f.Path))
                VaultHelper.AddFile(inVaultPath, null);

            return NpSuccessResponse<VaultWrapper>(res);
        }

        public async Task<ResponseModel> VaultContentExtract(VaultWrapper vaultWrapper, string contentFolder)
        {
            var v = vaultWrapper.Payload;
            try
            {
                // wipe out any leftover content from a previous extract so that the folder
                // contains exactly what is stored in the vault
                if (Directory.Exists(contentFolder))
                    Directory.Delete(contentFolder, true);

                Directory.CreateDirectory(contentFolder);
            }
            catch (Exception e) 
            {
                return ErrorResponse($"Error while trying to create Directory '{contentFolder}', exception is: {e.Message}");
            }
            
            foreach (var f in v.Files)
            {
                try
                {
                    // TODO - validate path, must stay inside contentFolder
                    var filePath = GetCombinedPath(contentFolder, f.Path);
                    await FileWriteAllBytesAsync(filePath, f.Content.ToByteArray());
                }
                catch (Exception e)
                {
                    return ErrorResponse($"Error while extracting the file '{f.Path}', exception is: {e.Message}");
                }
            }

            var closeVaultFile = GetCombinedPath(contentFolder, CLOSE_FILE);
            await File.WriteAllBytesAsync(closeVaultFile, Array.Empty<byte>());

            return SuccessResponse();
        }

        public async Task FileWriteAllBytesAsync(string path, byte[] bytes)
        {
            // this does not throw b/c of the missing folder - it will be created
            string directoryPath = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryPath); // this will create all missing folders, if any
            await File.WriteAllBytesAsync(path, bytes);
        }

        public async Task<ResponseModel> VaultExtract(string vaultFile, string contentFolder = null)
        {
            var openResponse = await VaultOpen(vaultFile);
            if (openResponse.IsError())
                return NpErrorResponse<ResponseModel>(openResponse);

            ContentFolder = contentFolder ?? GetContentFolder(vaultFile);
            await VaultContentExtract(VaultWrapper, ContentFolder);

            return SuccessResponse();
        }

        public static string GetRelativePath(string fromFolderPath, string toFilePath)
        {
            var fromUri = new Uri(fromFolderPath.EndsWith(Path.DirectorySeparatorChar) ? fromFolderPath : fromFolderPath + Path.DirectorySeparatorChar);
            var toUri = new Uri(toFilePath);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            // make a canonical representation by converting to a linux-style separator
            relativePath = relativePath.Replace(@"\",@"/");
            return relativePath;
        }

        public static string GetCombinedPath(string basePath, string inVaultPath)
        {
            inVaultPath = inVaultPath.Replace('/', Path.DirectorySeparatorChar);
            var res = Path.Join(basePath, inVaultPath); // Path.Combine?
            return res;
        }

        public bool IsVaultCloseCommand(string cmdArgument)
        {
            return Regex.IsMatch(cmdArgument, @"\.vault\.content[\\/]_close\.vault$");
        }

        public string GetContentFolderFromVaultCloseCommand(string cmdArgument)
        {
            return cmdArgument.Substring(0, cmdArgument.Length - 1 - CLOSE_FILE.Length);
        }

        public string GetVaultFileNameFromContentFolder(string contentFolder)
        {
            var vaultFileFullName = contentFolder.EndsWith(".content") ? contentFolder.Substring(0, contentFolder.Length - ".content".Length) : contentFolder;
            if (!vaultFileFullName.EndsWith(".vault"))
                vaultFileFullName += ".vault";
            return vaultFileFullName;
        }

        public async Task<ResponseModel> VaultContentSaveAndClose(string contentFolder)
        {
            // 1) collect files into new Vault object
            var v = new Proto.Vault();

            VaultHelper = new VaultHelper();

            // combine files and folders (root level only) into 1 list
            var paths = Directory.EnumerateFiles(contentFolder).Where(f => !IsVaultCloseCommand(f))
                .Union(Directory.EnumerateDirectories(contentFolder))
                .ToList();
            VaultHelper.AddPaths(paths);

            await LoadBuilderFilesIntoVault(v);

            // 1.1) figure VaultFileName
            var vaultFileFullName = GetVaultFileNameFromContentFolder(contentFolder);

            // wrapper
            VaultWrapper vaultWrapper = new VaultWrapper(this.AppName);
            vaultWrapper.IsNew = true;

            // TODO - refactor this - too much friction around VaultFileName - looks like maintaining it currently takes more code than it should
            vaultWrapper.FileName = Path.GetFileName(vaultFileFullName); // will go into Associated Data when encrypting the vault
            v.VaultFileName = vaultWrapper.FileName;

            // 2) wrap (encrypt) vault object
            vaultWrapper.Payload = v;
            var response = await WrapNewPayload(vaultWrapper); //.WrapUpdatedPayload();
            if (response.IsError())
            {
                return response;
            }

            // 3) save encrypted QrsEnvelope into .VAULT file
            await File.WriteAllBytesAsync(vaultFileFullName, vaultWrapper.QrsEnvelope.ToByteArray());
            VaultFullFileName = vaultFileFullName;


            // 4) delete Content folder
            Directory.Delete(contentFolder, true);

            return SuccessResponse();
            
        }

        #region File Export

        // Root-level vault files that hold saved export passwords use this prefix, so newly created
        // password files show up (and can be reused) in the export dialog next time. The same saved
        // password can be used for both zip and GPG exports.
        public const string PASSWORD_FILE_PREFIX = "__password";

        // Returns the in-vault names of root-level files that hold saved export passwords,
        // ordered descending so the most recently created one comes first.
        public List<string> GetPasswordFileNames()
        {
            if (VaultWrapper?.Payload == null)
                return new List<string>();

            return VaultWrapper.Payload.Files
                .Select(f => f.Path)
                .Where(p => !p.Contains('/') && p.StartsWith(PASSWORD_FILE_PREFIX, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p, StringComparer.Ordinal)
                .ToList();
        }

        // Reads the textual content (the saved password) of a file in the open vault.
        public string GetVaultFileText(string inVaultPath)
        {
            var file = VaultWrapper?.Payload?.Files?.FirstOrDefault(f => f.Path == inVaultPath);
            if (file == null)
                return null;
            return System.Text.Encoding.UTF8.GetString(file.Content.ToByteArray());
        }

        // Exports the selected files from the open vault into a standard, password-encrypted
        // (WinZip AES-256) zip file that can be decrypted with standard zip tools.
        // On success, Data contains the full path of the created zip file.
        public ResponseModel<List<string>> ExportSelectedFilesToZip(List<string> selectedPaths, string zipPath, string password)
        {
            if (VaultWrapper?.Payload == null)
                return NpErrorResponse<List<string>>("No vault is currently open.");

            if (selectedPaths == null || selectedPaths.Count == 0)
                return NpErrorResponse<List<string>>("No files selected to export.");

            if (string.IsNullOrEmpty(zipPath))
                return NpErrorResponse<List<string>>("Zip file path cannot be empty.");

            if (string.IsNullOrEmpty(password))
                return NpErrorResponse<List<string>>("Zip password cannot be empty.");

            var entries = new List<(string, byte[])>();
            foreach (var inVaultPath in selectedPaths)
            {
                var file = VaultWrapper.Payload.Files.FirstOrDefault(f => f.Path == inVaultPath);
                if (file == null)
                    return NpErrorResponse<List<string>>($"File '{inVaultPath}' was not found in the open vault.");
                entries.Add((inVaultPath, file.Content.ToByteArray()));
            }

            try
            {
                ZipExporter.CreateEncryptedZip(zipPath, entries, password);
            }
            catch (Exception e)
            {
                return NpErrorResponse<List<string>>($"Error while creating zip file '{zipPath}': {e.Message}");
            }

            return NpSuccessResponse(new List<string> { Path.GetFullPath(zipPath) });
        }

        // Exports the selected files from the open vault into individual OpenPGP passphrase-encrypted
        // (.gpg) files (one per selected file) inside outputFolder. Each file can be decrypted with
        // standard OpenPGP tools, for example: gpg -d <file>.gpg.
        // On success, Data contains the full paths of all the created GPG files.
        public ResponseModel<List<string>> ExportSelectedFilesToGpg(List<string> selectedPaths, string outputFolder, string password)
        {
            if (VaultWrapper?.Payload == null)
                return NpErrorResponse<List<string>>("No vault is currently open.");

            if (selectedPaths == null || selectedPaths.Count == 0)
                return NpErrorResponse<List<string>>("No files selected to export.");

            if (string.IsNullOrEmpty(outputFolder))
                return NpErrorResponse<List<string>>("Output folder cannot be empty.");

            if (string.IsNullOrEmpty(password))
                return NpErrorResponse<List<string>>("Password cannot be empty.");

            var createdFiles = new List<string>();
            foreach (var inVaultPath in selectedPaths)
            {
                var file = VaultWrapper.Payload.Files.FirstOrDefault(f => f.Path == inVaultPath);
                if (file == null)
                    return NpErrorResponse<List<string>>($"File '{inVaultPath}' was not found in the open vault.");

                var outputPath = Path.Combine(outputFolder, inVaultPath + ".gpg");
                try
                {
                    GpgExporter.CreateEncryptedFile(outputPath, inVaultPath, file.Content.ToByteArray(), password);
                }
                catch (Exception e)
                {
                    return NpErrorResponse<List<string>>($"Error while creating GPG file '{outputPath}': {e.Message}");
                }

                createdFiles.Add(Path.GetFullPath(outputPath));
            }

            return NpSuccessResponse(createdFiles);
        }

        // Stores a new export password in a root-level vault file (named with a descending-sortable
        // timestamp) and persists the whole vault so the password is not lost.
        public async Task<ResponseModel<string>> AddPasswordFileAndSave(string password)
        {
            if (VaultWrapper?.Payload == null)
                return NpErrorResponse<string>(ErrorResponse("No vault is currently open."));

            if (string.IsNullOrEmpty(password))
                return NpErrorResponse<string>(ErrorResponse("Export password cannot be empty."));

            if (string.IsNullOrWhiteSpace(VaultWrapper.File))
                return NpErrorResponse<string>(ErrorResponse("Please save the vault before storing an export password inside it."));

            var fileName = $"{PASSWORD_FILE_PREFIX}_{DateTime.Now:yyyyMMdd_HHmmss}";
            // In the unlikely case of two exports within the same second, keep the descending-sortable
            // prefix and append a short suffix so we never fail on a duplicate name.
            if (VaultWrapper.Payload.Files.Any(f => f.Path == fileName))
            {
                var baseName = fileName;
                var suffix = 1;
                do
                {
                    fileName = $"{baseName}_{suffix:D2}";
                    suffix++;
                }
                while (VaultWrapper.Payload.Files.Any(f => f.Path == fileName));
            }

            var file = new Proto.File
            {
                Path = fileName,
                NodeType = "f",
                Content = ByteString.CopyFrom(System.Text.Encoding.UTF8.GetBytes(password)),
                Padding = ""
            };
            VaultWrapper.Payload.Files.Add(file);
            VaultHelper.AddFile(fileName, null);

            // persist the new password file into the encrypted vault (DEK stays open, no QR round-trip)
            VaultWrapper.IsModifiedPayload = true;
            var response = await VaultSave(VaultWrapper);
            if (response.IsError())
                return NpErrorResponse<string>(response);

            VaultWrapper.IsModifiedPayload = false;
            return NpSuccessResponse(fileName);
        }

        #endregion

        public void OpenFolder(string folderPath)
        {
            if (OperatingSystem.IsWindows())
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "explorer",
                    Arguments = $"\"{folderPath}\"",
                    UseShellExecute = true
                };
                process.Start();
            }
            else if (OperatingSystem.IsMacCatalyst())
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "open",
                    Arguments = $"\"{folderPath}\"",
                    UseShellExecute = false
                };
                process.Start();
            }
            else
            {
                throw new PlatformNotSupportedException("This platform is not supported.");
            }
        }

    }
}
