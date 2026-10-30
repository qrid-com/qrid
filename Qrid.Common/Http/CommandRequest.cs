using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Http
{
    public class CommandRequest : Qrid.Common.Proto.BaseData
    {
        // rename to "CommandEnvelope"?
        // we use this class to pass digitally-signed JSON Command to server
        //
        //  CertSignedBytes object proto-serialized into byte[] and base64 encoded 
        //  CertSignedBytes.Bytes cointains proto-serialized StringObject whose .Value is JSON Command string
        public string CertSignedBytes { get; set; } 
    }
}
