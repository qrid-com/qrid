using Qrs.App.Vault.Client.Payload;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class HomePanel : QrsViewBase
{
	public HomePanel()
	{
		InitializeComponent();
	}

    private async void Button_Open_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultOpen();
    }

    private async void Button_Extract_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultOpenExtractView();
    }

    private async void Button_New_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultNew();
    }
}