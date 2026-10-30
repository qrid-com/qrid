using Qrid.Common.Utils;
using Qrs.App.PasswordManager.Client.Wrappers;
using Qrs.Proto;
using System.IO;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.App.PasswordManager.Client.Models
{
    public class Folder
    {
        //public List<ResourceInfo> Resources { get; set; } // TODO - remove this ?

        // Items to show on UI - searched and sorted
        public SearchedList Searched;
        public string FolderPath { get; set; } // folder path in OS

        // relative folder path starting from AppRoot folder, with path separators replaced with "/" to make it invariant to OS
        // this InvariantFolderPath goes into AssociatedData in AEAD when encrypting FolderKey in qrs1 file, so we want it to be the same even after data has been moved from Windows to MacOS, etc
        public string InvariantFolderPath { get; set; } 
        
        public FolderKeyWrapper FolderKeyWrapper { get; set; }
        public QrsEnvelope FolderKeyEnvelope => FolderKeyWrapper?.QrsEnvelope; // content of "folder_key.qrs1" file - which is FolderKey encrypted into QrsEnvelope
        public CR.CryptoKey FolderKey => FolderKeyWrapper?.Payload; // decrypted FolderKey
        public CR.CryptoKey FolderKeyDek => FolderKeyWrapper?.DEK; // decrypted DEK that encrypts FolderKey
        
        
        public AssociatedDataDek FolderKeyAdDek { get; set; } // associated data for DEK that encrypts folder key (for SEB in FolderKeyEnvelope.EncryptedDek)
        public AssociatedDataPayload FolderKeyAdPayload { get; set; } // associated data data for folder key itself (for SEB in FolderKeyEnvelope.EncryptedPayload)



        public string ErrorMessage { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);


        public Folder(string folderPath, string appRootPath)
        {
            FolderPath = folderPath;
            InvariantFolderPath = SharedUtils.GetInvariantFolderPath(folderPath, appRootPath);
            Searched = new SearchedList();
            FolderKeyWrapper = new FolderKeyWrapper(PwmContext.APP_NAME, this);
        }

        public string GetFolderKeyFileName()
        {
            return Path.Combine(FolderPath,"folder_key.qrs1");
        }
    }
}
