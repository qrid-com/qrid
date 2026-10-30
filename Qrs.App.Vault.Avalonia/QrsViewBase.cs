using Qrs.App.Vault.Avalonia.Controls;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;




namespace Qrs.App.Vault.Avalonia
{
    public class QrsViewBase : UserControl
    {
        public Qrs.App.Vault.Avalonia.App App
        {
            get
            {
                return (Qrs.App.Vault.Avalonia.App)Application.Current;
            }
        }

        public Qrs.App.Vault.Client.VaultContext Context
        {
            get
            {
                return App.Context;
            }
        }

        public Window Window => TopLevel.GetTopLevel(this) as Window;

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Context.LogDebug(message);
        }

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        // TODO - reaftor - move all these methods below from here and from QrsPageBase into UIUtils class


        public void Trim(TextBox textEdit)
        {
            textEdit.Text = textEdit.Text?.Trim() ?? string.Empty;
        }

        public void ShowError(string message)
        {
            LogDebug($"ERROR - ShowError(message='{message}')");
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // stay on this page
                await DisplayAlert("ERROR", message, "OK");
            });
        }

        private async Task DisplayAlert(string title, string message, string buttonText)
        {
            var dialog = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(title, message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.None, WindowStartupLocation.CenterOwner);
            await dialog.ShowWindowDialogAsync(this.Window); // show as new window
            //await dialog.ShowAsPopupAsync(this); // show as view in the center of the same window
        }

        //public async Task GoToNewRootAsync(Page newRootPage)
        //{
        //    Navigation.InsertPageBefore(newRootPage, Navigation.NavigationStack.First());
        //    await Navigation.PopToRootAsync();
        //}
    }
}
