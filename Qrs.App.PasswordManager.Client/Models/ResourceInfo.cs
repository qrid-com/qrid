using Qrid.Common.Utils;
using Qrs.App.PasswordManager.Client.Proto;
using Qrs.App.PasswordManager.Client.Wrappers;
using Qrs.Proto;
using System.Collections.Generic;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.App.PasswordManager.Client.Models
{
    public class ResourceInfo
    {
        public ResourceData ResourceData { get; set; }
        public string Id { get; set; } // GUID which is also the name of .qrs2 file
        public string QrsFile { get; set; } // file path in OS for .qrs2 file that stores the object
        public string QrsFileName => $"{Id}.qrs2";
        public void SetIdFromQrsFileName(string qrsFileName)
        {
            if (qrsFileName.EndsWith(".qrs2"))
                Id = qrsFileName.Substring(0, qrsFileName.Length - 5);
            else 
                Id = qrsFileName;
        }



        public SecretWrapper SecretWrapper { get; set; }

        // SecretEnvelope is only used during Migration to keep Envelope instance b/c SecretEnvelope.EncryptedDek and .EncryptedPayload
        // are produced at slightly different stages and we need to keep it together in some instance while Migration runs
        public QrsEnvelope SecretEnvelope => SecretWrapper?.QrsEnvelope; // ResourceData.SecretEnvelopeBytes deserialized into QrsEnvelope. 
        public CR.CryptoKey SecretDek => SecretWrapper?.DEK; // unwrapped DEK key
        public Secret Secret => SecretWrapper?.Payload; // plan-text decrypted secret




        public bool IsNew { get; set; } // for new files, need to create New Key when saving them
        public bool IsModified { get; set; } // has Data been modified
        public bool IsSecretOpen { get; set; } // has Secret been decrypted
        public bool IsSecretModified { get; set; } // has Secret been modified
        public bool IsAssociatedDataModified { get; set; } // has AssociatedData been modified (and so we need QR code roundtrip on Save)

        public string ErrorMessage { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        //
        public ResourceInfo()
        {
            PrepareObjects();
        }

        public void PrepareObjects()
        {
            ResourceData ??= new ResourceData();
            SecretWrapper ??= new SecretWrapper(PwmContext.APP_NAME, this);
        }

        // Secret

        public string Password
        {
            get { return Secret?.Password; }
            set { if (Secret != null) { Secret.Password = value; } }
        }

        public string SecretNotes
        {
            get { return Secret?.SecretNotes; }
            set { if (Secret != null) { Secret.SecretNotes = value; } }
        }

        // redirect to RD

        public string Name 
        {
            get { return ResourceData?.ResourceName; }
            set { if (ResourceData != null) { ResourceData.ResourceName = value; } }
        }

        public string Type
        {
            get { return ResourceData?.ResourceType; }
            set { if (ResourceData != null) { ResourceData.ResourceType = value; } }
        }

        public string Username
        {
            get { return ResourceData?.Username; }
            set { if (ResourceData != null) { ResourceData.Username = value; } }
        }

        public string Notes
        {
            get { return ResourceData?.ResourceNotes; }
            set { if (ResourceData != null) { ResourceData.ResourceNotes = value; } }
        }

        public string CreatedString
        {
            get
            {
                // Created is stored as UTC, so convert it to local time before displaying it
                var created = ResourceData?.Created;
                if (created == null)
                    return string.Empty;
                return SharedUtils.GetCreateDateString(created.ToDateTime().ToLocalTime());
            }
        }

        public string GetFileName()
        {
            return $"{Id}.qrs2";
        }

        // todo - this needs to be moved inside Secret class
        // for now just have it here so we could keep the UI (from Mail that already has drag-n-drop of attachments)
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();


    }
}
