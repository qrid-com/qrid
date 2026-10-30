using MsBox.Avalonia.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Qrs.App.Vault.Avalonia.ViewModels;

public class ProgressDialogViewModel : INotifyPropertyChanged
{
    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Starting...";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private readonly CancellationTokenSource _cts = new();

    public ICommand CancelCommand { get; }

    /// <summary>
    /// True = completed successfully, False = cancelled/failed.
    /// </summary>
    public bool Result { get; private set; }

    // Event to notify when process ends - used to auto-close dialog window
    public event Action? Completed;

    public ProgressDialogViewModel()
    {
        CancelCommand = new RelayCommand(_ => _cts.Cancel());
    }

    public async Task RunAsync(IList<string> files)
    {
        var progress = new Progress<double>(p => Progress = p);

        try
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    StatusMessage = $"Processing: {System.IO.Path.GetFileName(files[i])}";
                    DoWorkOnFile(files[i]);

                    ((IProgress<double>)progress).Report((i + 1) * 100.0 / files.Count);
                }

                StatusMessage = "Completed!";
                Result = true; // success
            }, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled by user.";
            Result = false; // cancelled
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            Result = false; // failed
        }

        Completed?.Invoke();
    }

    private void DoWorkOnFile(string file)
    {
        Thread.Sleep(200); // Simulate CPU work
        System.IO.File.Exists(file); // Simulate I/O
    }

    // --- INotifyPropertyChanged ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
