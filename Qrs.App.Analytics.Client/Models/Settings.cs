using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Analytics.Client.Models
{
    public class Settings
    {
        public int TimerPeriod { get; set; }

        public List<ContextSettings> ContextSettings { get; set; }

        public Settings()
        {

        }
    }
}
