using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{

    public class MainScreenVm : ViewModelBase
    {

        public ListVm ListVm { get; set; }
        public DetailsVm DetailsVm { get; set; }

        

        public MainScreenVm()
        {
            ListVm = new ListVm();
            DetailsVm = new DetailsVm();

            ListVm.DetailsVm = DetailsVm;
        }

        public override void StateChangedCascade()
        {
            ListVm?.StateChangedCascade();
            DetailsVm?.StateChangedCascade();
        }

    }
}
