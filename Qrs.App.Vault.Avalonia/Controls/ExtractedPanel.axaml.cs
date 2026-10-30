using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class ExtractedPanel : QrsViewBase
{

    public TextBox ContextFolderTextInput => contextFolderTextInput;

    public ExtractedPanel()
    {
        InitializeComponent();
    }

    private void Button_Open_Content_Folder_Clicked(object? sender, RoutedEventArgs e)
    {
        Context.OpenFolder(Context.ContentFolder);
    }

    private async void Button_Save_Into_Vault_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultSaveAndClose(Context.ContentFolder);
    }
}