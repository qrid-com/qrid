using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Qrs.App.Vault.Avalonia.Controls
{
    
    public partial class PanelHeader : UserControl
    {

        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<PanelHeader, string>(nameof(Title), defaultValue: string.Empty);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<string> SubtitleProperty =
            AvaloniaProperty.Register<PanelHeader, string>(nameof(Subtitle), defaultValue: string.Empty);

        public string Subtitle
        {
            get => GetValue(SubtitleProperty);
            set 
            { 
                SetValue(SubtitleProperty, value);
                IsSubtitleVisible = !string.IsNullOrWhiteSpace(value); 
            }
        }

        public static readonly StyledProperty<bool> IsSubtitleVisibleProperty =
            AvaloniaProperty.Register<PanelHeader, bool>(nameof(IsSubtitleVisible), defaultValue: false);

        public bool IsSubtitleVisible
        {
            get => GetValue(IsSubtitleVisibleProperty);
            set => SetValue(IsSubtitleVisibleProperty, value);
        }

        public PanelHeader()
        {
            InitializeComponent();
            DataContext = this; // !!!!!!!!!!!!!! or binding does not work
        }
    }
}