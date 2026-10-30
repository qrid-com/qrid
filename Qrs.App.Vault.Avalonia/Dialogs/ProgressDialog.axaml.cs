using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Qrs.App.Vault.Avalonia.ViewModels;
using System;

namespace Qrs.App.Vault.Avalonia.Dialogs;

public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ProgressDialogViewModel vm)
        {
            vm.Completed += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Close dialog and return result
                    Close(vm.Result);
                });
            };
        }
    }
}