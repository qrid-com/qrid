using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Analytics.Client
{
    public interface IAnalyticLogger
    {
        void Log(string message);
    }
}
