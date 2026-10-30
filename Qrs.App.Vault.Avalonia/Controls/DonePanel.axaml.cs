using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class DonePanel : QrsViewBase
{
    
    public PanelHeader TitleHeader => titleHeader;
    public TextBlock MessageLabel => messageLabel;

    public string Path;

    public DonePanel()
	{
		InitializeComponent();
	}

    public void SetButtonDoneVisible(bool visible)
    {
        buttonDone.IsVisible = visible;
        labelDone.IsVisible = visible;
    }
    
    public void SetButtonExitVisible(bool visible)
    {
        buttonExit.IsVisible = visible;
        labelExit.IsVisible = visible;
    }

    public void SetMessageAndPath(string message, string path)
    {
        Path = path;
        if (string.IsNullOrEmpty(path))
        {
            MessageLabel.Text = message;
        }
        else
        {
            var splits = message.Split("%path%");
            MessageLabel.Inlines.Clear();
            foreach (var m in splits)
                AddInline(m, path);
        }
    }

    private IBrush GetMainTextBrush()
    {

        var themeName = Application.Current.ActualThemeVariant;

        if (Application.Current.Resources.ThemeDictionaries.TryGetValue(themeName, out var themedDict)
            && themedDict is ResourceDictionary dict
            && dict.TryGetValue("MainTextBrush", out var value2)
            && value2 is IBrush brush)
        {
            return brush;
        }

        throw new KeyNotFoundException($"Themed resource 'MainTextBrush' for theme {themeName} not found.");
    }

    private void AddInline(string m, string path)
    {
        if (MessageLabel.Inlines.Count > 0) MessageLabel.Inlines.Add(new LineBreak());
        if (string.IsNullOrEmpty(m))
            // add PATH in blue color
            MessageLabel.Inlines.Add(new Span() { Foreground = GetMainTextBrush(), Inlines = { new Run(path) } });
        else
            // add M
            MessageLabel.Inlines.Add(new Run(m));
    }

    private void Button_Done_Clicked(object? sender, RoutedEventArgs e)
    {
        App.DoHome();
    }

    private void Button_Exit_Clicked(object? sender, RoutedEventArgs e)
    {
        App.Shutdown();
    }

    private void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Path)) { return; }

        Dispatcher.UIThread.Post(async () =>
        {
            await Window.Clipboard.SetTextAsync(Path);
        });

    }

    private void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Path)) { return; }

        Context.OpenFolder(Path);
    }


}