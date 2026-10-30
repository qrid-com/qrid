using Qrs.App.FileManager.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace Qrs.App.FileManager.WinForms
{
    public class QrsFormBase : Form
    {

        public Qrs.Client.Context.ClientContext Context
        {
            get
            {
                return QrsContext.Instance;
            }
        }

        public FmContext FmContext
        {
            get
            {
                return QrsContext.FmInstance;
            }
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Context.LogDebug(message);
        }

        

    }
}
