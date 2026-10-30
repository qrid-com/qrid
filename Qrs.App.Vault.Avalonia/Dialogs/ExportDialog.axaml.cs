using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Qrs.App.Vault.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.App.Vault.Avalonia.Dialogs;

public partial class ExportDialog : Window
{
    // Kind of the selected password option.
    private enum PasswordSourceKind
    {
        Reuse,
        Generate,
        Manual
    }

    // One entry in the password-option drop-down.
    private sealed class PasswordOption
    {
        public string Display { get; init; }
        public PasswordSourceKind Kind { get; init; }
        // In-vault file name to read the password from (only for Reuse).
        public string ReuseFileName { get; init; }
        public override string ToString() => Display;
    }

    // One entry in the export-format drop-down.
    private sealed class FormatOption
    {
        public string Display { get; init; }
        public ExportFormat Format { get; init; }
        public override string ToString() => Display;
    }

    private const int GeneratedPasswordLength = 16;

    private readonly List<string> _selectedPaths;
    private readonly string _defaultZipPath;
    private readonly string _defaultGpgFolder;
    private readonly List<string> _reusePasswordFileNames;

    private bool _suppressOptionChange;
    private bool _suppressFormatChange;

    private App App => (App)global::Avalonia.Application.Current;

    private ExportFormat SelectedFormat => (FormatComboBox.SelectedItem as FormatOption)?.Format ?? ExportFormat.Zip;

    public ExportDialog(List<string> selectedPaths, string defaultZipPath, string defaultGpgFolder, List<string> reusePasswordFileNames)
    {
        InitializeComponent();

        _selectedPaths = selectedPaths ?? new List<string>();
        _defaultZipPath = defaultZipPath;
        _defaultGpgFolder = defaultGpgFolder;
        _reusePasswordFileNames = reusePasswordFileNames ?? new List<string>();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        var formats = new List<FormatOption>
        {
            new FormatOption { Display = "Password-encrypted GPG files (separate GPG for each selected file)", Format = ExportFormat.Gpg },
            new FormatOption { Display = "Password-encrypted ZIP (single file)", Format = ExportFormat.Zip }
        };

        _suppressFormatChange = true;
        FormatComboBox.ItemsSource = formats;
        FormatComboBox.SelectedItem = formats.First();
        _suppressFormatChange = false;

        var options = BuildPasswordOptions();

        _suppressOptionChange = true;
        PasswordOptionComboBox.ItemsSource = options;
        PasswordOptionComboBox.SelectedItem = options.First();
        _suppressOptionChange = false;

        ApplySelectedFormat();
        ApplySelectedOption();
    }

    private List<PasswordOption> BuildPasswordOptions()
    {
        var options = new List<PasswordOption>();

        // one "Reuse '<name>'" entry per saved password file (already ordered descending)
        foreach (var fileName in _reusePasswordFileNames)
        {
            options.Add(new PasswordOption
            {
                Display = $"Reuse '{fileName}'",
                Kind = PasswordSourceKind.Reuse,
                ReuseFileName = fileName
            });
        }

        options.Add(new PasswordOption { Display = "Generate new password", Kind = PasswordSourceKind.Generate });
        options.Add(new PasswordOption { Display = "Manually type password", Kind = PasswordSourceKind.Manual });

        return options;
    }

