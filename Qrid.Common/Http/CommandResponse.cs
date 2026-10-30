using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Http
{
    public class CommandResponse : Qrid.Common.Proto.BaseData
    {
        public string ResponseObject { get; set; } // base64 encoded proto-serialized obj of what type?
    }
}
