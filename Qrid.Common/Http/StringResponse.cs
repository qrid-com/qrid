using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qrid.Common.Http
{
    public class StringResponse : Qrid.Common.Proto.BaseData
    {
        public string StringValue { get; set; }
        
    }

}
