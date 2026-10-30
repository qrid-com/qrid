using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;


namespace Qrs.App.Vault.Avalonia.Controls;

public partial class VaultPanel : VaultPanelBase
{
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set 
        { 
            _isExpanded = value;
            vaultContentPanel.IsVisible = value;
        }
    }

    public override ListBox VaultContentList => vaultContentList;

    public Control VaultContentPanel => vaultContentPanel;


    public VaultPanel() : base()
    {
		InitializeComponent();
	}

    private async void Button_Extract_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultExtract();
    }

    private async void Button_Export_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultExportSelectedFiles();
    }

    public async System.Threading.Tasks.Task EditSelectedFile()
    {
        var selected = vaultContentList.SelectedItem as string;
        await App.DoVaultEditFile(selected);
    }

    private async void ContextMenu_CreateNewFile_Click(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultAddNewFile();
    }

    private async void ContextMenu_ViewModify_Click(object? sender, RoutedEventArgs e)
    {
        await EditSelectedFile();
    }

    private async void ContextMenu_Export_Click(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultExportSelectedFiles();
    }

    private async void vaultContentList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        await EditSelectedFile();
    }
}
