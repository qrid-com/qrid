using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Qrs.Cli.Models
{
    public class EnvironmentItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string EncryptedValue { get; set; }

    }
}
