using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class PersistentState
    {
        public double ListWidth { get; set; } // between 0 and 100 - being bound into MadSplitter.Dimension property
        public int SortOrder { get; set; }
        public string SearchKeywords { get; set; }
        public PersistentState()
        {
            ListWidth = 50;
        }
    }
    
}