    private void FormatComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormatChange) return;
        ApplySelectedFormat();
    }

    private void ApplySelectedFormat()
    {
        if (SelectedFormat == ExportFormat.Gpg)
        {
            SummaryTextBlock.Text = $"Exporting {App.GetFileCount(_selectedPaths.Count)} into password-encrypted (AES-256) GPG files, one per file.";
            OutputLabel.Text = "Output folder:";
            OutputPathTextBox.Text = _defaultGpgFolder;
        }
        else
        {
            SummaryTextBlock.Text = $"Exporting {App.GetFileCount(_selectedPaths.Count)} into a password-encrypted (AES-256) zip file.";
            OutputLabel.Text = "Zip file:";
            OutputPathTextBox.Text = _defaultZipPath;
        }
    }

    private void PasswordOptionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressOptionChange) return;
        ApplySelectedOption();
    }

    private void ApplySelectedOption()
    {
        var option = PasswordOptionComboBox.SelectedItem as PasswordOption;
        if (option == null) return;

        switch (option.Kind)
        {
            case PasswordSourceKind.Generate:
                PasswordInputPanel.IsVisible = true;
                PasswordTextBox.Text = CryptoUtils.GetRandomPassword(GeneratedPasswordLength);
                break;
            case PasswordSourceKind.Manual:
                PasswordInputPanel.IsVisible = true;
                PasswordTextBox.Text = string.Empty;
                break;
            case PasswordSourceKind.Reuse:
            default:
                // password is read from the vault file, no input shown
                PasswordInputPanel.IsVisible = false;
                PasswordTextBox.Text = string.Empty;
                break;
        }
    }

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectedFormat == ExportFormat.Gpg)
        {
            await BrowseFolderAsync();
            return;
        }

        await BrowseZipFileAsync();
    }

    private async Task BrowseZipFileAsync()
    {
        var currentPath = OutputPathTextBox.Text;
        var suggestedName = string.IsNullOrWhiteSpace(currentPath) ? "export.zip" : Path.GetFileName(currentPath);

        IStorageFolder? startFolder = null;
        var currentDir = string.IsNullOrWhiteSpace(currentPath) ? null : Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrWhiteSpace(currentDir))
            startFolder = await StorageProvider.TryGetFolderFromPathAsync(currentDir);

        var result = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select zip file name to export into",
            SuggestedFileName = suggestedName,
            SuggestedStartLocation = startFolder,
            ShowOverwritePrompt = true,
            DefaultExtension = "zip"
        });

        var path = result?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
            OutputPathTextBox.Text = path;
    }

    private async Task BrowseFolderAsync()
    {
        var currentPath = OutputPathTextBox.Text;

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrWhiteSpace(currentPath))
            startFolder = await StorageProvider.TryGetFolderFromPathAsync(currentPath);

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the folder to export the GPG files into",
            AllowMultiple = false,
            SuggestedStartLocation = startFolder
        });

        var path = result?.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
            OutputPathTextBox.Text = path;
    }

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var format = SelectedFormat;

        var option = PasswordOptionComboBox.SelectedItem as PasswordOption;
        if (option == null)
        {
            await ShowErrorAsync("Please choose a password option.");
            return;
        }

        var outputPath = OutputPathTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            await ShowErrorAsync(format == ExportFormat.Gpg
                ? "Please provide an output folder for the GPG files."
                : "Please provide a path for the zip file.");
            return;
        }

        // resolve the password and whether it needs to be stored in the vault
        string password;
        bool isNewPassword;
        if (option.Kind == PasswordSourceKind.Reuse)
        {
            password = App.Context.GetVaultFileText(option.ReuseFileName);
            if (string.IsNullOrEmpty(password))
            {
                await ShowErrorAsync($"Could not read a password from '{option.ReuseFileName}'.");
                return;
            }
            isNewPassword = false;
        }
        else
        {
            password = PasswordTextBox.Text ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                await ShowErrorAsync("Please enter a password.");
                return;
            }
            isNewPassword = true;
        }

        ExportButton.IsEnabled = false;
        var response = await App.DoVaultExport(format, _selectedPaths, outputPath, password, isNewPassword);
        ExportButton.IsEnabled = true;

        if (response.IsError())
        {
            await ShowErrorAsync(response.Message);
            return;
        }

        var createdFiles = response.Data.Count > 0
            ? string.Join(Environment.NewLine, response.Data)
            : outputPath;

        if (format == ExportFormat.Gpg)
        {
            await ShowInfoAsync("Exported",
                "Selected files were exported into individual password-encrypted (AES-256) GPG files:\n\n" +
                $"{createdFiles}\n\n" +
                "Decrypt each file with an OpenPGP tool, for example: gpg -d <file>.gpg (gpg will prompt for the password)");
        }
        else
        {
            await ShowInfoAsync("Exported",
                "Selected files were exported into the password-encrypted (AES-256) zip file:\n\n" +
                $"{createdFiles}\n\n" +
                "Decrypt with an AES-capable zip tool, for example: 7z x <file>.zip (7z will prompt for the password) or WinZip. Will NOT work with the 'unzip' utility b/c it does not support AES");
        }
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, WindowStartupLocation.CenterOwner);
        await box.ShowWindowDialogAsync(this);
    }

    private async Task ShowErrorAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("ERROR", message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
        await box.ShowWindowDialogAsync(this);
    }
}
