using Qrs.App.FileManager.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.App.FileManager.WinForms
{
    public class QrsContext
    {

        public static Qrs.Client.Context.ClientContext Instance { get; set; }
        public static FmContext FmInstance { get; set; }



    }
}
