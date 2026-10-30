using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{


    public class DetailsVm : ViewModel<ResourceItem>
    {
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }
        public string Notes { get; set; }
        public string Username { get; set; }

        public bool IsNullModel => DataModel?.ResourceInfo == null;

        // full path of the .qrs2 file that stores this resource item (empty when not stored yet)
        public string StoredInFilePath => DataModel?.ResourceInfo?.QrsFile ?? string.Empty;

        // name of the .qrs2 file that stores this resource item
        public string StoredInFileName
        {
            get
            {
                var path = StoredInFilePath;
                if (string.IsNullOrWhiteSpace(path))
                    return string.Empty;
                return System.IO.Path.GetFileName(path);
            }
        }

        public bool IsStoredInFileAvailable => !string.IsNullOrWhiteSpace(StoredInFilePath);


        public SecretVm SecretVm { get; set; }

        public DetailsVm()
        {
            DataModel = new ResourceItem();
            DataModel.PrepareObjects();

            SecretVm = new SecretVm();
            //SecretVm.DetailsVm = this;
            Mode = EditMode.New;
        }

        public override void StateChangedCascade()
        {
            View?.StateChanged();
            SecretVm?.View?.StateChanged();
        }

        public override void DataModelSet(ResourceItem resourceItem, EditMode mode = EditMode.View)
        {
            base.DataModelSet(resourceItem, mode);

            SecretVm.DataModelSet(resourceItem?.ResourceInfo, mode);

            StateChangedCascade(); // do we need this here? above methods - do they call StateChanged?
        }

        public override void UpdateViewFromData()
        {
            // update ViewModel fields with data from DataModel
            // used when displaying data in a "View" mode, or when starting "Edit" mode
            ResourceName = DataModel?.ResourceInfo?.Name ?? string.Empty;
            ResourceType = DataModel?.ResourceInfo?.Type ?? string.Empty;
            Username = DataModel?.ResourceInfo?.Username ?? string.Empty;
            Notes = DataModel?.ResourceInfo?.Notes ?? string.Empty;
        }

        public override void UpdateDataFromView()
        {
            // update DataModel fields with data from ViewModel
            // used when "Saving" data edited in "New" or "Edit" mode

            string adChange = string.Empty;
            if (DataModel.ResourceInfo.Name != ResourceName)
            {
                adChange += $"DataModel.ResourceInfo.Name '{DataModel.ResourceInfo.Name}' != ResourceName '{ResourceName}'";
            }
            if (DataModel.ResourceInfo.Username != Username)
            {
                adChange += $" --- DataModel.ResourceInfo.Username '{DataModel.ResourceInfo.Username}' != Username '{Username}'";
            }

            DataModel.ResourceInfo.IsAssociatedDataModified = !string.IsNullOrWhiteSpace(adChange);
            
            DataModel.ResourceInfo.Name = ResourceName;
            DataModel.ResourceInfo.Type = ResourceType;
            DataModel.ResourceInfo.Username = Username;
            DataModel.ResourceInfo.Notes = Notes;

            SecretVm?.UpdateDataFromView();

        }

        public string GetModeString()
        {
            switch (Mode)
            {
                case EditMode.New: return "+ Adding New";
                case EditMode.View: return IsNullModel ? "Empty" : "Viewing";
                case EditMode.Edit: return "Editing";
                default: return "Unknown Mode";
            }
        }

    }
}
