using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Qrs.App.PasswordManager.Components.Code.ViewModels;
using Qrs.App.PasswordManager.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{
    public class ViewModelBase 
    {
        public QridComponentBase View { get; set; }

        public void StateChanged()
        {
            View?.StateChanged();           
        }

        public async Task StateChangedAsync()
        {
            if (View == null) { return; }

            await View.StateChangedAsync();
        }

        public virtual void StateChangedCascade()
        {
            View?.StateChangedCascade();
        }

    }

    public enum EditMode
    {
        New,
        View,
        Edit
    }

    public class ViewModel<DataModelType> : ViewModelBase where DataModelType : class, new()
    {

        public DataModelType DataModel { get; set; }
        public DataModelType LastViewedDataModel { get; set; } // to implement Cancel from AddNew mode, we need to get back to LastViewed
        public EditMode Mode { get; set; }
        public bool IsNew => Mode == EditMode.New;
        public bool IsView => Mode == EditMode.View;
        public bool IsEdit => Mode == EditMode.Edit;

        public bool IsModified { get; set; }

        public void LogDebug(string message)
        {
            if (View == null)
                throw new ApplicationException($".View is null while calling .LogDebug('{message}')");
            View.App.LogDebug(message);
        }

        public virtual void UpdateViewFromData()
        {
            // update ViewModel fields with data from DataModel
            // used when displaying data in a "View" mode, or when starting "Edit" mode
        }

        public virtual void UpdateDataFromView()
        {
            // update DataModel fields with data from ViewModel
            // used when "Saving" data edited in "New" or "Edit" mode
        }

        public virtual void InitNewViewModel()
        {
            // init UI with default values, called when "AddNew()" is called
        }

        public virtual void DataModelSet(DataModelType dm, EditMode mode = EditMode.View)
        {
            Mode = mode;
            LastViewedDataModel = DataModel;
            DataModel = dm;
            UpdateViewFromData();
            IsModified = false;
        }

        public virtual void AddNew(DataModelType newDm = null)
        {
            // start "Add New" mode

            LastViewedDataModel = DataModel;
            DataModelSet(newDm, EditMode.New);
            
            //??????????InitNewViewModel();
            //IsModified = false;
        }

        public virtual void Edit()
        {
            // start "Edit" mode
            LastViewedDataModel = DataModel;
            Mode = EditMode.Edit;
            IsModified = false;
        }

        public virtual void SaveIntoDataModel()
        {
            // if (Validate) {...}

            if (DataModel == null) 
            {
                LogDebug("SaveIntoDataModel() was called with DataModel==null. Creating new instance of DataModelType()");
                DataModel = new DataModelType(); 
            }

            // put UI (aka View) data into .DataModel
            UpdateDataFromView();

            IsModified = false;
            Mode = EditMode.View;
        }

        public virtual void Cancel()
        {
            // Cancelling changes done in AddNew or Edit mode
            
            DataModelSet(LastViewedDataModel);
        
        }

    }
}
