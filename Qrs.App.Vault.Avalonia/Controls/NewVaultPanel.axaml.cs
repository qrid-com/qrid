using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Google.Protobuf;
using Qrs.App.Vault.Client;
using Qrs.App.Vault.Client.Payload;
using Qrs.App.Vault.Client.Proto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using File = System.IO.File;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class NewVaultPanel : VaultPanelBase
{

    public override ListBox VaultContentList => vaultContentList;

    public NewVaultPanel() : base()
	{
		InitializeComponent();
    }
    
    private void FilesListBox_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
        else
            e.DragEffects = DragDropEffects.None;
    }

    private async void FilesListBox_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var droppedPaths = e.Data.GetFileNames();
            if (droppedPaths != null)
                AddPaths(droppedPaths.ToList());
        }
    }

    private async void Button_Save_Clicked(object? sender, RoutedEventArgs e)
    {
        await App.DoVaultSaveNew();
    }

    private async void Button_Add_Clicked(object? sender, RoutedEventArgs e)
    {
        await DoAddFiles();
    }

    private async void Button_Remove_Clicked(object? sender, RoutedEventArgs e)
    {
        await DoRemoveFiles();
    }

}