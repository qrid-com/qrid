using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Google.Protobuf;
using Qrs.App.Vault.Client.Payload;
using Qrs.App.Vault.Client.Proto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using static Google.Protobuf.Reflection.MessageDescriptor;
using File = System.IO.File;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class VaultPanelBase : QrsViewBase, INotifyPropertyChanged
{
    public virtual ListBox VaultContentList => null;
    public ObservableCollection<string> FileCollection { get; set; } = new ObservableCollection<string>();
    public string FileCount => App.GetFileCount(FileCollection.Count);

    public void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(FileCount));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        //if (PropertyChanged == null) Debug.WriteLine($"PropertyChanged == null !!!");

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public VaultPanelBase()
	{
        FileCollection.CollectionChanged += OnItemsChanged;

        // Super important otherwise binding does not work.
        // Commenting it out b/c I've added it to XAML: see ... DataContext="{Binding $self}"
        //DataContext = this; 
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        VaultContentList.ItemsSource = FileCollection;
    }

    public void InitFileCollectionFromContext()
    {
        FileCollection.Clear();
        foreach (var kv in Context.VaultHelper.Files.OrderBy(x => x.Key))
            FileCollection.Add(kv.Key);
    }

    public void AddFileToCollection(string inVaultPath)
    {
        if (!FileCollection.Contains(inVaultPath))
            FileCollection.Add(inVaultPath);
    }

    protected void AddPaths(List<string> paths)
    {
        var addedList = Context.VaultHelper.AddPaths(paths);
        foreach (var file in addedList)
            FileCollection.Add(file); // Add to ListBox - it is bound to FileCollection
    }

    protected void RemovePaths(List<string> selected)
    {
        foreach (var file in selected)
        {
            FileCollection.Remove(file);
            Context.VaultHelper.Files.Remove(file);
        }
    }

    protected async Task DoAddFiles()
    {
        // let user pick the 1 or more files (and folders?))
        var files = await App.HomePage.Window.StorageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Select one or more files", AllowMultiple = true });

        var pathList = files.Select(f => f.TryGetLocalPath()).ToList();
        AddPaths(pathList);
    }

    protected async Task DoRemoveFiles()
    {
        var selected = VaultContentList.SelectedItems?.Cast<string>().ToList();
        if (selected == null) return;

        RemovePaths(selected);
    }

}