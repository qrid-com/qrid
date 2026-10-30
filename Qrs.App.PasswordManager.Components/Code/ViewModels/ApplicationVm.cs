using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{
    
    public class ApplicationVm : ViewModelBase
    {
        public MainScreenVm MainScreenVm;
        

        public EditMode Mode => MainScreenVm.DetailsVm.Mode;
        public bool IsNew => MainScreenVm.DetailsVm.IsNew;
        public bool IsView => MainScreenVm.DetailsVm.IsView;
        public bool IsEdit => MainScreenVm.DetailsVm.IsEdit;

        public void SetMode(EditMode mode)
        {
            MainScreenVm.DetailsVm.Mode = mode;
        }

        public ApplicationVm()
        {
            MainScreenVm = new MainScreenVm();
            
        }



    }
}
