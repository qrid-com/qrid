using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class ApplicationSingleton
    {
        public ApplicationService AppSvc { get; set; } 

        public void RegisterAppSvc(ApplicationService appSvc)
        {
            if (AppSvc != null)
                throw new ApplicationException("AppSingleton already contains AppSvc instance! ");
            AppSvc = appSvc;
        }
        
    }
    
}
