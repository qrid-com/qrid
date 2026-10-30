using Google.Protobuf;
using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{

    public class SecretVm : ViewModel<ResourceInfo>
    {
        //public DetailsVm DetailsVm { get; set; }
        public bool IsSecretOpen { get; set; }
        public string Password { get; set; }
        public string SecretNotes { get; set; }
        public byte[] FileContent { get; set; }
        public string FileContentMessage { get; set; }
        public string FilePath { get; set; }
        public bool IsFilePathModified { get; set; }
        public bool IsFileContentModified { get; set; }
        public bool IsFileContentAvailable { get; set; }

        public bool IsReadOnly => IsView;

        public void FileContentSet(byte[] fileContent)
        {
            FileContent = fileContent;
            IsFileContentAvailable = FileContent != null && FileContent.Length > 0;
            FileContentMessage = IsFileContentAvailable
                ? $@"{FileContent.Length.ToString("N0")} bytes - use 'Export' button to create a physical file"
                : string.Empty;
        }

        public override void UpdateViewFromData()
        {
            if (DataModel?.Secret == null)
            {
                // reset UI
                IsSecretOpen = false;
                Password = string.Empty;
                SecretNotes = string.Empty;

                FileContentSet(null);
                IsFileContentModified = false;
            }
            else
            {
                // populate UI with Secret data
                IsSecretOpen = true;
                Password = DataModel.Secret.Password;
                SecretNotes = DataModel.Secret.SecretNotes;

                FileContentSet(DataModel.Secret.FileContent.ToByteArray());
                IsFileContentModified = false;
            }

            FilePath = DataModel?.ResourceData?.FilePath ?? string.Empty;
            IsFilePathModified = false;
        }

        public override void UpdateDataFromView()
        {
            // update DataModel fields with data from ViewModel
            // used when "Saving" data edited in "New" or "Edit" mode

            // TDOD - refactor this once we introduce AEAD.
            // This is b/c Website, Username will go into AD.
            // So editing Website would require Opening Secret, and re-encrypting b/c AD is changing, even if Secret is the same
            // 
            // but for now, we could just NOT update this if Secret was NOT opened

            LogDebug("SecretVm.UpdateDataFromView() - start");

            DataModel.IsSecretModified = false; // needs to be "true" to cause SecretEnvelopeBytes to be updated

            if (DataModel?.Secret != null)
            {
                LogDebug("SecretVm.UpdateDataFromView() - DataModel?.Secret != null");
                if (DataModel.Secret.Password != Password)
                {
                    LogDebug($"putting a new DIFFERENT Password value into DataModel.Secret.Password");
                    DataModel.Secret.Password = Password;
                    DataModel.IsSecretModified = true;
                }
                if (DataModel.Secret.SecretNotes != SecretNotes)
                {
                    LogDebug($"putting a new DIFFERENT SecretNotes value into DataModel.Secret.SecretNotes");
                    DataModel.Secret.SecretNotes = SecretNotes;
                    DataModel.IsSecretModified = true;
                }
                var newFileContent = ByteString.CopyFrom(FileContent);
                if (DataModel.Secret.FileContent != newFileContent)
                {
                    LogDebug($"putting a new DIFFERENT FileContent value into DataModel.Secret.FileContent");
                    DataModel.Secret.FileContent = newFileContent;
                    DataModel.IsSecretModified = true;
                }
                // re-encrypt Secret into SecretEnvelopeBytes - will be done a bit later in the flow, no need to do it here
            }

            var IsSecretModifiedString = DataModel.IsSecretModified ? "TRUE !!!!!!!!!!!!!!!!!!!!!!!!!!!!" : "false ...";
            LogDebug($"ResourceInfo.IsSecretModified = {IsSecretModifiedString}");

            if (DataModel?.ResourceData != null)
            {
                LogDebug("SecretVm.UpdateDataFromView() - DataModel?.ResourceData != null ................");
                DataModel.ResourceData.FilePath = FilePath;
            }

        }

        public void CloseSecret()
        {
            // close secret
            if (DataModel != null)
            {
                DataModel.SecretWrapper.ClearPayload();
            }

            // update UI
            UpdateViewFromData();
        }
    }
}
