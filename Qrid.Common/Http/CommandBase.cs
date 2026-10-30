using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Http
{
    public class CommandBase : Qrid.Common.Proto.BaseData
    {
        // base class for digitally-signed JSON Command that is being sent to CommandController
        // TODO - SECURITY VULNERABILITY issue - rafactor all this "Command" code so Command for one POST would not be able to be submitted to another POST URL and processed as another command
        public string CommandName { get; set; } // based on this, we will pick a method to process this Command
        public string ServiceName { get; set; }
    }
}
