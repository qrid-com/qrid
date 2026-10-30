using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.CA
{
    public class AwaitingScannerTask
    {
        public TaskCompletionSource<string> ScannerTaskSource { get; set; }
        public DateTime CreateDate { get; set; }

        public AwaitingScannerTask()
        {
            CreateDate = DateTime.Now;
        }
    }
}
