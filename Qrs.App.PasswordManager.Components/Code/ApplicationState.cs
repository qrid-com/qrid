using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class ApplicationState
    {
        public string SearchString { get; set; }
        public float ColumnWidth { get; set; }
        public float LeftMenuWidth { get; set; }
        public int PageIndex { get; set; } // current page index 0 based
        public float ListWidth { get; set; }
        public float ListHeight { get; set; }
        public float FirstItemHeight { get; set; }
        //public DividerState ScriptDivider { get; set; }
        public DividerState MainDivider { get; set; }
        public PersistentState PersistentState { get; set; }

        public ApplicationState()
        {
            ColumnWidth = 270; // 150 pixels by default
            LeftMenuWidth = 700;
            //ListWidth = 888;
            //ListHeight = 600;
            //ScriptDivider = new DividerState();
            MainDivider = new DividerState();
            PersistentState = new PersistentState();
            PageIndex = -1;
        }
    }


    public class DividerState
    {
        public bool IsDrag { get; set; }
        public float StartX { get; set; }
        public float StartWidth { get; set; }

    }

    
}
