using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace Qrs.App.Vault.Avalonia.Dialogs;

public partial class InputDialog : Window
{
    private Window ParentWindow => (Window)Owner;

    public InputDialog(string title)
    {
        InitializeComponent();
        this.InputLabel.Text = title;
    }

    
    private void CenterDialog()
    {
        var x = ParentWindow.Position.X + (ParentWindow.Bounds.Width - Width) / 2;
        var y = ParentWindow.Position.Y + (ParentWindow.Bounds.Height - Height) / 2;

        Position = new PixelPoint((int)x, (int)y);
    }

    private void LockSize()
    {
        MaxWidth = MinWidth = Width;
        MaxHeight = MinHeight = Height;
    }



    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        
        Close(this.InputBox.Text);
    }

    private async void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        
        Close(null);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LockSize();
        CenterDialog();
    }

}