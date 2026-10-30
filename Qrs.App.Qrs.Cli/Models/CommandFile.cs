using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Qrs.Cli.Models
{
    public class CommandFile
    {
        public string Title { get; set; } // used as Associated Data (AD) while wrapping DEK key with Master Key
        public string DekId { get; set; } // DekId used as Associated Data (AD)
        public string Key { get; set; } // key-value string also containing DEK key (wrapped with Master Key). Unwrapped DEK key is used to encrypt/decrypt environment variables
        public List<EnvironmentItem> Environment {  get; set; } // encrypted environment variables
        public string ProcessType { get; set; }
        public string ProcessFile { get; set; } // path to the executable file
        public string ProcessArguments { get; set; }

    }
}
