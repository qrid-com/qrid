using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Qrs.App.EmailTools.Avalonia.Services;

namespace Qrs.App.EmailTools.Avalonia;

/// <summary>
/// Modal popup that runs a deletion operation and displays its progress. The work is
/// supplied as a delegate so the window owns the lifetime of the run and closes itself
/// (or enables the Close button) when the operation finishes.
/// </summary>
public partial class DeletionProgressWindow : Window
{
    private readonly Func<IProgress<DeletionProgress>, Task<DeletionResult>>? _work;

    public DeletionResult? Result { get; private set; }

    // Parameterless constructor required by the Avalonia XAML loader / designer.
    public DeletionProgressWindow()
    {
        InitializeComponent();
    }

    public DeletionProgressWindow(Func<IProgress<DeletionProgress>, Task<DeletionResult>> work)
        : this()
    {
        _work = work;
    }

    private async void Window_Opened(object? sender, EventArgs e)
    {
        if (_work == null)
            return;

        var progress = new Progress<DeletionProgress>(Report);

        try
        {
            Result = await _work(progress);
            var result = Result;
            txtProgress.Text = $"Done. Deleted {result.ServerDeleted} from server, {result.ClientDeleted} from client. {result.Errors} error(s).";
            progressBar.Value = progressBar.Maximum;
        }
        catch (Exception ex)
        {
            txtProgress.Text = $"Error: {ex.Message}";
        }
        finally
        {
            btnClose.IsEnabled = true;
        }
    }

    private void Report(DeletionProgress progress)
    {
        // Progress<T> already marshals to the UI thread, but guard defensively.
        Dispatcher.UIThread.Post(() =>
        {
            progressBar.Maximum = Math.Max(1, progress.Total);
            progressBar.Value = Math.Min(progress.Processed, progressBar.Maximum);
            txtProgress.Text = progress.Total > 0
                ? $"{progress.Processed}/{progress.Total} — {progress.Message}"
                : progress.Message;
        });
    }

    private void btnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
