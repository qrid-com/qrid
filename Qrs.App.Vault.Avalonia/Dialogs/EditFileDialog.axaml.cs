using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Vault.Avalonia.Dialogs;

public partial class EditFileDialog : Window
{
    // Encoding choice shown in the drop-down.
    private sealed class EncodingOption
    {
        public string Name { get; init; }
        public Encoding Encoding { get; init; }
        public override string ToString() => Name;
    }

    // New-line choice shown in the drop-down.
    private sealed class NewLineOption
    {
        public string Name { get; init; }
        public string NewLine { get; init; }
        public override string ToString() => Name;
    }

    private readonly string _inVaultPath;

    // The bytes that currently represent the file. Re-decoded when the encoding is changed,
    // and updated after a successful save.
    private byte[] _fileBytes;

    private bool _isDirty;

    private void SetDirty(bool value)
    {
        _isDirty = value;
        DirtyLabel.Content = value ? "Modified" : string.Empty;
    }
    private bool _suppressDirty;
    private bool _suppressEncodingChange;
    private bool _forceClose;
    private string _originalText = string.Empty;

    private EncodingOption _currentEncoding;

    private App App => (App)global::Avalonia.Application.Current;

    public EditFileDialog(string inVaultPath, byte[] fileBytes)
    {
        InitializeComponent();

        _inVaultPath = inVaultPath;
        _fileBytes = fileBytes ?? Array.Empty<byte>();

        Title = $"Edit: {inVaultPath}";
    }

    private static List<EncodingOption> BuildEncodingOptions() => new()
    {
        new EncodingOption { Name = "UTF-8", Encoding = new UTF8Encoding(false) },
        new EncodingOption { Name = "UTF-8 with BOM", Encoding = new UTF8Encoding(true) },
        new EncodingOption { Name = "UTF-16 LE", Encoding = Encoding.Unicode },
        new EncodingOption { Name = "UTF-16 BE", Encoding = Encoding.BigEndianUnicode },
        new EncodingOption { Name = "UTF-32", Encoding = Encoding.UTF32 },
        new EncodingOption { Name = "ASCII", Encoding = Encoding.ASCII },
        new EncodingOption { Name = "ISO-8859-1 (Latin-1)", Encoding = Encoding.Latin1 },
    };

    private static List<NewLineOption> BuildNewLineOptions() => new()
    {
        new NewLineOption { Name = "Windows (CRLF)", NewLine = "\r\n" },
        new NewLineOption { Name = "Unix (LF)", NewLine = "\n" },
    };

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        var encodingOptions = BuildEncodingOptions();
        var newLineOptions = BuildNewLineOptions();

        _suppressEncodingChange = true;
        EncodingComboBox.ItemsSource = encodingOptions;
        _currentEncoding = encodingOptions.First(); // UTF-8 by default
        EncodingComboBox.SelectedItem = _currentEncoding;
        _suppressEncodingChange = false;

        NewLineComboBox.ItemsSource = newLineOptions;

        // Load the content once so we can detect the existing line-ending style.
        var initialText = _currentEncoding.Encoding.GetString(_fileBytes);
        var detectedNewLine = initialText.Contains("\r\n") ? "\r\n" : "\n";
        NewLineComboBox.SelectedItem = newLineOptions.First(o => o.NewLine == detectedNewLine);

        SetEditorText(initialText);
    }

    private void SetEditorText(string text)
    {
        _suppressDirty = true;
        ContentBox.Text = text;
        _suppressDirty = false;
        _originalText = text;
        SetDirty(false);
    }

    private void ReloadFromBytes(Encoding encoding)
    {
        SetEditorText(encoding.GetString(_fileBytes));
    }

    private void ContentBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressDirty) return;
        SetDirty(!string.Equals(ContentBox.Text ?? string.Empty, _originalText, StringComparison.Ordinal));
    }

    private async void EncodingComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressEncodingChange) return;

        var selected = EncodingComboBox.SelectedItem as EncodingOption;
        if (selected == null || selected == _currentEncoding) return;

        if (_isDirty)
        {
            var confirmed = await ConfirmAsync(
                "Unsaved changes",
                "This file has unsaved changes. Changing the encoding reloads the file and discards these changes. Continue?");
            if (!confirmed)
            {
                // revert the selection back to the previous encoding
                _suppressEncodingChange = true;
                EncodingComboBox.SelectedItem = _currentEncoding;
                _suppressEncodingChange = false;
                return;
            }
        }

        _currentEncoding = selected;
        ReloadFromBytes(selected.Encoding);
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        var encoding = (EncodingComboBox.SelectedItem as EncodingOption)?.Encoding ?? new UTF8Encoding(false);
        var newLine = (NewLineComboBox.SelectedItem as NewLineOption)?.NewLine ?? "\n";

        // normalize all line endings, then apply the chosen style
        var text = ContentBox.Text ?? string.Empty;
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        if (newLine != "\n")
            normalized = normalized.Replace("\n", newLine);

        var bytes = encoding.GetBytes(normalized);

        var response = await App.DoVaultSaveEditedFile(_inVaultPath, bytes);
        if (response.IsSuccess())
        {
            _fileBytes = bytes;
            _originalText = ContentBox.Text ?? string.Empty;
            SetDirty(false);
            await ShowInfoAsync(
                "Saved",
                $"'{_inVaultPath}' was saved into the vault file.");
        }
        else
        {
            await ShowErrorAsync(response.Message);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || !_isDirty) return;

        // defer the close until the user confirms
        e.Cancel = true;

        var confirmed = await ConfirmAsync(
            "Unsaved changes",
            "This file has unsaved changes. Close without saving?");
        if (confirmed)
        {
            _forceClose = true;
            Close();
        }
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question, WindowStartupLocation.CenterOwner);
        var result = await box.ShowWindowDialogAsync(this);
        return result == ButtonResult.Yes;
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info, WindowStartupLocation.CenterOwner);
        await box.ShowWindowDialogAsync(this);
    }

    private async Task ShowErrorAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("ERROR", message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
        await box.ShowWindowDialogAsync(this);
    }
}
