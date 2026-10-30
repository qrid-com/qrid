using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qrid.Common.Http
{
    public class HttpResponseModel<T> : ResponseModelBase where T : Qrid.Common.Proto.BaseData
    {
        public T Data { get; set; }

        
        
    }

}
