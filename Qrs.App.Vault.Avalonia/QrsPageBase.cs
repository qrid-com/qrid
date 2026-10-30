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
using System;
using Qrs.App.Vault.Avalonia.Dialogs;


namespace Qrs.App.Vault.Avalonia
{
    public class QrsPageBase : Window
    {
        public Qrs.App.Vault.Avalonia.App App
        {
            get
            {
                return (Qrs.App.Vault.Avalonia.App)Application.Current;
            }
        }

        public string AppVersion => "v1";
        public Qrs.App.Vault.Client.VaultContext Context => App.Context;
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



        public void Trim(TextBox textEdit)
        {
            textEdit.Text = textEdit.Text?.Trim() ?? string.Empty;
        }





        public async Task<string> ShowInput(string title)
        {
            var inputDialog = new InputDialog(title);
            var res = await inputDialog.ShowDialog<string>(this);
            return res;
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

        public async Task DisplayAlert(string title, string message, string buttonText)
        {
            var dialog = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(title, message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.None, WindowStartupLocation.CenterOwner);
            await dialog.ShowWindowDialogAsync(this); // show as new window
            //await dialog.ShowAsPopupAsync(this); // show as view in the center of the same window
        }

        //public async Task GoToNewRootAsync(Page newRootPage)
        //{
        //    Navigation.InsertPageBefore(newRootPage, Navigation.NavigationStack.First());
        //    await Navigation.PopToRootAsync();
        //}
    }
}
