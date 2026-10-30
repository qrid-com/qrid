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

public partial class ProcessingPanel : QrsViewBase
{
    
    public PanelHeader TitleHeader => titleHeader;
    

    public string Path;

    public ProcessingPanel()
	{
		InitializeComponent();
	}

    
    

}