using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Components.Code;
using Qrs.App.PasswordManager.Client.Models;


namespace Qrs.App.PasswordManager.Components.Components
{
    public partial class DetailsView
    {
        


        public async Task SaveClick()
        {
            await App.PerformItemSaveClick();
        }

        public async Task CancelClick()
        {
            await App.PerformItemCancelClick();
        }

        public async Task DeleteClick()
        {
            await App.PerformItemDeleteClick(Vm.DataModel.ResourceInfo);
        }

        public async Task NewClick()
        {
            await App.PerformNewItemShowClick();
        }

        public async Task EditClick()
        {
            await App.PerformItemEditClick();
        }

        public async Task CopyFilePathClick()
        {
            var filePath = Vm?.StoredInFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var isCopied = await App.CopyToClipboardAsync(filePath);
            if (!isCopied)
                await App.ShowErrorAsync("Could not copy the file path to the clipboard");
        }

        public async Task ShowFileDetailsClick()
        {
            var filePath = Vm?.StoredInFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            await App.ShowMessageBoxAsync("File Details", new MarkupString(System.Net.WebUtility.HtmlEncode(filePath)));
        }
        
    }
}
