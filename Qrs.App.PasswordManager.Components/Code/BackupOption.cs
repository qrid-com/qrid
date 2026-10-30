using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class BackupOption
    {
        public string BackupFolder { get; set; } 
        public bool IsSelected { get; set; }

        public bool IsError { get; set; }
        public string ResultMessage { get; set; }

        public BackupOption()
        {
            
        }
    }
    
}
